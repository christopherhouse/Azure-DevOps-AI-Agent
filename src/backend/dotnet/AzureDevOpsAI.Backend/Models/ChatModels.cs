using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzureDevOpsAI.Backend.Models;

/// <summary>
/// Chat message model.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// Message ID.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Message role (user, assistant, system).
    /// </summary>
    [Required]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Message content.
    /// </summary>
    [Required]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Message timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Chat request model.
/// </summary>
public class ChatRequest
{
    /// <summary>
    /// User message.
    /// </summary>
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Conversation ID.
    /// </summary>
    public string? ConversationId { get; set; }

    /// <summary>
    /// Context information.
    /// </summary>
    public Dictionary<string, object>? Context { get; set; }
}

/// <summary>
/// Citation model for AI responses.
/// </summary>
public class Citation
{
    /// <summary>
    /// Citation title or source name.
    /// </summary>
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Citation URL or reference.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Citation type (documentation, example, feature, etc.).
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Brief description of the cited content.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Chat response model.
/// </summary>
public class ChatResponse
{
    /// <summary>
    /// AI response message.
    /// </summary>
    [Required]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Conversation ID.
    /// </summary>
    [Required]
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Suggested follow-up actions.
    /// </summary>
    public List<string>? Suggestions { get; set; }

    /// <summary>
    /// Citations for the AI response.
    /// </summary>
    public List<Citation>? Citations { get; set; }

    /// <summary>
    /// Response metadata.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Thought process ID for this response.
    /// </summary>
    public string? ThoughtProcessId { get; set; }
}

/// <summary>
/// Thought process step model.
/// </summary>
public class ThoughtStep
{
    /// <summary>
    /// Step ID.
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Step description.
    /// </summary>
    [Required]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Step type (analysis, planning, tool_invocation, reasoning, etc.).
    /// </summary>
    [Required]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Step timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional details about the step.
    /// </summary>
    public Dictionary<string, object>? Details { get; set; }
}

/// <summary>
/// Tool invocation model.
/// </summary>
public class ToolInvocation
{
    /// <summary>
    /// Tool name.
    /// </summary>
    [Required]
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// Tool parameters.
    /// </summary>
    public Dictionary<string, object>? Parameters { get; set; }

    /// <summary>
    /// Tool result.
    /// </summary>
    public object? Result { get; set; }

    /// <summary>
    /// Execution status.
    /// </summary>
    [Required]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Error message if tool failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Execution timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Thought process model.
/// </summary>
public class ThoughtProcess
{
    /// <summary>
    /// Process ID.
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Associated conversation ID.
    /// </summary>
    [Required]
    public string ConversationId { get; set; } = string.Empty;

    /// <summary>
    /// Associated message ID.
    /// </summary>
    [Required]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Thought process steps.
    /// </summary>
    public List<ThoughtStep> Steps { get; set; } = new();

    /// <summary>
    /// Tool invocations during processing.
    /// </summary>
    public List<ToolInvocation> ToolInvocations { get; set; } = new();

    /// <summary>
    /// Process start time.
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Process end time.
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Total processing duration in milliseconds.
    /// </summary>
    public long? DurationMs { get; set; }
}

/// <summary>
/// Conversation model.
/// </summary>
public class Conversation
{
    /// <summary>
    /// Conversation ID.
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// User ID.
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Conversation title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Conversation messages.
    /// </summary>
    public List<ChatMessage> Messages { get; set; } = new();
}