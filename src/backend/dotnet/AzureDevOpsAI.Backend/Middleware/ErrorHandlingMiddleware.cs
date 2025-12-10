using System.Net;
using System.Text.Json;
using AzureDevOpsAI.Backend.Models;

namespace AzureDevOpsAI.Backend.Middleware;

/// <summary>
/// Error response model.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error details.
    /// </summary>
    public ErrorDetails Error { get; set; } = new();
}

/// <summary>
/// Error details model.
/// </summary>
public class ErrorDetails
{
    /// <summary>
    /// Error code.
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// Error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Error type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Validation error details (for validation errors).
    /// </summary>
    public object? Details { get; set; }
}

/// <summary>
/// Global error handling middleware.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse();

        switch (exception)
        {
            case MfaChallengeException mfaEx:
                response.Error.Code = (int)HttpStatusCode.Unauthorized;
                response.Error.Message = "Multi-factor authentication is required";
                response.Error.Type = "mfa_required";
                response.Error.Details = new
                {
                    claimsChallenge = mfaEx.ClaimsChallenge,
                    scopes = mfaEx.Scopes,
                    correlationId = mfaEx.CorrelationId,
                    errorCode = mfaEx.MsalException.ErrorCode,
                    classification = mfaEx.MsalException.Classification.ToString()
                };
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                // Add WWW-Authenticate header for OAuth2 compliance
                context.Response.Headers["WWW-Authenticate"] =
                    $"Bearer error=\"insufficient_claims\", error_description=\"{mfaEx.ClaimsChallenge}\"";
                break;

            case ArgumentException argEx:
                response.Error.Code = (int)HttpStatusCode.BadRequest;
                response.Error.Message = argEx.Message;
                response.Error.Type = "validation_error";
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                break;

            case UnauthorizedAccessException:
                response.Error.Code = (int)HttpStatusCode.Unauthorized;
                response.Error.Message = "Unauthorized access";
                response.Error.Type = "authentication_error";
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                break;

            case NotImplementedException:
                response.Error.Code = (int)HttpStatusCode.NotImplemented;
                response.Error.Message = "Feature not implemented";
                response.Error.Type = "not_implemented_error";
                context.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                break;

            default:
                response.Error.Code = (int)HttpStatusCode.InternalServerError;
                response.Error.Message = "Internal server error";
                response.Error.Type = "server_error";
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}