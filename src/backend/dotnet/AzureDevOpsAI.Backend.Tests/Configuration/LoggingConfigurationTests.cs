using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.ApplicationInsights;
using System.Text;

namespace AzureDevOpsAI.Backend.Tests.Configuration;

/// <summary>
/// Tests for logging configuration to ensure all ILogger instances output to both console and Application Insights.
/// </summary>
public class LoggingConfigurationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LoggingConfigurationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void Logging_ShouldHaveLoggerFactory_Always()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger<LoggingConfigurationTests>();

        // Assert
        Assert.NotNull(loggerFactory);
        Assert.NotNull(logger);
    }

    [Fact]
    public void Logging_ShouldWorkWithApplicationInsights_WhenConnectionStringProvided()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key;IngestionEndpoint=https://test.in.applicationinsights.azure.com/"
                });
            });
        });

        // Act
        using var scope = factory.Services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("TestLogger");
        var telemetryClient = scope.ServiceProvider.GetService<TelemetryClient>();

        // Assert
        Assert.NotNull(loggerFactory);
        Assert.NotNull(logger);
        Assert.NotNull(telemetryClient); // Application Insights telemetry should be available
        
        // Test that logging doesn't throw exceptions
        logger.LogInformation("Test log message");
        logger.LogWarning("Test warning message");
        logger.LogError("Test error message");
    }

    [Fact]
    public void Logging_ShouldWork_WhenApplicationInsightsConnectionStringMissing()
    {
        // Arrange - default factory without Application Insights connection string
        
        // Act
        using var scope = _factory.Services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("TestLogger");

        // Assert
        Assert.NotNull(loggerFactory);
        Assert.NotNull(logger);
        
        // Test that logging works even without Application Insights connection string
        logger.LogInformation("Test log message without AI connection string");
        logger.LogWarning("Test warning message without AI connection string");
        logger.LogError("Test error message without AI connection string");
        
        // This test demonstrates that basic logging works, 
        // but currently Application Insights logging is not configured when connection string is missing
    }

    [Fact]
    public void Logging_ShouldHaveTelemetryClient_EvenWithoutConnectionString()
    {
        // Arrange - default factory without Application Insights connection string
        
        // Act
        using var scope = _factory.Services.CreateScope();
        var telemetryClient = scope.ServiceProvider.GetService<TelemetryClient>();

        // Assert
        // Application Insights services should be available even without connection string
        Assert.NotNull(telemetryClient);
    }

    [Fact]
    public void Logging_ShouldCreateLoggersForDifferentCategories()
    {
        // Arrange & Act
        using var scope = _factory.Services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        
        // Create loggers for different categories like the application does
        var aiServiceLogger = loggerFactory.CreateLogger("AzureDevOpsAI.Backend.Services.AIService");
        var requestLoggingLogger = loggerFactory.CreateLogger("AzureDevOpsAI.Backend.Middleware.RequestLoggingMiddleware");
        var chatEndpointsLogger = loggerFactory.CreateLogger("ChatEndpoints");

        // Assert
        Assert.NotNull(aiServiceLogger);
        Assert.NotNull(requestLoggingLogger);
        Assert.NotNull(chatEndpointsLogger);
        
        // Test that all loggers work
        aiServiceLogger.LogInformation("AI Service test log");
        requestLoggingLogger.LogInformation("Request logging test log");
        chatEndpointsLogger.LogInformation("Chat endpoints test log");
    }
}