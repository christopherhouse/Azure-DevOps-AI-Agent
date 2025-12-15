using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;

namespace AzureDevOpsAI.Backend.Configuration;

/// <summary>
/// Extension methods for configuring authentication and authorization.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds authentication and authorization services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var azureAuthSettings = configuration.GetSection("AzureAuth").Get<AzureAuthSettings>();
        var securitySettings = configuration.GetSection("Security").Get<SecuritySettings>();

        if (azureAuthSettings != null && !securitySettings?.DisableAuth == true)
        {
            // Configure Microsoft Identity Web for web API authentication (without OBO token acquisition)
            services.AddMicrosoftIdentityWebApiAuthentication(configuration, "AzureAuth");

            services.AddAuthorization();
        }
        else
        {
            // Fallback to basic JWT Bearer authentication when Microsoft Identity Web is disabled
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = azureAuthSettings?.Authority;
                    options.Audience = azureAuthSettings?.Audience;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = azureAuthSettings != null,
                        ValidateAudience = azureAuthSettings != null,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = azureAuthSettings != null,
                        ClockSkew = TimeSpan.FromMinutes(5)
                    };
                });

            services.AddAuthorization();
        }

        return services;
    }
}
