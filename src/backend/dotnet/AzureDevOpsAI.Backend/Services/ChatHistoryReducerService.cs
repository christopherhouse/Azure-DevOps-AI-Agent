using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using AzureDevOpsAI.Backend.Configuration;
using Microsoft.Extensions.Options;

namespace AzureDevOpsAI.Backend.Services;

/// <summary>
/// Interface for chat history reduction strategies.
/// </summary>
public interface IChatHistoryReducer
{
    /// <summary>
    /// Reduce chat history to fit within token limits while preserving context.
    /// </summary>
    /// <param name="chatHistory">Full chat history</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Reduced chat history or null if no reduction needed</returns>
    Task<ChatHistory?> ReduceAsync(ChatHistory chatHistory, CancellationToken cancellationToken = default);
}

/// <summary>
/// Chat history truncation reducer that removes oldest messages.
/// </summary>
public class ChatHistoryTruncationReducer : IChatHistoryReducer
{
    private readonly ChatHistoryReductionSettings _settings;
    private readonly ILogger<ChatHistoryTruncationReducer> _logger;

    public ChatHistoryTruncationReducer(
        IOptions<ChatHistoryReductionSettings> settings,
        ILogger<ChatHistoryTruncationReducer> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Reduce chat history by truncating oldest messages.
    /// </summary>
    public Task<ChatHistory?> ReduceAsync(ChatHistory chatHistory, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogDebug("Chat history reduction is disabled");
            return Task.FromResult<ChatHistory?>(null);
        }

        var messageCount = chatHistory.Count;

        // Check if reduction is needed
        if (messageCount <= _settings.ThresholdCount)
        {
            _logger.LogDebug("Chat history message count ({Count}) below threshold ({Threshold}), no reduction needed",
                messageCount, _settings.ThresholdCount);
            return Task.FromResult<ChatHistory?>(null);
        }

        _logger.LogInformation("Reducing chat history from {CurrentCount} to {TargetCount} messages using truncation",
            messageCount, _settings.TargetCount);

        var reducedHistory = new ChatHistory();

        // Always keep system message if present
        var systemMessage = chatHistory.FirstOrDefault(m => m.Role == AuthorRole.System);
        if (systemMessage != null)
        {
            reducedHistory.Add(systemMessage);
        }

        // Calculate how many messages to keep
        var systemMessageCount = systemMessage != null ? 1 : 0;
        var messagesToKeep = _settings.TargetCount - systemMessageCount;

        // Keep the most recent N messages (excluding system message)
        var recentMessages = chatHistory
            .Where(m => m.Role != AuthorRole.System)
            .TakeLast(messagesToKeep)
            .ToList();

        foreach (var message in recentMessages)
        {
            reducedHistory.Add(message);
        }

        _logger.LogInformation("Truncated chat history to {FinalCount} messages (system: {SystemCount}, recent: {RecentCount})",
            reducedHistory.Count, systemMessageCount, recentMessages.Count);

        return Task.FromResult<ChatHistory?>(reducedHistory);
    }
}

/// <summary>
/// Chat history summarization reducer that summarizes older messages.
/// </summary>
public class ChatHistorySummarizationReducer : IChatHistoryReducer
{
    private readonly ChatHistoryReductionSettings _settings;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly ILogger<ChatHistorySummarizationReducer> _logger;
    private readonly Kernel _kernel;

    public ChatHistorySummarizationReducer(
        IOptions<ChatHistoryReductionSettings> settings,
        IChatCompletionService chatCompletionService,
        Kernel kernel,
        ILogger<ChatHistorySummarizationReducer> logger)
    {
        _settings = settings.Value;
        _chatCompletionService = chatCompletionService;
        _kernel = kernel;
        _logger = logger;
    }

    /// <summary>
    /// Reduce chat history by summarizing older messages.
    /// </summary>
    public async Task<ChatHistory?> ReduceAsync(ChatHistory chatHistory, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogDebug("Chat history reduction is disabled");
            return null;
        }

        var messageCount = chatHistory.Count;

        // Check if reduction is needed
        if (messageCount <= _settings.ThresholdCount)
        {
            _logger.LogDebug("Chat history message count ({Count}) below threshold ({Threshold}), no reduction needed",
                messageCount, _settings.ThresholdCount);
            return null;
        }

        _logger.LogInformation("Reducing chat history from {CurrentCount} messages using summarization",
            messageCount);

        var reducedHistory = new ChatHistory();

        // Always keep system message if present
        var systemMessage = chatHistory.FirstOrDefault(m => m.Role == AuthorRole.System);
        if (systemMessage != null)
        {
            reducedHistory.Add(systemMessage);
        }

        // Separate messages into: system, messages to summarize, and recent messages to keep
        var allMessages = chatHistory.Where(m => m.Role != AuthorRole.System).ToList();
        var recentMessages = allMessages.TakeLast(_settings.KeepLastMessages).ToList();
        var messagesToSummarize = allMessages.Take(allMessages.Count - _settings.KeepLastMessages).ToList();

        _logger.LogDebug("Messages to summarize: {SummarizeCount}, recent messages to keep: {KeepCount}",
            messagesToSummarize.Count, recentMessages.Count);

        // If there are messages to summarize, create a summary
        if (messagesToSummarize.Any())
        {
            var summary = await SummarizeMessagesAsync(messagesToSummarize, cancellationToken);
            
            // Add summary as a system message to provide context
            reducedHistory.AddSystemMessage($"Previous conversation summary: {summary}");
            
            _logger.LogInformation("Created summary of {MessageCount} older messages ({SummaryLength} chars)",
                messagesToSummarize.Count, summary.Length);
        }

        // Add recent messages
        foreach (var message in recentMessages)
        {
            reducedHistory.Add(message);
        }

        _logger.LogInformation("Reduced chat history to {FinalCount} messages (includes summary)",
            reducedHistory.Count);

        return reducedHistory;
    }

    /// <summary>
    /// Summarize a list of messages into a concise summary.
    /// </summary>
    private async Task<string> SummarizeMessagesAsync(List<ChatMessageContent> messages, CancellationToken cancellationToken)
    {
        // Build conversation text to summarize
        var conversationText = string.Join("\n", messages.Select(m =>
            $"{m.Role}: {m.Content}"));

        // Create summarization prompt
        var summarizationPrompt = $@"Provide a concise summary of the following conversation, highlighting key topics, decisions, and important context. Keep the summary brief but informative.

Conversation:
{conversationText}

Summary:";

        var summarizationHistory = new ChatHistory();
        summarizationHistory.AddSystemMessage("You are an expert at summarizing conversations concisely while preserving key information.");
        summarizationHistory.AddUserMessage(summarizationPrompt);

        try
        {
            var response = await _chatCompletionService.GetChatMessageContentAsync(
                summarizationHistory,
                cancellationToken: cancellationToken);

            return response.Content ?? "Unable to generate summary.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating conversation summary");
            // Return a simple concatenation as fallback
            return $"Previous conversation covered: {string.Join(", ", messages.Take(5).Select(m => {
                var contentLength = m.Content?.Length ?? 0;
                var previewLength = Math.Min(50, contentLength);
                return contentLength > 0 ? m.Content?.Substring(0, previewLength) : "";
            }))}...";
        }
    }
}
