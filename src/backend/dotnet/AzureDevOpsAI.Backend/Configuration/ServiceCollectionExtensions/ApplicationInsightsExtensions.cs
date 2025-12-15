namespace AzureDevOpsAI.Backend.Configuration;

/// <summary>
/// Extension methods for configuring Application Insights telemetry.
/// </summary>
public static class ApplicationInsightsExtensions
{
    /// <summary>
    /// Adds Application Insights telemetry services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddApplicationInsights(this IServiceCollection services, IConfiguration configuration)
    {
        var applicationInsightsSettings = configuration.GetSection("ApplicationInsights").Get<ApplicationInsightsSettings>();
        
        if (!string.IsNullOrEmpty(applicationInsightsSettings?.ConnectionString))
        {
            services.AddApplicationInsightsTelemetry(options =>
            {
                options.ConnectionString = applicationInsightsSettings.ConnectionString;
            });
        }
        else
        {
            // Add default Application Insights services even without connection string
            services.AddApplicationInsightsTelemetry();
        }

        return services;
    }
}
