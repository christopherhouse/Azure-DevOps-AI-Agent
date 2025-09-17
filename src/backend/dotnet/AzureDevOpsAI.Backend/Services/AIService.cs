using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Azure.Identity;
using AzureDevOpsAI.Backend.Configuration;
using AzureDevOpsAI.Backend.Models;
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
    private readonly string _systemPrompt;
    private readonly Dictionary<string, ChatHistory> _conversationHistory = new();

    public AIService(IOptions<AzureOpenAISettings> azureOpenAISettings, ILogger<AIService> logger)
    {
        _azureOpenAISettings = azureOpenAISettings.Value;
        _logger = logger;

        // Load system prompt from embedded resource
        _systemPrompt = LoadSystemPrompt();

        // Initialize Semantic Kernel
        var builder = Kernel.CreateBuilder();

        if (_azureOpenAISettings.UseManagedIdentity)
        {
            // Use managed identity authentication
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: _azureOpenAISettings.ChatDeploymentName,
                endpoint: _azureOpenAISettings.Endpoint,
                new DefaultAzureCredential());
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

        _logger.LogInformation("AI Service initialized with Azure OpenAI endpoint: {Endpoint}, Deployment: {Deployment}", 
            _azureOpenAISettings.Endpoint, _azureOpenAISettings.ChatDeploymentName);
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
        try
        {
            // Generate conversation ID if not provided
            conversationId ??= Guid.NewGuid().ToString();

            // Get or create chat history for conversation
            var chatHistory = GetOrCreateChatHistory(conversationId);

            // Add user message to history
            chatHistory.AddUserMessage(message);

            // Configure chat completion settings
            var executionSettings = new AzureOpenAIPromptExecutionSettings
            {
                MaxTokens = _azureOpenAISettings.MaxTokens,
                Temperature = _azureOpenAISettings.Temperature,
                TopP = 1.0,
                FrequencyPenalty = 0.0,
                PresencePenalty = 0.0
            };

            _logger.LogInformation("Processing chat message for conversation {ConversationId}", conversationId);

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

            // Add AI response to history
            chatHistory.AddAssistantMessage(response.Content);

            // Create response object
            var chatResponse = new ChatResponse
            {
                Message = response.Content,
                ConversationId = conversationId,
                Suggestions = GenerateSuggestions(message, response.Content)
            };

            _logger.LogInformation("Successfully processed chat message for conversation {ConversationId}", conversationId);

            return chatResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message for conversation {ConversationId}", conversationId);
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
}