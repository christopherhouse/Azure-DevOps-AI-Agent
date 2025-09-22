using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication;
using System.IdentityModel.Tokens.Jwt;
using AzureDevOpsAI.Backend.Configuration;

namespace AzureDevOpsAI.Backend.Services;

/// <summary>
/// Interface for managing user authentication context for OBO flow.
/// </summary>
public interface IUserAuthenticationContext
{
    /// <summary>
    /// Sets the current user's access token for the request.
    /// </summary>
    /// <param name="accessToken">The user's access token from the Authorization header</param>
    void SetUserToken(string accessToken);

    /// <summary>
    /// Gets a token credential that can be used for OBO token exchange.
    /// </summary>
    /// <returns>Token credential for the current user, or null if no user context</returns>
    TokenCredential? GetUserTokenCredential();

    /// <summary>
    /// Gets the current user's object ID from their token.
    /// </summary>
    /// <returns>User object ID, or null if no user context</returns>
    string? GetCurrentUserId();

    /// <summary>
    /// Clears the current user context.
    /// </summary>
    void ClearUserContext();
}

/// <summary>
/// Service to manage user authentication context for OBO flow in plugins.
/// </summary>
public class UserAuthenticationContext : IUserAuthenticationContext
{
    private readonly ILogger<UserAuthenticationContext> _logger;
    private readonly AzureAuthSettings _azureAuthSettings;
    private string? _userAccessToken;
    private string? _userId;

    public UserAuthenticationContext(
        ILogger<UserAuthenticationContext> logger,
        Microsoft.Extensions.Options.IOptions<AzureAuthSettings> azureAuthSettings)
    {
        _logger = logger;
        _azureAuthSettings = azureAuthSettings.Value;
    }

    /// <summary>
    /// Sets the current user's access token for the request.
    /// </summary>
    public void SetUserToken(string accessToken)
    {
        // Validate token before storing
        var userId = ExtractUserIdFromToken(accessToken);
        if (userId != null)
        {
            _userAccessToken = accessToken;
            _userId = userId;
            _logger.LogDebug("User authentication context set for user: {UserId}", _userId);
        }
        else
        {
            _logger.LogWarning("Invalid or malformed access token provided");
            _userAccessToken = null;
            _userId = null;
        }
    }

    /// <summary>
    /// Gets a token credential that can be used for OBO token exchange.
    /// </summary>
    public TokenCredential? GetUserTokenCredential()
    {
        if (string.IsNullOrEmpty(_userAccessToken))
        {
            _logger.LogWarning("No user access token available for OBO flow");
            return null;
        }

        try
        {
            // Create OnBehalfOfCredential for token exchange
            var oboCredential = new OnBehalfOfCredential(
                tenantId: _azureAuthSettings.TenantId,
                clientId: _azureAuthSettings.ClientId,
                clientSecret: _azureAuthSettings.ClientSecret,
                userAssertion: _userAccessToken);

            _logger.LogDebug("Created OnBehalfOfCredential for user: {UserId}", _userId ?? "unknown");
            return oboCredential;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create OnBehalfOfCredential for user: {UserId}", _userId ?? "unknown");
            return null;
        }
    }

    /// <summary>
    /// Gets the current user's object ID from their token.
    /// </summary>
    public string? GetCurrentUserId()
    {
        return _userId;
    }

    /// <summary>
    /// Clears the current user context.
    /// </summary>
    public void ClearUserContext()
    {
        _userAccessToken = null;
        _userId = null;
        _logger.LogDebug("User authentication context cleared");
    }

    /// <summary>
    /// Extracts user ID from JWT token.
    /// </summary>
    private string? ExtractUserIdFromToken(string accessToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(accessToken))
            {
                _logger.LogWarning("Invalid JWT token format");
                return null;
            }

            var jsonToken = handler.ReadJwtToken(accessToken);
            
            // Try common user ID claims
            var userIdClaim = jsonToken.Claims.FirstOrDefault(c => 
                c.Type == "oid" || c.Type == "sub" || c.Type == "objectidentifier");
            
            return userIdClaim?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract user ID from token");
            return null;
        }
    }
}