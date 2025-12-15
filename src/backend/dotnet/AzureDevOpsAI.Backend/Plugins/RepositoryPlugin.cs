using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Services;

namespace AzureDevOpsAI.Backend.Plugins;

/// <summary>
/// Semantic Kernel plugin for Azure DevOps Git repository operations.
/// </summary>
public class RepositoryPlugin
{
    private readonly IAzureDevOpsApiService _azureDevOpsApiService;
    private readonly ILogger<RepositoryPlugin> _logger;

    public RepositoryPlugin(IAzureDevOpsApiService azureDevOpsApiService, ILogger<RepositoryPlugin> logger)
    {
        _azureDevOpsApiService = azureDevOpsApiService;
        _logger = logger;
    }

    /// <summary>
    /// List all Git repositories in an Azure DevOps project.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization name</param>
    /// <param name="project">The project name or ID</param>
    /// <returns>List of repositories in the project</returns>
    [KernelFunction("list_repositories")]
    [Description("List all Git repositories in an Azure DevOps project. Returns repository names, IDs, URLs, and other details.")]
    public async Task<string> ListRepositoriesAsync(
        [Description("The Azure DevOps organization name")] string organization,
        [Description("The project name or ID")] string project)
    {
        try
        {
            _logger.LogInformation("Listing repositories for project: {Project} in organization: {Organization}", project, organization);

            // Use the Azure DevOps API service to get repositories
            var repositories = await _azureDevOpsApiService.GetAsync<RepositoryListResponse>(
                organization, $"{project}/_apis/git/repositories", "7.1");

            if (repositories?.Value == null || !repositories.Value.Any())
            {
                return JsonSerializer.Serialize(new { message = "No repositories found in this project.", repositories = new List<object>() });
            }

            // Return raw JSON data for the AI to format
            var repositoriesData = repositories.Value.Select(r => new
            {
                name = r.Name,
                id = r.Id,
                url = r.Url,
                remoteUrl = r.RemoteUrl,
                sshUrl = r.SshUrl,
                webUrl = r.WebUrl,
                defaultBranch = r.DefaultBranch,
                size = r.Size,
                isDisabled = r.IsDisabled,
                isFork = r.IsFork,
                project = r.Project != null ? new
                {
                    id = r.Project.Id,
                    name = r.Project.Name,
                    description = r.Project.Description,
                    state = r.Project.State,
                    visibility = r.Project.Visibility
                } : null
            }).ToList();

            _logger.LogInformation("Successfully retrieved {Count} repositories for project: {Project}", repositories.Value.Count, project);
            return JsonSerializer.Serialize(new
            {
                organization,
                project,
                totalRepositories = repositories.Value.Count,
                repositories = repositoriesData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing repositories for project: {Project} in organization: {Organization}", project, organization);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get details of a specific Git repository.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization name</param>
    /// <param name="project">The project name or ID</param>
    /// <param name="repositoryId">The repository name or ID</param>
    /// <returns>Repository details</returns>
    [KernelFunction("get_repository")]
    [Description("Get details of a specific Git repository in an Azure DevOps project. Returns repository information including name, ID, URLs, default branch, and size.")]
    public async Task<string> GetRepositoryAsync(
        [Description("The Azure DevOps organization name")] string organization,
        [Description("The project name or ID")] string project,
        [Description("The repository name or ID")] string repositoryId)
    {
        try
        {
            _logger.LogInformation("Getting repository: {RepositoryId} in project: {Project}, organization: {Organization}", repositoryId, project, organization);

            // Use the Azure DevOps API service to get repository details
            var repository = await _azureDevOpsApiService.GetAsync<GitRepository>(
                organization, $"{project}/_apis/git/repositories/{repositoryId}", "7.1");

            if (repository == null)
            {
                return JsonSerializer.Serialize(new { error = $"Repository '{repositoryId}' not found in project '{project}'." });
            }

            // Return raw JSON data for the AI to format
            var repositoryData = new
            {
                name = repository.Name,
                id = repository.Id,
                url = repository.Url,
                remoteUrl = repository.RemoteUrl,
                sshUrl = repository.SshUrl,
                webUrl = repository.WebUrl,
                defaultBranch = repository.DefaultBranch,
                size = repository.Size,
                isDisabled = repository.IsDisabled,
                isFork = repository.IsFork,
                project = repository.Project != null ? new
                {
                    id = repository.Project.Id,
                    name = repository.Project.Name,
                    description = repository.Project.Description,
                    state = repository.Project.State,
                    visibility = repository.Project.Visibility
                } : null
            };

            _logger.LogInformation("Successfully retrieved repository: {RepositoryName} ({RepositoryId})", repository.Name, repository.Id);
            return JsonSerializer.Serialize(new
            {
                organization,
                project,
                repository = repositoryData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting repository: {RepositoryId} in project: {Project}, organization: {Organization}", repositoryId, project, organization);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Create a new Git repository in an Azure DevOps project.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization name</param>
    /// <param name="project">The project name or ID</param>
    /// <param name="repositoryName">The name of the repository to create</param>
    /// <returns>Result of the repository creation operation</returns>
    [KernelFunction("create_repository")]
    [Description("Create a new Git repository in an Azure DevOps project. The repository will be initialized with default settings.")]
    public async Task<string> CreateRepositoryAsync(
        [Description("The Azure DevOps organization name")] string organization,
        [Description("The project name or ID")] string project,
        [Description("The name of the repository to create")] string repositoryName)
    {
        try
        {
            _logger.LogInformation("Creating repository '{RepositoryName}' in project: {Project}, organization: {Organization}", repositoryName, project, organization);

            // Validate inputs
            if (string.IsNullOrWhiteSpace(repositoryName))
            {
                return JsonSerializer.Serialize(new { error = "Repository name is required." });
            }

            // First, get the project details to obtain the project ID
            var projectDetails = await _azureDevOpsApiService.GetAsync<Project>(
                organization, $"projects/{project}", "7.1");

            if (projectDetails == null)
            {
                return JsonSerializer.Serialize(new { error = $"Project '{project}' not found." });
            }

            // Create the repository request payload
            var repositoryRequest = new
            {
                name = repositoryName,
                project = new
                {
                    id = projectDetails.Id
                }
            };

            // Use the Azure DevOps API service to create the repository
            var createdRepository = await _azureDevOpsApiService.PostAsync<GitRepository>(
                organization, $"{project}/_apis/git/repositories", repositoryRequest, "7.1");

            if (createdRepository == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to create repository. Please check the logs for more details." });
            }

            // Return raw JSON data for the AI to format
            var result = new
            {
                success = true,
                repositoryName = createdRepository.Name,
                repositoryId = createdRepository.Id,
                organization,
                project = createdRepository.Project?.Name ?? project,
                projectId = createdRepository.Project?.Id ?? projectDetails.Id,
                url = createdRepository.Url,
                remoteUrl = createdRepository.RemoteUrl,
                sshUrl = createdRepository.SshUrl,
                webUrl = createdRepository.WebUrl,
                defaultBranch = createdRepository.DefaultBranch
            };

            _logger.LogInformation("Successfully created repository '{RepositoryName}' with ID: {RepositoryId}", repositoryName, createdRepository.Id);
            return JsonSerializer.Serialize(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating repository '{RepositoryName}' in project: {Project}, organization: {Organization}", repositoryName, project, organization);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
