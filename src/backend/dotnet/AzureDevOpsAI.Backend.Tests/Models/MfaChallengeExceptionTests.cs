using Xunit;
using FluentAssertions;
using Microsoft.Identity.Client;
using AzureDevOpsAI.Backend.Models;

namespace AzureDevOpsAI.Backend.Tests.Models;

/// <summary>
/// Tests for the MfaChallengeException class.
/// </summary>
public class MfaChallengeExceptionTests
{
    [Fact]
    public void Constructor_WithMsalException_ShouldInitializePropertiesCorrectly()
    {
        // Arrange
        var scopes = new[] { "https://dev.azure.com/.default" };
        var claims = "eyJhY2Nlc3NfdG9rZW4iOnsibmJmIjp7ImVzc2VudGlhbCI6dHJ1ZX19fQ==";
        
        // Create a mock MSAL exception (we can't easily create a real one)
        var mockMsalException = new MsalUiRequiredException("AADSTS50079", "interaction_required");
        
        // Use reflection to set the Claims property since it's read-only
        var claimsProperty = typeof(MsalUiRequiredException).GetProperty("Claims");
        claimsProperty?.SetValue(mockMsalException, claims);
        
        // Act
        var exception = new MfaChallengeException(mockMsalException, scopes);

        // Assert
        exception.MsalException.Should().Be(mockMsalException);
        exception.Scopes.Should().BeEquivalentTo(scopes);
        exception.ClaimsChallenge.Should().Be(claims);
        exception.Message.Should().Be("Multi-factor authentication is required");
        exception.InnerException.Should().Be(mockMsalException);
    }

    [Fact]
    public void Constructor_WithCustomMessage_ShouldUseCustomMessage()
    {
        // Arrange
        var customMessage = "Custom MFA required message";
        var scopes = new[] { "scope1", "scope2" };
        var mockMsalException = new MsalUiRequiredException("AADSTS50079", "interaction_required");

        // Act
        var exception = new MfaChallengeException(mockMsalException, scopes, customMessage);

        // Assert
        exception.Message.Should().Be(customMessage);
        exception.Scopes.Should().BeEquivalentTo(scopes);
    }

    [Fact]
    public void Constructor_WithNullScopes_ShouldThrow()
    {
        // Arrange
        var mockMsalException = new MsalUiRequiredException("AADSTS50079", "interaction_required");

        // Act & Assert
        var act = () => new MfaChallengeException(mockMsalException, null!);
        act.Should().Throw<ArgumentNullException>();
    }
}