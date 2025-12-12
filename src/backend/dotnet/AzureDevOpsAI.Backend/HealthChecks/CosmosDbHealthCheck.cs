using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Azure.Identity;
using AzureDevOpsAI.Backend.Configuration;

namespace AzureDevOpsAI.Backend.HealthChecks;

/// <summary>
/// Health check for Cosmos DB connectivity and availability.
/// </summary>
public class CosmosDbHealthCheck : IHealthCheck
{
    private readonly CosmosDbSettings _settings;
    private readonly ILogger<CosmosDbHealthCheck> _logger;

    public CosmosDbHealthCheck(IOptions<CosmosDbSettings> settings, ILogger<CosmosDbHealthCheck> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate configuration
            if (string.IsNullOrEmpty(_settings.Endpoint))
            {
                _logger.LogError("Cosmos DB endpoint is not configured");
                return HealthCheckResult.Unhealthy("Cosmos DB endpoint is not configured");
            }

            // Create a temporary Cosmos client to test connectivity
            var clientOptions = new CosmosClientOptions
            {
                RequestTimeout = TimeSpan.FromSeconds(5),
                SerializerOptions = new CosmosSerializationOptions
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
                }
            };

            Azure.Core.TokenCredential credential;

            if (_settings.UseManagedIdentity)
            {
                credential = !string.IsNullOrEmpty(_settings.ClientId)
                    ? new ManagedIdentityCredential(_settings.ClientId)
                    : new ManagedIdentityCredential();
            }
            else
            {
                credential = new DefaultAzureCredential();
            }

            using var cosmosClient = new CosmosClient(_settings.Endpoint, credential, clientOptions);

            // Try to read the database account to verify connectivity
            var response = await cosmosClient.ReadAccountAsync();

            if (response == null)
            {
                _logger.LogWarning("Cosmos DB health check: Unable to read account information");
                return HealthCheckResult.Degraded("Unable to read Cosmos DB account information");
            }

            // Verify the database exists
            var database = cosmosClient.GetDatabase(_settings.DatabaseName);
            var databaseResponse = await database.ReadAsync(cancellationToken: cancellationToken);

            _logger.LogDebug("Cosmos DB health check passed. Database: {DatabaseName}", _settings.DatabaseName);

            return HealthCheckResult.Healthy($"Cosmos DB is accessible. Database: {_settings.DatabaseName}");
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogError(ex, "Cosmos DB health check failed: Authentication error");
            return HealthCheckResult.Unhealthy("Cosmos DB authentication failed", ex);
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogError(ex, "Cosmos DB health check failed: Database or resource not found");
            return HealthCheckResult.Unhealthy($"Cosmos DB database '{_settings.DatabaseName}' not found", ex);
        }
        catch (CosmosException ex)
        {
            _logger.LogError(ex, "Cosmos DB health check failed with status code: {StatusCode}", ex.StatusCode);
            return HealthCheckResult.Unhealthy($"Cosmos DB error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cosmos DB health check failed with unexpected error");
            return HealthCheckResult.Unhealthy($"Cosmos DB connection failed: {ex.Message}", ex);
        }
    }
}
