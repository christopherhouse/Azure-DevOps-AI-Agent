using Azure.Identity;
using Azure.Core;
using System.Text.Json;
using System.Text;
using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Configuration;
using Microsoft.Extensions.Options;

namespace AzureDevOpsAI.Backend.Services;

/// <summary>
/// Interface for Azure DevOps API operations using DefaultAzureCredential.
/// </summary>
public interface IAzureDevOpsApiService
{
    /// <summary>
    /// Makes an authenticated GET request to Azure DevOps API using managed identity.
    /// </summary>
    Task<T?> GetAsync<T>(string organization, string apiPath, string? apiVersion = "7.1", CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Makes an authenticated POST request to Azure DevOps API using managed identity.
    /// </summary>
    Task<T?> PostAsync<T>(string organization, string apiPath, object? body = null, string? apiVersion = "7.1", CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// Service for making authenticated calls to Azure DevOps APIs using DefaultAzureCredential with User Assigned Managed Identity.
/// </summary>
public class AzureDevOpsApiService : IAzureDevOpsApiService
{
    private readonly TokenCredential _credential;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureDevOpsApiService> _logger;
    
    private const string AzureDevOpsScope = "499b84ac-1321-427f-aa17-267ca6975798/.default";

    public AzureDevOpsApiService(HttpClient httpClient, ILogger<AzureDevOpsApiService> logger, IOptions<AzureOpenAISettings> azureOpenAISettings)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Configure DefaultAzureCredential with User Assigned Managed Identity client ID
        var credentialOptions = new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = azureOpenAISettings.Value.ClientId
        };
        
        _credential = new DefaultAzureCredential(credentialOptions);
        _logger.LogInformation("AzureDevOpsApiService initialized with DefaultAzureCredential using User Assigned Managed Identity client ID: {ClientId}", azureOpenAISettings.Value.ClientId);
    }

    /// <summary>
    /// Makes an authenticated GET request to Azure DevOps API using managed identity.
    /// </summary>
    public async Task<T?> GetAsync<T>(string organization, string apiPath, string? apiVersion = "7.1", CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var url = BuildApiUrl(organization, apiPath, apiVersion);
            _logger.LogDebug("Making GET request to Azure DevOps API: {Url}", url);

            // Acquire token using DefaultAzureCredential (User Assigned Managed Identity)
            var tokenRequestContext = new TokenRequestContext(new[] { AzureDevOpsScope });
            var accessToken = await _credential.GetTokenAsync(tokenRequestContext, cancellationToken);

            // Configure HttpClient with the access token
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.Token);

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                _logger.LogDebug("Successfully completed GET request to Azure DevOps API: {Url}", url);
                return result;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("GET request to Azure DevOps API failed. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to make GET request to Azure DevOps API: {Organization}/{ApiPath}", organization, apiPath);
            throw;
        }
        finally
        {
            // Clear the authorization header for future requests
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    /// <summary>
    /// Makes an authenticated POST request to Azure DevOps API using managed identity.
    /// </summary>
    public async Task<T?> PostAsync<T>(string organization, string apiPath, object? body = null, string? apiVersion = "7.1", CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var url = BuildApiUrl(organization, apiPath, apiVersion);
            _logger.LogDebug("Making POST request to Azure DevOps API: {Url}", url);

            // Acquire token using DefaultAzureCredential (User Assigned Managed Identity)
            var tokenRequestContext = new TokenRequestContext(new[] { AzureDevOpsScope });
            var accessToken = await _credential.GetTokenAsync(tokenRequestContext, cancellationToken);

            // Configure HttpClient with the access token
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.Token);

            HttpContent? content = null;
            if (body != null)
            {
                var jsonBody = JsonSerializer.Serialize(body);
                content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.PostAsync(url, content, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<T>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                _logger.LogDebug("Successfully completed POST request to Azure DevOps API: {Url}", url);
                return result;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("POST request to Azure DevOps API failed. Status: {StatusCode}, Error: {Error}", response.StatusCode, errorContent);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to make POST request to Azure DevOps API: {Organization}/{ApiPath}", organization, apiPath);
            throw;
        }
        finally
        {
            // Clear the authorization header for future requests
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
    }

    /// <summary>
    /// Builds the full Azure DevOps API URL.
    /// </summary>
    private static string BuildApiUrl(string organization, string apiPath, string? apiVersion)
    {
        var baseUrl = $"https://dev.azure.com/{organization}";
        var path = apiPath.StartsWith('/') ? apiPath : $"/_apis/{apiPath}";
        
        if (!string.IsNullOrEmpty(apiVersion))
        {
            var separator = path.Contains('?') ? "&" : "?";
            path += $"{separator}api-version={apiVersion}";
        }
        
        return $"{baseUrl}{path}";
    }
}