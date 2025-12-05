using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Plugins;
using AzureDevOpsAI.Backend.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AzureDevOpsAI.Backend.Tests.Plugins;

public class ProjectPluginTests
{
    private readonly Mock<IAzureDevOpsApiService> _mockApiService;
    private readonly Mock<ILogger<ProjectPlugin>> _mockLogger;
    private readonly ProjectPlugin _plugin;

    public ProjectPluginTests()
    {
        _mockApiService = new Mock<IAzureDevOpsApiService>();
        _mockLogger = new Mock<ILogger<ProjectPlugin>>();
        _plugin = new ProjectPlugin(_mockApiService.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializePlugin()
    {
        // Arrange & Act
        var plugin = new ProjectPlugin(_mockApiService.Object, _mockLogger.Object);

        // Assert
        plugin.Should().NotBeNull();
    }

    [Fact]
    public async Task ListProjectsAsync_ShouldReturnFormattedList_WhenProjectsExist()
    {
        // Arrange
        var organization = "test-org";
        var projects = new ProjectListResponse
        {
            Value = new List<Project>
            {
                new Project
                {
                    Id = "proj-1",
                    Name = "Project 1",
                    Description = "First test project",
                    State = ProjectState.WellFormed,
                    Visibility = ProjectVisibility.Private,
                    LastUpdateTime = new DateTime(2024, 1, 15, 10, 30, 0)
                },
                new Project
                {
                    Id = "proj-2",
                    Name = "Project 2",
                    Description = "Second test project",
                    State = ProjectState.WellFormed,
                    Visibility = ProjectVisibility.Public,
                    LastUpdateTime = new DateTime(2024, 2, 20, 14, 45, 0)
                }
            },
            Count = 2
        };

        _mockApiService
            .Setup(x => x.GetAsync<ProjectListResponse>(organization, "projects", "7.1", default))
            .ReturnsAsync(projects);

        // Act
        var result = await _plugin.ListProjectsAsync(organization);

        // Assert
        result.Should().Contain("Projects in organization 'test-org':");
        result.Should().Contain("**Project 1** (ID: proj-1)");
        result.Should().Contain("First test project");
        result.Should().Contain("**Project 2** (ID: proj-2)");
        result.Should().Contain("Second test project");
        result.Should().Contain("State: WellFormed");
        result.Should().Contain("Visibility: Private");
        result.Should().Contain("Visibility: Public");
        result.Should().Contain("Total: 2 project(s)");
    }

    [Fact]
    public async Task ListProjectsAsync_ShouldReturnNoProjectsMessage_WhenNoProjectsExist()
    {
        // Arrange
        var organization = "empty-org";
        var emptyProjects = new ProjectListResponse
        {
            Value = new List<Project>(),
            Count = 0
        };

        _mockApiService
            .Setup(x => x.GetAsync<ProjectListResponse>(organization, "projects", "7.1", default))
            .ReturnsAsync(emptyProjects);

        // Act
        var result = await _plugin.ListProjectsAsync(organization);

        // Assert
        result.Should().Be("No projects found in this organization.");
    }

    [Fact]
    public async Task ListProjectsAsync_ShouldReturnNoProjectsMessage_WhenNullResponse()
    {
        // Arrange
        var organization = "null-org";

        _mockApiService
            .Setup(x => x.GetAsync<ProjectListResponse>(organization, "projects", "7.1", default))
            .ReturnsAsync((ProjectListResponse?)null);

        // Act
        var result = await _plugin.ListProjectsAsync(organization);

        // Assert
        result.Should().Be("No projects found in this organization.");
    }

    [Fact]
    public async Task ListProjectsAsync_ShouldReturnErrorMessage_WhenExceptionOccurs()
    {
        // Arrange
        var organization = "error-org";
        var exceptionMessage = "API call failed";

        _mockApiService
            .Setup(x => x.GetAsync<ProjectListResponse>(organization, "projects", "7.1", default))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _plugin.ListProjectsAsync(organization);

        // Assert
        result.Should().StartWith("Error:");
        result.Should().Contain(exceptionMessage);
    }

    [Fact]
    public async Task ListProjectsAsync_ShouldHandleProjectWithoutDescription()
    {
        // Arrange
        var organization = "test-org";
        var projects = new ProjectListResponse
        {
            Value = new List<Project>
            {
                new Project
                {
                    Id = "proj-1",
                    Name = "Project Without Description",
                    Description = null,
                    State = ProjectState.WellFormed,
                    Visibility = ProjectVisibility.Private
                }
            },
            Count = 1
        };

        _mockApiService
            .Setup(x => x.GetAsync<ProjectListResponse>(organization, "projects", "7.1", default))
            .ReturnsAsync(projects);

        // Act
        var result = await _plugin.ListProjectsAsync(organization);

        // Assert
        result.Should().Contain("**Project Without Description** (ID: proj-1)");
        result.Should().NotContain("Description:");
        result.Should().Contain("Total: 1 project(s)");
    }

    [Fact]
    public async Task ListProjectsAsync_ShouldHandleProjectWithoutLastUpdateTime()
    {
        // Arrange
        var organization = "test-org";
        var projects = new ProjectListResponse
        {
            Value = new List<Project>
            {
                new Project
                {
                    Id = "proj-1",
                    Name = "Project Without LastUpdate",
                    State = ProjectState.WellFormed,
                    Visibility = ProjectVisibility.Private,
                    LastUpdateTime = null
                }
            },
            Count = 1
        };

        _mockApiService
            .Setup(x => x.GetAsync<ProjectListResponse>(organization, "projects", "7.1", default))
            .ReturnsAsync(projects);

        // Act
        var result = await _plugin.ListProjectsAsync(organization);

        // Assert
        result.Should().Contain("**Project Without LastUpdate** (ID: proj-1)");
        result.Should().NotContain("Last Updated:");
    }

    [Fact]
    public async Task ListProjectsAsync_ShouldLogInformationOnSuccess()
    {
        // Arrange
        var organization = "test-org";
        var projects = new ProjectListResponse
        {
            Value = new List<Project>
            {
                new Project
                {
                    Id = "proj-1",
                    Name = "Test Project",
                    State = ProjectState.WellFormed,
                    Visibility = ProjectVisibility.Private
                }
            },
            Count = 1
        };

        _mockApiService
            .Setup(x => x.GetAsync<ProjectListResponse>(organization, "projects", "7.1", default))
            .ReturnsAsync(projects);

        // Act
        await _plugin.ListProjectsAsync(organization);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Listing projects for organization")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully retrieved")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task ListProjectsAsync_ShouldLogErrorOnException()
    {
        // Arrange
        var organization = "error-org";

        _mockApiService
            .Setup(x => x.GetAsync<ProjectListResponse>(organization, "projects", "7.1", default))
            .ThrowsAsync(new Exception("Test error"));

        // Act
        await _plugin.ListProjectsAsync(organization);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error listing projects")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
