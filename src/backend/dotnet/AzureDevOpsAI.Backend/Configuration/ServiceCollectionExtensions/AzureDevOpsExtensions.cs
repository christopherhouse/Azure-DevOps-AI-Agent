using AzureDevOpsAI.Backend.Services;

namespace AzureDevOpsAI.Backend.Configuration;

/// <summary>
/// Extension methods for configuring Azure DevOps API services.
/// </summary>
public static class AzureDevOpsExtensions
{
    /// <summary>
    /// Adds Azure DevOps API service to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddAzureDevOpsApiService(this IServiceCollection services, IConfiguration configuration)
    {
        // Read ManagedIdentityClientId from configuration for AzureDevOpsApiService
        var managedIdentityClientIdForDevOps = configuration["ManagedIdentityClientId"];

        // Read Azure DevOps authentication settings
        var azureDevOpsSettings = configuration.GetSection("AzureDevOps").Get<AzureDevOpsSettings>() ?? new AzureDevOpsSettings();

        services.AddScoped<IAzureDevOpsApiService>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
            var logger = sp.GetRequiredService<ILogger<AzureDevOpsApiService>>();
            return new AzureDevOpsApiService(httpClient, logger, managedIdentityClientIdForDevOps, azureDevOpsSettings.Pat, azureDevOpsSettings.UsePat);
        });

        return services;
    }
}
