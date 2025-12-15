using System.Text.Json.Serialization;

namespace AzureDevOpsAI.Backend.Models;

/// <summary>
/// Represents a Git repository in Azure DevOps.
/// </summary>
public class GitRepository
{
    /// <summary>
    /// Repository ID (GUID).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Repository name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Repository URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Project information for this repository.
    /// </summary>
    [JsonPropertyName("project")]
    public RepositoryProject? Project { get; set; }

    /// <summary>
    /// Default branch (e.g., "refs/heads/main").
    /// </summary>
    [JsonPropertyName("defaultBranch")]
    public string? DefaultBranch { get; set; }

    /// <summary>
    /// Size of the repository in bytes.
    /// </summary>
    [JsonPropertyName("size")]
    public long? Size { get; set; }

    /// <summary>
    /// Remote URL for cloning.
    /// </summary>
    [JsonPropertyName("remoteUrl")]
    public string? RemoteUrl { get; set; }

    /// <summary>
    /// SSH URL for cloning.
    /// </summary>
    [JsonPropertyName("sshUrl")]
    public string? SshUrl { get; set; }

    /// <summary>
    /// Web URL for browsing.
    /// </summary>
    [JsonPropertyName("webUrl")]
    public string? WebUrl { get; set; }

    /// <summary>
    /// Indicates if the repository is disabled.
    /// </summary>
    [JsonPropertyName("isDisabled")]
    public bool IsDisabled { get; set; }

    /// <summary>
    /// Indicates if the repository is a fork.
    /// </summary>
    [JsonPropertyName("isFork")]
    public bool? IsFork { get; set; }
}

/// <summary>
/// Project information for a repository.
/// </summary>
public class RepositoryProject
{
    /// <summary>
    /// Project ID (GUID).
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Project name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Project description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Project URL.
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// Project state.
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    /// <summary>
    /// Project visibility (private or public).
    /// </summary>
    [JsonPropertyName("visibility")]
    public string? Visibility { get; set; }
}

/// <summary>
/// Response from listing repositories.
/// </summary>
public class RepositoryListResponse
{
    /// <summary>
    /// List of repositories.
    /// </summary>
    [JsonPropertyName("value")]
    public List<GitRepository> Value { get; set; } = new();

    /// <summary>
    /// Count of repositories.
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }
}
