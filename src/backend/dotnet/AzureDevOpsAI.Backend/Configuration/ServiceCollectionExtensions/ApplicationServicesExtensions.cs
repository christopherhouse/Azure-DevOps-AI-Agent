using AzureDevOpsAI.Backend.Services;
using Microsoft.Extensions.Options;

namespace AzureDevOpsAI.Backend.Configuration;

/// <summary>
/// Extension methods for configuring application services (CosmosDB, AI services, Azure DevOps API).
/// </summary>
public static class ApplicationServicesExtensions
{
    /// <summary>
    /// Adds CosmosDB, AI services, and Azure DevOps API services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Read ManagedIdentityClientId from configuration for AzureDevOpsApiService
        var managedIdentityClientIdForDevOps = configuration["ManagedIdentityClientId"];

        // Read Azure DevOps authentication settings
        var azureDevOpsSettings = configuration.GetSection("AzureDevOps").Get<AzureDevOpsSettings>() ?? new AzureDevOpsSettings();

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

        // Add AI services
        services.AddHttpClient();
        services.AddScoped<IAIService, AIService>();
        services.AddScoped<IAzureDevOpsApiService>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var logger = sp.GetRequiredService<ILogger<AzureDevOpsApiService>>();
            return new AzureDevOpsApiService(httpClient, logger, managedIdentityClientIdForDevOps, azureDevOpsSettings.Pat, azureDevOpsSettings.UsePat);
        });

        return services;
    }
}
