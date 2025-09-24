using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.ApplicationInsights;

namespace AzureDevOpsAI.Backend.Tests.Configuration;

/// <summary>
/// Tests to verify that logging is properly configured for both console and Application Insights.
/// </summary>
public class ApplicationInsightsLoggingTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;

    public ApplicationInsightsLoggingTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void ApplicationInsightsLogging_ShouldWork_WhenConnectionStringProvided()
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
        var logger = loggerFactory.CreateLogger("TestCategory");
        var telemetryClient = scope.ServiceProvider.GetService<TelemetryClient>();

        // Assert
        Assert.NotNull(loggerFactory);
        Assert.NotNull(logger);
        Assert.NotNull(telemetryClient);
        Assert.Equal("InstrumentationKey=test-key;IngestionEndpoint=https://test.in.applicationinsights.azure.com/", 
            telemetryClient.TelemetryConfiguration.ConnectionString);

        // Test that logging works without exceptions
        logger.LogInformation("Test log message to AI");
        logger.LogWarning("Test warning message to AI");
        logger.LogError("Test error message to AI");
    }

    [Fact]
    public void ApplicationInsightsLogging_ShouldWork_WhenConnectionStringMissing()
    {
        // Arrange - default factory without Application Insights connection string
        
        // Act
        using var scope = _factory.Services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("TestCategory");
        var telemetryClient = scope.ServiceProvider.GetService<TelemetryClient>();

        // Assert
        Assert.NotNull(loggerFactory);
        Assert.NotNull(logger);
        Assert.NotNull(telemetryClient); // Telemetry client should still be available
        
        // Test that logging works even without Application Insights connection string
        // This verifies our fix - logging should work consistently
        logger.LogInformation("Test log message without AI connection");
        logger.LogWarning("Test warning message without AI connection");
        logger.LogError("Test error message without AI connection");
    }

    [Fact]  
    public void ApplicationInsightsTelemetry_ShouldBeAvailable_EvenWithoutConnectionString()
    {
        // Arrange - default factory without Application Insights connection string
        
        // Act
        using var scope = _factory.Services.CreateScope();
        var telemetryClient = scope.ServiceProvider.GetService<TelemetryClient>();

        // Assert - This should pass based on existing ApplicationInsightsTests
        Assert.NotNull(telemetryClient);
        Assert.NotNull(telemetryClient.TelemetryConfiguration);
    }

    [Fact]
    public void Logging_ShouldWorkForAllCategories_WithAndWithoutApplicationInsights()
    {
        // Test both scenarios in one test
        var scenarios = new[]
        {
            new { Name = "With AI Connection", ConnectionString = "InstrumentationKey=test-key;IngestionEndpoint=https://test.in.applicationinsights.azure.com/" },
            new { Name = "Without AI Connection", ConnectionString = (string?)null }
        };

        foreach (var scenario in scenarios)
        {
            // Arrange
            var factory = scenario.ConnectionString != null
                ? _factory.WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((context, config) =>
                    {
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["ApplicationInsights:ConnectionString"] = scenario.ConnectionString
                        });
                    });
                })
                : _factory;

            // Act & Assert
            using var scope = factory.Services.CreateScope();
            var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            
            // Test different logger categories that are used in the application
            var loggerCategories = new[]
            {
                "AzureDevOpsAI.Backend.Services.AIService",
                "AzureDevOpsAI.Backend.Middleware.RequestLoggingMiddleware",
                "AzureDevOpsAI.Backend.Middleware.ErrorHandlingMiddleware",
                "ChatEndpoints",
                "ProjectEndpoints",
                "WorkItemEndpoints"
            };

            foreach (var category in loggerCategories)
            {
                var logger = loggerFactory.CreateLogger(category);
                Assert.NotNull(logger);
                
                // Test that logging works for all categories in both scenarios
                logger.LogInformation($"Test log from {category} - {scenario.Name}");
                logger.LogWarning($"Test warning from {category} - {scenario.Name}");
            }
        }
    }
}