using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzureDevOpsAI.Backend.Models;

/// <summary>
/// Graph group model representing an Azure DevOps group.
/// </summary>
public class GraphGroup
{
    /// <summary>
    /// Group descriptor (unique identifier).
    /// </summary>
    public string? Descriptor { get; set; }

    /// <summary>
    /// Group display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Group URL.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Group origin (aad, vsts, etc).
    /// </summary>
    public string? Origin { get; set; }

    /// <summary>
    /// Group origin ID.
    /// </summary>
    public string? OriginId { get; set; }

    /// <summary>
    /// Subject kind (group).
    /// </summary>
    public string? SubjectKind { get; set; }

    /// <summary>
    /// Group domain.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Mail address for the group.
    /// </summary>
    public string? MailAddress { get; set; }

    /// <summary>
    /// Group principal name.
    /// </summary>
    public string? PrincipalName { get; set; }

    /// <summary>
    /// Group description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this is a special/restricted group.
    /// </summary>
    public bool? IsRestrictedVisible { get; set; }

    /// <summary>
    /// Whether this is a cross-project group.
    /// </summary>
    public bool? IsCrossProject { get; set; }

    /// <summary>
    /// Whether this is a deleted group.
    /// </summary>
    public bool? IsDeleted { get; set; }

    /// <summary>
    /// Links to related resources.
    /// </summary>
    [JsonPropertyName("_links")]
    public Dictionary<string, LinkReference>? Links { get; set; }
}

/// <summary>
/// Graph group list response model from Azure DevOps Graph API.
/// </summary>
public class GraphGroupListResponse
{
    /// <summary>
    /// List of groups from the API response.
    /// </summary>
    public List<GraphGroup> Value { get; set; } = new();

    /// <summary>
    /// Total count of groups.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Continuation token for paging.
    /// </summary>
    public string? ContinuationToken { get; set; }
}

/// <summary>
/// Graph member model representing a member of a group (user or group).
/// </summary>
public class GraphMember
{
    /// <summary>
    /// Member descriptor (unique identifier).
    /// </summary>
    public string? Descriptor { get; set; }

    /// <summary>
    /// Member display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Member URL.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Member origin (aad, vsts, etc).
    /// </summary>
    public string? Origin { get; set; }

    /// <summary>
    /// Member origin ID.
    /// </summary>
    public string? OriginId { get; set; }

    /// <summary>
    /// Subject kind (user or group).
    /// </summary>
    public string? SubjectKind { get; set; }

    /// <summary>
    /// Member domain.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// Mail address for the member.
    /// </summary>
    public string? MailAddress { get; set; }

    /// <summary>
    /// Member principal name.
    /// </summary>
    public string? PrincipalName { get; set; }

    /// <summary>
    /// Directory alias.
    /// </summary>
    public string? DirectoryAlias { get; set; }

    /// <summary>
    /// Meta type (member, guest, etc).
    /// </summary>
    public string? MetaType { get; set; }

    /// <summary>
    /// Links to related resources.
    /// </summary>
    [JsonPropertyName("_links")]
    public Dictionary<string, LinkReference>? Links { get; set; }
}

/// <summary>
/// Graph member list response model from Azure DevOps Graph API.
/// </summary>
public class GraphMemberListResponse
{
    /// <summary>
    /// List of members from the API response.
    /// </summary>
    public List<GraphMember> Value { get; set; } = new();

    /// <summary>
    /// Total count of members.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Continuation token for paging.
    /// </summary>
    public string? ContinuationToken { get; set; }
}

/// <summary>
/// Graph group creation context for creating new groups.
/// </summary>
public class GraphGroupCreationContext
{
    /// <summary>
    /// Display name for the new group.
    /// </summary>
    [Required]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Description for the new group.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Storage key (used internally by API).
    /// </summary>
    public string? StorageKey { get; set; }
}

/// <summary>
/// Graph membership state model representing a membership relationship.
/// </summary>
public class GraphMembershipState
{
    /// <summary>
    /// Whether the membership is active.
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// Links to related resources.
    /// </summary>
    [JsonPropertyName("_links")]
    public Dictionary<string, LinkReference>? Links { get; set; }
}

/// <summary>
/// Graph descriptor response model for descriptor operations.
/// </summary>
public class GraphDescriptor
{
    /// <summary>
    /// Descriptor value.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Links to related resources.
    /// </summary>
    [JsonPropertyName("_links")]
    public Dictionary<string, LinkReference>? Links { get; set; }
}

/// <summary>
/// Slim group model containing only essential attributes to reduce payload size.
/// </summary>
public class SlimGroup
{
    /// <summary>
    /// Group description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Group principal name.
    /// </summary>
    public string? PrincipalName { get; set; }

    /// <summary>
    /// Group origin (aad, vsts, etc).
    /// </summary>
    public string? Origin { get; set; }

    /// <summary>
    /// Group display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Group descriptor (unique identifier).
    /// </summary>
    public string? Descriptor { get; set; }
}

/// <summary>
/// Slim group list response model with reduced attributes.
/// </summary>
public class SlimGroupListResponse
{
    /// <summary>
    /// List of slim groups from the API response.
    /// </summary>
    public List<SlimGroup> Value { get; set; } = new();

    /// <summary>
    /// Total count of groups.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// Continuation token for paging.
    /// </summary>
    public string? ContinuationToken { get; set; }
}
