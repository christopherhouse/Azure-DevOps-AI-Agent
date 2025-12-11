using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzureDevOpsAI.Backend.Models;

/// <summary>
/// Subject query request for searching users and groups.
/// </summary>
public class SubjectQueryRequest
{
    /// <summary>
    /// The search query string (email, display name, or principal name).
    /// </summary>
    [Required]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Optional list of subject kinds to filter by (e.g., User, Group).
    /// If not provided, searches both users and groups.
    /// </summary>
    public List<SubjectKind>? SubjectKind { get; set; }

    /// <summary>
    /// Optional scope descriptor to limit search to specific project or organization scope.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ScopeDescriptor { get; set; }
}

/// <summary>
/// Subject query response containing matching subjects.
/// </summary>
public class SubjectQueryResponse
{
    /// <summary>
    /// List of subjects matching the query.
    /// </summary>
    public List<GraphSubject> Value { get; set; } = new();

    /// <summary>
    /// Number of results returned.
    /// </summary>
    public int Count { get; set; }
}

/// <summary>
/// Graph subject representing a user or group.
/// </summary>
public class GraphSubject
{
    /// <summary>
    /// Subject descriptor (unique identifier).
    /// </summary>
    public string? Descriptor { get; set; }

    /// <summary>
    /// Display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Subject kind (User or Group).
    /// </summary>
    public string? SubjectKind { get; set; }

    /// <summary>
    /// Principal name (email for users, group path for groups).
    /// </summary>
    public string? PrincipalName { get; set; }

    /// <summary>
    /// Mail address.
    /// </summary>
    public string? MailAddress { get; set; }

    /// <summary>
    /// Subject origin (aad, vsts, etc).
    /// </summary>
    public string? Origin { get; set; }

    /// <summary>
    /// Origin ID.
    /// </summary>
    public string? OriginId { get; set; }

    /// <summary>
    /// Domain.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Directory alias.
    /// </summary>
    public string? DirectoryAlias { get; set; }

    /// <summary>
    /// Subject URL.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Container descriptor (project or organization scope).
    /// </summary>
    public string? ContainerDescriptor { get; set; }

    /// <summary>
    /// Links to related resources.
    /// </summary>
    [JsonPropertyName("_links")]
    public Dictionary<string, LinkReference>? Links { get; set; }
}
