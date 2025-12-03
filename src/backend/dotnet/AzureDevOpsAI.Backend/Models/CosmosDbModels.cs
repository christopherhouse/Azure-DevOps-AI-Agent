using System.Text.Json.Serialization;

namespace AzureDevOpsAI.Backend.Models;

/// <summary>
/// Document model for storing chat history in Cosmos DB.
/// </summary>
public class ChatHistoryDocument
{
    /// <summary>
    /// Unique document ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Conversation ID (partition key).
    /// </summary>
    [JsonPropertyName("conversationId")]
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// List of chat messages in this conversation.
    /// </summary>
    [JsonPropertyName("messages")]
    public List<ChatMessageEntry> Messages { get; set; } = new();

    /// <summary>
    /// Timestamp when the conversation was created.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the conversation was last updated.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// System prompt used for this conversation.
    /// </summary>
    [JsonPropertyName("systemPrompt")]
    public string? SystemPrompt { get; set; }
}

/// <summary>
/// Individual chat message entry within a conversation.
/// </summary>
public class ChatMessageEntry
{
    /// <summary>
    /// Message role (system, user, assistant).
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Message content.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the message was added.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Document model for storing thought process in Cosmos DB.
/// </summary>
public class ThoughtProcessDocument
{
    /// <summary>
    /// Unique document ID (thought process ID).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Conversation ID (partition key).
    /// </summary>
    [JsonPropertyName("conversationId")]
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Associated message ID.
    /// </summary>
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Thought process steps.
    /// </summary>
    [JsonPropertyName("steps")]
    public List<ThoughtStepEntry> Steps { get; set; } = new();

    /// <summary>
    /// Tool invocations during processing.
    /// </summary>
    [JsonPropertyName("toolInvocations")]
    public List<ToolInvocationEntry> ToolInvocations { get; set; } = new();

    /// <summary>
    /// Process start time.
    /// </summary>
    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Process end time.
    /// </summary>
    [JsonPropertyName("endTime")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Total processing duration in milliseconds.
    /// </summary>
    [JsonPropertyName("durationMs")]
    public long? DurationMs { get; set; }
}

/// <summary>
/// Thought step entry for Cosmos DB storage.
/// </summary>
public class ThoughtStepEntry
{
    /// <summary>
    /// Step ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Step description.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Step type (analysis, planning, tool_invocation, reasoning, etc.).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Step timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional details about the step.
    /// </summary>
    [JsonPropertyName("details")]
    public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// Tool invocation entry for Cosmos DB storage.
/// </summary>
public class ToolInvocationEntry
{
    /// <summary>
    /// Tool name.
    /// </summary>
    [JsonPropertyName("toolName")]
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// Tool parameters.
    /// </summary>
    [JsonPropertyName("parameters")]
    public Dictionary<string, object>? Parameters { get; set; }

    /// <summary>
    /// Tool result.
    /// </summary>
    [JsonPropertyName("result")]
    public object? Result { get; set; }

    /// <summary>
    /// Execution status.
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Error message if tool failed.
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Execution timestamp.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
