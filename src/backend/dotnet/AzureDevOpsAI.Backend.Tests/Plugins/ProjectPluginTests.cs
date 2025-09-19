using AzureDevOpsAI.Backend.Plugins;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace AzureDevOpsAI.Backend.Tests.Plugins;

public class ProjectPluginTests : IDisposable
{
    private readonly Mock<ILogger<ProjectPlugin>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly HttpClient _httpClient;
    private readonly ProjectPlugin _projectPlugin;

    public ProjectPluginTests()
    {
        _mockLogger = new Mock<ILogger<ProjectPlugin>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _projectPlugin = new ProjectPlugin(_httpClient, _mockLogger.Object);
    }

    [Fact]
    public async Task GetProcessTemplatesAsync_ShouldReturnFormattedTemplates_WhenApiCallSucceeds()
    {
        // Arrange
        var organization = "test-org";
        var token = "test-token";
        var responseData = new
        {
            count = 3,
            value = new[]
            {
                new
                {
                    typeId = "adcc42ab-9882-485e-a3ed-7678f01f66bc",
                    name = "Agile",
                    description = "This template is flexible and will work great for most teams using Agile planning methods, including those practicing Scrum.",
                    isEnabled = true,
                    isDefault = true,
                    customizationType = "System"
                },
                new
                {
                    typeId = "27450541-8e31-4150-9947-dc59f998fc01",
                    name = "CMMI",
                    description = "This template is for more formal projects requiring a framework for process improvement and an auditable record of decisions.",
                    isEnabled = true,
                    isDefault = false,
                    customizationType = "System"
                },
                new
                {
                    typeId = "6b724908-ef14-45cf-84f8-768b5384da45",
                    name = "Scrum",
                    description = "This template is for teams who follow the Scrum framework.",
                    isEnabled = true,
                    isDefault = false,
                    customizationType = "System"
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(responseData);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _projectPlugin.GetProcessTemplatesAsync(organization, token);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("Available process templates:");
        result.Should().Contain("• **Agile** (ID: adcc42ab-9882-485e-a3ed-7678f01f66bc)");
        result.Should().Contain("• **CMMI** (ID: 27450541-8e31-4150-9947-dc59f998fc01)");
        result.Should().Contain("• **Scrum** (ID: 6b724908-ef14-45cf-84f8-768b5384da45)");
        result.Should().Contain("(Default)"); // Should show default marker for Agile
    }

    [Fact]
    public async Task GetProcessTemplatesAsync_ShouldReturnError_WhenApiCallFails()
    {
        // Arrange
        var organization = "test-org";
        var token = "invalid-token";
        
        var httpResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("Unauthorized", System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _projectPlugin.GetProcessTemplatesAsync(organization, token);

        // Assert
        result.Should().StartWith("Error: Failed to get process templates");
        result.Should().Contain("Status: Unauthorized");
    }

    [Fact]
    public async Task CreateProjectAsync_ShouldReturnSuccess_WhenApiCallSucceeds()
    {
        // Arrange
        var organization = "test-org";
        var projectName = "Test Project";
        var description = "Test project description";
        var processTemplateId = "adcc42ab-9882-485e-a3ed-7678f01f66bc";
        var token = "test-token";
        var visibility = "Private";

        var responseData = new
        {
            id = "12345678-1234-1234-1234-123456789012",
            name = projectName,
            description = description,
            url = $"https://dev.azure.com/{organization}/{projectName}",
            status = "wellFormed"
        };

        var jsonResponse = JsonSerializer.Serialize(responseData);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _projectPlugin.CreateProjectAsync(organization, projectName, description, processTemplateId, token, visibility);

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("✅ Project 'Test Project' created successfully!");
        result.Should().Contain("• **Project ID**: 12345678-1234-1234-1234-123456789012");
        result.Should().Contain("• **Organization**: test-org");
        result.Should().Contain("• **Visibility**: Private");
        result.Should().Contain("• **Source Control**: Git");
        result.Should().Contain("• **Process Template ID**: adcc42ab-9882-485e-a3ed-7678f01f66bc");
        result.Should().Contain("• **Description**: Test project description");
    }

    [Fact]
    public async Task CreateProjectAsync_ShouldReturnError_WhenProjectNameIsEmpty()
    {
        // Arrange
        var organization = "test-org";
        var projectName = "";
        var description = "Test project description";
        var processTemplateId = "adcc42ab-9882-485e-a3ed-7678f01f66bc";
        var token = "test-token";

        // Act
        var result = await _projectPlugin.CreateProjectAsync(organization, projectName, description, processTemplateId, token);

        // Assert
        result.Should().Be("Error: Project name is required.");
    }

    [Fact]
    public async Task CreateProjectAsync_ShouldReturnError_WhenProcessTemplateIdIsEmpty()
    {
        // Arrange
        var organization = "test-org";
        var projectName = "Test Project";
        var description = "Test project description";
        var processTemplateId = "";
        var token = "test-token";

        // Act
        var result = await _projectPlugin.CreateProjectAsync(organization, projectName, description, processTemplateId, token);

        // Assert
        result.Should().Be("Error: Process template ID is required. Use get_process_templates to find available template IDs.");
    }

    [Fact]
    public async Task CreateProjectAsync_ShouldReturnError_WhenVisibilityIsInvalid()
    {
        // Arrange
        var organization = "test-org";
        var projectName = "Test Project";
        var description = "Test project description";
        var processTemplateId = "adcc42ab-9882-485e-a3ed-7678f01f66bc";
        var token = "test-token";
        var visibility = "Invalid";

        // Act
        var result = await _projectPlugin.CreateProjectAsync(organization, projectName, description, processTemplateId, token, visibility);

        // Assert
        result.Should().Be("Error: Invalid visibility value. Use 'Private' or 'Public'.");
    }

    [Fact]
    public async Task FindProcessTemplateAsync_ShouldReturnExactMatch_WhenTemplateExists()
    {
        // Arrange
        var organization = "test-org";
        var templateName = "Agile";
        var token = "test-token";
        
        var responseData = new
        {
            count = 3,
            value = new[]
            {
                new
                {
                    typeId = "adcc42ab-9882-485e-a3ed-7678f01f66bc",
                    name = "Agile",
                    description = "This template is flexible and will work great for most teams using Agile planning methods, including those practicing Scrum.",
                    isEnabled = true,
                    isDefault = true,
                    customizationType = "System"
                },
                new
                {
                    typeId = "6b724908-ef14-45cf-84f8-768b5384da45",
                    name = "Scrum",
                    description = "This template is for teams who follow the Scrum framework.",
                    isEnabled = true,
                    isDefault = false,
                    customizationType = "System"
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(responseData);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _projectPlugin.FindProcessTemplateAsync(organization, templateName, token);

        // Assert
        result.Should().Be("Found exact match: 'Agile' (ID: adcc42ab-9882-485e-a3ed-7678f01f66bc)");
    }

    [Fact]
    public async Task FindProcessTemplateAsync_ShouldReturnNoMatch_WhenTemplateDoesNotExist()
    {
        // Arrange
        var organization = "test-org";
        var templateName = "NonExistent";
        var token = "test-token";
        
        var responseData = new
        {
            count = 2,
            value = new[]
            {
                new
                {
                    typeId = "adcc42ab-9882-485e-a3ed-7678f01f66bc",
                    name = "Agile",
                    description = "This template is flexible and will work great for most teams using Agile planning methods, including those practicing Scrum.",
                    isEnabled = true,
                    isDefault = true,
                    customizationType = "System"
                },
                new
                {
                    typeId = "6b724908-ef14-45cf-84f8-768b5384da45",
                    name = "Scrum",
                    description = "This template is for teams who follow the Scrum framework.",
                    isEnabled = true,
                    isDefault = false,
                    customizationType = "System"
                }
            }
        };

        var jsonResponse = JsonSerializer.Serialize(responseData);
        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(httpResponse);

        // Act
        var result = await _projectPlugin.FindProcessTemplateAsync(organization, templateName, token);

        // Assert
        result.Should().StartWith("No process template found matching 'NonExistent'");
        result.Should().Contain("Available templates: Agile, Scrum");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}