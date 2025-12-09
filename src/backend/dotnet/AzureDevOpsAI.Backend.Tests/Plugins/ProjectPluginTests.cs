using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Plugins;
using AzureDevOpsAI.Backend.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

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

        // Assert - verify JSON format
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("organization").GetString().Should().Be("test-org");
        jsonDoc.RootElement.GetProperty("totalProjects").GetInt32().Should().Be(2);
        
        var projectsArray = jsonDoc.RootElement.GetProperty("projects");
        projectsArray.GetArrayLength().Should().Be(2);
        
        var firstProject = projectsArray[0];
        firstProject.GetProperty("name").GetString().Should().Be("Project 1");
        firstProject.GetProperty("id").GetString().Should().Be("proj-1");
        firstProject.GetProperty("description").GetString().Should().Be("First test project");
        firstProject.GetProperty("state").GetString().Should().Be("WellFormed");
        firstProject.GetProperty("visibility").GetString().Should().Be("Private");
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

        // Assert - verify JSON format
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("message").GetString().Should().Be("No projects found in this organization.");
        jsonDoc.RootElement.GetProperty("projects").GetArrayLength().Should().Be(0);
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

        // Assert - verify JSON format
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("message").GetString().Should().Be("No projects found in this organization.");
        jsonDoc.RootElement.GetProperty("projects").GetArrayLength().Should().Be(0);
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

        // Assert - verify JSON format
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Be(exceptionMessage);
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

        // Assert - verify JSON format
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("totalProjects").GetInt32().Should().Be(1);
        
        var projectsArray = jsonDoc.RootElement.GetProperty("projects");
        var firstProject = projectsArray[0];
        firstProject.GetProperty("name").GetString().Should().Be("Project Without Description");
        firstProject.GetProperty("id").GetString().Should().Be("proj-1");
        
        // Description may be null or empty string in JSON
        var hasDescription = firstProject.TryGetProperty("description", out var descProp);
        if (hasDescription && descProp.ValueKind != JsonValueKind.Null)
        {
            descProp.GetString().Should().BeNullOrEmpty();
        }
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

        // Assert - verify JSON format
        var jsonDoc = JsonDocument.Parse(result);
        var projectsArray = jsonDoc.RootElement.GetProperty("projects");
        var firstProject = projectsArray[0];
        firstProject.GetProperty("name").GetString().Should().Be("Project Without LastUpdate");
        
        // lastUpdateTime may be null in JSON
        var hasLastUpdate = firstProject.TryGetProperty("lastUpdateTime", out var lastUpdateProp);
        if (hasLastUpdate)
        {
            lastUpdateProp.ValueKind.Should().Be(JsonValueKind.Null);
        }
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
