using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Azure.Identity;
using Azure.Core;
using AzureDevOpsAI.Backend.Configuration;
using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Plugins;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace AzureDevOpsAI.Backend.Services;

/// <summary>
/// Interface for AI chat service.
/// </summary>
public interface IAIService
{
    /// <summary>
    /// Process a chat message and return AI response.
    /// </summary>
    /// <param name="message">User message</param>
    /// <param name="conversationId">Conversation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>AI response with citations</returns>
    Task<ChatResponse> ProcessChatMessageAsync(string message, string? conversationId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get chat history for a conversation.
    /// </summary>
    /// <param name="conversationId">Conversation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chat history</returns>
    Task<ChatHistory> GetChatHistoryAsync(string conversationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get thought process for a specific message.
    /// </summary>
    /// <param name="thoughtProcessId">Thought process ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Thought process details</returns>
    Task<ThoughtProcess> GetThoughtProcessAsync(string thoughtProcessId, CancellationToken cancellationToken = default);
}

/// <summary>
/// AI service implementation using Semantic Kernel and Azure OpenAI.
/// </summary>
public class AIService : IAIService
{
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly AzureOpenAISettings _azureOpenAISettings;
    private readonly ILogger<AIService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IUserAuthenticationContext _userAuthenticationContext;
    private readonly IAzureDevOpsApiService _azureDevOpsApiService;
    private readonly string _systemPrompt;
    private readonly Dictionary<string, ChatHistory> _conversationHistory = new();
    private readonly Dictionary<string, ThoughtProcess> _thoughtProcesses = new();

    public AIService(IOptions<AzureOpenAISettings> azureOpenAISettings, ILogger<AIService> logger, 
        IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory, IUserAuthenticationContext userAuthenticationContext,
        IAzureDevOpsApiService azureDevOpsApiService)
    {
        _azureOpenAISettings = azureOpenAISettings.Value;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _userAuthenticationContext = userAuthenticationContext;
        _azureDevOpsApiService = azureDevOpsApiService;

        // Load system prompt from embedded resource
        _systemPrompt = LoadSystemPrompt();

        // Initialize Semantic Kernel
        var builder = Kernel.CreateBuilder();

        if (_azureOpenAISettings.UseManagedIdentity)
        {
            // Use managed identity authentication
            var credentialOptions = new DefaultAzureCredentialOptions();
            
            // Configure User Assigned Managed Identity client ID (required when using managed identity)
            credentialOptions.ManagedIdentityClientId = _azureOpenAISettings.ClientId;
            _logger.LogInformation("Configured DefaultAzureCredential with User Assigned Managed Identity client ID: {ClientId}", _azureOpenAISettings.ClientId);
            
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: _azureOpenAISettings.ChatDeploymentName,
                endpoint: _azureOpenAISettings.Endpoint,
                new DefaultAzureCredential(credentialOptions));
        }
        else
        {
            // Use API key authentication
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: _azureOpenAISettings.ChatDeploymentName,
                endpoint: _azureOpenAISettings.Endpoint,
                apiKey: _azureOpenAISettings.ApiKey!);
        }

        _kernel = builder.Build();
        _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

        // Note: Plugins are now registered per request in ProcessChatMessageAsync to include user context
        
        _logger.LogInformation("AI Service initialized with Azure OpenAI endpoint: {Endpoint}, Deployment: {Deployment}", 
            _azureOpenAISettings.Endpoint, _azureOpenAISettings.ChatDeploymentName);
    }

