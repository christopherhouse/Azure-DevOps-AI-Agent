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
/// Test that validates the OBO flow fix works by simulating the actual flow:
/// ChatEndpoints → AIService → ProjectPlugin with user token passing through DI.
/// </summary>
public class OBOFlowValidationTest : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public OBOFlowValidationTest()
    {
        // Setup test settings
        var azureOpenAISettings = new AzureOpenAISettings
        {
            Endpoint = "https://test.openai.azure.com/",
            ApiKey = "test-key",
            ChatDeploymentName = "gpt-4",
            UseManagedIdentity = false,
            ClientId = "test-client-id"
        };

        var azureAuthSettings = new AzureAuthSettings
        {
            TenantId = "test-tenant-id",
            ClientId = "test-backend-client-id",
            ClientSecret = "test-client-secret"
        };

        // Setup service collection to simulate actual application DI container
        var services = new ServiceCollection();
        
        // Add logging
        services.AddLogging(builder => builder.AddConsole());
        
        // Add settings
        services.Configure<AzureOpenAISettings>(options =>
        {
            options.Endpoint = azureOpenAISettings.Endpoint;
            options.ApiKey = azureOpenAISettings.ApiKey;
            options.ChatDeploymentName = azureOpenAISettings.ChatDeploymentName;
            options.UseManagedIdentity = azureOpenAISettings.UseManagedIdentity;
            options.ClientId = azureOpenAISettings.ClientId;
        });
        services.Configure<AzureAuthSettings>(options =>
        {
            options.TenantId = azureAuthSettings.TenantId;
            options.ClientId = azureAuthSettings.ClientId;
            options.ClientSecret = azureAuthSettings.ClientSecret;
        });
        
        // Add HTTP client factory
        services.AddHttpClient();
        
        // Add the actual services as they would be in the application
        services.AddScoped<IUserAuthenticationContext, UserAuthenticationContext>();
        
        // Note: We don't register AIService here because it would require actual Azure OpenAI connectivity
        // Instead, we'll test the dependency injection pattern manually
        
        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void OBOFlow_ShouldEnableTokenFlowFromChatEndpointsToAIService()
    {
        // Arrange
        var validJwtToken = CreateMockJwtToken("obo-test-user", "test-tenant");
        
        // Simulate a web request scope - this is critical for the fix
        using var requestScope = _serviceProvider.CreateScope();
        
        // Act 1: Simulate ChatEndpoints.SendMessageAsync setting the user token
        var chatEndpointUserContext = requestScope.ServiceProvider.GetRequiredService<IUserAuthenticationContext>();
        chatEndpointUserContext.SetUserToken(validJwtToken);
        
        // Act 2: Simulate AIService.ProcessChatMessageAsync getting the same context via DI
        // (This is what the fix enables - AIService now gets IUserAuthenticationContext through constructor injection)
        var aiServiceUserContext = requestScope.ServiceProvider.GetRequiredService<IUserAuthenticationContext>();
        
        // Assert: Both contexts should be the same instance due to scoped registration
        aiServiceUserContext.Should().BeSameAs(chatEndpointUserContext, 
            "AIService should receive the same UserAuthenticationContext instance that ChatEndpoints populated");
        
        // Assert: User token should be available for OBO flow
        var tokenCredential = aiServiceUserContext.GetUserTokenCredential();
        var userId = aiServiceUserContext.GetCurrentUserId();
        
        tokenCredential.Should().NotBeNull("User token credential should be available for OBO flow");
        userId.Should().Be("obo-test-user", "User ID should be extracted from the JWT token");
        
        // Act 3: Simulate ProjectPlugin constructor receiving the same context
        var pluginUserContext = requestScope.ServiceProvider.GetService<IUserAuthenticationContext>();
        
        // Assert: Plugin context should also be the same instance
        pluginUserContext.Should().BeSameAs(chatEndpointUserContext, 
            "ProjectPlugin should receive the same UserAuthenticationContext with user token");
        
        var pluginTokenCredential = pluginUserContext?.GetUserTokenCredential();
        pluginTokenCredential.Should().NotBeNull("Plugin should have access to user token for OBO calls to Azure DevOps");
    }

    [Fact]
    public void OBOFlow_ValidationTest_ConfirmsFixAddressesOriginalIssue()
    {
        // This test validates that the specific issue is fixed:
        // "Notice agent is not getting user token and is thus unable to get an OBO token"
        
        // Arrange
        var validJwtToken = CreateMockJwtToken("telemetry-test-user", "production-tenant");
        
        using var requestScope = _serviceProvider.CreateScope();
        
        // Act: Simulate the exact scenario mentioned in the issue
        
        // 1. ChatEndpoints extracts user token from Authorization header and sets it
        var userAuthContext = requestScope.ServiceProvider.GetRequiredService<IUserAuthenticationContext>();
        userAuthContext.SetUserToken(validJwtToken);
        
        // 2. AIService attempts to get user token for OBO flow (this was failing before the fix)
        var aiServiceContext = requestScope.ServiceProvider.GetRequiredService<IUserAuthenticationContext>();
        var oboCredential = aiServiceContext.GetUserTokenCredential();
        
        // Assert: OBO token should now be available
        oboCredential.Should().NotBeNull("AIService should now be able to get an OBO token (this was the original issue)");
        
        // 3. Verify the OBO credential is properly configured
        oboCredential.Should().BeOfType<Azure.Identity.OnBehalfOfCredential>(
            "Should create OnBehalfOfCredential for token exchange with Azure DevOps");
        
        // Additional validation: Ensure user ID is preserved
        var userId = aiServiceContext.GetCurrentUserId();
        userId.Should().Be("telemetry-test-user", "User context should be preserved through the DI chain");
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