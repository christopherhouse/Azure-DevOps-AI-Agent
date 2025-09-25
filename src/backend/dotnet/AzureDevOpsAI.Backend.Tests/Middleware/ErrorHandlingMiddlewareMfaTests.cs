using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using Moq;
using System.Text;
using System.Text.Json;
using AzureDevOpsAI.Backend.Middleware;
using AzureDevOpsAI.Backend.Models;

namespace AzureDevOpsAI.Backend.Tests.Middleware;

/// <summary>
/// Tests for MFA challenge exception handling in ErrorHandlingMiddleware.
/// </summary>
public class ErrorHandlingMiddlewareMfaTests
{
    private readonly Mock<ILogger<ErrorHandlingMiddleware>> _logger;
    private readonly ErrorHandlingMiddleware _middleware;

    public ErrorHandlingMiddlewareMfaTests()
    {
        _logger = new Mock<ILogger<ErrorHandlingMiddleware>>();
        var next = new Mock<RequestDelegate>();
        _middleware = new ErrorHandlingMiddleware(next.Object, _logger.Object);
    }

    [Fact]
    public async Task HandleMfaChallengeException_ShouldReturn401WithCorrectResponse()
    {
        // Arrange
        var claims = "eyJhY2Nlc3NfdG9rZW4iOnsibmJmIjp7ImVzc2VudGlhbCI6dHJ1ZX19fQ==";
        var scopes = new[] { "https://dev.azure.com/.default" };
        
        var msalException = new MsalUiRequiredException("AADSTS50079", "interaction_required");
        var claimsProperty = typeof(MsalUiRequiredException).GetProperty("Claims");
        claimsProperty?.SetValue(msalException, claims);
        
        var mfaException = new MfaChallengeException(msalException, scopes);

        var httpContext = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;

        var next = new Mock<RequestDelegate>();
        next.Setup(x => x(httpContext)).ThrowsAsync(mfaException);

        var middleware = new ErrorHandlingMiddleware(next.Object, _logger.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.StatusCode.Should().Be(401);
        httpContext.Response.ContentType.Should().Be("application/json");
        
        // Verify WWW-Authenticate header
        httpContext.Response.Headers.Should().ContainKey("WWW-Authenticate");
        var authHeader = httpContext.Response.Headers["WWW-Authenticate"].ToString();
        authHeader.Should().Contain("Bearer error=\"insufficient_claims\"");
        authHeader.Should().Contain(claims);

        // Verify response body
        responseStream.Position = 0;
        var responseContent = await new StreamReader(responseStream).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        response.GetProperty("error").GetProperty("code").GetInt32().Should().Be(401);
        response.GetProperty("error").GetProperty("message").GetString().Should().Be("Multi-factor authentication is required");
        response.GetProperty("error").GetProperty("type").GetString().Should().Be("mfa_required");
        
        var details = response.GetProperty("error").GetProperty("details");
        details.GetProperty("claimsChallenge").GetString().Should().Be(claims);
        details.GetProperty("scopes").EnumerateArray().First().GetString().Should().Be(scopes[0]);
        details.GetProperty("errorCode").GetString().Should().Be("AADSTS50079");
        // We don't validate correlationId since it may be null or generated
        details.GetProperty("classification").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task HandleMfaChallengeException_WithoutClaimsChallenge_ShouldStillReturnCorrectResponse()
    {
        // Arrange
        var scopes = new[] { "https://dev.azure.com/.default" };
        var msalException = new MsalUiRequiredException("AADSTS50079", "interaction_required");
        var mfaException = new MfaChallengeException(msalException, scopes);

        var httpContext = new DefaultHttpContext();
        var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;

        var next = new Mock<RequestDelegate>();
        next.Setup(x => x(httpContext)).ThrowsAsync(mfaException);

        var middleware = new ErrorHandlingMiddleware(next.Object, _logger.Object);

        // Act
        await middleware.InvokeAsync(httpContext);

        // Assert
        httpContext.Response.StatusCode.Should().Be(401);
        
        // Verify response includes null claims challenge
        responseStream.Position = 0;
        var responseContent = await new StreamReader(responseStream).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        var details = response.GetProperty("error").GetProperty("details");
        details.GetProperty("claimsChallenge").ValueKind.Should().Be(JsonValueKind.Null);
        details.GetProperty("scopes").EnumerateArray().First().GetString().Should().Be(scopes[0]);
    }
}