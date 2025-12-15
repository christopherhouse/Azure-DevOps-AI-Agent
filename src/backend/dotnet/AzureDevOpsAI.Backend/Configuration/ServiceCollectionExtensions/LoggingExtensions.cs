using Microsoft.Extensions.Logging;

namespace AzureDevOpsAI.Backend.Configuration;

/// <summary>
/// Extension methods for configuring logging services.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Adds and configures logging providers and filters.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The web application builder for method chaining.</returns>
    public static WebApplicationBuilder AddLogging(this WebApplicationBuilder builder)
    {
        // Configure logging - DON'T clear providers to keep Application Insights logger
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        // Ensure all trace levels are captured
        builder.Logging.SetMinimumLevel(LogLevel.Trace);

        // Configure specific log levels if needed
        builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
        builder.Logging.AddFilter("System", LogLevel.Warning);
        builder.Logging.AddFilter("AzureDevOpsAI", LogLevel.Trace);

        return builder;
    }
}
