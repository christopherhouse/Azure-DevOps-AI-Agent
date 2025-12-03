using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using AzureDevOpsAI.Backend.Services;
using AzureDevOpsAI.Backend.Models;
using Moq;

namespace AzureDevOpsAI.Backend.Tests;

/// <summary>
/// Custom WebApplicationFactory for testing that properly configures mock services.
/// </summary>
/// <typeparam name="TStartup">The startup class</typeparam>
public class TestWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup>
    where TStartup : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Set up test configuration that disables auth
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:DisableAuth"] = "true",
                ["Security:JwtSecretKey"] = "test-secret-key-that-is-long-enough-for-jwt-validation-and-meets-minimum-requirements",
                ["App:AppName"] = "Azure DevOps AI Agent Backend - Test",
                ["App:AppVersion"] = "1.0.0-test",
                ["App:Environment"] = "test",
                ["AzureOpenAI:Endpoint"] = "https://test.openai.azure.com/",
                ["AzureOpenAI:ApiKey"] = "test-api-key", 
                ["AzureOpenAI:ChatDeploymentName"] = "test-deployment",
                ["AzureOpenAI:ClientId"] = "test-client-id",
                ["ApplicationInsights:ConnectionString"] = "InstrumentationKey=test-key;IngestionEndpoint=https://test.in.applicationinsights.azure.com/",
                // CosmosDB configuration for tests - will use mocked service
                ["CosmosDb:Endpoint"] = "https://test-cosmos.documents.azure.com:443/",
                ["CosmosDb:DatabaseName"] = "TestDatabase",
                ["CosmosDb:UseManagedIdentity"] = "false"
            });
        });

        builder.ConfigureServices((context, services) =>
        {
            // Remove the real CosmosDbService and add a mock
            var cosmosDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICosmosDbService));
            if (cosmosDescriptor != null)
            {
                services.Remove(cosmosDescriptor);
            }

            // Add mock ICosmosDbService for tests
            var mockCosmosDbService = new Mock<ICosmosDbService>();
            mockCosmosDbService
                .Setup(x => x.GetChatHistoryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ChatHistoryDocument?)null);
            mockCosmosDbService
                .Setup(x => x.GetThoughtProcessAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ThoughtProcessDocument?)null);
            mockCosmosDbService
                .Setup(x => x.SaveChatHistoryAsync(It.IsAny<ChatHistoryDocument>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            mockCosmosDbService
                .Setup(x => x.SaveThoughtProcessAsync(It.IsAny<ThoughtProcessDocument>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            services.AddSingleton(mockCosmosDbService.Object);

            // Add mock ITokenAcquisition for tests that need it
            // This handles the case where authentication is disabled but services still depend on ITokenAcquisition
            var mockTokenAcquisition = new Mock<ITokenAcquisition>();
            mockTokenAcquisition
                .Setup(x => x.GetAccessTokenForUserAsync(It.IsAny<string[]>(), null, null, null, null))
                .ReturnsAsync("test-access-token");
            
            services.AddSingleton(mockTokenAcquisition.Object);
        });
    }
}