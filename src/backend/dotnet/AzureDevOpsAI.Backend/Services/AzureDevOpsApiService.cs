using System.Diagnostics.CodeAnalysis;
using Azure.Identity;
using Azure.Core;
using System.Text.Json;
using System.Text;
using AzureDevOpsAI.Backend.Models;
using System.IdentityModel.Tokens.Jwt;

namespace AzureDevOpsAI.Backend.Services;

/// <summary>
/// Interface for Azure DevOps API operations using Azure Identity or Personal Access Token.
/// </summary>
public interface IAzureDevOpsApiService
{
    /// <summary>
    /// Makes an authenticated GET request to Azure DevOps API using managed identity or PAT.
    /// </summary>
    Task<T?> GetAsync<T>(string organization, string apiPath, string? apiVersion = "7.1", CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Makes an authenticated POST request to Azure DevOps API using managed identity or PAT.
    /// </summary>
    Task<T?> PostAsync<T>(string organization, string apiPath, object? body = null, string? apiVersion = "7.1", CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// Service for making authenticated calls to Azure DevOps APIs using ManagedIdentityCredential with User Assigned Managed Identity or Personal Access Token.
/// </summary>
public class AzureDevOpsApiService : IAzureDevOpsApiService
{
    private readonly TokenCredential? _credential;
    private readonly HttpClient _httpClient;
    private readonly ILogger<AzureDevOpsApiService> _logger;
    private readonly string? _pat;
    private readonly bool _usePat;
    
    private const string AzureDevOpsScope = "https://app.vssps.visualstudio.com/.default";
    private const string ExpectedAudience = "499b84ac-1321-427f-aa17-267ca6975798";
    
    // Centralized JSON serializer options for consistent behavior
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AzureDevOpsApiService(HttpClient httpClient, ILogger<AzureDevOpsApiService> logger, string? managedIdentityClientId, string? pat = null, bool usePat = false)
    {
        _httpClient = httpClient;
        _logger = logger;
        _pat = pat;
        _usePat = usePat;

        if (_usePat)
        {
            if (string.IsNullOrWhiteSpace(_pat))
            {
                throw new ArgumentException("Personal Access Token cannot be null or empty when UsePat is true", nameof(pat));
            }
            _logger.LogInformation("AzureDevOpsApiService initialized with Personal Access Token authentication");
        }
        else
        {
            _credential = string.IsNullOrWhiteSpace(managedIdentityClientId) ? new DefaultAzureCredential() : new DefaultAzureCredential(new DefaultAzureCredentialOptions{ ManagedIdentityClientId = managedIdentityClientId });

            if (string.IsNullOrWhiteSpace(managedIdentityClientId))
            {
                _logger.LogInformation("AzureDevOpsApiService initialized with DefaultAzureCredential");
            }
            else
            {
                _logger.LogInformation("AzureDevOpsApiService initialized with User Assigned Managed Identity, client-id: {ManagedIdentityCientId}",
                    managedIdentityClientId);
            }
        }
    }

    /// <summary>
    /// Makes an authenticated GET request to Azure DevOps API using managed identity or PAT.
    /// </summary>
    public async Task<T?> GetAsync<T>(string organization, string apiPath, string? apiVersion = "7.1", CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var url = BuildApiUrl(organization, apiPath, apiVersion);
            _logger.LogDebug("Making GET request to Azure DevOps API: {Url}", url);

            // Create HttpRequestMessage with authorization header (thread-safe approach)
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            
            // Set authorization header based on authentication type
            if (_usePat)
            {
                // Use Basic Authentication with PAT
                var patBytes = System.Text.Encoding.ASCII.GetBytes($":{_pat}");
                var base64Pat = Convert.ToBase64String(patBytes);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64Pat);
            }
            else
            {
                // Acquire token using ManagedIdentityCredential (User Assigned Managed Identity)
                var tokenRequestContext = new TokenRequestContext(new[] { AzureDevOpsScope });
                var accessToken = await _credential!.GetTokenAsync(tokenRequestContext, cancellationToken);
                
                // Log token metadata for troubleshooting (not the token itself)
                LogTokenMetadata(accessToken);

                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.Token);
            }
            
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
    /// Makes an authenticated POST request to Azure DevOps API using managed identity or PAT.
    /// </summary>
    public async Task<T?> PostAsync<T>(string organization, string apiPath, object? body = null, string? apiVersion = "7.1", CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var url = BuildApiUrl(organization, apiPath, apiVersion);
            _logger.LogDebug("Making POST request to Azure DevOps API: {Url}", url);

            // Create HttpRequestMessage with authorization header (thread-safe approach)
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            
            // Set authorization header based on authentication type
            if (_usePat)
            {
                // Use Basic Authentication with PAT
                var patBytes = System.Text.Encoding.ASCII.GetBytes($":{_pat}");
                var base64Pat = Convert.ToBase64String(patBytes);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", base64Pat);
            }
            else
            {
                // Acquire token using ManagedIdentityCredential (User Assigned Managed Identity)
                var tokenRequestContext = new TokenRequestContext(new[] { AzureDevOpsScope });
                var accessToken = await _credential!.GetTokenAsync(tokenRequestContext, cancellationToken);
                
                // Log token metadata for troubleshooting (not the token itself)
                LogTokenMetadata(accessToken);

                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.Token);
            }
            
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
    /// Logs key claims including aud, tid, oid, upn, appid for diagnostic purposes.
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
                
                // Extract key claims for troubleshooting
                var audience = jwtToken.Audiences.FirstOrDefault();
                var issuer = jwtToken.Issuer;
                var tenantId = GetClaimValue(jwtToken, "tid");
                var objectId = GetClaimValue(jwtToken, "oid");
                var upn = GetClaimValue(jwtToken, "upn");
                var appId = GetClaimValue(jwtToken, "appid");
                var scopes = GetClaimValue(jwtToken, "scp");
                
                // Log all diagnostic claims at Information level for visibility
                _logger.LogInformation(
                    "Token diagnostics - aud: {Audience}, tid: {TenantId}, oid: {ObjectId}, upn: {Upn}, appid: {AppId}, scp: {Scopes}, iss: {Issuer}",
                    audience ?? "(not present)",
                    tenantId ?? "(not present)",
                    objectId ?? "(not present)",
                    upn ?? "(not present)",
                    appId ?? "(not present)",
                    scopes ?? "(not present)",
                    issuer ?? "(not present)");
                
                // Log token validity period for additional diagnostics
                _logger.LogDebug(
                    "Token validity - ValidFrom: {ValidFrom}, ValidTo: {ValidTo}, IssuedAt: {IssuedAt}",
                    jwtToken.ValidFrom,
                    jwtToken.ValidTo,
                    jwtToken.IssuedAt);
                
                // Verify expected audience for Azure DevOps
                if (audience != ExpectedAudience)
                {
                    _logger.LogWarning("Token audience mismatch. Expected: {ExpectedAudience}, Actual: {Audience}", ExpectedAudience, audience);
                }
            }
            else
            {
                _logger.LogWarning("Unable to read token as JWT - token format may be invalid");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decode token metadata");
        }
    }
    
    /// <summary>
    /// Safely extracts a claim value from a JWT token.
    /// </summary>
    private static string? GetClaimValue(JwtSecurityToken jwtToken, string claimType)
    {
        return jwtToken.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
    }
}