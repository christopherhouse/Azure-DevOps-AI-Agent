namespace AzureDevOpsAI.Backend.Configuration;

/// <summary>
/// Extension methods for configuring health checks.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adds health check services for application dependencies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddHealthCheckServices(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<AzureDevOpsAI.Backend.HealthChecks.CosmosDbHealthCheck>(
                "cosmosdb",
                tags: new[] { "db", "cosmosdb", "ready" })
            .AddCheck<AzureDevOpsAI.Backend.HealthChecks.AzureOpenAIHealthCheck>(
                "azureopenai",
                tags: new[] { "ai", "azureopenai", "ready" });

        return services;
    }
}
