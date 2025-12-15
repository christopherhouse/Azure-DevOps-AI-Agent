using AzureDevOpsAI.Backend.Endpoints;
using Microsoft.Extensions.Options;

namespace AzureDevOpsAI.Backend.Configuration;

/// <summary>
/// Extension methods for mapping application endpoints.
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Maps all application endpoints including health checks, root, and API endpoints.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The web application for method chaining.</returns>
    public static WebApplication MapApplicationEndpoints(this WebApplication app, IConfiguration configuration)
    {
        var securitySettings = configuration.GetSection("Security").Get<SecuritySettings>();

        // Health check endpoint with detailed status
        app.MapGet("/health", async (Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckService healthCheckService) =>
        {
            var appSettings = app.Services.GetService<IOptions<AppSettings>>()?.Value;
            var healthReport = await healthCheckService.CheckHealthAsync();
            
            var response = new
            {
                status = healthReport.Status.ToString().ToLower(),
                message = "Azure DevOps AI Agent Backend is running",
                version = appSettings?.AppVersion ?? "1.0.0",
                environment = appSettings?.Environment ?? "development",
                checks = healthReport.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString().ToLower(),
                    description = e.Value.Description ?? string.Empty,
                    duration = e.Value.Duration.TotalMilliseconds
                })
            };

            return healthReport.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy
                ? Results.Ok(response)
                : Results.Json(response, statusCode: 503);
        })
        .WithName("HealthCheck")
        .WithSummary("Health check endpoint with dependency status")
        .WithOpenApi()
        .AllowAnonymous();

        // Root endpoint
        app.MapGet("/", () =>
        {
            var appSettings = app.Services.GetService<IOptions<AppSettings>>()?.Value;
            return Results.Ok(new
            {
                message = "Azure DevOps AI Agent Backend API",
                version = appSettings?.AppVersion ?? "1.0.0",
                docs_url = app.Environment.IsDevelopment() || securitySettings?.DisableAuth == true
                    ? "/docs"
                    : "Documentation disabled in production"
            });
        })
        .WithName("Root")
        .WithSummary("Root endpoint")
        .WithOpenApi()
        .AllowAnonymous();

        // Map API endpoints
        app.MapChatEndpoints();

        // Map health checks
        app.MapHealthChecks("/health/ready");

        return app;
    }
}
