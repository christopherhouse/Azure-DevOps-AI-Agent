using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AzureDevOpsAI.Backend.Models;

/// <summary>
/// Account license type enum for Azure DevOps user entitlements.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AccountLicenseType>))]
public enum AccountLicenseType
{
    /// <summary>
    /// No license assigned.
    /// </summary>
    None,
    /// <summary>
    /// Early adopter license.
    /// </summary>
    EarlyAdopter,
    /// <summary>
    /// Express license (free).
    /// </summary>
    Express,
    /// <summary>
    /// Professional license.
    /// </summary>
    Professional,
    /// <summary>
    /// Advanced license.
    /// </summary>
    Advanced,
    /// <summary>
    /// Stakeholder license (limited access).
    /// </summary>
    Stakeholder
}

/// <summary>
/// Subject kind enum for user entitlements.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SubjectKind>))]
public enum SubjectKind
{
    /// <summary>
    /// User subject.
    /// </summary>
    User,
    /// <summary>
    /// Group subject.
    /// </summary>
    Group
}

/// <summary>
/// User entitlement model representing a user's access to Azure DevOps.
/// </summary>
public class UserEntitlement
{
    /// <summary>
    /// User entitlement ID.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// User information.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Access level information.
    /// </summary>
    public AccessLevel? AccessLevel { get; set; }

    /// <summary>
    /// Last access date.
    /// </summary>
    public DateTime? LastAccessedDate { get; set; }

    /// <summary>
    /// Date when entitlement was added.
    /// </summary>
    public DateTime? DateCreated { get; set; }

    /// <summary>
    /// Project entitlements for the user.
    /// </summary>
    public List<ProjectEntitlement>? ProjectEntitlements { get; set; }

    /// <summary>
    /// Extensions assigned to the user.
    /// </summary>
    public List<object>? Extensions { get; set; }

    /// <summary>
    /// Group assignments for the user.
    /// </summary>
    public List<object>? GroupAssignments { get; set; }
}

/// <summary>
/// User information model.
/// </summary>
public class User
{
    /// <summary>
    /// User principal name (email).
    /// </summary>
    public string? PrincipalName { get; set; }

    /// <summary>
    /// User display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// User ID (GUID).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Subject kind (User or Group).
    /// </summary>
    public string? SubjectKind { get; set; }

    /// <summary>
    /// User domain.
    /// </summary>
    public string? Domain { get; set; }

    /// <summary>
    /// User origin (aad, vsts, etc).
    /// </summary>
    public string? Origin { get; set; }

    /// <summary>
    /// User origin ID.
    /// </summary>
    public string? OriginId { get; set; }

    /// <summary>
    /// Meta type (member, guest, etc).
    /// </summary>
    public string? MetaType { get; set; }

    /// <summary>
    /// Directory alias.
    /// </summary>
    public string? DirectoryAlias { get; set; }

    /// <summary>
    /// Mail address.
    /// </summary>
    public string? MailAddress { get; set; }

    /// <summary>
    /// User URL.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// User descriptor.
    /// </summary>
    public string? Descriptor { get; set; }

    /// <summary>
    /// Links to related resources.
    /// </summary>
    [JsonPropertyName("_links")]
    public Dictionary<string, LinkReference>? Links { get; set; }
}

/// <summary>
/// Access level model.
/// </summary>
public class AccessLevel
{
    /// <summary>
    /// License display name.
    /// </summary>
    public string? LicenseDisplayName { get; set; }

    /// <summary>
    /// Account license type.
    /// </summary>
    public string? AccountLicenseType { get; set; }

    /// <summary>
    /// License status (Active, Inactive).
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Licensing source.
    /// </summary>
    public string? LicensingSource { get; set; }

    /// <summary>
    /// MSDN license type.
    /// </summary>
    public string? MsdnLicenseType { get; set; }

    /// <summary>
    /// GitHub license type.
    /// </summary>
    public string? GitHubLicenseType { get; set; }

    /// <summary>
    /// Status message.
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// Assignment source.
    /// </summary>
    public string? AssignmentSource { get; set; }
}

