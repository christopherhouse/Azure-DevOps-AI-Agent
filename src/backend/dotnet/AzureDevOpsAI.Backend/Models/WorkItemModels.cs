using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzureDevOpsAI.Backend.Models;

/// <summary>
/// Work item type enum.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorkItemType
{
    [JsonPropertyName("User Story")]
    UserStory,
    Task,
    Bug,
    Epic,
    Feature,
    [JsonPropertyName("Test Case")]
    TestCase
}

/// <summary>
/// Work item state enum.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorkItemState
{
    New,
    Active,
    Resolved,
    Closed,
    Removed
}

/// <summary>
/// Work item creation request model.
/// </summary>
public class WorkItemCreate
{
    /// <summary>
    /// Work item title.
    /// </summary>
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Work item type.
    /// </summary>
    public WorkItemType WorkItemType { get; set; }

    /// <summary>
    /// Work item description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Assigned user email.
    /// </summary>
    public string? AssignedTo { get; set; }

    /// <summary>
    /// Priority (1-4).
    /// </summary>
    [Range(1, 4)]
    public int? Priority { get; set; } = 2;

    /// <summary>
    /// Work item tags.
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Parent work item ID.
    /// </summary>
    public int? ParentId { get; set; }
}

/// <summary>
/// Work item update request model.
/// </summary>
public class WorkItemUpdate
{
    /// <summary>
    /// Work item title.
    /// </summary>
    [StringLength(255, MinimumLength = 1)]
    public string? Title { get; set; }

    /// <summary>
    /// Work item description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Assigned user email.
    /// </summary>
    public string? AssignedTo { get; set; }

    /// <summary>
    /// Work item state.
    /// </summary>
    public WorkItemState? State { get; set; }

    /// <summary>
    /// Priority (1-4).
    /// </summary>
    [Range(1, 4)]
    public int? Priority { get; set; }

    /// <summary>
    /// Work item tags.
    /// </summary>
    public List<string>? Tags { get; set; }
}

/// <summary>
/// Work item model.
/// </summary>
public class WorkItem
{
    /// <summary>
    /// Work item ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Work item title.
    /// </summary>
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Work item type.
    /// </summary>
    [Required]
    public string WorkItemType { get; set; } = string.Empty;

    /// <summary>
    /// Work item state.
    /// </summary>
    [Required]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Work item description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Assigned user.
    /// </summary>
    public string? AssignedTo { get; set; }

    /// <summary>
    /// Created by user.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Creation date.
    /// </summary>
    public DateTime? CreatedDate { get; set; }

    /// <summary>
    /// Last change date.
    /// </summary>
    public DateTime? ChangedDate { get; set; }

    /// <summary>
    /// Priority.
    /// </summary>
    public int? Priority { get; set; }

    /// <summary>
    /// Work item tags.
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Work item URL.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Project ID.
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// Parent work item ID.
    /// </summary>
    public int? ParentId { get; set; }
}

/// <summary>
/// Work item list response model.
/// </summary>
public class WorkItemList
{
    /// <summary>
    /// List of work items.
    /// </summary>
    public List<WorkItem> WorkItems { get; set; } = new();

    /// <summary>
    /// Total count of work items.
    /// </summary>
    public int Count { get; set; }
}