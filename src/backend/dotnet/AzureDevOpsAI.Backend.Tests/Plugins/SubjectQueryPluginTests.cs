using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Plugins;
using AzureDevOpsAI.Backend.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace AzureDevOpsAI.Backend.Tests.Plugins;

public class SubjectQueryPluginTests
{
    private readonly Mock<IAzureDevOpsApiService> _mockApiService;
    private readonly Mock<ILogger<SubjectQueryPlugin>> _mockLogger;
    private readonly SubjectQueryPlugin _plugin;

    public SubjectQueryPluginTests()
    {
        _mockApiService = new Mock<IAzureDevOpsApiService>();
        _mockLogger = new Mock<ILogger<SubjectQueryPlugin>>();
        _plugin = new SubjectQueryPlugin(_mockApiService.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializePlugin()
    {
        // Arrange & Act
        var plugin = new SubjectQueryPlugin(_mockApiService.Object, _mockLogger.Object);

        // Assert
        plugin.Should().NotBeNull();
    }

    [Fact]
    public async Task QuerySubjectsAsync_ShouldReturnUsers_WhenSearchingForUsers()
    {
        // Arrange
        var organization = "test-org";
        var query = "john@example.com";
        var subjectKind = new[] { SubjectKind.User };

        var response = new SubjectQueryResponse
        {
            Value = new List<GraphSubject>
            {
                new GraphSubject
                {
                    Descriptor = "aad.MWY3ZDQ4YTktNzU1Yy03YzQwLWJlNTktYmQ3YjYwYzA0ZTY5",
                    DisplayName = "John Doe",
                    SubjectKind = "User",
                    PrincipalName = "john@example.com",
                    MailAddress = "john@example.com",
                    Origin = "aad",
                    OriginId = "1f7d48a9-755c-7c40-be59-bd7b60c04e69",
                    Domain = "example.com",
                    DirectoryAlias = "john"
                }
            },
            Count = 1
        };

        _mockApiService
            .Setup(x => x.PostAsync<SubjectQueryResponse>(
                organization,
                It.Is<string>(s => s.Contains("graph/subjectquery")),
                It.Is<SubjectQueryRequest>(r => r.Query == query && r.SubjectKind != null && r.SubjectKind.Contains(SubjectKind.User)),
                "7.1-preview.1",
                default))
            .ReturnsAsync(response);

        // Act
        var result = await _plugin.QuerySubjectsAsync(organization, query, subjectKind);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("john@example.com");
        result.Should().Contain("John Doe");
        result.Should().Contain("aad.MWY3ZDQ4YTktNzU1Yy03YzQwLWJlNTktYmQ3YjYwYzA0ZTY5");

        var jsonResult = JsonSerializer.Deserialize<JsonElement>(result);
        jsonResult.GetProperty("totalResults").GetInt32().Should().Be(1);
        jsonResult.GetProperty("subjectKind").GetString().Should().Be("User");
    }

    [Fact]
    public async Task QuerySubjectsAsync_ShouldReturnGroups_WhenSearchingForGroups()
    {
        // Arrange
        var organization = "test-org";
        var query = "[MyProject]\\Contributors";
        var subjectKind = new[] { SubjectKind.Group };

        var response = new SubjectQueryResponse
        {
            Value = new List<GraphSubject>
            {
                new GraphSubject
                {
                    Descriptor = "vssgp.Uy0xLTktMTU1MTM3NDI0NS0xMjA0NDAwOTY5",
                    DisplayName = "Contributors",
                    SubjectKind = "Group",
                    PrincipalName = "[MyProject]\\Contributors",
                    MailAddress = null,
                    Origin = "vsts",
                    OriginId = null,
                    Domain = "vstfs:///Classification/TeamProject/12345678-1234-1234-1234-123456789012"
                }
            },
            Count = 1
        };

        _mockApiService
            .Setup(x => x.PostAsync<SubjectQueryResponse>(
                organization,
                It.Is<string>(s => s.Contains("graph/subjectquery")),
                It.Is<SubjectQueryRequest>(r => r.Query == query && r.SubjectKind != null && r.SubjectKind.Contains(SubjectKind.Group)),
                "7.1-preview.1",
                default))
            .ReturnsAsync(response);

        // Act
        var result = await _plugin.QuerySubjectsAsync(organization, query, subjectKind);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("Contributors");
        result.Should().Contain("vssgp.Uy0xLTktMTU1MTM3NDI0NS0xMjA0NDAwOTY5");

        var jsonResult = JsonSerializer.Deserialize<JsonElement>(result);
        jsonResult.GetProperty("totalResults").GetInt32().Should().Be(1);
        jsonResult.GetProperty("subjectKind").GetString().Should().Be("Group");
    }

    [Fact]
    public async Task QuerySubjectsAsync_ShouldReturnBothUsersAndGroups_WhenNoSubjectKindSpecified()
    {
        // Arrange
        var organization = "test-org";
        var query = "admin";

        var response = new SubjectQueryResponse
        {
            Value = new List<GraphSubject>
            {
                new GraphSubject
                {
                    Descriptor = "aad.MWY3ZDQ4YTktNzU1Yy03YzQwLWJlNTktYmQ3YjYwYzA0ZTY5",
                    DisplayName = "Admin User",
                    SubjectKind = "User",
                    PrincipalName = "admin@example.com",
                    MailAddress = "admin@example.com",
                    Origin = "aad"
                },
                new GraphSubject
                {
                    Descriptor = "vssgp.Uy0xLTktMTU1MTM3NDI0NS0xMjA0NDAwOTY5",
                    DisplayName = "Administrators",
                    SubjectKind = "Group",
                    PrincipalName = "[MyProject]\\Administrators",
                    Origin = "vsts"
                }
            },
            Count = 2
        };

        _mockApiService
            .Setup(x => x.PostAsync<SubjectQueryResponse>(
                organization,
                It.Is<string>(s => s.Contains("graph/subjectquery")),
                It.Is<SubjectQueryRequest>(r => r.Query == query && r.SubjectKind == null),
                "7.1-preview.1",
                default))
            .ReturnsAsync(response);

        // Act
        var result = await _plugin.QuerySubjectsAsync(organization, query, null);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("Admin User");
        result.Should().Contain("Administrators");

        var jsonResult = JsonSerializer.Deserialize<JsonElement>(result);
        jsonResult.GetProperty("totalResults").GetInt32().Should().Be(2);
        jsonResult.GetProperty("subjectKind").GetString().Should().Be("all");
    }

    [Fact]
    public async Task QuerySubjectsAsync_ShouldReturnEmptyResult_WhenNoSubjectsFound()
    {
        // Arrange
        var organization = "test-org";
        var query = "nonexistent@example.com";

        var response = new SubjectQueryResponse
        {
            Value = new List<GraphSubject>(),
            Count = 0
        };

        _mockApiService
            .Setup(x => x.PostAsync<SubjectQueryResponse>(
                organization,
                It.Is<string>(s => s.Contains("graph/subjectquery")),
                It.IsAny<SubjectQueryRequest>(),
                "7.1-preview.1",
                default))
            .ReturnsAsync(response);

        // Act
        var result = await _plugin.QuerySubjectsAsync(organization, query, null);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("No subjects found matching the query");

        var jsonResult = JsonSerializer.Deserialize<JsonElement>(result);
        jsonResult.GetProperty("message").GetString().Should().Contain("No subjects found");
        jsonResult.GetProperty("results").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task QuerySubjectsAsync_ShouldReturnError_WhenQueryIsEmpty()
    {
        // Arrange
        var organization = "test-org";
        var query = "";

        // Act
        var result = await _plugin.QuerySubjectsAsync(organization, query, null);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("error");
        result.Should().Contain("Search query is required");

        var jsonResult = JsonSerializer.Deserialize<JsonElement>(result);
        jsonResult.GetProperty("error").GetString().Should().Contain("Search query is required");
    }


    [Fact]
    public async Task QuerySubjectsAsync_ShouldAcceptMultipleSubjectKinds()
    {
        // Arrange
        var organization = "test-org";
        var query = "admin";
        var subjectKind = new[] { SubjectKind.User, SubjectKind.Group };

        var response = new SubjectQueryResponse
        {
            Value = new List<GraphSubject>
            {
                new GraphSubject
                {
                    Descriptor = "aad.MWY3ZDQ4YTktNzU1Yy03YzQwLWJlNTktYmQ3YjYwYzA0ZTY5",
                    DisplayName = "Admin User",
                    SubjectKind = "User",
                    PrincipalName = "admin@example.com"
                },
                new GraphSubject
                {
                    Descriptor = "vssgp.Uy0xLTktMTU1MTM3NDI0NS0xMjA0NDAwOTY5",
                    DisplayName = "Administrators",
                    SubjectKind = "Group",
                    PrincipalName = "[MyProject]\\Administrators"
                }
            },
            Count = 2
        };

        _mockApiService
            .Setup(x => x.PostAsync<SubjectQueryResponse>(
                organization,
                It.Is<string>(s => s.Contains("graph/subjectquery")),
                It.Is<SubjectQueryRequest>(r => r.Query == query && 
                    r.SubjectKind != null && 
                    r.SubjectKind.Contains(SubjectKind.User) && 
                    r.SubjectKind.Contains(SubjectKind.Group)),
                "7.1-preview.1",
                default))
            .ReturnsAsync(response);

        // Act
        var result = await _plugin.QuerySubjectsAsync(organization, query, subjectKind);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("Admin User");
        result.Should().Contain("Administrators");

        var jsonResult = JsonSerializer.Deserialize<JsonElement>(result);
        jsonResult.GetProperty("totalResults").GetInt32().Should().Be(2);
        jsonResult.GetProperty("subjectKind").GetString().Should().Be("User, Group");
    }

    [Fact]
    public async Task QuerySubjectsAsync_ShouldReturnError_WhenApiThrowsException()
    {
        // Arrange
        var organization = "test-org";
        var query = "test@example.com";

        _mockApiService
            .Setup(x => x.PostAsync<SubjectQueryResponse>(
                organization,
                It.IsAny<string>(),
                It.IsAny<SubjectQueryRequest>(),
                "7.1-preview.1",
                default))
            .ThrowsAsync(new Exception("API Error"));

        // Act
        var result = await _plugin.QuerySubjectsAsync(organization, query, null);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("error");
        result.Should().Contain("API Error");

        var jsonResult = JsonSerializer.Deserialize<JsonElement>(result);
        jsonResult.GetProperty("error").GetString().Should().Contain("API Error");
    }
}
