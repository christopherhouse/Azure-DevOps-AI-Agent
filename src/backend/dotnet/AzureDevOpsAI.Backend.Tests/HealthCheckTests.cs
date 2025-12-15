using Microsoft.AspNetCore.Mvc.Testing;
using FluentAssertions;
using System.Net;
using System.Text.Json;

namespace AzureDevOpsAI.Backend.Tests;

public class HealthCheckTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly TestWebApplicationFactory<Program> _factory;

    public HealthCheckTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_ShouldReturnHealthStatusWithAppInfo()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        // Status can be either OK (all healthy) or ServiceUnavailable (some unhealthy) in tests
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("status");
        content.Should().Contain("Azure DevOps AI Agent Backend is running");
        content.Should().Contain("version");
    }

    [Fact]
    public async Task Health_ShouldIncludeDependencyChecks()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        // Status can be either OK or ServiceUnavailable in tests
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        
        // Verify the response contains checks array
        jsonDoc.RootElement.TryGetProperty("checks", out var checks).Should().BeTrue();
        checks.GetArrayLength().Should().BeGreaterThan(0);
        
        // Verify CosmosDB health check is present
        var checkNames = checks.EnumerateArray().Select(c => c.GetProperty("name").GetString()).ToList();
        checkNames.Should().Contain("cosmosdb");
        checkNames.Should().Contain("azureopenai");
    }

    [Fact]
    public async Task Health_ChecksShouldHaveStatusAndDescription()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        // Status can be either OK or ServiceUnavailable in tests
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        
        var content = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(content);
        var checks = jsonDoc.RootElement.GetProperty("checks");
        
        // Verify each check has required properties
        foreach (var check in checks.EnumerateArray())
        {
            check.TryGetProperty("name", out _).Should().BeTrue();
            check.TryGetProperty("status", out var status).Should().BeTrue();
            check.TryGetProperty("description", out _).Should().BeTrue();
            check.TryGetProperty("duration", out _).Should().BeTrue();
            
            // Status should be one of: healthy, degraded, unhealthy
            var statusStr = status.GetString();
            statusStr.Should().BeOneOf("healthy", "degraded", "unhealthy");
        }
    }

    [Fact]
    public async Task HealthReady_ShouldReturnHealthStatus()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert
        // Status can be either OK (all healthy) or ServiceUnavailable (some unhealthy) in tests
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        
        var content = await response.Content.ReadAsStringAsync();
        // Response should contain a health status string (Healthy, Degraded, or Unhealthy)
        content.Should().MatchRegex("^(Healthy|Degraded|Unhealthy)$");
    }

    [Fact]
    public async Task RootEndpoint_ShouldReturnApiInfo()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Azure DevOps AI Agent Backend API");
        content.Should().Contain("version");
        content.Should().Contain("docs_url");
    }
}