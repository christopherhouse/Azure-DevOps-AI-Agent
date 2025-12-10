using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Services;

namespace AzureDevOpsAI.Backend.Plugins;

/// <summary>
/// Semantic Kernel plugin for Azure DevOps project operations.
/// </summary>
public class ProjectPlugin
{
    private readonly IAzureDevOpsApiService _azureDevOpsApiService;
    private readonly ILogger<ProjectPlugin> _logger;

    public ProjectPlugin(IAzureDevOpsApiService azureDevOpsApiService, ILogger<ProjectPlugin> logger)
    {
        _azureDevOpsApiService = azureDevOpsApiService;
        _logger = logger;
    }

    /// <summary>
    /// Get available process templates for an Azure DevOps organization.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization name</param>
    /// <returns>List of available process templates</returns>
    [KernelFunction("get_process_templates")]
    [Description("Get the available process templates for an Azure DevOps organization. This is used to find the correct template ID when creating a new project.")]
    public async Task<string> GetProcessTemplatesAsync(
        [Description("The Azure DevOps organization name")] string organization)
    {
        try
        {
            _logger.LogInformation("Getting process templates for organization: {Organization}", organization);

            // Use the Azure DevOps API service with automatic OBO token acquisition
            var processTemplates = await _azureDevOpsApiService.GetAsync<ProcessTemplateList>(
                organization, "work/processes", "7.1");

            if (processTemplates?.Value == null || !processTemplates.Value.Any())
            {
                return JsonSerializer.Serialize(new { message = "No process templates found for this organization.", templates = new List<object>() });
            }

            // Return raw JSON data for the AI to format
            var templatesData = processTemplates.Value.Where(t => t.IsEnabled).Select(t => new
            {
                name = t.Name,
                id = t.TypeId,
                description = t.Description,
                customizationType = t.CustomizationType,
                isDefault = t.IsDefault
            }).ToList();

            _logger.LogInformation("Successfully retrieved {Count} process templates", processTemplates.Value.Count);
            return JsonSerializer.Serialize(new { organization, templates = templatesData });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting process templates for organization: {Organization}", organization);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new project in an Azure DevOps organization.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization name</param>
    /// <param name="projectName">The name of the project to create</param>
    /// <param name="description">Description of the project (optional)</param>
    /// <param name="processTemplateId">The process template type ID to use</param>
    /// <param name="visibility">Project visibility (Private or Public), defaults to Private</param>
    /// <returns>Result of the project creation operation</returns>
    [KernelFunction("create_project")]
    [Description("Create a new project in an Azure DevOps organization. Use get_process_templates first to find the correct process template ID.")]
    public async Task<string> CreateProjectAsync(
        [Description("The Azure DevOps organization name")] string organization,
        [Description("The name of the project to create")] string projectName,
        [Description("Description of the project (optional)")] string? description,
        [Description("The process template type ID (use get_process_templates to find available IDs)")] string processTemplateId,
        [Description("Project visibility: Private or Public (defaults to Private)")] string visibility = "Private")
    {
        try
        {
            _logger.LogInformation("Creating project '{ProjectName}' in organization: {Organization}", projectName, organization);

            // Validate inputs
            if (string.IsNullOrWhiteSpace(projectName))
            {
                return JsonSerializer.Serialize(new { error = "Project name is required." });
            }

            if (string.IsNullOrWhiteSpace(processTemplateId))
            {
                return JsonSerializer.Serialize(new { error = "Process template ID is required. Use get_process_templates to find available template IDs." });
            }

            // Parse visibility
            if (!Enum.TryParse<ProjectVisibility>(visibility, true, out var projectVisibility))
            {
                return JsonSerializer.Serialize(new { error = "Invalid visibility value. Use 'Private' or 'Public'." });
            }

            // Create the project request payload
            var projectRequest = new
            {
                name = projectName,
                description = description ?? string.Empty,
                visibility = projectVisibility == ProjectVisibility.Public ? "public" : "private",
                capabilities = new
                {
                    versioncontrol = new
                    {
                        sourceControlType = "Git"
                    },
                    processTemplate = new
                    {
                        templateTypeId = processTemplateId
                    }
                }
            };

            // Use the Azure DevOps API service to create the project
            var createdProject = await _azureDevOpsApiService.PostAsync<object>(
                organization, "projects", projectRequest, "7.1");

            if (createdProject == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to create project. Please check the logs for more details." });
            }

            // Parse the response to get project details
            var projectElement = JsonSerializer.SerializeToElement(createdProject);

            string? projectId = null;
            string? projectUrl = null;
            string? status = null;

            if (projectElement.TryGetProperty("id", out var idElement))
            {
                projectId = idElement.GetString();
            }

            if (projectElement.TryGetProperty("url", out var urlElement))
            {
                projectUrl = urlElement.GetString();
            }

            if (projectElement.TryGetProperty("status", out var statusElement))
            {
                status = statusElement.GetString();
            }

            // Return raw JSON data for the AI to format
            var result = new
            {
                success = true,
                projectName,
                projectId,
                organization,
                visibility,
                sourceControl = "Git",
                processTemplateId,
                description,
                projectUrl,
                status
            };

            _logger.LogInformation("Successfully created project '{ProjectName}' with ID: {ProjectId}", projectName, projectId);
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project '{ProjectName}' in organization: {Organization}", projectName, organization);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Find a process template by name or partial name match.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization name</param>
    /// <param name="templateName">The name or partial name of the process template to find</param>
    /// <returns>The process template ID if found, or an error message</returns>
    [KernelFunction("find_process_template")]
    [Description("Find a process template by name (e.g., 'Agile', 'Scrum', 'CMMI'). Returns the template ID needed for project creation.")]
    public async Task<string> FindProcessTemplateAsync(
        [Description("The Azure DevOps organization name")] string organization,
        [Description("The name or partial name of the process template to find")] string templateName)
    {
        try
        {
            _logger.LogInformation("Finding process template '{TemplateName}' in organization: {Organization}", templateName, organization);

            // Use the API service to get process templates
            var processTemplates = await _azureDevOpsApiService.GetAsync<ProcessTemplateList>(
                organization, "work/processes", "7.1");

            if (processTemplates?.Value == null || !processTemplates.Value.Any())
            {
                return JsonSerializer.Serialize(new { error = "No process templates found for this organization." });
            }

            // Find exact match first
            var exactMatch = processTemplates.Value.FirstOrDefault(t =>
                t.IsEnabled && string.Equals(t.Name, templateName, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
            {
                return JsonSerializer.Serialize(new
                {
                    matchType = "exact",
                    template = new
                    {
                        name = exactMatch.Name,
                        id = exactMatch.TypeId,
                        description = exactMatch.Description,
                        isDefault = exactMatch.IsDefault
                    }
                });
            }

            // Find partial matches
            var partialMatches = processTemplates.Value.Where(t =>
                t.IsEnabled && t.Name.Contains(templateName, StringComparison.OrdinalIgnoreCase)).ToList();

            if (partialMatches.Count == 1)
            {
                var match = partialMatches.First();
                return JsonSerializer.Serialize(new
                {
                    matchType = "partial",
                    template = new
                    {
                        name = match.Name,
                        id = match.TypeId,
                        description = match.Description,
                        isDefault = match.IsDefault
                    }
                });
            }

            if (partialMatches.Count > 1)
            {
                return JsonSerializer.Serialize(new
                {
                    matchType = "multiple",
                    searchTerm = templateName,
                    matches = partialMatches.Select(m => new
                    {
                        name = m.Name,
                        id = m.TypeId,
                        description = m.Description,
                        isDefault = m.IsDefault
                    }).ToList()
                });
            }

            // No matches found
            var availableTemplates = processTemplates.Value.Where(t => t.IsEnabled).Select(t => t.Name).ToList();
            return JsonSerializer.Serialize(new
            {
                matchType = "none",
                searchTerm = templateName,
                availableTemplates
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding process template '{TemplateName}' in organization: {Organization}", templateName, organization);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// List all projects in an Azure DevOps organization.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization name</param>
    /// <returns>List of projects in the organization</returns>
    [KernelFunction("list_projects")]
    [Description("List all projects in an Azure DevOps organization. Returns project names, IDs, descriptions, and other details.")]
    public async Task<string> ListProjectsAsync(
        [Description("The Azure DevOps organization name")] string organization)
    {
        try
        {
            _logger.LogInformation("Listing projects for organization: {Organization}", organization);

            // Use the Azure DevOps API service to get projects
            var projects = await _azureDevOpsApiService.GetAsync<ProjectListResponse>(
                organization, "projects", "7.1");

            if (projects?.Value == null || !projects.Value.Any())
            {
                return JsonSerializer.Serialize(new { message = "No projects found in this organization.", projects = new List<object>() });
            }

            // Return raw JSON data for the AI to format
            var projectsData = projects.Value.Select(p => new
            {
                name = p.Name,
                id = p.Id,
                description = p.Description,
                state = p.State.ToString(),
                visibility = p.Visibility.ToString(),
                lastUpdateTime = p.LastUpdateTime?.ToString("yyyy-MM-dd HH:mm:ss")
            }).ToList();

            _logger.LogInformation("Successfully retrieved {Count} projects for organization: {Organization}", projects.Value.Count, organization);
            return JsonSerializer.Serialize(new
            {
                organization,
                totalProjects = projects.Value.Count,
                projects = projectsData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing projects for organization: {Organization}", organization);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}