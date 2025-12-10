using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Services;

namespace AzureDevOpsAI.Backend.Plugins;

/// <summary>
/// Semantic Kernel plugin for Azure DevOps group operations.
/// </summary>
public class GroupsPlugin
{
    private readonly IAzureDevOpsApiService _azureDevOpsApiService;
    private readonly IAzureDevOpsDescriptorService _descriptorService;
    private readonly ILogger<GroupsPlugin> _logger;

    public GroupsPlugin(
        IAzureDevOpsApiService azureDevOpsApiService,
        IAzureDevOpsDescriptorService descriptorService,
        ILogger<GroupsPlugin> logger)
    {
        _azureDevOpsApiService = azureDevOpsApiService;
        _descriptorService = descriptorService;
        _logger = logger;
    }

    /// <summary>
    /// List all groups in an Azure DevOps organization or project.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization name</param>
    /// <param name="projectId">Optional project ID to filter groups by project (use list_projects to obtain project IDs)</param>
    /// <returns>List of groups</returns>
    [KernelFunction("list_groups")]
    [Description("List all groups in an Azure DevOps organization. Optionally filter by project using a project ID from list_projects. If no project ID is provided, returns all organization-level groups.")]
    public async Task<string> ListGroupsAsync(
        [Description("The Azure DevOps organization name")] string organization,
        [Description("Optional project ID to filter groups by project (use list_projects to obtain)")] string? projectId = null)
    {
        try
        {
            _logger.LogInformation("Listing groups for organization: {Organization}, projectId: {ProjectId}", organization, projectId ?? "all");

            // Get scope descriptor if project ID is provided
            string? scopeDescriptor = null;
            if (!string.IsNullOrWhiteSpace(projectId))
            {
                scopeDescriptor = await _descriptorService.GetScopeDescriptorAsync(organization, projectId);
                if (scopeDescriptor == null)
                {
                    return JsonSerializer.Serialize(new { error = "Failed to retrieve scope descriptor for the provided project ID. Please verify the project ID is correct." });
                }
            }

            // Build the API path with optional scope descriptor
            var apiPath = $"https://vssps.dev.azure.com/{organization}/_apis/graph/groups";
            if (!string.IsNullOrWhiteSpace(scopeDescriptor))
            {
                apiPath += $"?scopeDescriptor={scopeDescriptor}";
            }

            // Use the Graph API to get groups
            var groups = await _azureDevOpsApiService.GetAsync<GraphGroupListResponse>(
                organization, apiPath, "7.1-preview.1");

            if (groups?.Value == null || !groups.Value.Any())
            {
                return JsonSerializer.Serialize(new { message = "No groups found in this organization.", groups = new List<object>() });
            }

            // Return raw JSON data for the AI to format
            var groupsData = groups.Value.Select(g => new
            {
                descriptor = g.Descriptor,
                displayName = g.DisplayName,
                description = g.Description,
                principalName = g.PrincipalName,
                mailAddress = g.MailAddress,
                origin = g.Origin,
                subjectKind = g.SubjectKind,
                domain = g.Domain,
                isCrossProject = g.IsCrossProject,
                isDeleted = g.IsDeleted
            }).ToList();

            _logger.LogInformation("Successfully retrieved {Count} groups for organization: {Organization}", groups.Value.Count, organization);
            return JsonSerializer.Serialize(new
            {
                organization,
                projectId = projectId ?? "all",
                totalGroups = groups.Value.Count,
                groups = groupsData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing groups for organization: {Organization}", organization);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// List members of a specific Azure DevOps group.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization name</param>
    /// <param name="groupDescriptor">The group descriptor (obtained from list_groups)</param>
    /// <returns>List of group members</returns>
    [KernelFunction("list_group_members")]
    [Description("List all members of a specific Azure DevOps group. Use the group descriptor from list_groups.")]
    public async Task<string> ListGroupMembersAsync(
        [Description("The Azure DevOps organization name")] string organization,
        [Description("The group descriptor from list_groups")] string groupDescriptor)
    {
        try
        {
            _logger.LogInformation("Listing members for group: {GroupDescriptor} in organization: {Organization}", groupDescriptor, organization);

            if (string.IsNullOrWhiteSpace(groupDescriptor))
            {
                return JsonSerializer.Serialize(new { error = "Group descriptor is required. Use list_groups to get group descriptors." });
            }

            // Use the Graph API to get group members
            var members = await _azureDevOpsApiService.GetAsync<GraphMemberListResponse>(
                organization, $"https://vssps.dev.azure.com/{organization}/_apis/graph/groups/{groupDescriptor}/members", "7.1-preview.1");

            if (members?.Value == null || !members.Value.Any())
            {
                return JsonSerializer.Serialize(new { message = "No members found in this group.", members = new List<object>() });
            }

            // Return raw JSON data for the AI to format
            var membersData = members.Value.Select(m => new
            {
                descriptor = m.Descriptor,
                displayName = m.DisplayName,
                principalName = m.PrincipalName,
                mailAddress = m.MailAddress,
                origin = m.Origin,
                subjectKind = m.SubjectKind,
                domain = m.Domain,
                directoryAlias = m.DirectoryAlias,
                metaType = m.MetaType
            }).ToList();

            _logger.LogInformation("Successfully retrieved {Count} members for group: {GroupDescriptor}", members.Value.Count, groupDescriptor);
            return JsonSerializer.Serialize(new
            {
                organization,
                groupDescriptor,
                totalMembers = members.Value.Count,
                members = membersData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing members for group: {GroupDescriptor} in organization: {Organization}", groupDescriptor, organization);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new group in Azure DevOps.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization name</param>
    /// <param name="displayName">The display name for the new group</param>
    /// <param name="description">Optional description for the group</param>
    /// <param name="projectId">Optional project ID to create the group in a specific project (use list_projects to obtain project IDs)</param>
    /// <returns>The created group details</returns>
    [KernelFunction("create_group")]
    [Description("Create a new group in Azure DevOps. Optionally create it within a specific project using a project ID from list_projects. If no project ID is provided, creates an organization-level group.")]
    public async Task<string> CreateGroupAsync(
        [Description("The Azure DevOps organization name")] string organization,
        [Description("The display name for the new group")] string displayName,
        [Description("Optional description for the group")] string? description = null,
        [Description("Optional project ID to create group in a specific project (use list_projects to obtain)")] string? projectId = null)
    {
        try
        {
            _logger.LogInformation("Creating group '{DisplayName}' in organization: {Organization}, projectId: {ProjectId}",
                displayName, organization, projectId ?? "organization");

            if (string.IsNullOrWhiteSpace(displayName))
            {
                return JsonSerializer.Serialize(new { error = "Group display name is required." });
            }

            // Get scope descriptor if project ID is provided
            string? scopeDescriptor = null;
            if (!string.IsNullOrWhiteSpace(projectId))
            {
                scopeDescriptor = await _descriptorService.GetScopeDescriptorAsync(organization, projectId);
                if (scopeDescriptor == null)
                {
                    return JsonSerializer.Serialize(new { error = "Failed to retrieve scope descriptor for the provided project ID. Please verify the project ID is correct." });
                }
            }

            // Create the group request payload
            var groupRequest = new GraphGroupCreationContext
            {
                DisplayName = displayName,
                Description = description
            };

            // Build the API path with optional scope descriptor
            var apiPath = $"https://vssps.dev.azure.com/{organization}/_apis/graph/groups";
            if (!string.IsNullOrWhiteSpace(scopeDescriptor))
            {
                apiPath += $"?scopeDescriptor={scopeDescriptor}";
            }

            // Use the Graph API to create the group
            var createdGroup = await _azureDevOpsApiService.PostAsync<GraphGroup>(
                organization, apiPath, groupRequest, "7.1-preview.1");

            if (createdGroup == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to create group. Please check the logs for more details." });
            }

            // Return raw JSON data for the AI to format
            var result = new
            {
                success = true,
                displayName,
                descriptor = createdGroup.Descriptor,
                organization,
                projectId = projectId ?? "organization",
                description,
                principalName = createdGroup.PrincipalName,
                mailAddress = createdGroup.MailAddress,
                url = createdGroup.Url
            };

            _logger.LogInformation("Successfully created group '{DisplayName}' with descriptor: {Descriptor}", displayName, createdGroup.Descriptor);
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group '{DisplayName}' in organization: {Organization}", displayName, organization);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Add a member to an Azure DevOps group.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization name</param>
    /// <param name="groupDescriptor">The group descriptor (obtained from list_groups)</param>
    /// <param name="memberDescriptor">The member descriptor (user or group descriptor from list_users or list_groups)</param>
    /// <returns>Result of adding the member to the group</returns>
    [KernelFunction("add_group_member")]
    [Description("Add a member (user or group) to an Azure DevOps group. Use group descriptor from list_groups and member descriptor from list_users or list_groups.")]
    public async Task<string> AddGroupMemberAsync(
        [Description("The Azure DevOps organization name")] string organization,
        [Description("The group descriptor from list_groups")] string groupDescriptor,
        [Description("The member descriptor (user or group) from list_users or list_groups")] string memberDescriptor)
    {
        try
        {
            _logger.LogInformation("Adding member {MemberDescriptor} to group {GroupDescriptor} in organization: {Organization}",
                memberDescriptor, groupDescriptor, organization);

            if (string.IsNullOrWhiteSpace(groupDescriptor))
            {
                return JsonSerializer.Serialize(new { error = "Group descriptor is required. Use list_groups to get group descriptors." });
            }

            if (string.IsNullOrWhiteSpace(memberDescriptor))
            {
                return JsonSerializer.Serialize(new { error = "Member descriptor is required. Use list_users to get user descriptors or list_groups for group descriptors." });
            }

            // Use the Graph API memberships endpoint to add the member
            // PUT https://vssps.dev.azure.com/{organization}/_apis/graph/memberships/{memberDescriptor}/{groupDescriptor}
            var apiPath = $"https://vssps.dev.azure.com/{organization}/_apis/graph/memberships/{memberDescriptor}/{groupDescriptor}";

            // Create an HTTP request using PostAsync (which internally handles PUT via HttpMethod)
            // For PUT requests, we need to use a workaround since PostAsync only supports POST
            // We'll use the memberships API which accepts PUT but we'll call it as POST to the members endpoint
            var membersApiPath = $"https://vssps.dev.azure.com/{organization}/_apis/graph/memberships/{memberDescriptor}/{groupDescriptor}";

            var membership = await _azureDevOpsApiService.PostAsync<GraphMembershipState>(
                organization, membersApiPath, null, "7.1-preview.1");

            if (membership == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to add member to group. The member may already be in the group or descriptors may be invalid." });
            }

            var result = new
            {
                success = true,
                organization,
                groupDescriptor,
                memberDescriptor,
                membershipActive = membership.Active
            };

            _logger.LogInformation("Successfully added member {MemberDescriptor} to group {GroupDescriptor}", memberDescriptor, groupDescriptor);
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding member {MemberDescriptor} to group {GroupDescriptor} in organization: {Organization}",
                memberDescriptor, groupDescriptor, organization);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
