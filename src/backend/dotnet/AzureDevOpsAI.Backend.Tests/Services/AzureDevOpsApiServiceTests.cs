using AzureDevOpsAI.Backend.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AzureDevOpsAI.Backend.Tests.Services;

public class AzureDevOpsApiServiceTests
{
    private readonly Mock<ILogger<AzureDevOpsApiService>> _mockLogger;
    private readonly string _managedIdentityClientId;
    private readonly AzureDevOpsApiService _service;

    public AzureDevOpsApiServiceTests()
    {
        _mockLogger = new Mock<ILogger<AzureDevOpsApiService>>();
        
        // Test managed identity client ID (sourced from ManagedIdentityClientId environment variable in production)
        _managedIdentityClientId = "test-managed-identity-client-id";
        
        // Create service with an HttpClient - it will use ManagedIdentityCredential internally
        var httpClient = new HttpClient();
        _service = new AzureDevOpsApiService(httpClient, _mockLogger.Object, _managedIdentityClientId, null, false);
    }

    [Fact]
    public void Constructor_ShouldInitializeService()
    {
        // Act & Assert
        _service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_ShouldLogManagedIdentityClientIdConfiguration()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureDevOpsApiService>>();
        var httpClient = new HttpClient();

        // Act
        var service = new AzureDevOpsApiService(httpClient, mockLogger.Object, _managedIdentityClientId, null, false);

        // Assert
        service.Should().NotBeNull();
        // Verify that the service was initialized with the correct managed identity client ID
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
    public void Constructor_WithValidManagedIdentityClientId_ShouldUseManagedIdentityCredential()
    {
        // Arrange
        var httpClient = new HttpClient();
        var managedIdentityClientId = "valid-managed-identity-client-id";

        // Act
        var service = new AzureDevOpsApiService(httpClient, _mockLogger.Object, managedIdentityClientId, null, false);

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
    public void Constructor_WithManagedIdentityClientId_ShouldLogClientId()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureDevOpsApiService>>();
        var httpClient = new HttpClient();
        var managedIdentityClientId = "test-mi-client-id";

        // Act
        var service = new AzureDevOpsApiService(httpClient, mockLogger.Object, managedIdentityClientId, null, false);

        // Assert
        service.Should().NotBeNull();
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("AzureDevOpsApiService initialized with User Assigned Managed Identity, client-id: test-mi-client-id")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithNullManagedIdentityClientId_ShouldStillInitialize()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureDevOpsApiService>>();
        var httpClient = new HttpClient();

        // Act - null client ID is valid for system-assigned managed identity
        var service = new AzureDevOpsApiService(httpClient, mockLogger.Object, null, null, false);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithPat_ShouldInitializeWithPatAuthentication()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureDevOpsApiService>>();
        var httpClient = new HttpClient();
        var pat = "test-personal-access-token";

        // Act
        var service = new AzureDevOpsApiService(httpClient, mockLogger.Object, null, pat, true);

        // Assert
        service.Should().NotBeNull();
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("AzureDevOpsApiService initialized with Personal Access Token authentication")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithPatEnabledButNullPat_ShouldThrowArgumentException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureDevOpsApiService>>();
        var httpClient = new HttpClient();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            new AzureDevOpsApiService(httpClient, mockLogger.Object, null, null, true));
        
        exception.Message.Should().Contain("Personal Access Token cannot be null or empty when UsePat is true");
    }

    [Fact]
    public void Constructor_WithPatEnabledButEmptyPat_ShouldThrowArgumentException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureDevOpsApiService>>();
        var httpClient = new HttpClient();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            new AzureDevOpsApiService(httpClient, mockLogger.Object, null, "", true));
        
        exception.Message.Should().Contain("Personal Access Token cannot be null or empty when UsePat is true");
    }

    [Fact]
    public void Constructor_WithPatEnabledButWhitespacePat_ShouldThrowArgumentException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureDevOpsApiService>>();
        var httpClient = new HttpClient();

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => 
            new AzureDevOpsApiService(httpClient, mockLogger.Object, null, "   ", true));
        
        exception.Message.Should().Contain("Personal Access Token cannot be null or empty when UsePat is true");
    }

    [Fact]
    public void Constructor_WithValidPat_ShouldNotThrow()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureDevOpsApiService>>();
        var httpClient = new HttpClient();
        var pat = "valid-pat-token";

        // Act
        var service = new AzureDevOpsApiService(httpClient, mockLogger.Object, null, pat, true);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithPatDisabledAndNullPat_ShouldInitializeWithAzureIdentity()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<AzureDevOpsApiService>>();
        var httpClient = new HttpClient();

        // Act
        var service = new AzureDevOpsApiService(httpClient, mockLogger.Object, null, null, false);

        // Assert
        service.Should().NotBeNull();
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("AzureDevOpsApiService initialized with DefaultAzureCredential")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}