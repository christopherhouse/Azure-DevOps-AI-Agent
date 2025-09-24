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
                return "No process templates found for this organization.";
            }

            // Format the response for the AI
            var result = "Available process templates:\n\n";
            foreach (var template in processTemplates.Value.Where(t => t.IsEnabled))
            {
                result += $"• **{template.Name}** (ID: {template.TypeId})\n";
                result += $"  Description: {template.Description}\n";
                result += $"  Type: {template.CustomizationType}";
                if (template.IsDefault)
                {
                    result += " (Default)";
                }
                result += "\n\n";
            }

            _logger.LogInformation("Successfully retrieved {Count} process templates", processTemplates.Value.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting process templates for organization: {Organization}", organization);
            return $"Error: {ex.Message}";
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
                return "Error: Project name is required.";
            }

            if (string.IsNullOrWhiteSpace(processTemplateId))
            {
                return "Error: Process template ID is required. Use get_process_templates to find available template IDs.";
            }

            // Parse visibility
            if (!Enum.TryParse<ProjectVisibility>(visibility, true, out var projectVisibility))
            {
                return "Error: Invalid visibility value. Use 'Private' or 'Public'.";
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
                return "Error: Failed to create project. Please check the logs for more details.";
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

            var result = $"✅ Project '{projectName}' created successfully!\n\n";
            result += $"• **Project ID**: {projectId}\n";
            result += $"• **Organization**: {organization}\n";
            result += $"• **Visibility**: {visibility}\n";
            result += $"• **Source Control**: Git\n";
            result += $"• **Process Template ID**: {processTemplateId}\n";
            
            if (!string.IsNullOrEmpty(description))
            {
                result += $"• **Description**: {description}\n";
            }

            if (!string.IsNullOrEmpty(projectUrl))
            {
                result += $"• **Project URL**: {projectUrl}\n";
            }

            if (!string.IsNullOrEmpty(status))
            {
                result += $"• **Status**: {status}\n";
            }

            result += "\nThe project is now ready for use in Azure DevOps!";

            _logger.LogInformation("Successfully created project '{ProjectName}' with ID: {ProjectId}", projectName, projectId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project '{ProjectName}' in organization: {Organization}", projectName, organization);
            return $"Error: {ex.Message}";
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
                return "Error: No process templates found for this organization.";
            }

            // Find exact match first
            var exactMatch = processTemplates.Value.FirstOrDefault(t => 
                t.IsEnabled && string.Equals(t.Name, templateName, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
            {
                return $"Found exact match: '{exactMatch.Name}' (ID: {exactMatch.TypeId})";
            }

            // Find partial matches
            var partialMatches = processTemplates.Value.Where(t => 
                t.IsEnabled && t.Name.Contains(templateName, StringComparison.OrdinalIgnoreCase)).ToList();

            if (partialMatches.Count == 1)
            {
                var match = partialMatches.First();
                return $"Found match: '{match.Name}' (ID: {match.TypeId})";
            }

            if (partialMatches.Count > 1)
            {
                var result = $"Found multiple matches for '{templateName}':\n\n";
                foreach (var match in partialMatches)
                {
                    result += $"• **{match.Name}** (ID: {match.TypeId})\n";
                }
                result += "\nPlease be more specific or use the exact template name.";
                return result;
            }

            // No matches found
            var availableTemplates = string.Join(", ", processTemplates.Value.Where(t => t.IsEnabled).Select(t => t.Name));
            return $"No process template found matching '{templateName}'. Available templates: {availableTemplates}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding process template '{TemplateName}' in organization: {Organization}", templateName, organization);
            return $"Error: {ex.Message}";
        }
    }
}