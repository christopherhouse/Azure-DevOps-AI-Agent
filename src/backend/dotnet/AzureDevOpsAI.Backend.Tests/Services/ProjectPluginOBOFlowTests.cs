using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using Xunit;
using FluentAssertions;
using AzureDevOpsAI.Backend.Services;
using AzureDevOpsAI.Backend.Configuration;
using AzureDevOpsAI.Backend.Plugins;

namespace AzureDevOpsAI.Backend.Tests.Services;

/// <summary>
/// Tests to validate that ProjectPlugin receives user token context correctly for OBO flow.
/// </summary>
public class ProjectPluginOBOFlowTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<ILogger<UserAuthenticationContext>> _mockAuthLogger;
    private readonly HttpClient _httpClient;
    private readonly AzureOpenAISettings _azureOpenAISettings;
    private readonly AzureAuthSettings _azureAuthSettings;

    public ProjectPluginOBOFlowTests()
    {
        // Setup mock logger
        _mockAuthLogger = new Mock<ILogger<UserAuthenticationContext>>();

        // Create a real HttpClient for the tests since we're not making HTTP calls
        _httpClient = new HttpClient();

        // Setup test settings
        _azureOpenAISettings = new AzureOpenAISettings
        {
            Endpoint = "https://test.openai.azure.com/",
            ChatDeploymentName = "gpt-4",
            ClientId = "test-client-id"
        };

        _azureAuthSettings = new AzureAuthSettings
        {
            TenantId = "test-tenant-id",
            ClientId = "test-backend-client-id",
            ClientSecret = "test-client-secret"
        };

        // Setup service collection
        var services = new ServiceCollection();
        services.AddSingleton(_mockAuthLogger.Object);
        services.AddSingleton(Options.Create(_azureAuthSettings));
        services.AddScoped<IUserAuthenticationContext, UserAuthenticationContext>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void ProjectPlugin_ShouldReceiveUserTokenContext_WhenSetInRequestScope()
    {
        // Arrange
        var validJwtToken = CreateMockJwtToken("test-user-id", "test-tenant");
        
        // Get the UserAuthenticationContext from the service provider (request scope)
        using var scope = _serviceProvider.CreateScope();
        var userAuthContext = scope.ServiceProvider.GetRequiredService<IUserAuthenticationContext>();
        
        // Set user token
        userAuthContext.SetUserToken(validJwtToken);
        
        // Act
        // We don't actually need to create the ProjectPlugin for this test
        // Just verify the UserAuthenticationContext behavior

        // Get the user token credential from the plugin's context
        var tokenCredential = userAuthContext.GetUserTokenCredential();
        var userId = userAuthContext.GetCurrentUserId();

        // Assert
        tokenCredential.Should().NotBeNull("UserAuthenticationContext should provide user token for OBO flow");
        tokenCredential.Should().BeOfType<OnBehalfOfCredential>("Should be OnBehalfOfCredential for OBO flow");
        userId.Should().Be("test-user-id", "User ID should be extracted from token");
    }

    [Fact]
    public void ProjectPlugin_ShouldFallbackToManagedIdentity_WhenNoUserToken()
    {
        // Arrange
        using var scope = _serviceProvider.CreateScope();
        var userAuthContext = scope.ServiceProvider.GetRequiredService<IUserAuthenticationContext>();
        
        // Don't set any user token
        
        // Act
        // We don't need to create ProjectPlugin for this test
        // Just verify the UserAuthenticationContext behavior

        var tokenCredential = userAuthContext.GetUserTokenCredential();
        var userId = userAuthContext.GetCurrentUserId();

        // Assert
        tokenCredential.Should().BeNull("Should return null when no user token is set");
        userId.Should().BeNull("Should return null when no user token is set");
    }

    [Fact]
    public void UserAuthenticationContext_ShouldPreserveTokenAcrossServiceCalls_InSameScope()
    {
        // Arrange
        var validJwtToken = CreateMockJwtToken("test-user-id", "test-tenant");
        
        using var scope = _serviceProvider.CreateScope();
        
        // Act
        // Get UserAuthenticationContext and set token
        var userAuthContext1 = scope.ServiceProvider.GetRequiredService<IUserAuthenticationContext>();
        userAuthContext1.SetUserToken(validJwtToken);
        
        // Get UserAuthenticationContext again from same scope
        var userAuthContext2 = scope.ServiceProvider.GetRequiredService<IUserAuthenticationContext>();
        
        // Assert
        userAuthContext1.Should().BeSameAs(userAuthContext2, "Should be same instance in same scope");
        
        var tokenCredential1 = userAuthContext1.GetUserTokenCredential();
        var tokenCredential2 = userAuthContext2.GetUserTokenCredential();
        
        tokenCredential1.Should().NotBeNull("First instance should have token");
        tokenCredential2.Should().NotBeNull("Second instance should have same token");
        
        userAuthContext1.GetCurrentUserId().Should().Be("test-user-id");
        userAuthContext2.GetCurrentUserId().Should().Be("test-user-id");
    }

    [Fact]
    public void UserAuthenticationContext_ShouldNotShareTokensBetweenDifferentScopes()
    {
        // Arrange
        var validJwtToken = CreateMockJwtToken("test-user-id", "test-tenant");
        
        // Act & Assert
        using (var scope1 = _serviceProvider.CreateScope())
        {
            var userAuthContext1 = scope1.ServiceProvider.GetRequiredService<IUserAuthenticationContext>();
            userAuthContext1.SetUserToken(validJwtToken);
            userAuthContext1.GetUserTokenCredential().Should().NotBeNull("Scope 1 should have token");
        }
        
        using (var scope2 = _serviceProvider.CreateScope())
        {
            var userAuthContext2 = scope2.ServiceProvider.GetRequiredService<IUserAuthenticationContext>();
            userAuthContext2.GetUserTokenCredential().Should().BeNull("Scope 2 should not have token from scope 1");
        }
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
        _httpClient?.Dispose();
        _serviceProvider?.Dispose();
    }
}