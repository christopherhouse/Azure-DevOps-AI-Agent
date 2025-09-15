using AzureDevOpsAI.Backend.Configuration;
using AzureDevOpsAI.Backend.Endpoints;
using AzureDevOpsAI.Backend.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Add configuration
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("App"));
builder.Services.Configure<AzureAuthSettings>(builder.Configuration.GetSection("AzureAuth"));
builder.Services.Configure<AzureDevOpsSettings>(builder.Configuration.GetSection("AzureDevOps"));
builder.Services.Configure<ApplicationInsightsSettings>(builder.Configuration.GetSection("ApplicationInsights"));
builder.Services.Configure<SecuritySettings>(builder.Configuration.GetSection("Security"));

// Add OpenTelemetry (Azure Monitor will be configured via environment variables)
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("AzureDevOpsAI.Backend"));

// Add authentication
var azureAuthSettings = builder.Configuration.GetSection("AzureAuth").Get<AzureAuthSettings>();
var securitySettings = builder.Configuration.GetSection("Security").Get<SecuritySettings>();

if (azureAuthSettings != null && !securitySettings?.DisableAuth == true)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = azureAuthSettings.Authority;
            options.Audience = azureAuthSettings.Audience;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
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
app.UseMiddleware<SecurityHeadersMiddleware>();
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

// Map API endpoints - commented out for initial demonstration
// app.MapChatEndpoints();
// app.MapProjectEndpoints();
// app.MapWorkItemEndpoints();

// Map health checks
app.MapHealthChecks("/health/ready");

app.Run();