    /// <summary>
    /// Register plugins with current user authentication context.
    /// </summary>
    private void RegisterPluginsWithUserContext()
    {
        try
        {
            // Clear any existing plugins
            _kernel.Plugins.Clear();
            
            // Use the injected user authentication context from the current request scope
            var userAuthContext = _userAuthenticationContext;
            
            // Register plugins with the Azure DevOps API service
            var projectPlugin = new ProjectPlugin(
                _azureDevOpsApiService,
                _loggerFactory.CreateLogger<ProjectPlugin>());
                
            _kernel.ImportPluginFromObject(projectPlugin, nameof(ProjectPlugin));
            
            _logger.LogDebug("Plugins registered with user authentication context: {HasUserToken}", 
                userAuthContext?.GetUserTokenCredential() != null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register plugins with user context");
            throw;
        }
    }

    /// <summary>
    /// Mock implementation for testing scenarios where no user context is available.
    /// </summary>
    private class MockUserAuthenticationContext : IUserAuthenticationContext
    {
        public void SetUserToken(string accessToken) { }
        public TokenCredential? GetUserTokenCredential() => null;
        public string? GetCurrentUserId() => null;
        public void ClearUserContext() { }
    }

    /// <summary>
    /// Load system prompt from embedded resource.
    /// </summary>
    /// <returns>System prompt text</returns>
    private string LoadSystemPrompt()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "AzureDevOpsAI.Backend.Resources.system-prompt.txt";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Process a chat message and return AI response.
    /// </summary>
    public async Task<ChatResponse> ProcessChatMessageAsync(string message, string? conversationId = null, CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        var thoughtProcessId = Guid.NewGuid().ToString();
        var messageId = Guid.NewGuid().ToString();
        
        try
        {
            // Generate conversation ID if not provided
            conversationId ??= Guid.NewGuid().ToString();

            // Initialize thought process tracking
            var thoughtProcess = new ThoughtProcess
            {
                Id = thoughtProcessId,
                ConversationId = conversationId,
                MessageId = messageId,
                StartTime = startTime
            };

            // Track initial analysis
            thoughtProcess.Steps.Add(new ThoughtStep
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Analyzing user message",
                Type = "analysis",
                Details = new Dictionary<string, object>
                {
                    ["message_length"] = message.Length,
                    ["conversation_id"] = conversationId
                }
            });

            // Get or create chat history for conversation
            var chatHistory = GetOrCreateChatHistory(conversationId);

            // Track planning step
            thoughtProcess.Steps.Add(new ThoughtStep
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Planning response approach",
                Type = "planning",
                Details = new Dictionary<string, object>
                {
                    ["history_length"] = chatHistory.Count,
                    ["has_system_prompt"] = !string.IsNullOrEmpty(_systemPrompt)
                }
            });

            // Add user message to history
            chatHistory.AddUserMessage(message);

            // Configure chat completion settings
            var executionSettings = new AzureOpenAIPromptExecutionSettings
            {
                MaxTokens = _azureOpenAISettings.MaxTokens,
                Temperature = _azureOpenAISettings.Temperature,
                TopP = 1.0,
                FrequencyPenalty = 0.0,
                PresencePenalty = 0.0,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            // Track reasoning step
            thoughtProcess.Steps.Add(new ThoughtStep
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Generating AI response using Azure OpenAI",
                Type = "reasoning",
                Details = new Dictionary<string, object>
                {
                    ["model_settings"] = new
                    {
                        MaxTokens = executionSettings.MaxTokens,
                        Temperature = executionSettings.Temperature
                    }
                }
            });

            _logger.LogInformation("Processing chat message for conversation {ConversationId}", conversationId);

            // Register plugins with the injected user authentication context
            RegisterPluginsWithUserContext();

            // Get AI response
            var response = await _chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                _kernel,
                cancellationToken);

            if (response == null || string.IsNullOrEmpty(response.Content))
            {
                throw new InvalidOperationException("AI service returned empty response");
            }

            // Track response generation completion
            thoughtProcess.Steps.Add(new ThoughtStep
            {
                Id = Guid.NewGuid().ToString(),
                Description = "AI response generated successfully",
                Type = "completion",
                Details = new Dictionary<string, object>
                {
                    ["response_length"] = response.Content.Length,
                    ["tokens_used"] = response.Metadata?.GetValueOrDefault("Usage", null) ?? "unknown"
                }
            });

            // Add AI response to history
            chatHistory.AddAssistantMessage(response.Content);

            // Track post-processing
            thoughtProcess.Steps.Add(new ThoughtStep
            {
                Id = Guid.NewGuid().ToString(),
                Description = "Generating suggestions and citations",
                Type = "post_processing"
            });

            // Finalize thought process
            var endTime = DateTime.UtcNow;
            thoughtProcess.EndTime = endTime;
            thoughtProcess.DurationMs = (long)(endTime - startTime).TotalMilliseconds;

