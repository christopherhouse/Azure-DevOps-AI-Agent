using System.ComponentModel;
using System.Text.Json;
using Microsoft.SemanticKernel;
using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Services;

namespace AzureDevOpsAI.Backend.Plugins;

/// <summary>
/// Semantic Kernel plugin for Azure DevOps subject query operations.
/// Provides functionality to search for users and groups using the Subject Query API.
/// </summary>
public class SubjectQueryPlugin
{
    private readonly IAzureDevOpsApiService _azureDevOpsApiService;
    private readonly ILogger<SubjectQueryPlugin> _logger;

    public SubjectQueryPlugin(
        IAzureDevOpsApiService azureDevOpsApiService,
        ILogger<SubjectQueryPlugin> logger)
    {
        _azureDevOpsApiService = azureDevOpsApiService;
        _logger = logger;
    }

    /// <summary>
    /// Query for users and/or groups in an Azure DevOps organization.
    /// This is the primary function for finding descriptors and details about users and groups.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization name</param>
    /// <param name="query">The search query - can be an email address, display name, or principal name</param>
    /// <param name="subjectKind">Optional filter for subject types: User, Group, or omit to search both. Can specify multiple types as an array.</param>
    /// <returns>List of matching subjects with their descriptors and details</returns>
    [KernelFunction("query_subjects")]
    [Description("Search for users and/or groups in Azure DevOps by email, display name, or principal name. This is the PRIMARY function to use when you need to find descriptors for users or groups. Returns descriptor, display name, principal name, email, origin, and other details. Use subjectKind with User and/or Group values to filter results.")]
    public async Task<string> QuerySubjectsAsync(
        [Description("The Azure DevOps organization name")] string organization,
        [Description("Search query - email address, display name, or principal name (e.g., 'john@example.com', 'John Doe', or '[ProjectName]\\GroupName')")] string query,
        [Description("Optional subject type filter: array of SubjectKind enum values (User and/or Group), or omit for both")] SubjectKind[]? subjectKind = null)
    {
        try
        {
            var subjectKindStr = subjectKind != null ? string.Join(", ", subjectKind) : "all";
            _logger.LogInformation("Querying subjects in organization: {Organization}, query: {Query}, subjectKind: {SubjectKind}",
                organization, query, subjectKindStr);

            if (string.IsNullOrWhiteSpace(query))
            {
                return JsonSerializer.Serialize(new { error = "Search query is required. Please provide an email, display name, or principal name." });
            }

            // Build the subject query request
            var subjectQueryRequest = new SubjectQueryRequest
            {
                SubjectQuery = query
            };

            // Add subject kind filter if specified
            if (subjectKind != null && subjectKind.Length > 0)
            {
                subjectQueryRequest.SubjectKind = subjectKind.ToList();
                _logger.LogInformation("Filtering by subject kind: {SubjectKind}", string.Join(", ", subjectKind));
            }
            else
            {
                _logger.LogInformation("No subject kind filter specified, searching both users and groups");
            }

            // Call the Subject Query API
            var apiPath = $"https://vssps.dev.azure.com/{organization}/_apis/graph/subjectquery";
            var response = await _azureDevOpsApiService.PostAsync<SubjectQueryResponse>(
                organization,
                apiPath,
                subjectQueryRequest,
                "7.1-preview.1");

            if (response == null || response.Value == null || response.Value.Count == 0)
            {
                return JsonSerializer.Serialize(new
                {
                    message = "No subjects found matching the query.",
                    query,
                    subjectKind = subjectKindStr,
                    results = new List<object>()
                });
            }

            // Format results for AI consumption
            var results = response.Value.Select(s => new
            {
                descriptor = s.Descriptor ?? "N/A",
                displayName = s.DisplayName ?? "Unknown",
                subjectKind = s.SubjectKind ?? "Unknown",
                principalName = s.PrincipalName ?? "N/A",
                mailAddress = s.MailAddress ?? "N/A",
                origin = s.Origin ?? "N/A",
                originId = s.OriginId ?? "N/A",
                domain = s.Domain ?? "N/A",
                directoryAlias = s.DirectoryAlias ?? "N/A"
            }).ToList();

            _logger.LogInformation("Successfully queried {Count} subjects for query: {Query}", results.Count, query);

            return JsonSerializer.Serialize(new
            {
                organization,
                query,
                subjectKind = subjectKindStr,
                totalResults = results.Count,
                results
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying subjects in organization: {Organization}, query: {Query}",
                organization, query);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
