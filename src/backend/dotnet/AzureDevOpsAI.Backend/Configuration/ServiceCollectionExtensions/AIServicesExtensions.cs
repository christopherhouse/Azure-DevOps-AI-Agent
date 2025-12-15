using AzureDevOpsAI.Backend.Services;

namespace AzureDevOpsAI.Backend.Configuration;

/// <summary>
/// Extension methods for configuring AI services (Azure OpenAI).
/// </summary>
public static class AIServicesExtensions
{
    /// <summary>
    /// Adds AI services (Azure OpenAI) to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddAIServices(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddScoped<IAIService, AIService>();

        return services;
    }
}