            // Store thought process
            _thoughtProcesses[thoughtProcessId] = thoughtProcess;

            // Create response object with citations
            var chatResponse = new ChatResponse
            {
                Message = response.Content,
                ConversationId = conversationId,
                ThoughtProcessId = thoughtProcessId,
                Suggestions = GenerateSuggestions(message, response.Content),
                Citations = GenerateCitations(message, response.Content)
            };

            _logger.LogInformation("Successfully processed chat message for conversation {ConversationId}", conversationId);

            return chatResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message for conversation {ConversationId}", conversationId);
            
            // Store error in thought process if it was created
            if (_thoughtProcesses.ContainsKey(thoughtProcessId))
            {
                _thoughtProcesses[thoughtProcessId].Steps.Add(new ThoughtStep
                {
                    Id = Guid.NewGuid().ToString(),
                    Description = "Error occurred during processing",
                    Type = "error",
                    Details = new Dictionary<string, object>
                    {
                        ["error_message"] = ex.Message,
                        ["error_type"] = ex.GetType().Name
                    }
                });
                _thoughtProcesses[thoughtProcessId].EndTime = DateTime.UtcNow;
                _thoughtProcesses[thoughtProcessId].DurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            }
            
            throw;
        }
    }

    /// <summary>
    /// Get chat history for a conversation.
    /// </summary>
    public async Task<ChatHistory> GetChatHistoryAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Make method async for interface compatibility
        return _conversationHistory.GetValueOrDefault(conversationId) ?? new ChatHistory();
    }

    /// <summary>
    /// Get thought process for a specific message.
    /// </summary>
    public async Task<ThoughtProcess> GetThoughtProcessAsync(string thoughtProcessId, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Make method async for interface compatibility
        
        if (_thoughtProcesses.TryGetValue(thoughtProcessId, out var thoughtProcess))
        {
            return thoughtProcess;
        }
        
        throw new KeyNotFoundException($"Thought process with ID '{thoughtProcessId}' not found");
    }

    /// <summary>
    /// Get or create chat history for a conversation.
    /// </summary>
    private ChatHistory GetOrCreateChatHistory(string conversationId)
    {
        if (!_conversationHistory.TryGetValue(conversationId, out var chatHistory))
        {
            chatHistory = new ChatHistory();
            // Add system message to new conversations
            chatHistory.AddSystemMessage(_systemPrompt);
            _conversationHistory[conversationId] = chatHistory;
        }

        return chatHistory;
    }

    /// <summary>
    /// Generate suggestions based on the user message and AI response.
    /// </summary>
    private List<string> GenerateSuggestions(string userMessage, string aiResponse)
    {
        var suggestions = new List<string>();

        // Generate contextual suggestions based on keywords in the user message
        var lowerMessage = userMessage.ToLowerInvariant();

        if (lowerMessage.Contains("pipeline") || lowerMessage.Contains("ci/cd"))
        {
            suggestions.Add("How do I create a YAML pipeline?");
            suggestions.Add("What are the best practices for pipeline security?");
            suggestions.Add("How do I set up multi-stage pipelines?");
        }
        else if (lowerMessage.Contains("work item") || lowerMessage.Contains("backlog") || lowerMessage.Contains("board"))
        {
            suggestions.Add("How do I customize work item types?");
            suggestions.Add("What are the different work item states?");
            suggestions.Add("How do I set up sprint planning?");
        }
        else if (lowerMessage.Contains("repository") || lowerMessage.Contains("git") || lowerMessage.Contains("branch"))
        {
            suggestions.Add("How do I set up branch policies?");
            suggestions.Add("What are the best Git branching strategies?");
            suggestions.Add("How do I configure pull request templates?");
        }
        else if (lowerMessage.Contains("test") || lowerMessage.Contains("testing"))
        {
            suggestions.Add("How do I set up automated testing?");
            suggestions.Add("What are Azure Test Plans?");
            suggestions.Add("How do I integrate testing into pipelines?");
        }
        else
        {
            // Default suggestions for general DevOps questions
            suggestions.Add("How do I get started with Azure DevOps?");
            suggestions.Add("What are Azure DevOps best practices?");
            suggestions.Add("How do I migrate from other tools to Azure DevOps?");
        }

        return suggestions.Take(3).ToList(); // Return top 3 suggestions
    }

    /// <summary>
    /// Generate citations based on the user message and AI response.
    /// </summary>
    private List<Citation> GenerateCitations(string userMessage, string aiResponse)
    {
        var citations = new List<Citation>();
        var lowerMessage = userMessage.ToLowerInvariant();

        // Add contextual citations based on keywords in the user message
        if (lowerMessage.Contains("pipeline") || lowerMessage.Contains("ci/cd") || lowerMessage.Contains("yaml"))
        {
            citations.Add(new Citation
            {
                Title = "Azure Pipelines Documentation",
                Url = "https://docs.microsoft.com/en-us/azure/devops/pipelines/",
                Type = "documentation",
                Description = "Official Microsoft documentation for Azure Pipelines, including YAML syntax and best practices"
            });

            citations.Add(new Citation
            {
                Title = "YAML Schema Reference",
                Url = "https://docs.microsoft.com/en-us/azure/devops/pipelines/yaml-schema/",
                Type = "reference",
                Description = "Complete YAML schema reference for Azure Pipelines"
            });
        }

        if (lowerMessage.Contains("work item") || lowerMessage.Contains("backlog") || lowerMessage.Contains("board") || lowerMessage.Contains("agile"))
        {
            citations.Add(new Citation
            {
                Title = "Azure Boards Documentation",
                Url = "https://docs.microsoft.com/en-us/azure/devops/boards/",
                Type = "documentation",
                Description = "Official documentation for Azure Boards work item tracking and project management"
            });

            citations.Add(new Citation
            {
                Title = "Work Item Types and Workflow",
                Url = "https://docs.microsoft.com/en-us/azure/devops/boards/work-items/guidance/",
                Type = "guide",
                Description = "Guidance on work item types, states, and workflow customization"
            });
        }

        if (lowerMessage.Contains("repository") || lowerMessage.Contains("git") || lowerMessage.Contains("branch") || lowerMessage.Contains("pull request"))
        {
            citations.Add(new Citation
            {
                Title = "Azure Repos Documentation",
                Url = "https://docs.microsoft.com/en-us/azure/devops/repos/",
                Type = "documentation", 
                Description = "Official documentation for Azure Repos Git repositories and version control"
            });

            citations.Add(new Citation
            {
                Title = "Branch Policies and Pull Requests",
                Url = "https://docs.microsoft.com/en-us/azure/devops/repos/git/branch-policies/",
                Type = "guide",
                Description = "Guide to setting up branch policies and pull request workflows"
            });
        }

        if (lowerMessage.Contains("test") || lowerMessage.Contains("testing"))
        {
            citations.Add(new Citation
            {
                Title = "Azure Test Plans Documentation",
                Url = "https://docs.microsoft.com/en-us/azure/devops/test/",
                Type = "documentation",
                Description = "Official documentation for Azure Test Plans and testing in Azure DevOps"
            });
        }

        if (lowerMessage.Contains("artifact") || lowerMessage.Contains("package") || lowerMessage.Contains("feed"))
        {
            citations.Add(new Citation
            {
                Title = "Azure Artifacts Documentation",
                Url = "https://docs.microsoft.com/en-us/azure/devops/artifacts/",
                Type = "documentation",
                Description = "Official documentation for Azure Artifacts package management"
            });
        }

        // Always include general Azure DevOps documentation if no specific citations
        if (citations.Count == 0)
        {
            citations.Add(new Citation
            {
                Title = "Azure DevOps Documentation",
                Url = "https://docs.microsoft.com/en-us/azure/devops/",
                Type = "documentation",
                Description = "Official Microsoft Azure DevOps documentation and learning resources"
            });
        }

        // Add best practices reference for most queries
        if (!lowerMessage.Contains("what is") && !lowerMessage.Contains("introduction"))
        {
            citations.Add(new Citation
            {
                Title = "Azure DevOps Best Practices",
                Url = "https://docs.microsoft.com/en-us/azure/devops/learn/",
                Type = "best-practices",
                Description = "Collection of best practices and guidance for Azure DevOps implementation"
            });
        }

        return citations.Take(3).ToList(); // Return top 3 most relevant citations
    }
}