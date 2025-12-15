using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Plugins;
using AzureDevOpsAI.Backend.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace AzureDevOpsAI.Backend.Tests.Plugins;

public class RepositoryPluginTests
{
    private readonly Mock<IAzureDevOpsApiService> _mockApiService;
    private readonly Mock<ILogger<RepositoryPlugin>> _mockLogger;
    private readonly RepositoryPlugin _plugin;

    public RepositoryPluginTests()
    {
        _mockApiService = new Mock<IAzureDevOpsApiService>();
        _mockLogger = new Mock<ILogger<RepositoryPlugin>>();
        _plugin = new RepositoryPlugin(_mockApiService.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializePlugin()
    {
        // Arrange & Act
        var plugin = new RepositoryPlugin(_mockApiService.Object, _mockLogger.Object);

        // Assert
        plugin.Should().NotBeNull();
    }

    [Fact]
    public async Task ListRepositoriesAsync_ShouldReturnFormattedList_WhenRepositoriesExist()
    {
        // Arrange
        var organization = "test-org";
        var project = "test-project";
        var repositories = new RepositoryListResponse
        {
            Value = new List<GitRepository>
            {
                new GitRepository
                {
                    Id = "repo-1",
                    Name = "Repository 1",
                    Url = "https://dev.azure.com/test-org/test-project/_apis/git/repositories/repo-1",
                    RemoteUrl = "https://test-org@dev.azure.com/test-org/test-project/_git/Repository-1",
                    SshUrl = "git@ssh.dev.azure.com:v3/test-org/test-project/Repository-1",
                    WebUrl = "https://dev.azure.com/test-org/test-project/_git/Repository-1",
                    DefaultBranch = "refs/heads/main",
                    Size = 1024000,
                    IsDisabled = false,
                    IsFork = false,
                    Project = new RepositoryProject
                    {
                        Id = "proj-1",
                        Name = "test-project",
                        Description = "Test project",
                        State = "wellFormed",
                        Visibility = "private"
                    }
                },
                new GitRepository
                {
                    Id = "repo-2",
                    Name = "Repository 2",
                    Url = "https://dev.azure.com/test-org/test-project/_apis/git/repositories/repo-2",
                    RemoteUrl = "https://test-org@dev.azure.com/test-org/test-project/_git/Repository-2",
                    DefaultBranch = "refs/heads/master",
                    Size = 512000,
                    IsDisabled = false,
                    IsFork = true
                }
            },
            Count = 2
        };

        _mockApiService
            .Setup(x => x.GetAsync<RepositoryListResponse>(organization, $"{project}/_apis/git/repositories", "7.1", default))
            .ReturnsAsync(repositories);

        // Act
        var result = await _plugin.ListRepositoriesAsync(organization, project);

        // Assert - verify JSON format
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("organization").GetString().Should().Be("test-org");
        jsonDoc.RootElement.GetProperty("project").GetString().Should().Be("test-project");
        jsonDoc.RootElement.GetProperty("totalRepositories").GetInt32().Should().Be(2);

        var repositoriesArray = jsonDoc.RootElement.GetProperty("repositories");
        repositoriesArray.GetArrayLength().Should().Be(2);

        var firstRepo = repositoriesArray[0];
        firstRepo.GetProperty("name").GetString().Should().Be("Repository 1");
        firstRepo.GetProperty("id").GetString().Should().Be("repo-1");
        firstRepo.GetProperty("defaultBranch").GetString().Should().Be("refs/heads/main");
        firstRepo.GetProperty("size").GetInt64().Should().Be(1024000);
        firstRepo.GetProperty("isDisabled").GetBoolean().Should().BeFalse();
        firstRepo.GetProperty("isFork").GetBoolean().Should().BeFalse();

        var projectInfo = firstRepo.GetProperty("project");
        projectInfo.GetProperty("id").GetString().Should().Be("proj-1");
        projectInfo.GetProperty("name").GetString().Should().Be("test-project");
    }

    [Fact]
    public async Task ListRepositoriesAsync_ShouldReturnNoRepositoriesMessage_WhenNoRepositoriesExist()
    {
        // Arrange
        var organization = "empty-org";
        var project = "empty-project";
        var emptyRepositories = new RepositoryListResponse
        {
            Value = new List<GitRepository>(),
            Count = 0
        };

        _mockApiService
            .Setup(x => x.GetAsync<RepositoryListResponse>(organization, $"{project}/_apis/git/repositories", "7.1", default))
            .ReturnsAsync(emptyRepositories);

        // Act
        var result = await _plugin.ListRepositoriesAsync(organization, project);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("message").GetString().Should().Be("No repositories found in this project.");
        var repositoriesArray = jsonDoc.RootElement.GetProperty("repositories");
        repositoriesArray.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task ListRepositoriesAsync_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        var organization = "test-org";
        var project = "test-project";
        var errorMessage = "API connection failed";

        _mockApiService
            .Setup(x => x.GetAsync<RepositoryListResponse>(organization, $"{project}/_apis/git/repositories", "7.1", default))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _plugin.ListRepositoriesAsync(organization, project);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Be(errorMessage);
    }

    [Fact]
    public async Task GetRepositoryAsync_ShouldReturnRepositoryDetails_WhenRepositoryExists()
    {
        // Arrange
        var organization = "test-org";
        var project = "test-project";
        var repositoryId = "repo-1";
        var repository = new GitRepository
        {
            Id = repositoryId,
            Name = "Test Repository",
            Url = "https://dev.azure.com/test-org/test-project/_apis/git/repositories/repo-1",
            RemoteUrl = "https://test-org@dev.azure.com/test-org/test-project/_git/Test-Repository",
            SshUrl = "git@ssh.dev.azure.com:v3/test-org/test-project/Test-Repository",
            WebUrl = "https://dev.azure.com/test-org/test-project/_git/Test-Repository",
            DefaultBranch = "refs/heads/main",
            Size = 2048000,
            IsDisabled = false,
            IsFork = false,
            Project = new RepositoryProject
            {
                Id = "proj-1",
                Name = "test-project",
                Description = "Test project description",
                State = "wellFormed",
                Visibility = "private"
            }
        };

        _mockApiService
            .Setup(x => x.GetAsync<GitRepository>(organization, $"{project}/_apis/git/repositories/{repositoryId}", "7.1", default))
            .ReturnsAsync(repository);

        // Act
        var result = await _plugin.GetRepositoryAsync(organization, project, repositoryId);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("organization").GetString().Should().Be("test-org");
        jsonDoc.RootElement.GetProperty("project").GetString().Should().Be("test-project");

        var repoData = jsonDoc.RootElement.GetProperty("repository");
        repoData.GetProperty("name").GetString().Should().Be("Test Repository");
        repoData.GetProperty("id").GetString().Should().Be("repo-1");
        repoData.GetProperty("defaultBranch").GetString().Should().Be("refs/heads/main");
        repoData.GetProperty("size").GetInt64().Should().Be(2048000);
        repoData.GetProperty("remoteUrl").GetString().Should().Contain("_git/Test-Repository");
    }

    [Fact]
    public async Task GetRepositoryAsync_ShouldReturnError_WhenRepositoryNotFound()
    {
        // Arrange
        var organization = "test-org";
        var project = "test-project";
        var repositoryId = "nonexistent-repo";

        _mockApiService
            .Setup(x => x.GetAsync<GitRepository>(organization, $"{project}/_apis/git/repositories/{repositoryId}", "7.1", default))
            .ReturnsAsync((GitRepository?)null);

        // Act
        var result = await _plugin.GetRepositoryAsync(organization, project, repositoryId);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("not found");
    }

    [Fact]
    public async Task GetRepositoryAsync_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        var organization = "test-org";
        var project = "test-project";
        var repositoryId = "repo-1";
        var errorMessage = "API connection failed";

        _mockApiService
            .Setup(x => x.GetAsync<GitRepository>(organization, $"{project}/_apis/git/repositories/{repositoryId}", "7.1", default))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _plugin.GetRepositoryAsync(organization, project, repositoryId);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Be(errorMessage);
    }

    [Fact]
    public async Task CreateRepositoryAsync_ShouldReturnSuccess_WhenRepositoryCreated()
    {
        // Arrange
        var organization = "test-org";
        var project = "test-project";
        var repositoryName = "New Repository";
        var projectDetails = new Project
        {
            Id = "proj-1",
            Name = "test-project",
            Description = "Test project",
            State = ProjectState.WellFormed,
            Visibility = ProjectVisibility.Private
        };
        var createdRepository = new GitRepository
        {
            Id = "new-repo-1",
            Name = repositoryName,
            Url = "https://dev.azure.com/test-org/test-project/_apis/git/repositories/new-repo-1",
            RemoteUrl = "https://test-org@dev.azure.com/test-org/test-project/_git/New-Repository",
            SshUrl = "git@ssh.dev.azure.com:v3/test-org/test-project/New-Repository",
            WebUrl = "https://dev.azure.com/test-org/test-project/_git/New-Repository",
            DefaultBranch = null, // New repos may not have a default branch yet
            Project = new RepositoryProject
            {
                Id = "proj-1",
                Name = "test-project"
            }
        };

        _mockApiService
            .Setup(x => x.GetAsync<Project>(organization, $"projects/{project}", "7.1", default))
            .ReturnsAsync(projectDetails);

        _mockApiService
            .Setup(x => x.PostAsync<GitRepository>(
                organization,
                $"{project}/_apis/git/repositories",
                It.Is<object>(o => o != null),
                "7.1",
                default))
            .ReturnsAsync(createdRepository);

        // Act
        var result = await _plugin.CreateRepositoryAsync(organization, project, repositoryName);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        jsonDoc.RootElement.GetProperty("repositoryName").GetString().Should().Be("New Repository");
        jsonDoc.RootElement.GetProperty("repositoryId").GetString().Should().Be("new-repo-1");
        jsonDoc.RootElement.GetProperty("organization").GetString().Should().Be("test-org");
        jsonDoc.RootElement.GetProperty("projectId").GetString().Should().Be("proj-1");
        jsonDoc.RootElement.GetProperty("remoteUrl").GetString().Should().Contain("_git/New-Repository");
    }

    [Fact]
    public async Task CreateRepositoryAsync_ShouldReturnError_WhenRepositoryNameIsEmpty()
    {
        // Arrange
        var organization = "test-org";
        var project = "test-project";
        var repositoryName = "";

        // Act
        var result = await _plugin.CreateRepositoryAsync(organization, project, repositoryName);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Be("Repository name is required.");
    }

    [Fact]
    public async Task CreateRepositoryAsync_ShouldReturnError_WhenProjectNotFound()
    {
        // Arrange
        var organization = "test-org";
        var project = "nonexistent-project";
        var repositoryName = "New Repository";

        _mockApiService
            .Setup(x => x.GetAsync<Project>(organization, $"projects/{project}", "7.1", default))
            .ReturnsAsync((Project?)null);

        // Act
        var result = await _plugin.CreateRepositoryAsync(organization, project, repositoryName);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("not found");
    }

    [Fact]
    public async Task CreateRepositoryAsync_ShouldReturnError_WhenCreationFails()
    {
        // Arrange
        var organization = "test-org";
        var project = "test-project";
        var repositoryName = "New Repository";
        var projectDetails = new Project
        {
            Id = "proj-1",
            Name = "test-project",
            Description = "Test project",
            State = ProjectState.WellFormed,
            Visibility = ProjectVisibility.Private
        };

        _mockApiService
            .Setup(x => x.GetAsync<Project>(organization, $"projects/{project}", "7.1", default))
            .ReturnsAsync(projectDetails);

        _mockApiService
            .Setup(x => x.PostAsync<GitRepository>(
                organization,
                $"{project}/_apis/git/repositories",
                It.Is<object>(o => o != null),
                "7.1",
                default))
            .ReturnsAsync((GitRepository?)null);

        // Act
        var result = await _plugin.CreateRepositoryAsync(organization, project, repositoryName);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Failed to create repository");
    }

    [Fact]
    public async Task CreateRepositoryAsync_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        var organization = "test-org";
        var project = "test-project";
        var repositoryName = "New Repository";
        var errorMessage = "API connection failed";

        _mockApiService
            .Setup(x => x.GetAsync<Project>(organization, $"projects/{project}", "7.1", default))
            .ThrowsAsync(new Exception(errorMessage));

        // Act
        var result = await _plugin.CreateRepositoryAsync(organization, project, repositoryName);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Be(errorMessage);
    }
}
