using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using Xunit;
using FluentAssertions;
using AzureDevOpsAI.Backend.Services;
using AzureDevOpsAI.Backend.Configuration;

namespace AzureDevOpsAI.Backend.Tests.Integration;

/// <summary>
/// Integration test to demonstrate that the OBO flow fix works end-to-end.
/// </summary>
public class OBOFlowIntegrationTest : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public OBOFlowIntegrationTest()
    {
        // Setup test settings
        var azureAuthSettings = new AzureAuthSettings
        {
            TenantId = "test-tenant-id",
            ClientId = "test-backend-client-id",
            ClientSecret = "test-client-secret"
        };

        // Setup service collection to simulate actual application DI container
        var services = new ServiceCollection();
        
        // Add logging with simple mock
        var mockAuthLogger = new Mock<ILogger<UserAuthenticationContext>>();
        services.AddSingleton(mockAuthLogger.Object);
        
        // Add settings
        services.AddSingleton(Options.Create(azureAuthSettings));
        
        // Add the actual services as they would be in the application
        services.AddScoped<IUserAuthenticationContext, UserAuthenticationContext>();
        
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void OBOFlow_ShouldWorkEndToEnd_WhenUserTokenIsSetInRequestScope()
    {
        // Arrange
        var validJwtToken = CreateMockJwtToken("integration-test-user", "test-tenant");
        
        // Simulate a web request scope
        using var requestScope = _serviceProvider.CreateScope();
        
        // Act - Simulate ChatEndpoints setting the user token
        var userAuthContext = requestScope.ServiceProvider.GetRequiredService<IUserAuthenticationContext>();
        userAuthContext.SetUserToken(validJwtToken);
        
        // Simulate AIService using the same request scope (this is what our fix enables)
        var userAuthContextFromAIService = requestScope.ServiceProvider.GetService<IUserAuthenticationContext>();
        
        // Assert - The AIService should get the same instance with the user token
        userAuthContextFromAIService.Should().BeSameAs(userAuthContext, 
            "AIService should get the same UserAuthenticationContext instance from the request scope");
            
        var tokenCredential = userAuthContextFromAIService?.GetUserTokenCredential();
        var userId = userAuthContextFromAIService?.GetCurrentUserId();
        
        tokenCredential.Should().NotBeNull("AIService should be able to get the user token for OBO flow");
        userId.Should().Be("integration-test-user", "User ID should be preserved through the request scope");
        
        // Verify that ProjectPlugin would receive the same context
        // (We're not creating ProjectPlugin here to keep the test focused, but this demonstrates the fix)
        var contextForPlugin = requestScope.ServiceProvider.GetService<IUserAuthenticationContext>();
        contextForPlugin.Should().BeSameAs(userAuthContext, 
            "ProjectPlugin should receive the same UserAuthenticationContext with user token");
    }

    [Fact]
    public void OBOFlow_ShouldIsolateTokensBetweenRequests()
    {
        // Arrange
        var userToken1 = CreateMockJwtToken("user1", "tenant1");
        var userToken2 = CreateMockJwtToken("user2", "tenant2");
        
        string? userId1 = null;
        string? userId2 = null;
        
        // Act - Simulate two separate web requests
        using (var request1Scope = _serviceProvider.CreateScope())
        {
            var userAuthContext1 = request1Scope.ServiceProvider.GetRequiredService<IUserAuthenticationContext>();
            userAuthContext1.SetUserToken(userToken1);
            userId1 = userAuthContext1.GetCurrentUserId();
        }
        
        using (var request2Scope = _serviceProvider.CreateScope())
        {
            var userAuthContext2 = request2Scope.ServiceProvider.GetRequiredService<IUserAuthenticationContext>();
            userAuthContext2.SetUserToken(userToken2);
            userId2 = userAuthContext2.GetCurrentUserId();
        }
        
        // Assert - Each request should have its own isolated context
        userId1.Should().Be("user1", "First request should have user1");
        userId2.Should().Be("user2", "Second request should have user2");
        userId1.Should().NotBe(userId2, "Requests should be isolated from each other");
    }

    [Fact]
    public void OBOFlow_ShouldFallbackGracefully_WhenNoUserTokenIsSet()
    {
        // Arrange & Act
        using var requestScope = _serviceProvider.CreateScope();
        var userAuthContext = requestScope.ServiceProvider.GetRequiredService<IUserAuthenticationContext>();
        
        // Don't set any user token - simulate scenario where no Bearer token was provided
        
        var tokenCredential = userAuthContext.GetUserTokenCredential();
        var userId = userAuthContext.GetCurrentUserId();
        
        // Assert - Should gracefully handle missing token
        tokenCredential.Should().BeNull("Should return null when no user token is available");
        userId.Should().BeNull("Should return null when no user token is available");
        
        // This would cause ProjectPlugin to fall back to managed identity
    }

    /// <summary>
    /// Create a mock JWT token for testing.
    /// </summary>
    private static string CreateMockJwtToken(string userId, string tenantId)
    {
        var handler = new JwtSecurityTokenHandler();
        var descriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim("oid", userId),
                new System.Security.Claims.Claim("tid", tenantId),
                new System.Security.Claims.Claim("aud", "test-audience"),
                new System.Security.Claims.Claim("iss", $"https://sts.windows.net/{tenantId}/")
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("test-key-that-is-long-enough-for-hmac256")),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature)
        };

        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}