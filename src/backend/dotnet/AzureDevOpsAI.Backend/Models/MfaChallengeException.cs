using Microsoft.Identity.Client;

namespace AzureDevOpsAI.Backend.Models;

/// <summary>
/// Exception thrown when MFA is required for OBO token acquisition.
/// Contains the necessary claims and challenge information for the client to handle.
/// </summary>
public class MfaChallengeException : Exception
{
    /// <summary>
    /// The underlying MSAL UI required exception.
    /// </summary>
    public MsalUiRequiredException MsalException { get; }

    /// <summary>
    /// The scopes that were requested when MFA was required.
    /// </summary>
    public string[] Scopes { get; }

    /// <summary>
    /// The claims challenge from the MFA requirement.
    /// </summary>
    public string? ClaimsChallenge { get; }

    /// <summary>
    /// The correlation ID for troubleshooting.
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Initializes a new instance of the MfaChallengeException class.
    /// </summary>
    /// <param name="msalException">The underlying MSAL exception.</param>
    /// <param name="scopes">The scopes that were requested.</param>
    /// <param name="message">Optional custom message.</param>
    public MfaChallengeException(MsalUiRequiredException msalException, string[] scopes, string? message = null)
        : base(message ?? "Multi-factor authentication is required", msalException)
    {
        MsalException = msalException ?? throw new ArgumentNullException(nameof(msalException));
        Scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
        ClaimsChallenge = msalException.Claims;
        CorrelationId = msalException.CorrelationId;
    }
}