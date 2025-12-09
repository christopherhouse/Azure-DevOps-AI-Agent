using System.ComponentModel.DataAnnotations;

namespace AzureDevOpsAI.Backend.Configuration;

/// <summary>
/// Application configuration settings.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Application name.
    /// </summary>
    public string AppName { get; set; } = "Azure DevOps AI Agent Backend";

    /// <summary>
    /// Application version.
    /// </summary>
    public string AppVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Enable debug mode.
    /// </summary>
    public bool Debug { get; set; } = false;

    /// <summary>
    /// Environment name.
    /// </summary>
    public string Environment { get; set; } = "development";

    /// <summary>
    /// Server host.
    /// </summary>
    public string Host { get; set; } = "0.0.0.0";

    /// <summary>
    /// Server port.
    /// </summary>
    public int Port { get; set; } = 8000;
}

/// <summary>
/// Azure authentication configuration.
/// </summary>
public class AzureAuthSettings
{
    /// <summary>
    /// Azure AD instance URL (e.g., https://login.microsoftonline.com/).
    /// </summary>
    public string Instance { get; set; } = "https://login.microsoftonline.com/";

    /// <summary>
    /// Azure tenant ID.
    /// </summary>
    [Required]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Azure client ID.
    /// </summary>
    [Required]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Azure client secret.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// JWT authority URL.
    /// </summary>
    public string Authority => $"{Instance.TrimEnd('/')}/{TenantId}/v2.0";

    /// <summary>
    /// JWT audience.
    /// </summary>
    public string Audience => ClientId;
}

/// <summary>
/// Azure DevOps configuration.
/// </summary>
public class AzureDevOpsSettings
{
    /// <summary>
    /// Azure DevOps organization.
    /// </summary>
    public string? Organization { get; set; }

    /// <summary>
    /// Azure DevOps Personal Access Token.
    /// </summary>
    public string? Pat { get; set; }

    /// <summary>
    /// Use Personal Access Token for authentication. When false, uses Azure Identity (DefaultAzureCredential).
    /// </summary>
    public bool UsePat { get; set; } = false;
}

/// <summary>
/// Application Insights configuration.
/// </summary>
public class ApplicationInsightsSettings
{
    /// <summary>
    /// Application Insights connection string.
    /// </summary>
    public string? ConnectionString { get; set; }
}

/// <summary>
/// Azure OpenAI configuration.
/// </summary>
public class AzureOpenAISettings
{
    /// <summary>
    /// Azure OpenAI endpoint.
    /// </summary>
    [Required]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Azure OpenAI API key (optional if using managed identity).
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Azure OpenAI chat deployment name.
    /// </summary>
    [Required]
    public string ChatDeploymentName { get; set; } = string.Empty;

    /// <summary>
    /// Azure OpenAI API version.
    /// </summary>
    public string ApiVersion { get; set; } = "2024-02-01";

    /// <summary>
    /// Maximum tokens for chat completion.
    /// </summary>
    public int MaxTokens { get; set; } = 4000;

    /// <summary>
    /// Temperature for chat completion (0.0 to 1.0).
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Use managed identity for authentication instead of API key.
    /// </summary>
    public bool UseManagedIdentity { get; set; } = false;

    /// <summary>
    /// Azure client ID for User Assigned Managed Identity (required when using UAMI).
    /// </summary>
    [Required]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Use User Assigned Managed Identity. When false, ManagedIdentityCredential will use System Assigned Managed Identity.
    /// </summary>
    public bool UseUserAssignedIdentity { get; set; } = true;
}

/// <summary>
/// Security configuration.
/// </summary>
public class SecuritySettings
{
    /// <summary>
    /// JWT secret key.
    /// </summary>
    [Required]
    public string JwtSecretKey { get; set; } = string.Empty;

    /// <summary>
    /// JWT algorithm.
    /// </summary>
    public string JwtAlgorithm { get; set; } = "HS256";

    /// <summary>
    /// JWT expiration time in minutes.
    /// </summary>
    public int JwtExpireMinutes { get; set; } = 60;

    /// <summary>
    /// Feature flag to disable authentication for testing/development.
    /// </summary>
    public bool DisableAuth { get; set; } = true;
}

/// <summary>
/// Cosmos DB configuration settings.
/// </summary>
public class CosmosDbSettings
{
    /// <summary>
    /// Cosmos DB account endpoint URL. Required for the application to function.
    /// </summary>
    [Required]
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Database name. Defaults to "AzureDevOpsAIAgent" if not specified.
    /// </summary>
    public string DatabaseName { get; set; } = "AzureDevOpsAIAgent";

    /// <summary>
    /// Container name for chat history.
    /// </summary>
    public string ChatHistoryContainerName { get; set; } = "chat-history";

    /// <summary>
    /// Container name for thought process.
    /// </summary>
    public string ThoughtProcessContainerName { get; set; } = "thought-process";

    /// <summary>
    /// Use managed identity for authentication.
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;

    /// <summary>
    /// Client ID for User Assigned Managed Identity (if using UAMI).
    /// </summary>
    public string? ClientId { get; set; }
}