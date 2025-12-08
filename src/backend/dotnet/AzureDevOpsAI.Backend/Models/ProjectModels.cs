using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AzureDevOpsAI.Backend.Models;

/// <summary>
/// Project visibility enum.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ProjectVisibility>))]
public enum ProjectVisibility
{
    Private,
    Public
}

/// <summary>
/// Project state enum matching Azure DevOps REST API values.
/// Values: wellFormed, deleting, new, createPending, unchanged, deleted, all
/// </summary>
[JsonConverter(typeof(ProjectStateJsonConverter))]
public enum ProjectState
{
    /// <summary>
    /// Project is well-formed and fully functional.
    /// </summary>
    WellFormed,
    /// <summary>
    /// Project is in the process of being deleted.
    /// </summary>
    Deleting,
    /// <summary>
    /// Project is new/being created.
    /// </summary>
    New,
    /// <summary>
    /// Project creation is pending.
    /// </summary>
    CreatePending,
    /// <summary>
    /// Project is unchanged (filter state).
    /// </summary>
    Unchanged,
    /// <summary>
    /// Project has been deleted.
    /// </summary>
    Deleted,
    /// <summary>
    /// All states (filter state).
    /// </summary>
    All
}

/// <summary>
/// Custom JSON converter for ProjectState that handles camelCase values from Azure DevOps REST API.
/// </summary>
public class ProjectStateJsonConverter : JsonConverter<ProjectState>
{
    public override ProjectState Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value))
        {
            return ProjectState.WellFormed;
        }
        
        // Handle camelCase values from Azure DevOps REST API
        return value.ToLowerInvariant() switch
        {
            "wellformed" => ProjectState.WellFormed,
            "deleting" => ProjectState.Deleting,
            "new" => ProjectState.New,
            "createpending" => ProjectState.CreatePending,
            "unchanged" => ProjectState.Unchanged,
            "deleted" => ProjectState.Deleted,
            "all" => ProjectState.All,
            _ => ProjectState.WellFormed // Default to WellFormed for unknown values
        };
    }

    public override void Write(Utf8JsonWriter writer, ProjectState value, JsonSerializerOptions options)
    {
        // Serialize using camelCase to match Azure DevOps REST API format
        var stringValue = value switch
        {
            ProjectState.WellFormed => "wellFormed",
            ProjectState.Deleting => "deleting",
            ProjectState.New => "new",
            ProjectState.CreatePending => "createPending",
            ProjectState.Unchanged => "unchanged",
            ProjectState.Deleted => "deleted",
            ProjectState.All => "all",
            _ => "wellFormed"
        };
        writer.WriteStringValue(stringValue);
    }
}

/// <summary>
/// Project creation request model.
/// </summary>
public class ProjectCreate
{
    /// <summary>
    /// Project name.
    /// </summary>
    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Project description.
    /// </summary>
    [StringLength(255)]
    public string? Description { get; set; }

    /// <summary>
    /// Project visibility.
    /// </summary>
    public ProjectVisibility Visibility { get; set; } = ProjectVisibility.Private;

    /// <summary>
    /// Source control type.
    /// </summary>
    public string SourceControlType { get; set; } = "Git";

    /// <summary>
    /// Process template ID.
    /// </summary>
    public string? TemplateTypeId { get; set; }
}

/// <summary>
/// Project update request model.
/// </summary>
public class ProjectUpdate
{
    /// <summary>
    /// Project name.
    /// </summary>
    [StringLength(64, MinimumLength = 1)]
    public string? Name { get; set; }

    /// <summary>
    /// Project description.
    /// </summary>
    [StringLength(255)]
    public string? Description { get; set; }
}

/// <summary>
/// Project model.
/// </summary>
public class Project
{
    /// <summary>
    /// Project ID.
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Project name.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Project description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Project URL.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Project state.
    /// </summary>
    public ProjectState State { get; set; }

    /// <summary>
    /// Project visibility.
    /// </summary>
    public ProjectVisibility Visibility { get; set; }

    /// <summary>
    /// Project revision.
    /// </summary>
    public int? Revision { get; set; }

    /// <summary>
    /// Last update time.
    /// </summary>
    public DateTime? LastUpdateTime { get; set; }

    /// <summary>
    /// Project capabilities.
    /// </summary>
    public Dictionary<string, object>? Capabilities { get; set; }
}

/// <summary>
/// Project list response model.
/// </summary>
public class ProjectList
{
    /// <summary>
    /// List of projects.
    /// </summary>
    public List<Project> Projects { get; set; } = new();

    /// <summary>
    /// Total count of projects.
    /// </summary>
    public int Count { get; set; }
}

/// <summary>
/// Project list API response model (matches Azure DevOps REST API response format).
/// </summary>
public class ProjectListResponse
{
    /// <summary>
    /// List of projects from the API response.
    /// </summary>
    public List<Project> Value { get; set; } = new();

    /// <summary>
    /// Total count of projects.
    /// </summary>
    public int Count { get; set; }
}

/// <summary>
/// Process template customization type enum.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ProcessCustomizationType>))]
public enum ProcessCustomizationType
{
    System,
    Inherited,
    Custom
}

/// <summary>
/// Process template model.
/// </summary>
public class ProcessTemplate
{
    /// <summary>
    /// Process template type ID.
    /// </summary>
    [Required]
    public string TypeId { get; set; } = string.Empty;

    /// <summary>
    /// Process template reference name.
    /// </summary>
    public string? ReferenceName { get; set; }

    /// <summary>
    /// Process template name.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Process template description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Parent process type ID.
    /// </summary>
    public string? ParentProcessTypeId { get; set; }

    /// <summary>
    /// Indicates if the process is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Indicates if this is the default process.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Process customization type.
    /// </summary>
    public ProcessCustomizationType CustomizationType { get; set; }
}

/// <summary>
/// Process templates list response model.
/// </summary>
public class ProcessTemplateList
{
    /// <summary>
    /// List of process templates.
    /// </summary>
    public List<ProcessTemplate> Value { get; set; } = new();

    /// <summary>
    /// Total count of process templates.
    /// </summary>
    public int Count { get; set; }
}