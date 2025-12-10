using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Services;

namespace AzureDevOpsAI.Backend.Plugins;

/// <summary>
/// Semantic Kernel plugin for Azure DevOps user operations.
/// </summary>
public class UsersPlugin
{
    private readonly IAzureDevOpsApiService _azureDevOpsApiService;
    private readonly ILogger<UsersPlugin> _logger;

    public UsersPlugin(IAzureDevOpsApiService azureDevOpsApiService, ILogger<UsersPlugin> logger)
    {
        _azureDevOpsApiService = azureDevOpsApiService;
        _logger = logger;
    }

    /// <summary>
    /// List all users in an Azure DevOps organization with their licensing levels and GUIDs.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization name</param>
    /// <returns>List of users with their entitlements and licensing information</returns>
    [KernelFunction("list_users")]
    [Description("List all users in an Azure DevOps organization. Returns user principal names, display names, GUIDs, licensing levels, and last access dates.")]
    public async Task<string> ListUsersAsync(
        [Description("The Azure DevOps organization name")] string organization)
    {
        try
        {
            _logger.LogInformation("Listing users for organization: {Organization}", organization);

            // Use vsaex API endpoint for user entitlements
            // The endpoint is https://vsaex.dev.azure.com/{organization}/_apis/userentitlements
            var userEntitlements = await _azureDevOpsApiService.GetAsync<UserEntitlementListResponse>(
                organization, "https://vsaex.dev.azure.com/" + organization + "/_apis/userentitlements", "7.1");

            if (userEntitlements?.Items == null || !userEntitlements.Items.Any())
            {
                return JsonSerializer.Serialize(new { message = "No users found in this organization.", users = new List<object>() });
            }

            // Return raw JSON data for the AI to format
            var usersData = userEntitlements.Items.Where(e => e.User != null).Select(e => new
            {
                displayName = e.User!.DisplayName ?? "Unknown",
                principalName = e.User.PrincipalName ?? "N/A",
                userId = e.User.Id ?? "N/A",
                license = e.AccessLevel?.LicenseDisplayName ?? e.AccessLevel?.AccountLicenseType ?? "N/A",
                status = e.AccessLevel?.Status ?? "N/A",
                lastAccessedDate = e.LastAccessedDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                dateCreated = e.DateCreated?.ToString("yyyy-MM-dd")
            }).ToList();

            _logger.LogInformation("Successfully retrieved {Count} users for organization: {Organization}",
                userEntitlements.Items.Count, organization);

            return JsonSerializer.Serialize(new
            {
                organization,
                totalUsers = userEntitlements.Items.Count,
                users = usersData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing users for organization: {Organization}", organization);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Add a user entitlement to an Azure DevOps organization.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization name</param>
    /// <param name="principalName">User principal name (email address)</param>
    /// <param name="accountLicenseType">Account license type (e.g., express, stakeholder, advanced)</param>
    /// <param name="projectEntitlementsJson">Optional JSON array of project entitlements with projectId and groupType</param>
    /// <returns>Result of the user entitlement creation operation</returns>
    [KernelFunction("add_user_entitlement")]
    [Description("Add a user entitlement to an Azure DevOps organization. Assigns a license and optionally grants access to specific projects with roles. Project IDs can be obtained using list_projects. Valid groupType values: projectReader, projectContributor, projectAdministrator.")]
    public async Task<string> AddUserEntitlementAsync(
        [Description("The Azure DevOps organization name")] string organization,
        [Description("User principal name (email address)")] string principalName,
        [Description("Account license type: express, stakeholder, advanced, professional")] string accountLicenseType,
        [Description("Optional JSON array of project entitlements. Format: [{\"projectId\":\"guid\",\"groupType\":\"projectContributor\"}]. Use list_projects to get project IDs. Valid groupType: projectReader, projectContributor, projectAdministrator")] string? projectEntitlementsJson = null)
    {
        try
        {
            _logger.LogInformation("Adding user entitlement for {PrincipalName} in organization: {Organization}",
                principalName, organization);

            // Validate inputs
            if (string.IsNullOrWhiteSpace(principalName))
            {
                return JsonSerializer.Serialize(new { error = "Principal name (email address) is required." });
            }

            if (string.IsNullOrWhiteSpace(accountLicenseType))
            {
                return JsonSerializer.Serialize(new { error = "Account license type is required. Valid values: express, stakeholder, advanced, professional, earlyAdopter." });
            }

            // Normalize license type to lowercase
            var normalizedLicenseType = accountLicenseType.ToLower();

            // Validate license type
            var validLicenseTypes = new[] { "express", "stakeholder", "advanced", "professional", "earlyadopter" };
            if (!validLicenseTypes.Contains(normalizedLicenseType, StringComparer.OrdinalIgnoreCase))
            {
                return JsonSerializer.Serialize(new { error = $"Invalid license type '{accountLicenseType}'. Valid values: express, stakeholder, advanced, professional, earlyAdopter." });
            }

            // Create the user entitlement request
            var request = new AddUserEntitlementRequest
            {
                AccessLevel = new AccessLevelRequest
                {
                    AccountLicenseType = normalizedLicenseType
                },
                User = new UserRequest
                {
                    PrincipalName = principalName,
                    SubjectKind = "user"
                }
            };

            List<object>? parsedProjectEntitlements = null;

            // Parse and add project entitlements if provided
            if (!string.IsNullOrWhiteSpace(projectEntitlementsJson))
            {
                try
                {
                    var projectEntitlements = System.Text.Json.JsonSerializer.Deserialize<List<ProjectEntitlementInput>>(projectEntitlementsJson);
                    if (projectEntitlements != null && projectEntitlements.Any())
                    {
                        request.ProjectEntitlements = projectEntitlements.Select(pe => new ProjectEntitlementRequest
                        {
                            ProjectRef = new ProjectReferenceRequest
                            {
                                Id = pe.ProjectId
                            },
                            Group = new GroupRequest
                            {
                                GroupType = pe.GroupType
                            }
                        }).ToList();

                        parsedProjectEntitlements = projectEntitlements.Select(pe => new
                        {
                            projectId = pe.ProjectId,
                            groupType = pe.GroupType
                        } as object).ToList();

                        _logger.LogInformation("Adding user with {Count} project entitlements", projectEntitlements.Count);
                    }
                }
                catch (System.Text.Json.JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse project entitlements JSON");
                    return JsonSerializer.Serialize(new { error = $"Invalid project entitlements JSON format. Expected format: [{{\"projectId\":\"guid\",\"groupType\":\"projectContributor\"}}]. Error: {ex.Message}" });
                }
            }
            else
            {
                _logger.LogInformation("No project entitlements specified. User will have organization-level access only.");
            }

            // Use vsaex API endpoint for user entitlements
            var result = await _azureDevOpsApiService.PostAsync<object>(
                organization,
                "https://vsaex.dev.azure.com/" + organization + "/_apis/userentitlements",
                request,
                "7.1");

            if (result == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to add user entitlement. Please check the logs for more details." });
            }

            // Return raw JSON data for the AI to format
            var response = new
            {
                success = true,
                user = principalName,
                organization,
                license = accountLicenseType,
                projectEntitlements = parsedProjectEntitlements,
                hasProjectAccess = parsedProjectEntitlements != null && parsedProjectEntitlements.Any()
            };

            _logger.LogInformation("Successfully added user entitlement for {PrincipalName}", principalName);
            return JsonSerializer.Serialize(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user entitlement for {PrincipalName} in organization: {Organization}",
                principalName, organization);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}

/// <summary>
/// Helper class for parsing project entitlement input.
/// </summary>
internal class ProjectEntitlementInput
{
    [System.Text.Json.Serialization.JsonPropertyName("projectId")]
    public string ProjectId { get; set; } = string.Empty;

    [System.Text.Json.Serialization.JsonPropertyName("groupType")]
    public string GroupType { get; set; } = string.Empty;
}
