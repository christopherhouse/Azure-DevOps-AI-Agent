using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Services;

namespace AzureDevOpsAI.Backend.Plugins;

/// <summary>
/// Semantic Kernel plugin for Azure DevOps user entitlement management operations.
/// </summary>
public class UserEntitlementPlugin
{
    private readonly IAzureDevOpsApiService _azureDevOpsApiService;
    private readonly ILogger<UserEntitlementPlugin> _logger;
    private const string VsaexBaseUrl = "https://vsaex.dev.azure.com";

    public UserEntitlementPlugin(IAzureDevOpsApiService azureDevOpsApiService, ILogger<UserEntitlementPlugin> logger)
    {
        _azureDevOpsApiService = azureDevOpsApiService;
        _logger = logger;
    }

    /// <summary>
    /// List user entitlements in an Azure DevOps organization with their licensing levels and GUIDs.
    /// Supports paging for large result sets.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization name</param>
    /// <param name="top">Optional number of results to return per page (default: 100, max: 10000)</param>
    /// <param name="continuationToken">Optional continuation token from previous page for retrieving next page of results</param>
    /// <returns>List of user entitlements with their licensing information</returns>
    [KernelFunction("list_entitlements")]
    [Description("List user entitlements in an Azure DevOps organization with paging support. Returns user principal names, display names, GUIDs, licensing levels, and last access dates. For large organizations, use 'top' to limit results per page (default 100, max 10000) and 'continuationToken' from previous response to get next page.")]
    public async Task<string> ListEntitlementsAsync(
        [Description("The Azure DevOps organization name")] string organization,
        [Description("Optional: Number of entitlements to return per page. Default is 100, maximum is 10000. Use smaller values for faster responses.")] int? top = null,
        [Description("Optional: Continuation token from previous page's response. Include this to retrieve the next page of results when there are more entitlements available.")] string? continuationToken = null)
    {
        try
        {
            _logger.LogInformation("Listing user entitlements for organization: {Organization}, top: {Top}, hasContinuationToken: {HasToken}", 
                organization, top ?? 100, !string.IsNullOrEmpty(continuationToken));

            // Build API URL with query parameters
            var apiPath = $"{VsaexBaseUrl}/{organization}/_apis/userentitlements";
            var queryParams = new List<string>();
            
            if (top.HasValue)
            {
                // Validate top parameter
                if (top.Value < 1 || top.Value > 10000)
                {
                    return JsonSerializer.Serialize(new { error = "The 'top' parameter must be between 1 and 10000." });
                }
                queryParams.Add($"top={top.Value}");
            }
            
            if (!string.IsNullOrWhiteSpace(continuationToken))
            {
                queryParams.Add($"continuationToken={Uri.EscapeDataString(continuationToken)}");
            }
            
            if (queryParams.Any())
            {
                apiPath += "?" + string.Join("&", queryParams);
            }

            // Use vsaex API endpoint for user entitlements with API version 5.1-preview.2
            var userEntitlements = await _azureDevOpsApiService.GetAsync<UserEntitlementListResponse>(
                organization, apiPath, "5.1-preview.2");

            if (userEntitlements?.Items == null || !userEntitlements.Items.Any())
            {
                return JsonSerializer.Serialize(new 
                { 
                    message = "No user entitlements found in this organization.", 
                    entitlements = new List<object>(),
                    continuationToken = (string?)null,
                    hasMoreResults = false
                });
            }

            // Return raw JSON data for the AI to format
            var entitlementsData = userEntitlements.Items.Where(e => e.User != null).Select(e => new
            {
                id = e.Id ?? "N/A",
                displayName = e.User!.DisplayName ?? "Unknown",
                principalName = e.User.PrincipalName ?? "N/A",
                userId = e.User.Id ?? "N/A",
                license = e.AccessLevel?.LicenseDisplayName ?? e.AccessLevel?.AccountLicenseType ?? "N/A",
                status = e.AccessLevel?.Status ?? "N/A",
                lastAccessedDate = e.LastAccessedDate?.ToString("yyyy-MM-dd HH:mm:ss"),
                dateCreated = e.DateCreated?.ToString("yyyy-MM-dd")
            }).ToList();

            _logger.LogInformation("Successfully retrieved {Count} user entitlements for organization: {Organization}, hasMoreResults: {HasMore}",
                userEntitlements.Items.Count, organization, !string.IsNullOrEmpty(userEntitlements.ContinuationToken));

            return JsonSerializer.Serialize(new
            {
                organization,
                pageSize = userEntitlements.Items.Count,
                totalCount = userEntitlements.TotalCount,
                entitlements = entitlementsData,
                continuationToken = userEntitlements.ContinuationToken,
                hasMoreResults = !string.IsNullOrEmpty(userEntitlements.ContinuationToken)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing user entitlements for organization: {Organization}", organization);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create a user entitlement in an Azure DevOps organization.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization name</param>
    /// <param name="principalName">User principal name (email address)</param>
    /// <param name="accountLicenseType">Account license type (e.g., express, stakeholder, advanced)</param>
    /// <param name="projectEntitlementsJson">Optional JSON array of project entitlements with projectId and groupType</param>
    /// <returns>Result of the user entitlement creation operation</returns>
    [KernelFunction("create_entitlement")]
    [Description("Create a user entitlement in an Azure DevOps organization. Assigns a license and optionally grants access to specific projects with roles. Project IDs can be obtained using list_projects. Valid groupType values: projectReader, projectContributor, projectAdministrator.")]
    public async Task<string> CreateEntitlementAsync(
        [Description("The Azure DevOps organization name")] string organization,
        [Description("User principal name (email address)")] string principalName,
        [Description("Account license type: express, stakeholder, advanced, professional")] string accountLicenseType,
        [Description("Optional JSON array of project entitlements. Format: [{\"projectId\":\"guid\",\"groupType\":\"projectContributor\"}]. Use list_projects to get project IDs. Valid groupType: projectReader, projectContributor, projectAdministrator")] string? projectEntitlementsJson = null)
    {
        try
        {
            _logger.LogInformation("Creating user entitlement for {PrincipalName} in organization: {Organization}",
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
                    var projectEntitlements = JsonSerializer.Deserialize<List<ProjectEntitlementInput>>(projectEntitlementsJson);
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

                        _logger.LogInformation("Creating user with {Count} project entitlements", projectEntitlements.Count);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse project entitlements JSON");
                    return JsonSerializer.Serialize(new { error = $"Invalid project entitlements JSON format. Expected format: [{{\"projectId\":\"guid\",\"groupType\":\"projectContributor\"}}]. Error: {ex.Message}" });
                }
            }
            else
            {
                _logger.LogInformation("No project entitlements specified. User will have organization-level access only.");
            }

            // Use vsaex API endpoint for user entitlements with API version 5.1-preview.2
            var result = await _azureDevOpsApiService.PostAsync<object>(
                organization,
                $"{VsaexBaseUrl}/{organization}/_apis/userentitlements",
                request,
                "5.1-preview.2");

            if (result == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to create user entitlement. Please check the logs for more details." });
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

            _logger.LogInformation("Successfully created user entitlement for {PrincipalName}", principalName);
            return JsonSerializer.Serialize(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user entitlement for {PrincipalName} in organization: {Organization}",
                principalName, organization);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing user entitlement in an Azure DevOps organization.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization name</param>
    /// <param name="userId">The user ID (GUID) of the entitlement to update</param>
    /// <param name="accountLicenseType">New account license type (optional)</param>
    /// <param name="projectEntitlementsJson">Optional JSON array of project entitlements to replace existing ones</param>
    /// <returns>Result of the user entitlement update operation</returns>
    [KernelFunction("update_entitlement")]
    [Description("Update an existing user entitlement in an Azure DevOps organization. Can update license type and/or project entitlements. User ID can be obtained from list_entitlements. Valid license types: express, stakeholder, advanced, professional. Valid groupType values: projectReader, projectContributor, projectAdministrator.")]
    public async Task<string> UpdateEntitlementAsync(
        [Description("The Azure DevOps organization name")] string organization,
        [Description("The user ID (GUID) from list_entitlements")] string userId,
        [Description("Optional new account license type: express, stakeholder, advanced, professional")] string? accountLicenseType = null,
        [Description("Optional JSON array of project entitlements to replace existing ones. Format: [{\"projectId\":\"guid\",\"groupType\":\"projectContributor\"}]")] string? projectEntitlementsJson = null)
    {
        try
        {
            _logger.LogInformation("Updating user entitlement {UserId} in organization: {Organization}", userId, organization);

            // Validate inputs
            if (string.IsNullOrWhiteSpace(userId))
            {
                return JsonSerializer.Serialize(new { error = "User ID is required. Use list_entitlements to find the user ID." });
            }

            // Build patch operations array
            var patchOperations = new List<object>();

            // Add license type update if provided
            if (!string.IsNullOrWhiteSpace(accountLicenseType))
            {
                var normalizedLicenseType = accountLicenseType.ToLower();
                var validLicenseTypes = new[] { "express", "stakeholder", "advanced", "professional", "earlyadopter" };
                
                if (!validLicenseTypes.Contains(normalizedLicenseType, StringComparer.OrdinalIgnoreCase))
                {
                    return JsonSerializer.Serialize(new { error = $"Invalid license type '{accountLicenseType}'. Valid values: express, stakeholder, advanced, professional, earlyAdopter." });
                }

                patchOperations.Add(new
                {
                    op = "replace",
                    path = "/accessLevel",
                    value = new
                    {
                        accountLicenseType = normalizedLicenseType,
                        licensingSource = "account"
                    }
                });
            }

            // Add project entitlements update if provided
            List<object>? parsedProjectEntitlements = null;
            if (!string.IsNullOrWhiteSpace(projectEntitlementsJson))
            {
                try
                {
                    var projectEntitlements = JsonSerializer.Deserialize<List<ProjectEntitlementInput>>(projectEntitlementsJson);
                    if (projectEntitlements != null && projectEntitlements.Any())
                    {
                        var projectEntitlementsValue = projectEntitlements.Select(pe => new
                        {
                            projectRef = new { id = pe.ProjectId },
                            group = new { groupType = pe.GroupType }
                        }).ToList();

                        patchOperations.Add(new
                        {
                            op = "replace",
                            path = "/projectEntitlements",
                            value = projectEntitlementsValue
                        });

                        parsedProjectEntitlements = projectEntitlements.Select(pe => new
                        {
                            projectId = pe.ProjectId,
                            groupType = pe.GroupType
                        } as object).ToList();

                        _logger.LogInformation("Updating user with {Count} project entitlements", projectEntitlements.Count);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse project entitlements JSON");
                    return JsonSerializer.Serialize(new { error = $"Invalid project entitlements JSON format. Expected format: [{{\"projectId\":\"guid\",\"groupType\":\"projectContributor\"}}]. Error: {ex.Message}" });
                }
            }

            if (patchOperations.Count == 0)
            {
                return JsonSerializer.Serialize(new { error = "At least one field (accountLicenseType or projectEntitlements) must be provided for update." });
            }

            // Use vsaex API endpoint for user entitlements with PATCH and API version 5.1-preview.2
            var result = await _azureDevOpsApiService.PatchAsync<object>(
                organization,
                $"{VsaexBaseUrl}/{organization}/_apis/userentitlements/{userId}",
                patchOperations,
                "5.1-preview.2");

            if (result == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to update user entitlement. Please check the logs for more details." });
            }

            // Return raw JSON data for the AI to format
            var response = new
            {
                success = true,
                userId,
                organization,
                updatedLicense = accountLicenseType,
                updatedProjectEntitlements = parsedProjectEntitlements,
                hasProjectAccess = parsedProjectEntitlements != null && parsedProjectEntitlements.Any()
            };

            _logger.LogInformation("Successfully updated user entitlement {UserId}", userId);
            return JsonSerializer.Serialize(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user entitlement {UserId} in organization: {Organization}", userId, organization);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete a user entitlement from an Azure DevOps organization.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization name</param>
    /// <param name="userId">The user ID (GUID) of the entitlement to delete</param>
    /// <returns>Result of the user entitlement deletion operation</returns>
    [KernelFunction("delete_entitlement")]
    [Description("Delete a user entitlement from an Azure DevOps organization. This removes the user's access to the organization. User ID can be obtained from list_entitlements.")]
    public async Task<string> DeleteEntitlementAsync(
        [Description("The Azure DevOps organization name")] string organization,
        [Description("The user ID (GUID) from list_entitlements")] string userId)
    {
        try
        {
            _logger.LogInformation("Deleting user entitlement {UserId} from organization: {Organization}", userId, organization);

            // Validate inputs
            if (string.IsNullOrWhiteSpace(userId))
            {
                return JsonSerializer.Serialize(new { error = "User ID is required. Use list_entitlements to find the user ID." });
            }

            // Use vsaex API endpoint for user entitlements with DELETE and API version 5.1-preview.2
            var success = await _azureDevOpsApiService.DeleteAsync(
                organization,
                $"{VsaexBaseUrl}/{organization}/_apis/userentitlements/{userId}",
                "5.1-preview.2");

            if (!success)
            {
                return JsonSerializer.Serialize(new { error = "Failed to delete user entitlement. The user may not exist or you may not have permission to delete it." });
            }

            _logger.LogInformation("Successfully deleted user entitlement {UserId}", userId);
            return JsonSerializer.Serialize(new
            {
                success = true,
                userId,
                organization,
                message = "User entitlement deleted successfully."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user entitlement {UserId} from organization: {Organization}", userId, organization);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
