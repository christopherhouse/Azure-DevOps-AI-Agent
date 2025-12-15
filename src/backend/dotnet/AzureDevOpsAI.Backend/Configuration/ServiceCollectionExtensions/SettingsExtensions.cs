namespace AzureDevOpsAI.Backend.Configuration;

/// <summary>
/// Extension methods for configuring application settings.
/// </summary>
public static class SettingsExtensions
{
    /// <summary>
    /// Adds and configures all application settings from configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddAppSettings(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure all settings sections
        services.Configure<AppSettings>(configuration.GetSection("App"));
        services.Configure<AzureAuthSettings>(configuration.GetSection("AzureAuth"));
        services.Configure<AzureDevOpsSettings>(configuration.GetSection("AzureDevOps"));
        services.Configure<ApplicationInsightsSettings>(configuration.GetSection("ApplicationInsights"));
        services.Configure<SecuritySettings>(configuration.GetSection("Security"));
        
        // Configure AzureOpenAI settings with ManagedIdentityClientId override
        services.Configure<AzureOpenAISettings>(options =>
        {
            configuration.GetSection("AzureOpenAI").Bind(options);

            // Override ClientId with ManagedIdentityClientId environment variable if provided
            var managedIdentityClientId = configuration["ManagedIdentityClientId"];
            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                options.ClientId = managedIdentityClientId;
            }
        });
        
        // Configure CosmosDb settings with ManagedIdentityClientId override
        services.Configure<CosmosDbSettings>(options =>
        {
            configuration.GetSection("CosmosDb").Bind(options);

            // Override ClientId with ManagedIdentityClientId environment variable if provided
            var managedIdentityClientId = configuration["ManagedIdentityClientId"];
            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                options.ClientId = managedIdentityClientId;
            }
        });

        return services;
    }
}
