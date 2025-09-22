using AzureDevOpsAI.Backend.Configuration;
using AzureDevOpsAI.Backend.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace AzureDevOpsAI.Backend.Tests.Services;

public class UserAuthenticationContextTests
{
    private readonly Mock<ILogger<UserAuthenticationContext>> _mockLogger;
    private readonly AzureAuthSettings _azureAuthSettings;
    private readonly UserAuthenticationContext _userAuthContext;

    public UserAuthenticationContextTests()
    {
        _mockLogger = new Mock<ILogger<UserAuthenticationContext>>();
        _azureAuthSettings = new AzureAuthSettings
        {
            TenantId = "12345678-1234-1234-1234-123456789012",
            ClientId = "87654321-4321-4321-4321-210987654321",
            ClientSecret = "test-client-secret"
        };

        var mockOptions = new Mock<IOptions<AzureAuthSettings>>();
        mockOptions.Setup(o => o.Value).Returns(_azureAuthSettings);

        _userAuthContext = new UserAuthenticationContext(_mockLogger.Object, mockOptions.Object);
    }

    [Fact]
    public void GetCurrentUserId_ShouldReturnNull_WhenNoTokenSet()
    {
        // Act
        var userId = _userAuthContext.GetCurrentUserId();

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void GetUserTokenCredential_ShouldReturnNull_WhenNoTokenSet()
    {
        // Act
        var credential = _userAuthContext.GetUserTokenCredential();

        // Assert
        credential.Should().BeNull();
    }

    [Fact]
    public void SetUserToken_ShouldExtractUserId_FromValidJwtToken()
    {
        // Arrange
        var mockToken = CreateMockJwtToken("test-user-id", "test-tenant");

        // Act
        _userAuthContext.SetUserToken(mockToken);

        // Assert
        var userId = _userAuthContext.GetCurrentUserId();
        userId.Should().Be("test-user-id");
    }

    [Fact]
    public void SetUserToken_ShouldCreateOnBehalfOfCredential_WithValidToken()
    {
        // Arrange
        var mockToken = CreateMockJwtToken("test-user-id", "test-tenant");

        // Act
        _userAuthContext.SetUserToken(mockToken);
        var credential = _userAuthContext.GetUserTokenCredential();

        // Assert
        credential.Should().NotBeNull();
        credential.Should().BeOfType<Azure.Identity.OnBehalfOfCredential>();
    }

    [Fact]
    public void SetUserToken_ShouldHandleInvalidToken_Gracefully()
    {
        // Arrange
        var invalidToken = "invalid-jwt-token";

        // Act
        _userAuthContext.SetUserToken(invalidToken);

        // Assert
        var userId = _userAuthContext.GetCurrentUserId();
        userId.Should().BeNull();
        
        var credential = _userAuthContext.GetUserTokenCredential();
        credential.Should().BeNull();
    }

    [Fact]
    public void ClearUserContext_ShouldResetAllState()
    {
        // Arrange
        var mockToken = CreateMockJwtToken("test-user-id", "test-tenant");
        _userAuthContext.SetUserToken(mockToken);

        // Act
        _userAuthContext.ClearUserContext();

        // Assert
        var userId = _userAuthContext.GetCurrentUserId();
        userId.Should().BeNull();
        
        var credential = _userAuthContext.GetUserTokenCredential();
        credential.Should().BeNull();
    }

    [Theory]
    [InlineData("oid", "object-id-claim")]
    [InlineData("sub", "subject-claim")]
    [InlineData("objectidentifier", "object-identifier-claim")]
    public void SetUserToken_ShouldExtractUserId_FromDifferentClaims(string claimType, string expectedUserId)
    {
        // Arrange
        var mockToken = CreateMockJwtTokenWithClaim(claimType, expectedUserId);

        // Act
        _userAuthContext.SetUserToken(mockToken);

        // Assert
        var userId = _userAuthContext.GetCurrentUserId();
        userId.Should().Be(expectedUserId);
    }

    private string CreateMockJwtToken(string userId, string tenantId)
    {
        return CreateMockJwtTokenWithClaim("oid", userId);
    }

    private string CreateMockJwtTokenWithClaim(string claimType, string claimValue)
    {
        // Create a simple mock JWT token for testing
        // Note: This is not a real signed token, just for testing token parsing
        var claims = new[]
        {
            new Claim(claimType, claimValue),
            new Claim("aud", _azureAuthSettings.ClientId),
            new Claim("iss", $"https://login.microsoftonline.com/{_azureAuthSettings.TenantId}/v2.0"),
            new Claim("exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString())
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            // Note: For testing, we don't need a real signature
            SigningCredentials = null
        };

        // Create unsigned token for testing purposes
        var jwtToken = new JwtSecurityToken(
            issuer: $"https://login.microsoftonline.com/{_azureAuthSettings.TenantId}/v2.0",
            audience: _azureAuthSettings.ClientId,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1)
        );

        return tokenHandler.WriteToken(jwtToken);
    }
}