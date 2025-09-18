using Microsoft.ApplicationInsights;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using AzureDevOpsAI.Backend.Configuration;

namespace AzureDevOpsAI.Backend.Tests.Configuration;

public class ApplicationInsightsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApplicationInsightsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void ApplicationInsights_ShouldBeRegistered_WhenConnectionStringIsProvided()
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
        var telemetryClient = scope.ServiceProvider.GetService<TelemetryClient>();

        // Assert
        Assert.NotNull(telemetryClient);
        Assert.NotNull(telemetryClient.TelemetryConfiguration);
        Assert.Equal("InstrumentationKey=test-key;IngestionEndpoint=https://test.in.applicationinsights.azure.com/", 
            telemetryClient.TelemetryConfiguration.ConnectionString);
    }

    [Fact]
    public void ApplicationInsights_ShouldBeRegistered_WhenConnectionStringIsEmpty()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApplicationInsights:ConnectionString"] = ""
                });
            });
        });

        // Act
        using var scope = factory.Services.CreateScope();
        var telemetryClient = scope.ServiceProvider.GetService<TelemetryClient>();

        // Assert
        // When no connection string is provided, Application Insights should still be available
        Assert.NotNull(telemetryClient);
        Assert.NotNull(telemetryClient.TelemetryConfiguration);
    }

    [Fact]
    public void ApplicationInsights_ShouldBeRegistered_WhenConnectionStringIsNull()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ApplicationInsights:ConnectionString"] = null
                });
            });
        });

        // Act
        using var scope = factory.Services.CreateScope();
        var telemetryClient = scope.ServiceProvider.GetService<TelemetryClient>();

        // Assert
        // When no connection string is provided, Application Insights should still be available
        Assert.NotNull(telemetryClient);
        Assert.NotNull(telemetryClient.TelemetryConfiguration);
    }
}