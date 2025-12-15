using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using AzureDevOpsAI.Backend.Configuration;
using AzureDevOpsAI.Backend.Services;

namespace AzureDevOpsAI.Backend.Tests.Configuration;

/// <summary>
/// Tests to verify that Program.cs refactoring maintains all functionality.
/// Validates that all extension methods properly register services and configuration.
/// </summary>
public class ProgramRefactoringTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;

    public ProgramRefactoringTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Program_ShouldRegisterLoggingServices()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger<ProgramRefactoringTests>();

        // Assert
        Assert.NotNull(loggerFactory);
        Assert.NotNull(logger);
    }

    [Fact]
    public void Program_ShouldRegisterAllSettingsServices()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var appSettings = scope.ServiceProvider.GetService<IOptions<AppSettings>>();
        var azureAuthSettings = scope.ServiceProvider.GetService<IOptions<AzureAuthSettings>>();
        var azureDevOpsSettings = scope.ServiceProvider.GetService<IOptions<AzureDevOpsSettings>>();
        var applicationInsightsSettings = scope.ServiceProvider.GetService<IOptions<ApplicationInsightsSettings>>();
        var securitySettings = scope.ServiceProvider.GetService<IOptions<SecuritySettings>>();
        var azureOpenAISettings = scope.ServiceProvider.GetService<IOptions<AzureOpenAISettings>>();
        var cosmosDbSettings = scope.ServiceProvider.GetService<IOptions<CosmosDbSettings>>();

        // Assert
        Assert.NotNull(appSettings);
        Assert.NotNull(azureAuthSettings);
        Assert.NotNull(azureDevOpsSettings);
        Assert.NotNull(applicationInsightsSettings);
        Assert.NotNull(securitySettings);
        Assert.NotNull(azureOpenAISettings);
        Assert.NotNull(cosmosDbSettings);
    }

    [Fact]
    public void Program_ShouldRegisterApplicationServices()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var cosmosDbService = scope.ServiceProvider.GetService<ICosmosDbService>();
        var aiService = scope.ServiceProvider.GetService<IAIService>();
        var azureDevOpsApiService = scope.ServiceProvider.GetService<IAzureDevOpsApiService>();
        var httpClientFactory = scope.ServiceProvider.GetService<IHttpClientFactory>();

        // Assert
        Assert.NotNull(cosmosDbService);
        Assert.NotNull(aiService);
        Assert.NotNull(azureDevOpsApiService);
        Assert.NotNull(httpClientFactory);
    }

    [Fact]
    public void Program_ShouldRegisterApplicationInsights()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var telemetryClient = scope.ServiceProvider.GetService<Microsoft.ApplicationInsights.TelemetryClient>();

        // Assert
        Assert.NotNull(telemetryClient);
    }

    [Fact]
    public void Program_ShouldRegisterHealthCheckServices()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var healthCheckService = scope.ServiceProvider.GetService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>();

        // Assert
        Assert.NotNull(healthCheckService);
    }

    [Fact]
    public async Task Program_ShouldMapHealthCheckEndpoint()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Program_ShouldMapRootEndpoint()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Azure DevOps AI Agent Backend API", content);
    }

    [Fact]
    public async Task Program_ShouldMapHealthReadyEndpoint()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert - Should return OK or ServiceUnavailable, not NotFound
        Assert.NotEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Program_ShouldHaveSwaggerDocumentation()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Swagger should be available in test environment (DisableAuth = true)
        var response = await client.GetAsync("/docs/index.html");

        // Assert - Should return OK or redirect, not NotFound
        Assert.NotEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public void Program_ShouldKeepOriginalFunctionality()
    {
        // This test verifies that the refactored Program.cs still creates a valid web application
        // by checking that the application can be built and basic services are registered

        // Arrange & Act - Factory creation already validates this
        var services = _factory.Services;

        // Assert - Verify critical services are present
        using var scope = services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        
        // Core services
        Assert.NotNull(serviceProvider.GetService<ILoggerFactory>());
        Assert.NotNull(serviceProvider.GetService<IConfiguration>());
        
        // Application-specific services
        Assert.NotNull(serviceProvider.GetService<ICosmosDbService>());
        Assert.NotNull(serviceProvider.GetService<IAIService>());
        Assert.NotNull(serviceProvider.GetService<IAzureDevOpsApiService>());
        
        // Health check services
        Assert.NotNull(serviceProvider.GetService<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService>());
        
        // Configuration options
        Assert.NotNull(serviceProvider.GetService<IOptions<AppSettings>>());
        Assert.NotNull(serviceProvider.GetService<IOptions<SecuritySettings>>());
    }
}
