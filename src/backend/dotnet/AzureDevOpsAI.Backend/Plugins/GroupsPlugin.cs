using System.ComponentModel;
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
    /// <returns>List of groups with essential attributes only</returns>
    [KernelFunction("list_groups")]
    [Description("List all groups in an Azure DevOps organization. Optionally filter by project using a project ID from list_projects. If no project ID is provided, returns all organization-level groups.")]
    public async Task<SlimGroupListResponse> ListGroupsAsync(
        [Description("The Azure DevOps organization name")] string organization,
        [Description("Optional project ID to filter groups by project (use list_projects to obtain)")] string? projectId = null)
    {
        _logger.LogInformation("Listing groups for organization: {Organization}, projectId: {ProjectId}", organization, projectId ?? "all");

        // Get scope descriptor if project ID is provided
        string? scopeDescriptor = null;
        if (!string.IsNullOrWhiteSpace(projectId))
        {
            scopeDescriptor = await _descriptorService.GetScopeDescriptorAsync(organization, projectId);
            if (scopeDescriptor == null)
            {
                throw new InvalidOperationException($"Failed to retrieve scope descriptor for project ID: {projectId}");
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

        if (groups == null)
        {
            throw new InvalidOperationException("Failed to retrieve groups from Azure DevOps API");
        }

        // Transform to slim groups with only essential attributes
        var slimGroups = new SlimGroupListResponse
        {
            Value = groups.Value.Select(g => new SlimGroup
            {
                Description = g.Description,
                PrincipalName = g.PrincipalName,
                Origin = g.Origin,
                DisplayName = g.DisplayName,
                Descriptor = g.Descriptor
            }).ToList(),
            Count = groups.Count,
            ContinuationToken = groups.ContinuationToken
        };

        _logger.LogInformation("Successfully retrieved {Count} groups for organization: {Organization}", slimGroups.Value?.Count ?? 0, organization);
        return slimGroups;
    }

    /// <summary>
    /// List members of a specific Azure DevOps group.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization name</param>
    /// <param name="groupDescriptor">The group descriptor (obtained from list_groups)</param>
    /// <returns>List of group members</returns>
    [KernelFunction("list_group_members")]
    [Description("List all members of a specific Azure DevOps group. Use the group descriptor from list_groups.")]
    public async Task<GraphMemberListResponse> ListGroupMembersAsync(
        [Description("The Azure DevOps organization name")] string organization,
        [Description("The group descriptor from list_groups")] string groupDescriptor)
    {
        _logger.LogInformation("Listing members for group: {GroupDescriptor} in organization: {Organization}", groupDescriptor, organization);

        if (string.IsNullOrWhiteSpace(groupDescriptor))
        {
            throw new ArgumentException("Group descriptor is required. Use list_groups to get group descriptors.", nameof(groupDescriptor));
        }

        // Use the Graph API to get group members
        var members = await _azureDevOpsApiService.GetAsync<GraphMemberListResponse>(
            organization, $"https://vssps.dev.azure.com/{organization}/_apis/graph/groups/{groupDescriptor}/members", "7.1-preview.1");

        if (members == null)
        {
            throw new InvalidOperationException("Failed to retrieve group members from Azure DevOps API");
        }

        _logger.LogInformation("Successfully retrieved {Count} members for group: {GroupDescriptor}", members.Value?.Count ?? 0, groupDescriptor);
        return members;
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
    public async Task<GraphGroup> CreateGroupAsync(
        [Description("The Azure DevOps organization name")] string organization,
        [Description("The display name for the new group")] string displayName,
        [Description("Optional description for the group")] string? description = null,
        [Description("Optional project ID to create group in a specific project (use list_projects to obtain)")] string? projectId = null)
    {
        _logger.LogInformation("Creating group '{DisplayName}' in organization: {Organization}, projectId: {ProjectId}",
            displayName, organization, projectId ?? "organization");

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Group display name is required.", nameof(displayName));
        }

        // Get scope descriptor if project ID is provided
        string? scopeDescriptor = null;
        if (!string.IsNullOrWhiteSpace(projectId))
        {
            scopeDescriptor = await _descriptorService.GetScopeDescriptorAsync(organization, projectId);
            if (scopeDescriptor == null)
            {
                throw new InvalidOperationException($"Failed to retrieve scope descriptor for project ID: {projectId}");
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
            throw new InvalidOperationException("Failed to create group in Azure DevOps");
        }

        _logger.LogInformation("Successfully created group '{DisplayName}' with descriptor: {Descriptor}", displayName, createdGroup.Descriptor);
        return createdGroup;
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
    public async Task<GraphMembershipState> AddGroupMemberAsync(
        [Description("The Azure DevOps organization name")] string organization,
        [Description("The group descriptor from list_groups")] string groupDescriptor,
        [Description("The member descriptor (user or group) from list_users or list_groups")] string memberDescriptor)
    {
        _logger.LogInformation("Adding member {MemberDescriptor} to group {GroupDescriptor} in organization: {Organization}",
            memberDescriptor, groupDescriptor, organization);

        if (string.IsNullOrWhiteSpace(groupDescriptor))
        {
            throw new ArgumentException("Group descriptor is required. Use list_groups to get group descriptors.", nameof(groupDescriptor));
        }

        if (string.IsNullOrWhiteSpace(memberDescriptor))
        {
            throw new ArgumentException("Member descriptor is required. Use list_users to get user descriptors or list_groups for group descriptors.", nameof(memberDescriptor));
        }

        // Use the Graph API memberships endpoint to add the member
        var apiPath = $"https://vssps.dev.azure.com/{organization}/_apis/graph/memberships/{memberDescriptor}/{groupDescriptor}";

        var membership = await _azureDevOpsApiService.PutAsync<GraphMembershipState>(
            organization, apiPath, null, "7.1-preview.1");

        if (membership == null)
        {
            throw new InvalidOperationException("Failed to add member to group. The member may already be in the group or descriptors may be invalid.");
        }

        _logger.LogInformation("Successfully added member {MemberDescriptor} to group {GroupDescriptor}", memberDescriptor, groupDescriptor);
        return membership;
    }
}
