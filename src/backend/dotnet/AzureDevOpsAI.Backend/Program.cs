using AzureDevOpsAI.Backend.Configuration;
using AzureDevOpsAI.Backend.Endpoints;
using AzureDevOpsAI.Backend.Middleware;
using AzureDevOpsAI.Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Microsoft.Identity.Abstractions;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure logging - DON'T clear providers to keep Application Insights logger
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Ensure all trace levels are captured
builder.Logging.SetMinimumLevel(LogLevel.Trace);

// Configure specific log levels if needed
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);
builder.Logging.AddFilter("AzureDevOpsAI", LogLevel.Trace);

// Add configuration
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("App"));
builder.Services.Configure<AzureAuthSettings>(builder.Configuration.GetSection("AzureAuth"));
builder.Services.Configure<AzureDevOpsSettings>(builder.Configuration.GetSection("AzureDevOps"));
builder.Services.Configure<ApplicationInsightsSettings>(builder.Configuration.GetSection("ApplicationInsights"));
builder.Services.Configure<SecuritySettings>(builder.Configuration.GetSection("Security"));
builder.Services.Configure<AzureOpenAISettings>(options =>
{
    builder.Configuration.GetSection("AzureOpenAI").Bind(options);

    // Override ClientId with ManagedIdentityClientId environment variable if provided
    var managedIdentityClientId = builder.Configuration["ManagedIdentityClientId"];
    if (!string.IsNullOrEmpty(managedIdentityClientId))
    {
        options.ClientId = managedIdentityClientId;
    }
});
builder.Services.Configure<CosmosDbSettings>(options =>
{
    builder.Configuration.GetSection("CosmosDb").Bind(options);

    // Override ClientId with ManagedIdentityClientId environment variable if provided
    var managedIdentityClientId = builder.Configuration["ManagedIdentityClientId"];
    if (!string.IsNullOrEmpty(managedIdentityClientId))
    {
        options.ClientId = managedIdentityClientId;
    }
});

// Read ManagedIdentityClientId from configuration for AzureDevOpsApiService
var managedIdentityClientIdForDevOps = builder.Configuration["ManagedIdentityClientId"];

// Read Azure DevOps authentication settings
var azureDevOpsSettings = builder.Configuration.GetSection("AzureDevOps").Get<AzureDevOpsSettings>() ?? new AzureDevOpsSettings();

// Add CosmosDB service - required, no fallback to in-memory storage
// The validation happens at service construction time to allow tests to override
builder.Services.AddSingleton<ICosmosDbService>(sp =>
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
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddScoped<IAzureDevOpsApiService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    var logger = sp.GetRequiredService<ILogger<AzureDevOpsApiService>>();
    return new AzureDevOpsApiService(httpClient, logger, managedIdentityClientIdForDevOps, azureDevOpsSettings.Pat, azureDevOpsSettings.UsePat);
});

// Add Application Insights
var applicationInsightsSettings = builder.Configuration.GetSection("ApplicationInsights").Get<ApplicationInsightsSettings>();
if (!string.IsNullOrEmpty(applicationInsightsSettings?.ConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = applicationInsightsSettings.ConnectionString;
    });

    // Explicitly add Application Insights logging provider
    builder.Logging.AddApplicationInsights(
        configureTelemetryConfiguration: (config) =>
            config.ConnectionString = applicationInsightsSettings.ConnectionString,
        configureApplicationInsightsLoggerOptions: (options) =>
        {
            // Capture all log levels
            options.IncludeScopes = true;
            options.TrackExceptionsAsExceptionTelemetry = true;
        });
}
else
{
    // Add default Application Insights services even without connection string
    builder.Services.AddApplicationInsightsTelemetry();
}

// Add authentication
var azureAuthSettings = builder.Configuration.GetSection("AzureAuth").Get<AzureAuthSettings>();
var securitySettings = builder.Configuration.GetSection("Security").Get<SecuritySettings>();

if (azureAuthSettings != null && !securitySettings?.DisableAuth == true)
{
    // Configure Microsoft Identity Web for web API authentication (without OBO token acquisition)
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAuth");

    builder.Services.AddAuthorization();
}
else
{
    // Fallback to basic JWT Bearer authentication when Microsoft Identity Web is disabled
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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

    builder.Services.AddAuthorization();
}

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Azure DevOps AI Agent Backend API",
        Version = "v1.0.0",
        Description = "Backend API for Azure DevOps AI Agent with Entra ID authentication"
    });

    // Add JWT authentication to Swagger
    if (!securitySettings?.DisableAuth == true)
    {
        c.AddSecurityDefinition("Bearer", new()
        {
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter JWT token"
        });

        c.AddSecurityRequirement(new()
        {
            {
                new()
                {
                    Reference = new()
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    }
});

// Add health checks
builder.Services.AddHealthChecks();

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline
app.UseMiddleware<RequestLoggingMiddleware>();
// app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

// Swagger configuration
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

// Health check endpoints
app.MapGet("/health", () =>
{
    var appSettings = app.Services.GetService<Microsoft.Extensions.Options.IOptions<AppSettings>>()?.Value;
    return Results.Ok(new
    {
        status = "healthy",
        message = "Azure DevOps AI Agent Backend is running",
        version = appSettings?.AppVersion ?? "1.0.0",
        environment = appSettings?.Environment ?? "development"
    });
})
.WithName("HealthCheck")
.WithSummary("Health check endpoint")
.WithOpenApi()
.AllowAnonymous();

app.MapGet("/", () =>
{
    var appSettings = app.Services.GetService<Microsoft.Extensions.Options.IOptions<AppSettings>>()?.Value;
    return Results.Ok(new
    {
        message = "Azure DevOps AI Agent Backend API",
        version = appSettings?.AppVersion ?? "1.0.0",
        docs_url = app.Environment.IsDevelopment() || securitySettings?.DisableAuth == true
            ? "/docs"
            : "Documentation disabled in production"
    });
})
.WithName("Root")
.WithSummary("Root endpoint")
.WithOpenApi()
.AllowAnonymous();

// Map API endpoints
app.MapChatEndpoints();
//app.MapProjectEndpoints();
//app.MapWorkItemEndpoints();

// Map health checks
app.MapHealthChecks("/health/ready");

app.Run();

// Make Program class accessible for testing
public partial class Program { }
