using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Azure.Identity;
using Azure.Core;
using AzureDevOpsAI.Backend.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace AzureDevOpsAI.Backend.HealthChecks;

/// <summary>
/// Health check for Azure OpenAI service connectivity and availability.
/// </summary>
public class AzureOpenAIHealthCheck : IHealthCheck
{
    private const string HealthCheckMessage = "ping";
    private readonly AzureOpenAISettings _settings;
    private readonly ILogger<AzureOpenAIHealthCheck> _logger;

    public AzureOpenAIHealthCheck(IOptions<AzureOpenAISettings> settings, ILogger<AzureOpenAIHealthCheck> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate configuration
            if (string.IsNullOrEmpty(_settings.Endpoint))
            {
                _logger.LogError("Azure OpenAI endpoint is not configured");
                return HealthCheckResult.Unhealthy("Azure OpenAI endpoint is not configured");
            }

            if (string.IsNullOrEmpty(_settings.ChatDeploymentName))
            {
                _logger.LogError("Azure OpenAI chat deployment name is not configured");
                return HealthCheckResult.Unhealthy("Azure OpenAI chat deployment name is not configured");
            }

            // Build a minimal Semantic Kernel instance for health check
            var kernelBuilder = Kernel.CreateBuilder();

            if (_settings.UseManagedIdentity)
            {
                TokenCredential credential;

                if (_settings.UseUserAssignedIdentity && !string.IsNullOrEmpty(_settings.ClientId))
                {
                    credential = new ManagedIdentityCredential(_settings.ClientId);
                }
                else
                {
                    credential = new ManagedIdentityCredential();
                }

                kernelBuilder.AddAzureOpenAIChatCompletion(
                    deploymentName: _settings.ChatDeploymentName,
                    endpoint: _settings.Endpoint,
                    credentials: credential);
            }
            else if (!string.IsNullOrEmpty(_settings.ApiKey))
            {
                kernelBuilder.AddAzureOpenAIChatCompletion(
                    deploymentName: _settings.ChatDeploymentName,
                    endpoint: _settings.Endpoint,
                    apiKey: _settings.ApiKey);
            }
            else
            {
                _logger.LogError("Azure OpenAI authentication not configured. Either enable managed identity or provide an API key.");
                return HealthCheckResult.Unhealthy("Azure OpenAI authentication not configured");
            }

            var kernel = kernelBuilder.Build();
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // Create a minimal chat history for health check
            var chatHistory = new ChatHistory();
            chatHistory.AddUserMessage(HealthCheckMessage);

            // Set a short timeout for health check
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(10));

            // Make a minimal test request to verify the service is responding
            // Note: Semantic Kernel doesn't expose simpler health check endpoints,
            // so we use a minimal chat completion with MaxTokens=1 to minimize cost
            var executionSettings = new AzureOpenAIPromptExecutionSettings
            {
                MaxTokens = 1 // Minimize token usage for health check
            };

            var response = await chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                kernel,
                cts.Token);

            if (response != null)
            {
                _logger.LogDebug("Azure OpenAI health check passed. Deployment: {DeploymentName}", _settings.ChatDeploymentName);
                return HealthCheckResult.Healthy($"Azure OpenAI is accessible. Deployment: {_settings.ChatDeploymentName}");
            }

            _logger.LogWarning("Azure OpenAI health check: Received empty response");
            return HealthCheckResult.Degraded("Azure OpenAI returned empty response");
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 401)
        {
            _logger.LogError(ex, "Azure OpenAI health check failed: Authentication error");
            return HealthCheckResult.Unhealthy("Azure OpenAI authentication failed", ex);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogError(ex, "Azure OpenAI health check failed: Deployment not found");
            return HealthCheckResult.Unhealthy($"Azure OpenAI deployment '{_settings.ChatDeploymentName}' not found", ex);
        }
        catch (Azure.RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure OpenAI health check failed with status code: {StatusCode}", ex.Status);
            return HealthCheckResult.Unhealthy($"Azure OpenAI error: {ex.Message}", ex);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Azure OpenAI health check timed out");
            return HealthCheckResult.Degraded("Azure OpenAI health check timed out");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure OpenAI health check failed with unexpected error");
            return HealthCheckResult.Unhealthy($"Azure OpenAI connection failed: {ex.Message}", ex);
        }
    }
}
