using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzureDevOpsAI.Backend.Models;

/// <summary>
/// Project visibility enum.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProjectVisibility
{
    Private,
    Public
}

/// <summary>
/// Project state enum.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProjectState
{
    Creating,
    Created,
    Deleting,
    Deleted
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