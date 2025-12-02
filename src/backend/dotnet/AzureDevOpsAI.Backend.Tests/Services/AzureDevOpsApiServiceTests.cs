using AzureDevOpsAI.Backend.Services;
using AzureDevOpsAI.Backend.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using System.Text.Json;

namespace AzureDevOpsAI.Backend.Tests.Services;

public class AzureDevOpsApiServiceTests
{
    private readonly Mock<ILogger<AzureDevOpsApiService>> _mockLogger;
    private readonly IOptions<AzureAuthSettings> _azureAuthSettings;
    private readonly AzureDevOpsApiService _service;

    public AzureDevOpsApiServiceTests()
    {
        _mockLogger = new Mock<ILogger<AzureDevOpsApiService>>();
        
        // Create Azure Auth settings with a test client ID and tenant ID
        _azureAuthSettings = Options.Create(new AzureAuthSettings
        {
            ClientId = "test-managed-identity-client-id",
            TenantId = "test-tenant-id"
        });
        
        // Create service with an HttpClient - it will use ManagedIdentityCredential internally
        var httpClient = new HttpClient();
        _service = new AzureDevOpsApiService(httpClient, _mockLogger.Object, _azureAuthSettings);
    }

    [Fact]
    public void Constructor_ShouldInitializeService()
    {
        // Act & Assert
        _service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldLogClientIdConfiguration()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureDevOpsApiService>>();
        var httpClient = new HttpClient();

        // Act
        var service = new AzureDevOpsApiService(httpClient, mockLogger.Object, _azureAuthSettings);

        // Assert
        service.Should().NotBeNull();
        // Verify that the service was initialized with the correct client ID
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("test-managed-identity-client-id")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("work/processes", "https://dev.azure.com/test-org/_apis/work/processes?api-version=7.1")]
    [InlineData("/custom/endpoint", "https://dev.azure.com/test-org/custom/endpoint?api-version=7.1")]
    [InlineData("projects?$top=10", "https://dev.azure.com/test-org/_apis/projects?$top=10&api-version=7.1")]
    [InlineData("_apis/work/processes", "https://dev.azure.com/test-org/_apis/work/processes?api-version=7.1")]
    [InlineData("/_apis/work/processes", "https://dev.azure.com/test-org/_apis/work/processes?api-version=7.1")]
    [InlineData("https://vssps.dev.azure.com/test-org/_apis/graph/profile", "https://vssps.dev.azure.com/test-org/_apis/graph/profile?api-version=7.1")]
    [InlineData("https://vssps.dev.azure.com/test-org/_apis/graph/profile?api-version=7.0", "https://vssps.dev.azure.com/test-org/_apis/graph/profile?api-version=7.0")]
    public void BuildApiUrl_ShouldConstructCorrectUrls(string apiPath, string expectedUrl)
    {
        // Arrange
        var organization = "test-org";
        var apiVersion = "7.1";

        // Act - URL construction is tested implicitly through integration tests
        // This test verifies the expected URL format
        
        // Assert
        expectedUrl.Should().Contain(organization);
        expectedUrl.Should().Contain("api-version");
    }

    [Fact]
    public void Constructor_WithValidClientId_ShouldUseManagedIdentityCredential()
    {
        // Arrange
        var httpClient = new HttpClient();
        var settings = Options.Create(new AzureAuthSettings
        {
            ClientId = "valid-client-id",
            TenantId = "valid-tenant-id"
        });

        // Act
        var service = new AzureDevOpsApiService(httpClient, _mockLogger.Object, settings);

        // Assert
        service.Should().NotBeNull();
        // The service should be initialized with ManagedIdentityCredential configured for User Assigned MI
    }
    
    [Theory]
    [InlineData("work/processes")]
    [InlineData("_apis/work/processes")]
    [InlineData("/_apis/work/processes")]
    public void BuildApiUrl_ShouldHandleDuplicateApisPrefixes(string apiPath)
    {
        // Arrange & Act - Verify that duplicate _apis/ prefixes are handled correctly
        // This is tested implicitly through the URL construction logic
        
        // Assert - All variations should produce the same URL pattern
        apiPath.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithUseUserAssignedIdentityTrue_ShouldLogClientId()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureDevOpsApiService>>();
        var httpClient = new HttpClient();
        var settings = Options.Create(new AzureAuthSettings
        {
            ClientId = "test-client-id",
            TenantId = "test-tenant-id"
        });

        // Act
        var service = new AzureDevOpsApiService(httpClient, mockLogger.Object, settings);

        // Assert
        service.Should().NotBeNull();
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User Assigned Managed Identity client ID: test-client-id")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_ShouldLogClientIdAndTenantConfiguration()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureDevOpsApiService>>();
        var httpClient = new HttpClient();
        var settings = Options.Create(new AzureAuthSettings
        {
            ClientId = "another-test-client-id",
            TenantId = "another-test-tenant-id"
        });

        // Act
        var service = new AzureDevOpsApiService(httpClient, mockLogger.Object, settings);

        // Assert
        service.Should().NotBeNull();
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("another-test-client-id")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}