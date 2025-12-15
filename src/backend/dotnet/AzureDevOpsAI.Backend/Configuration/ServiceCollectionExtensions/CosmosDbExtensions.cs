using AzureDevOpsAI.Backend.Services;
using Microsoft.Extensions.Options;

namespace AzureDevOpsAI.Backend.Configuration;

/// <summary>
/// Extension methods for configuring CosmosDB services.
/// </summary>
public static class CosmosDbExtensions
{
    /// <summary>
    /// Adds CosmosDB service to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddCosmosDbService(this IServiceCollection services)
    {
        // Add CosmosDB service - required, no fallback to in-memory storage
        // The validation happens at service construction time to allow tests to override
        services.AddSingleton<ICosmosDbService>(sp =>
        {
            var cosmosDbSettings = sp.GetRequiredService<IOptions<CosmosDbSettings>>().Value;
            if (string.IsNullOrEmpty(cosmosDbSettings.Endpoint))
            {
                throw new InvalidOperationException("CosmosDB endpoint is required. Configure CosmosDb:Endpoint in appsettings.json or environment variables.");
            }
            var logger = sp.GetRequiredService<ILogger<CosmosDbService>>();
            return new CosmosDbService(sp.GetRequiredService<IOptions<CosmosDbSettings>>(), logger);
        });

        return services;
    }
}
