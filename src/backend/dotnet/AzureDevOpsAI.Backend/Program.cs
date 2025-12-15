using AzureDevOpsAI.Backend.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.AddLogging();

// Add configuration settings
builder.Services.AddAppSettings(builder.Configuration);

// Add application services
builder.Services.AddCosmosDbService();
builder.Services.AddAIServices();
builder.Services.AddAzureDevOpsApiService(builder.Configuration);

// Add Application Insights
builder.Services.AddApplicationInsights(builder.Configuration);

// Add authentication
builder.Services.AddAuthenticationServices(builder.Configuration);

// Add Swagger/OpenAPI
builder.Services.AddSwaggerDocumentation(builder.Configuration);

// Add health checks
builder.Services.AddHealthCheckServices();

// Build the application
var app = builder.Build();

// Configure middleware pipeline
app.UseApplicationMiddleware(builder.Configuration);

// Map endpoints
app.MapApplicationEndpoints(builder.Configuration);

app.Run();

// Make Program class accessible for testing
public partial class Program { }
