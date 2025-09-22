using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Configuration;
using AzureDevOpsAI.Backend.Services;
using Azure.Identity;
using Azure.Core;
using Microsoft.Extensions.Options;

namespace AzureDevOpsAI.Backend.Plugins;

/// <summary>
/// Semantic Kernel plugin for Azure DevOps project operations.
/// </summary>
public class ProjectPlugin
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProjectPlugin> _logger;
    private readonly DefaultAzureCredential _azureCredential;
    private readonly IUserAuthenticationContext _userAuthContext;
    private const string AzureDevOpsScope = "499b84ac-1321-427f-aa17-267ca6975798/.default"; // Azure DevOps service scope

    public ProjectPlugin(HttpClient httpClient, ILogger<ProjectPlugin> logger, 
        IOptions<AzureOpenAISettings> azureOpenAISettings, IUserAuthenticationContext userAuthContext)
    {
        _httpClient = httpClient;
        _logger = logger;
        _userAuthContext = userAuthContext;
        
        // Configure the same managed identity credential as used in AIService (fallback)
        var credentialOptions = new DefaultAzureCredentialOptions();
        credentialOptions.ManagedIdentityClientId = azureOpenAISettings.Value.ClientId;
        _azureCredential = new DefaultAzureCredential(credentialOptions);
        
        _logger.LogInformation("ProjectPlugin configured with User Assigned Managed Identity client ID: {ClientId}", azureOpenAISettings.Value.ClientId);
    }

    /// <summary>
    /// Get Azure DevOps access token using OBO flow with user's token, or managed identity as fallback.
    /// </summary>
    private async Task<string> GetAzureDevOpsAccessTokenAsync()
    {
        try
        {
            // Try to get user's token credential for OBO flow first
            var userTokenCredential = _userAuthContext.GetUserTokenCredential();
            var userId = _userAuthContext.GetCurrentUserId();
            
            if (userTokenCredential != null)
            {
                _logger.LogInformation("Using OBO flow to acquire Azure DevOps token for user: {UserId}", userId ?? "unknown");
                
                var tokenRequestContext = new TokenRequestContext(new[] { AzureDevOpsScope });
                var accessToken = await userTokenCredential.GetTokenAsync(tokenRequestContext, CancellationToken.None);
                
                _logger.LogDebug("Successfully acquired Azure DevOps token via OBO flow for user: {UserId}", userId ?? "unknown");
                return accessToken.Token;
            }
            else
            {
                _logger.LogWarning("No user authentication context available, falling back to managed identity");
                
                // Fallback to managed identity (original behavior)
                var tokenRequestContext = new TokenRequestContext(new[] { AzureDevOpsScope });
                var accessToken = await _azureCredential.GetTokenAsync(tokenRequestContext, CancellationToken.None);
                
                _logger.LogDebug("Successfully acquired Azure DevOps token via managed identity (fallback)");
                return accessToken.Token;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acquire Azure DevOps access token");
            throw;
        }
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

            // Get access token using managed identity
            var accessToken = await GetAzureDevOpsAccessTokenAsync();

            // Set up authentication header with Bearer token
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            // Make API call to get process templates
            var url = $"https://dev.azure.com/{organization}/_apis/work/processes?api-version=7.1";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get process templates. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
                return $"Error: Failed to get process templates. Status: {response.StatusCode}. {errorContent}";
            }

            var content = await response.Content.ReadAsStringAsync();
            var processTemplates = JsonSerializer.Deserialize<ProcessTemplateList>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

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

            // Get access token using managed identity
            var accessToken = await GetAzureDevOpsAccessTokenAsync();

            // Set up authentication header with Bearer token
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

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

            var jsonContent = JsonSerializer.Serialize(projectRequest, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            // Make API call to create project
            var url = $"https://dev.azure.com/{organization}/_apis/projects?api-version=7.1";
            var response = await _httpClient.PostAsync(url, content);

            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to create project. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, responseContent);
                return $"Error: Failed to create project. Status: {response.StatusCode}. {responseContent}";
            }

            // Parse the response to get project details
            using var document = JsonDocument.Parse(responseContent);
            var root = document.RootElement;

            string? projectId = null;
            string? projectUrl = null;
            string? status = null;

            if (root.TryGetProperty("id", out var idElement))
            {
                projectId = idElement.GetString();
            }

            if (root.TryGetProperty("url", out var urlElement))
            {
                projectUrl = urlElement.GetString();
            }

            if (root.TryGetProperty("status", out var statusElement))
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

            // Get all process templates first
            var templatesResult = await GetProcessTemplatesAsync(organization);
            
            if (templatesResult.StartsWith("Error:"))
            {
                return templatesResult;
            }

            // Get access token using managed identity for direct API call
            var accessToken = await GetAzureDevOpsAccessTokenAsync();

            // Set up authentication header with Bearer token
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var url = $"https://dev.azure.com/{organization}/_apis/work/processes?api-version=7.1";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return $"Error: Failed to get process templates. Status: {response.StatusCode}";
            }

            var content = await response.Content.ReadAsStringAsync();
            var processTemplates = JsonSerializer.Deserialize<ProcessTemplateList>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

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