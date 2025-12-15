using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Azure.Identity;
using Azure.Core;
using AzureDevOpsAI.Backend.Configuration;
using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Plugins;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
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
    /// <param name="conversationId">Conversation ID (required, used as partition key in CosmosDB)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Thought process details</returns>
    Task<ThoughtProcess> GetThoughtProcessAsync(string thoughtProcessId, string conversationId, CancellationToken cancellationToken = default);
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
    private readonly IAzureDevOpsApiService _azureDevOpsApiService;
    private readonly ICosmosDbService _cosmosDbService;
    private readonly string _systemPrompt;
    private readonly IChatHistoryReducer? _chatHistoryReducer;

    /// <summary>
    /// Rate limit metadata keys that may be available in Azure OpenAI response headers.
    /// </summary>
    private static readonly string[] RateLimitMetadataKeys =
    {
        "x-ratelimit-remaining-tokens",
        "x-ratelimit-remaining-requests",
        "RateLimitRemainingTokens",
        "RateLimitRemainingRequests"
    };

    public AIService(IOptions<AzureOpenAISettings> azureOpenAISettings, ILogger<AIService> logger,
        IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory,
        IAzureDevOpsApiService azureDevOpsApiService,
        ICosmosDbService cosmosDbService)
    {
        _azureOpenAISettings = azureOpenAISettings.Value;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
        _azureDevOpsApiService = azureDevOpsApiService;
        _cosmosDbService = cosmosDbService ?? throw new ArgumentNullException(nameof(cosmosDbService), "CosmosDB service is required.");

        // Load system prompt from embedded resource
        _systemPrompt = LoadSystemPrompt();

        // Initialize Semantic Kernel
        var builder = Kernel.CreateBuilder();

        if (_azureOpenAISettings.UseManagedIdentity)
        {
            // Use managed identity authentication with ManagedIdentityCredential
            TokenCredential credential;

            // Configure User Assigned Managed Identity with ClientId
            if (_azureOpenAISettings.UseUserAssignedIdentity)
            {
                credential = new ManagedIdentityCredential(_azureOpenAISettings.ClientId);
                _logger.LogInformation("Configured ManagedIdentityCredential with User Assigned Managed Identity client ID: {ClientId}", _azureOpenAISettings.ClientId);
            }
            else
            {
                // Use system-assigned managed identity
                credential = new DefaultAzureCredential();
                _logger.LogInformation("Configured ManagedIdentityCredential with System Assigned Managed Identity");
            }

            builder.AddAzureOpenAIChatCompletion(
                deploymentName: _azureOpenAISettings.ChatDeploymentName,
                endpoint: _azureOpenAISettings.Endpoint,
                credential);
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

        // Initialize chat history reducer if enabled
        if (_azureOpenAISettings.EnableChatHistoryReducer)
        {
            _chatHistoryReducer = new ChatHistorySummarizationReducer(
                service: _chatCompletionService,
                targetCount: _azureOpenAISettings.ChatHistoryReducerTargetCount,
                thresholdCount: _azureOpenAISettings.ChatHistoryReducerThresholdCount)
            {
                UseSingleSummary = _azureOpenAISettings.ChatHistoryReducerUseSingleSummary
            };

            _logger.LogInformation("Chat history reducer enabled - TargetCount: {TargetCount}, ThresholdCount: {ThresholdCount}, UseSingleSummary: {UseSingleSummary}",
                _azureOpenAISettings.ChatHistoryReducerTargetCount,
                _azureOpenAISettings.ChatHistoryReducerThresholdCount,
                _azureOpenAISettings.ChatHistoryReducerUseSingleSummary);
        }
        else
        {
            _logger.LogInformation("Chat history reducer disabled");
        }

        // Note: Plugins are now registered per request in ProcessChatMessageAsync to include user context

        _logger.LogInformation("AI Service initialized with Azure OpenAI endpoint: {Endpoint}, Deployment: {Deployment}, CosmosDB: Enabled",
            _azureOpenAISettings.Endpoint, _azureOpenAISettings.ChatDeploymentName);
    }

    /// <summary>
    /// Register plugins for Azure DevOps operations.
    /// </summary>
    private void RegisterPluginsWithUserContext()
    {
        try
        {
            // Clear any existing plugins
            _kernel.Plugins.Clear();

            // Register plugins with the Azure DevOps API service (using managed identity)
            var projectPlugin = new ProjectPlugin(
                _azureDevOpsApiService,
                _loggerFactory.CreateLogger<ProjectPlugin>());

            _kernel.ImportPluginFromObject(projectPlugin, nameof(ProjectPlugin));

            var usersPlugin = new UsersPlugin(
                _azureDevOpsApiService,
                _loggerFactory.CreateLogger<UsersPlugin>());

            _kernel.ImportPluginFromObject(usersPlugin, nameof(UsersPlugin));

            var groupsPlugin = new GroupsPlugin(
                _azureDevOpsApiService,
                _loggerFactory.CreateLogger<GroupsPlugin>());

            _kernel.ImportPluginFromObject(groupsPlugin, nameof(GroupsPlugin));

            var subjectQueryPlugin = new SubjectQueryPlugin(
                _azureDevOpsApiService,
                _loggerFactory.CreateLogger<SubjectQueryPlugin>());

            _kernel.ImportPluginFromObject(subjectQueryPlugin, nameof(SubjectQueryPlugin));

            var repositoryPlugin = new RepositoryPlugin(
                _azureDevOpsApiService,
                _loggerFactory.CreateLogger<RepositoryPlugin>());

            _kernel.ImportPluginFromObject(repositoryPlugin, nameof(RepositoryPlugin));

            _logger.LogDebug("Plugins registered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register plugins");
            throw;
        }
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

        // Diagnostic logging: Track incoming conversation context
        var wasConversationIdProvided = !string.IsNullOrEmpty(conversationId);
        _logger.LogDebug("[ConversationContext] ProcessChatMessageAsync called - ConversationId provided: {WasProvided}, Value: {ConversationId}",
            wasConversationIdProvided, conversationId ?? "(null)");

        try
        {
            // Generate conversation ID if not provided
            var originalConversationId = conversationId;
            conversationId ??= Guid.NewGuid().ToString();

            if (!wasConversationIdProvided)
            {
                _logger.LogInformation("[ConversationContext] New conversation started - Generated ConversationId: {ConversationId}", conversationId);
            }

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
            var (chatHistory, isExistingConversation) = await GetOrCreateChatHistoryAsync(conversationId, cancellationToken);

            // Diagnostic logging: Track conversation history state
            _logger.LogDebug("[ConversationContext] History lookup - ConversationId: {ConversationId}, ExistingConversation: {IsExisting}, MessageCount: {MessageCount}",
                conversationId, isExistingConversation, chatHistory.Count);

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
            var messageCountBeforeAdd = chatHistory.Count;
            chatHistory.AddUserMessage(message);

            // Diagnostic logging: Track message addition
            _logger.LogDebug("[ConversationContext] User message added - ConversationId: {ConversationId}, MessagesBefore: {Before}, MessagesAfter: {After}, MessagePreview: {Preview}",
                conversationId, messageCountBeforeAdd, chatHistory.Count, message.Length > 50 ? message.Substring(0, 50) + "..." : message);

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

            // Diagnostic logging: Detailed history summary before AI call
            _logger.LogDebug("[ConversationContext] Preparing AI request - ConversationId: {ConversationId}, TotalMessages: {TotalMessages}, HistorySummary: {Summary}",
                conversationId, chatHistory.Count, GetChatHistorySummary(chatHistory));

            // Apply chat history reduction if enabled (for model input only, not storage)
            ChatHistory reducedHistory = chatHistory;
            if (_chatHistoryReducer != null)
            {
                var originalMessageCount = chatHistory.Count;
                var reducedMessages = await _chatHistoryReducer.ReduceAsync(chatHistory, cancellationToken);

                if (reducedMessages != null)
                {
                    // Create new ChatHistory with reduced messages
                    reducedHistory = new ChatHistory();
                    foreach (var msg in reducedMessages)
                    {
                        reducedHistory.Add(msg);
                    }

                    _logger.LogInformation("[ChatHistoryReducer] History reduced for model input - ConversationId: {ConversationId}, OriginalCount: {OriginalCount}, ReducedCount: {ReducedCount}, ReductionPercent: {ReductionPercent:F1}%",
                        conversationId, originalMessageCount, reducedHistory.Count,
                        (originalMessageCount - reducedHistory.Count) * 100.0 / originalMessageCount);

                    // Track reduction in thought process
                    thoughtProcess.Steps.Add(new ThoughtStep
                    {
                        Id = Guid.NewGuid().ToString(),
                        Description = "Chat history reduced for efficient token usage",
                        Type = "history_reduction",
                        Details = new Dictionary<string, object>
                        {
                            ["original_message_count"] = originalMessageCount,
                            ["reduced_message_count"] = reducedHistory.Count,
                            ["messages_removed"] = originalMessageCount - reducedHistory.Count
                        }
                    });
                }
                else
                {
                    _logger.LogDebug("[ChatHistoryReducer] No reduction needed - ConversationId: {ConversationId}, MessageCount: {MessageCount}",
                        conversationId, chatHistory.Count);
                }
            }

            // Register plugins for Azure DevOps operations
            RegisterPluginsWithUserContext();

            // Get AI response using the reduced history (if applicable)
            var response = await _chatCompletionService.GetChatMessageContentAsync(
                reducedHistory,
                executionSettings,
                _kernel,
                cancellationToken);

            if (response == null || string.IsNullOrEmpty(response.Content))
            {
                throw new InvalidOperationException("AI service returned empty response");
            }

            // Log token metrics
            LogTokenMetrics(response.Metadata, conversationId);

            // Track response generation completion
            var completionDetails = new Dictionary<string, object>
            {
                ["response_length"] = response.Content.Length
            };

            // Extract serializable token usage to avoid JSON serialization errors
            var tokenUsage = ExtractSerializableTokenUsage(response.Metadata);
            if (tokenUsage != null)
            {
                completionDetails["tokens_used"] = tokenUsage;
            }
            else
            {
                completionDetails["tokens_used"] = "unknown";
            }

            thoughtProcess.Steps.Add(new ThoughtStep
            {
                Id = Guid.NewGuid().ToString(),
                Description = "AI response generated successfully",
                Type = "completion",
                Details = completionDetails
            });

            // Add AI response to history
            var messageCountBeforeResponse = chatHistory.Count;
            chatHistory.AddAssistantMessage(response.Content);

            // Diagnostic logging: Track AI response addition
            _logger.LogDebug("[ConversationContext] AI response added - ConversationId: {ConversationId}, MessagesBefore: {Before}, MessagesAfter: {After}, ResponseLength: {Length}",
                conversationId, messageCountBeforeResponse, chatHistory.Count, response.Content.Length);

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

            // Store chat history and thought process (CosmosDB or in-memory fallback)
            await SaveChatHistoryAsync(conversationId, chatHistory, cancellationToken);
            await SaveThoughtProcessAsync(thoughtProcessId, thoughtProcess, conversationId, cancellationToken);

            // Create response object with citations
            var chatResponse = new ChatResponse
            {
                Message = response.Content,
                ConversationId = conversationId,
                Format = "markdown", // AI responses are formatted in markdown
                Timestamp = endTime, // Use the end time of processing
                ThoughtProcessId = thoughtProcessId,
                Suggestions = GenerateSuggestions(message, response.Content),
                Citations = GenerateCitations(message, response.Content)
            };

            _logger.LogInformation("Successfully processed chat message for conversation {ConversationId}", conversationId);

            // Diagnostic logging: Final conversation state
            _logger.LogDebug("[ConversationContext] Request completed - ConversationId: {ConversationId}, FinalMessageCount: {MessageCount}, ProcessingTimeMs: {Duration}",
                conversationId, chatHistory.Count, thoughtProcess.DurationMs);

            return chatResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message for conversation {ConversationId}", conversationId);

            // Store error in thought process
            var errorThoughtProcess = new ThoughtProcess
            {
                Id = thoughtProcessId,
                ConversationId = conversationId ?? string.Empty,
                MessageId = messageId,
                StartTime = startTime,
                EndTime = DateTime.UtcNow,
                DurationMs = (long)(DateTime.UtcNow - startTime).TotalMilliseconds
            };
            errorThoughtProcess.Steps.Add(new ThoughtStep
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

            // Try to save the error thought process
            try
            {
                await SaveThoughtProcessAsync(thoughtProcessId, errorThoughtProcess, conversationId ?? string.Empty, cancellationToken);
            }
            catch (Exception saveEx)
            {
                _logger.LogWarning(saveEx, "Failed to save error thought process for {ThoughtProcessId}", thoughtProcessId);
            }

            throw;
        }
    }

    /// <summary>
    /// Get chat history for a conversation.
    /// </summary>
    public async Task<ChatHistory> GetChatHistoryAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        var document = await _cosmosDbService.GetChatHistoryAsync(conversationId, cancellationToken);
        if (document != null)
        {
            return ConvertToChatHistory(document);
        }
        return new ChatHistory();
    }

    /// <summary>
    /// Get thought process for a specific message.
    /// </summary>
    public async Task<ThoughtProcess> GetThoughtProcessAsync(string thoughtProcessId, string conversationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(conversationId))
        {
            throw new ArgumentException("Conversation ID cannot be null or empty", nameof(conversationId));
        }

        var document = await _cosmosDbService.GetThoughtProcessAsync(thoughtProcessId, conversationId, cancellationToken);
        if (document != null)
        {
            return ConvertToThoughtProcess(document);
        }

        throw new KeyNotFoundException($"Thought process with ID '{thoughtProcessId}' not found");
    }

    /// <summary>
    /// Get or create chat history for a conversation.
    /// </summary>
    private async Task<(ChatHistory chatHistory, bool isExisting)> GetOrCreateChatHistoryAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        var document = await _cosmosDbService.GetChatHistoryAsync(conversationId, cancellationToken);
        if (document != null)
        {
            var chatHistory = ConvertToChatHistory(document);
            _logger.LogDebug("[ConversationContext] Existing ChatHistory retrieved from CosmosDB - ConversationId: {ConversationId}, MessageCount: {MessageCount}",
                conversationId, chatHistory.Count);
            return (chatHistory, true);
        }
        else
        {
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(_systemPrompt);
            _logger.LogInformation("[ConversationContext] New ChatHistory created - ConversationId: {ConversationId}, SystemPromptLength: {PromptLength}",
                conversationId, _systemPrompt.Length);
            return (chatHistory, false);
        }
    }

    /// <summary>
    /// Save chat history to CosmosDB.
    /// </summary>
    private async Task SaveChatHistoryAsync(string conversationId, ChatHistory chatHistory, CancellationToken cancellationToken = default)
    {
        var document = ConvertToDocument(conversationId, chatHistory);
        await _cosmosDbService.SaveChatHistoryAsync(document, cancellationToken);
    }

    /// <summary>
    /// Save thought process to CosmosDB.
    /// </summary>
    private async Task SaveThoughtProcessAsync(string thoughtProcessId, ThoughtProcess thoughtProcess, string conversationId, CancellationToken cancellationToken = default)
    {
        var document = ConvertToDocument(thoughtProcess);
        await _cosmosDbService.SaveThoughtProcessAsync(document, cancellationToken);
    }

    /// <summary>
    /// Convert ChatHistoryDocument to Semantic Kernel ChatHistory.
    /// </summary>
    private ChatHistory ConvertToChatHistory(ChatHistoryDocument document)
    {
        var chatHistory = new ChatHistory();
        foreach (var message in document.Messages)
        {
            switch (message.Role.ToLowerInvariant())
            {
                case "system":
                    chatHistory.AddSystemMessage(message.Content);
                    break;
                case "user":
                    chatHistory.AddUserMessage(message.Content);
                    break;
                case "assistant":
                    chatHistory.AddAssistantMessage(message.Content);
                    break;
            }
        }
        return chatHistory;
    }

    /// <summary>
    /// Convert Semantic Kernel ChatHistory to ChatHistoryDocument.
    /// </summary>
    private ChatHistoryDocument ConvertToDocument(string conversationId, ChatHistory chatHistory)
    {
        var document = new ChatHistoryDocument
        {
            Id = conversationId,
            ConversationId = conversationId,
            Messages = chatHistory.Select(m => new ChatMessageEntry
            {
                Role = m.Role.ToString(),
                Content = m.Content ?? string.Empty,
                Timestamp = DateTime.UtcNow
            }).ToList()
        };

        // Set system prompt if first message is system
        if (document.Messages.Count > 0 && document.Messages[0].Role.Equals("System", StringComparison.OrdinalIgnoreCase))
        {
            document.SystemPrompt = document.Messages[0].Content;
        }

        return document;
    }

    /// <summary>
    /// Convert ThoughtProcess to ThoughtProcessDocument.
    /// </summary>
    private ThoughtProcessDocument ConvertToDocument(ThoughtProcess thoughtProcess)
    {
        return new ThoughtProcessDocument
        {
            Id = thoughtProcess.Id,
            ConversationId = thoughtProcess.ConversationId,
            MessageId = thoughtProcess.MessageId,
            Steps = thoughtProcess.Steps.Select(s => new ThoughtStepEntry
            {
                Id = s.Id,
                Description = s.Description,
                Type = s.Type,
                Timestamp = s.Timestamp,
                Details = s.Details
            }).ToList(),
            ToolInvocations = thoughtProcess.ToolInvocations.Select(t => new ToolInvocationEntry
            {
                ToolName = t.ToolName,
                Parameters = t.Parameters,
                Result = t.Result,
                Status = t.Status,
                ErrorMessage = t.ErrorMessage,
                Timestamp = t.Timestamp
            }).ToList(),
            StartTime = thoughtProcess.StartTime,
            EndTime = thoughtProcess.EndTime,
            DurationMs = thoughtProcess.DurationMs
        };
    }

    /// <summary>
    /// Convert ThoughtProcessDocument to ThoughtProcess.
    /// </summary>
    private ThoughtProcess ConvertToThoughtProcess(ThoughtProcessDocument document)
    {
        return new ThoughtProcess
        {
            Id = document.Id,
            ConversationId = document.ConversationId,
            MessageId = document.MessageId,
            Steps = document.Steps.Select(s => new ThoughtStep
            {
                Id = s.Id,
                Description = s.Description,
                Type = s.Type,
                Timestamp = s.Timestamp,
                Details = s.Details
            }).ToList(),
            ToolInvocations = document.ToolInvocations.Select(t => new ToolInvocation
            {
                ToolName = t.ToolName,
                Parameters = t.Parameters,
                Result = t.Result,
                Status = t.Status,
                ErrorMessage = t.ErrorMessage,
                Timestamp = t.Timestamp
            }).ToList(),
            StartTime = document.StartTime,
            EndTime = document.EndTime,
            DurationMs = document.DurationMs
        };
    }

    /// <summary>
    /// Generate a summary of chat history for diagnostic logging.
    /// </summary>
    private string GetChatHistorySummary(ChatHistory chatHistory)
    {
        var roles = chatHistory.GroupBy(m => m.Role.ToString())
            .Select(g => $"{g.Key}:{g.Count()}")
            .ToList();
        return $"[{string.Join(", ", roles)}]";
    }

    /// <summary>
    /// Extract serializable token usage information from metadata.
    /// This converts the ChatTokenUsage object to a simple dictionary to avoid
    /// JSON serialization issues with non-serializable properties like JsonPatch&.
    /// </summary>
    /// <param name="metadata">Response metadata containing token usage information</param>
    /// <returns>Dictionary with serializable token usage data or null if not available</returns>
    private Dictionary<string, object>? ExtractSerializableTokenUsage(IReadOnlyDictionary<string, object?>? metadata)
    {
        if (metadata == null || !metadata.TryGetValue("Usage", out var usageObject) || usageObject is not ChatTokenUsage tokenUsage)
        {
            return null;
        }

        var usageData = new Dictionary<string, object>
        {
            ["input_tokens"] = tokenUsage.InputTokenCount,
            ["output_tokens"] = tokenUsage.OutputTokenCount,
            ["total_tokens"] = tokenUsage.TotalTokenCount
        };

        // Add input token details if available
        if (tokenUsage.InputTokenDetails != null)
        {
            usageData["cached_tokens"] = tokenUsage.InputTokenDetails.CachedTokenCount;
            usageData["audio_tokens"] = tokenUsage.InputTokenDetails.AudioTokenCount;
        }

        // Add output token details if available
        if (tokenUsage.OutputTokenDetails != null)
        {
            usageData["reasoning_tokens"] = tokenUsage.OutputTokenDetails.ReasoningTokenCount;
        }

        return usageData;
    }

    /// <summary>
    /// Log token metrics from the chat completion response.
    /// </summary>
    /// <param name="metadata">Response metadata containing token usage information</param>
    /// <param name="conversationId">The conversation ID for correlation</param>
    private void LogTokenMetrics(IReadOnlyDictionary<string, object?>? metadata, string conversationId)
    {
        if (metadata == null)
        {
            _logger.LogWarning("[TokenMetrics] No metadata available for conversation {ConversationId}", conversationId);
            return;
        }

        // Extract token usage from metadata
        if (metadata.TryGetValue("Usage", out var usageObject) && usageObject is ChatTokenUsage tokenUsage)
        {
            _logger.LogInformation(
                "[TokenMetrics] ConversationId: {ConversationId}, PromptTokens: {PromptTokens}, CompletionTokens: {CompletionTokens}, TotalTokens: {TotalTokens}",
                conversationId,
                tokenUsage.InputTokenCount,
                tokenUsage.OutputTokenCount,
                tokenUsage.TotalTokenCount);

            // Log token details breakdown if available
            if (tokenUsage.InputTokenDetails != null)
            {
                _logger.LogDebug(
                    "[TokenMetrics] InputTokenDetails - ConversationId: {ConversationId}, CachedTokens: {CachedTokens}, AudioTokens: {AudioTokens}",
                    conversationId,
                    tokenUsage.InputTokenDetails.CachedTokenCount,
                    tokenUsage.InputTokenDetails.AudioTokenCount);
            }

            if (tokenUsage.OutputTokenDetails != null)
            {
                _logger.LogDebug(
                    "[TokenMetrics] OutputTokenDetails - ConversationId: {ConversationId}, ReasoningTokens: {ReasoningTokens}",
                    conversationId,
                    tokenUsage.OutputTokenDetails.ReasoningTokenCount);
            }
        }
        else
        {
            _logger.LogWarning("[TokenMetrics] Token usage not available in response metadata for conversation {ConversationId}", conversationId);
        }

        // Log rate limit headers if available in metadata
        LogRateLimitInfo(metadata, conversationId);
    }

    /// <summary>
    /// Log rate limit information from response metadata if available.
    /// </summary>
    /// <param name="metadata">Response metadata that may contain rate limit information</param>
    /// <param name="conversationId">The conversation ID for correlation</param>
    private void LogRateLimitInfo(IReadOnlyDictionary<string, object?>? metadata, string conversationId)
    {
        if (metadata == null)
        {
            return;
        }

        foreach (var key in RateLimitMetadataKeys)
        {
            if (metadata.TryGetValue(key, out var value) && value != null)
            {
                _logger.LogInformation(
                    "[RateLimitInfo] ConversationId: {ConversationId}, {Key}: {Value}",
                    conversationId,
                    key,
                    value);
            }
        }
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