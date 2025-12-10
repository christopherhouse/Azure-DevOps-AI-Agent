using AzureDevOpsAI.Backend.Models;

namespace AzureDevOpsAI.Backend.Services;

/// <summary>
/// Interface for Azure DevOps descriptor operations.
/// Descriptors are unique identifiers used in the Graph API for scoping operations to projects, organizations, users, and groups.
/// </summary>
public interface IAzureDevOpsDescriptorService
{
    /// <summary>
    /// Get the scope descriptor for a project or organization.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization name</param>
    /// <param name="storageKey">The storage key (project ID or organization ID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The scope descriptor value</returns>
    Task<string?> GetScopeDescriptorAsync(string organization, string storageKey, CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for retrieving Azure DevOps descriptors via the Graph API.
/// Descriptors are used to scope operations to specific projects, organizations, users, or groups.
/// </summary>
public class AzureDevOpsDescriptorService : IAzureDevOpsDescriptorService
{
    private readonly IAzureDevOpsApiService _azureDevOpsApiService;
    private readonly ILogger<AzureDevOpsDescriptorService> _logger;

    public AzureDevOpsDescriptorService(IAzureDevOpsApiService azureDevOpsApiService, ILogger<AzureDevOpsDescriptorService> logger)
    {
        _azureDevOpsApiService = azureDevOpsApiService;
        _logger = logger;
    }

    /// <summary>
    /// Get the scope descriptor for a project or organization.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization name</param>
    /// <param name="storageKey">The storage key (project ID or organization ID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The scope descriptor value, or null if not found</returns>
    public async Task<string?> GetScopeDescriptorAsync(string organization, string storageKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Getting scope descriptor for storage key: {StorageKey} in organization: {Organization}", storageKey, organization);

            // Use the Graph API to get the descriptor
            var descriptor = await _azureDevOpsApiService.GetAsync<GraphDescriptor>(
                organization,
                $"https://vssps.dev.azure.com/{organization}/_apis/graph/descriptors/{storageKey}",
                "7.1",
                cancellationToken);

            if (descriptor?.Value == null)
            {
                _logger.LogWarning("Failed to retrieve scope descriptor for storage key: {StorageKey}", storageKey);
                return null;
            }

            _logger.LogInformation("Successfully retrieved scope descriptor for storage key: {StorageKey}", storageKey);
            return descriptor.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting scope descriptor for storage key: {StorageKey} in organization: {Organization}", storageKey, organization);
            throw;
        }
    }
}
