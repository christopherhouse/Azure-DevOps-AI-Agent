using Azure.Identity;
using Azure.Core;
using System.Text.Json;
using System.Text;
using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Configuration;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;

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
    private const string ExpectedAudience = "499b84ac-1321-427f-aa17-267ca6975798";
    
    // Centralized JSON serializer options for consistent behavior
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AzureDevOpsApiService(HttpClient httpClient, ILogger<AzureDevOpsApiService> logger, IOptions<AzureOpenAISettings> azureOpenAISettings)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Configure DefaultAzureCredential with optional User Assigned Managed Identity client ID
        var credentialOptions = new DefaultAzureCredentialOptions();
        
        if (azureOpenAISettings.Value.UseUserAssignedIdentity)
        {
            credentialOptions.ManagedIdentityClientId = azureOpenAISettings.Value.ClientId;
            _credential = new DefaultAzureCredential(credentialOptions);
            _logger.LogInformation("AzureDevOpsApiService initialized with DefaultAzureCredential using User Assigned Managed Identity client ID: {ClientId}", azureOpenAISettings.Value.ClientId);
        }
        else
        {
            _credential = new DefaultAzureCredential(credentialOptions);
            _logger.LogInformation("AzureDevOpsApiService initialized with DefaultAzureCredential without User Assigned Managed Identity");
        }
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
            
            // Log token metadata for troubleshooting (not the token itself)
            LogTokenMetadata(accessToken);

            // Create HttpRequestMessage with authorization header (thread-safe approach)
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.Token);
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<T>(content, JsonOptions);
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
            
            // Log token metadata for troubleshooting (not the token itself)
            LogTokenMetadata(accessToken);

            // Create HttpRequestMessage with authorization header (thread-safe approach)
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.Token);
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            if (body != null)
            {
                var jsonBody = JsonSerializer.Serialize(body, JsonOptions);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<T>(responseContent, JsonOptions);
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
    }

    /// <summary>
    /// Builds the full Azure DevOps API URL with resilient path handling.
    /// </summary>
    private static string BuildApiUrl(string organization, string apiPath, string? apiVersion)
    {
        // Handle full URLs (don't modify them)
        if (apiPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
            apiPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            // If it's already a full URL, just add API version if needed
            if (!string.IsNullOrEmpty(apiVersion) && !apiPath.Contains("api-version=", StringComparison.OrdinalIgnoreCase))
            {
                var separator = apiPath.Contains('?') ? "&" : "?";
                return $"{apiPath}{separator}api-version={apiVersion}";
            }
            return apiPath;
        }
        
        var baseUrl = $"https://dev.azure.com/{organization}";
        
        // Remove duplicate /_apis/ prefixes
        var normalizedPath = apiPath.TrimStart('/');
        
        // If path already starts with _apis/, use it as-is with leading slash
        // Otherwise, prefix with /_apis/
        var path = normalizedPath.StartsWith("_apis/", StringComparison.OrdinalIgnoreCase) 
            ? $"/{normalizedPath}" 
            : $"/_apis/{normalizedPath}";
        
        if (!string.IsNullOrEmpty(apiVersion))
        {
            var separator = path.Contains('?') ? "&" : "?";
            path += $"{separator}api-version={apiVersion}";
        }
        
        return $"{baseUrl}{path}";
    }
    
    /// <summary>
    /// Logs token metadata for troubleshooting without exposing the token itself.
    /// </summary>
    private void LogTokenMetadata(AccessToken accessToken)
    {
        try
        {
            _logger.LogDebug("Token metadata - ExpiresOn: {ExpiresOn}", accessToken.ExpiresOn);
            
            // Decode JWT to verify audience and issuer
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(accessToken.Token))
            {
                var jwtToken = handler.ReadJwtToken(accessToken.Token);
                
                var audience = jwtToken.Audiences.FirstOrDefault();
                var issuer = jwtToken.Issuer;
                
                _logger.LogDebug("Token metadata - Audience: {Audience}, Issuer: {Issuer}", audience, issuer);
                
                // Verify expected audience for Azure DevOps
                if (audience != ExpectedAudience)
                {
                    _logger.LogWarning("Token audience mismatch. Expected: {ExpectedAudience}, Actual: {Audience}", ExpectedAudience, audience);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decode token metadata");
        }
    }
}