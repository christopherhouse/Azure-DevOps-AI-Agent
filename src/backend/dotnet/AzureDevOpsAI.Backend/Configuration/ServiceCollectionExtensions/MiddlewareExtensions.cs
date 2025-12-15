using AzureDevOpsAI.Backend.Middleware;

namespace AzureDevOpsAI.Backend.Configuration;

/// <summary>
/// Extension methods for configuring application middleware pipeline.
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Configures the middleware pipeline including logging, error handling, and authentication.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The web application for method chaining.</returns>
    public static WebApplication UseApplicationMiddleware(this WebApplication app, IConfiguration configuration)
    {
        var securitySettings = configuration.GetSection("Security").Get<SecuritySettings>();

        // Add middleware in order
        app.UseMiddleware<RequestLoggingMiddleware>();
        // app.UseMiddleware<SecurityHeadersMiddleware>();
        app.UseMiddleware<ErrorHandlingMiddleware>();

        // Configure Swagger (development or when auth is disabled)
        if (app.Environment.IsDevelopment() || securitySettings?.DisableAuth == true)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Azure DevOps AI Agent Backend API v1");
                c.RoutePrefix = "docs";
            });
        }

        // Authentication middleware (if enabled)
        if (!securitySettings?.DisableAuth == true)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        return app;
    }
}