/// <summary>
/// Project entitlement model.
/// </summary>
public class ProjectEntitlement
{
    /// <summary>
    /// Project reference.
    /// </summary>
    public ProjectReference? ProjectRef { get; set; }

    /// <summary>
    /// Group information (role assignment).
    /// </summary>
    public Group? Group { get; set; }
}

/// <summary>
/// Project reference model.
/// </summary>
public class ProjectReference
{
    /// <summary>
    /// Project ID (GUID).
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Project name.
    /// </summary>
    public string? Name { get; set; }
}

/// <summary>
/// Group model for role assignments.
/// </summary>
public class Group
{
    /// <summary>
    /// Group type (e.g., "projectContributor", "projectReader", "projectAdministrator").
    /// </summary>
    public string? GroupType { get; set; }

    /// <summary>
    /// Group display name.
    /// </summary>
    public string? DisplayName { get; set; }
}

/// <summary>
/// Link reference model for API links.
/// </summary>
public class LinkReference
{
    /// <summary>
    /// Link href.
    /// </summary>
    public string? Href { get; set; }
}

/// <summary>
/// User entitlement list response model from Azure DevOps API.
/// </summary>
public class UserEntitlementListResponse
{
    /// <summary>
    /// List of user entitlements.
    /// </summary>
    [JsonPropertyName("items")]
    public List<UserEntitlement> Items { get; set; } = new();

    /// <summary>
    /// Continuation token for paging.
    /// </summary>
    public string? ContinuationToken { get; set; }

    /// <summary>
    /// Total count of user entitlements.
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// Add user entitlement request model for Azure DevOps API.
/// </summary>
public class AddUserEntitlementRequest
{
    /// <summary>
    /// Access level for the user.
    /// </summary>
    [Required]
    public AccessLevelRequest AccessLevel { get; set; } = new();

    /// <summary>
    /// User information.
    /// </summary>
    [Required]
    public UserRequest User { get; set; } = new();

    /// <summary>
    /// Project entitlements to grant user access to specific projects.
    /// </summary>
    public List<ProjectEntitlementRequest>? ProjectEntitlements { get; set; }
}

/// <summary>
/// Access level request for adding user entitlement.
/// </summary>
public class AccessLevelRequest
{
    /// <summary>
    /// Account license type (e.g., "express", "stakeholder", "advanced").
    /// </summary>
    [Required]
    public string AccountLicenseType { get; set; } = string.Empty;
}

/// <summary>
/// User request for adding user entitlement.
/// </summary>
public class UserRequest
{
    /// <summary>
    /// User principal name (email address).
    /// </summary>
    [Required]
    [EmailAddress]
    public string PrincipalName { get; set; } = string.Empty;

    /// <summary>
    /// Subject kind (typically "user").
    /// </summary>
    [Required]
    public string SubjectKind { get; set; } = "user";
}

/// <summary>
/// Project entitlement request for adding user entitlement.
/// </summary>
public class ProjectEntitlementRequest
{
    /// <summary>
    /// Project reference with project ID.
    /// </summary>
    [Required]
    public ProjectReferenceRequest ProjectRef { get; set; } = new();

    /// <summary>
    /// Group assignment (role).
    /// </summary>
    [Required]
    public GroupRequest Group { get; set; } = new();
}

/// <summary>
/// Project reference request.
/// </summary>
public class ProjectReferenceRequest
{
    /// <summary>
    /// Project ID (GUID).
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Group request for role assignment.
/// </summary>
public class GroupRequest
{
    /// <summary>
    /// Group type (e.g., "projectContributor", "projectReader", "projectAdministrator").
    /// </summary>
    [Required]
    public string GroupType { get; set; } = string.Empty;
}

/// <summary>
/// Helper class for parsing project entitlement input from JSON.
/// </summary>
public class ProjectEntitlementInput
{
    /// <summary>
    /// Project ID (GUID).
    /// </summary>
    [JsonPropertyName("projectId")]
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Group type (e.g., "projectContributor", "projectReader", "projectAdministrator").
    /// </summary>
    [JsonPropertyName("groupType")]
    public string GroupType { get; set; } = string.Empty;
}
