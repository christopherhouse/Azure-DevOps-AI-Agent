using System.Text.Json;
using AzureDevOpsAI.Backend.Models;
using FluentAssertions;

namespace AzureDevOpsAI.Backend.Tests.Models;

/// <summary>
/// Tests for ProjectModels JSON serialization/deserialization.
/// These tests validate that the ProjectState enum correctly parses Azure DevOps REST API responses.
/// </summary>
public class ProjectModelsJsonTests
{
    private static readonly JsonSerializerOptions CaseInsensitiveOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public void ProjectState_ShouldDeserialize_WellFormedValue()
    {
        // Arrange - This is the actual format returned by Azure DevOps REST API
        var json = """{"state": "wellFormed"}""";

        // Act
        var result = JsonSerializer.Deserialize<Project>(json, CaseInsensitiveOptions);

        // Assert
        result.Should().NotBeNull();
        result!.State.Should().Be(ProjectState.WellFormed);
    }

    [Fact]
    public void ProjectState_ShouldDeserialize_DeletingValue()
    {
        // Arrange
        var json = """{"state": "deleting"}""";

        // Act
        var result = JsonSerializer.Deserialize<Project>(json, CaseInsensitiveOptions);

        // Assert
        result.Should().NotBeNull();
        result!.State.Should().Be(ProjectState.Deleting);
    }

    [Fact]
    public void ProjectState_ShouldDeserialize_NewValue()
    {
        // Arrange
        var json = """{"state": "new"}""";

        // Act
        var result = JsonSerializer.Deserialize<Project>(json, CaseInsensitiveOptions);

        // Assert
        result.Should().NotBeNull();
        result!.State.Should().Be(ProjectState.New);
    }

    [Fact]
    public void ProjectListResponse_ShouldDeserialize_AzureDevOpsApiFormat()
    {
        // Arrange - This is the actual format returned by Azure DevOps REST API
        var json = """
        {
            "count": 2,
            "value": [
                {
                    "id": "eb6e4656-77fc-42a1-9181-4c6d8e9da5d1",
                    "name": "Fabrikam-Fiber-TFVC",
                    "description": "Team Foundation Version Control projects.",
                    "url": "https://dev.azure.com/fabrikam/_apis/projects/eb6e4656-77fc-42a1-9181-4c6d8e9da5d1",
                    "state": "wellFormed",
                    "visibility": "private"
                },
                {
                    "id": "6ce954b1-ce1f-45d1-b94d-e6bf2464ba2c",
                    "name": "Fabrikam-Fiber-Git",
                    "description": "Git projects",
                    "url": "https://dev.azure.com/fabrikam/_apis/projects/6ce954b1-ce1f-45d1-b94d-e6bf2464ba2c",
                    "state": "wellFormed",
                    "visibility": "public"
                }
            ]
        }
        """;

        // Act
        var result = JsonSerializer.Deserialize<ProjectListResponse>(json, CaseInsensitiveOptions);

        // Assert
        result.Should().NotBeNull();
        result!.Count.Should().Be(2);
        result.Value.Should().HaveCount(2);
        
        result.Value[0].Id.Should().Be("eb6e4656-77fc-42a1-9181-4c6d8e9da5d1");
        result.Value[0].Name.Should().Be("Fabrikam-Fiber-TFVC");
        result.Value[0].State.Should().Be(ProjectState.WellFormed);
        result.Value[0].Visibility.Should().Be(ProjectVisibility.Private);
        
        result.Value[1].Id.Should().Be("6ce954b1-ce1f-45d1-b94d-e6bf2464ba2c");
        result.Value[1].Name.Should().Be("Fabrikam-Fiber-Git");
        result.Value[1].State.Should().Be(ProjectState.WellFormed);
        result.Value[1].Visibility.Should().Be(ProjectVisibility.Public);
    }

    [Fact]
    public void ProjectState_ShouldSerialize_ToCorrectApiFormat()
    {
        // Arrange
        var project = new Project
        {
            Id = "test-id",
            Name = "Test Project",
            State = ProjectState.WellFormed,
            Visibility = ProjectVisibility.Private
        };

        // Act
        var json = JsonSerializer.Serialize(project);

        // Assert - State should serialize to camelCase "wellFormed"
        json.Should().Contain("\"State\":\"wellFormed\"");
        // Visibility uses default JsonStringEnumConverter which uses PascalCase
        json.Should().Contain("\"Visibility\":\"Private\"");
    }
}
