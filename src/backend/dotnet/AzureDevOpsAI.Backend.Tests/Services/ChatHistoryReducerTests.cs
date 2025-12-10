using AzureDevOpsAI.Backend.Configuration;
using AzureDevOpsAI.Backend.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AzureDevOpsAI.Backend.Tests.Services;

public class ChatHistoryReducerTests
{
    private static (Mock<IOptions<AzureOpenAISettings>>, Mock<ILogger<AIService>>, Mock<IHttpClientFactory>, Mock<ILoggerFactory>, Mock<IAzureDevOpsApiService>, Mock<ICosmosDbService>) CreateMocks(AzureOpenAISettings settings)
    {
        var mockOptions = new Mock<IOptions<AzureOpenAISettings>>();
        mockOptions.Setup(o => o.Value).Returns(settings);

        var mockLogger = new Mock<ILogger<AIService>>();
        var mockHttpClientFactory = new Mock<IHttpClientFactory>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();
        var mockAzureDevOpsApiService = new Mock<IAzureDevOpsApiService>();
        var mockCosmosDbService = new Mock<ICosmosDbService>();
        
        // Setup HttpClient mock
        var mockHttpClient = new Mock<HttpClient>();
        mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(mockHttpClient.Object);
        
        // Setup LoggerFactory mock
        var mockPluginLogger = new Mock<ILogger>();
        mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(mockPluginLogger.Object);

        return (mockOptions, mockLogger, mockHttpClientFactory, mockLoggerFactory, mockAzureDevOpsApiService, mockCosmosDbService);
    }

    [Fact]
    public void Constructor_ShouldInitializeChatHistoryReducer_WhenEnabled()
    {
        // Arrange
        var azureOpenAISettings = new AzureOpenAISettings
        {
            Endpoint = "https://test.openai.azure.com/",
            ApiKey = "test-key",
            ChatDeploymentName = "gpt-4",
            UseManagedIdentity = false,
            ClientId = "test-client-id",
            EnableChatHistoryReducer = true,
            ChatHistoryReducerTargetCount = 15,
            ChatHistoryReducerThresholdCount = 5,
            ChatHistoryReducerUseSingleSummary = true
        };

        var (mockOptions, mockLogger, mockHttpClientFactory, mockLoggerFactory, mockAzureDevOpsApiService, mockCosmosDbService) = CreateMocks(azureOpenAISettings);

        // Act
        var exception = Record.Exception(() => new AIService(mockOptions.Object, mockLogger.Object, mockHttpClientFactory.Object, mockLoggerFactory.Object, mockAzureDevOpsApiService.Object, mockCosmosDbService.Object));

        // Assert
        exception.Should().BeNull("Constructor should succeed with chat history reducer enabled");
        
        // Verify that the logger was called with chat history reducer info
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Chat history reducer enabled")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_ShouldNotInitializeChatHistoryReducer_WhenDisabled()
    {
        // Arrange
        var azureOpenAISettings = new AzureOpenAISettings
        {
            Endpoint = "https://test.openai.azure.com/",
            ApiKey = "test-key",
            ChatDeploymentName = "gpt-4",
            UseManagedIdentity = false,
            ClientId = "test-client-id",
            EnableChatHistoryReducer = false
        };

        var (mockOptions, mockLogger, mockHttpClientFactory, mockLoggerFactory, mockAzureDevOpsApiService, mockCosmosDbService) = CreateMocks(azureOpenAISettings);

        // Act
        var exception = Record.Exception(() => new AIService(mockOptions.Object, mockLogger.Object, mockHttpClientFactory.Object, mockLoggerFactory.Object, mockAzureDevOpsApiService.Object, mockCosmosDbService.Object));

        // Assert
        exception.Should().BeNull("Constructor should succeed with chat history reducer disabled");
        
        // Verify that the logger was called with disabled message
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Chat history reducer disabled")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData(10, 5)]
    [InlineData(15, 5)]
    [InlineData(20, 10)]
    public void Constructor_ShouldAcceptVariousReducerConfiguration_Values(int targetCount, int thresholdCount)
    {
        // Arrange
        var azureOpenAISettings = new AzureOpenAISettings
        {
            Endpoint = "https://test.openai.azure.com/",
            ApiKey = "test-key",
            ChatDeploymentName = "gpt-4",
            UseManagedIdentity = false,
            ClientId = "test-client-id",
            EnableChatHistoryReducer = true,
            ChatHistoryReducerTargetCount = targetCount,
            ChatHistoryReducerThresholdCount = thresholdCount,
            ChatHistoryReducerUseSingleSummary = true
        };

        var (mockOptions, mockLogger, mockHttpClientFactory, mockLoggerFactory, mockAzureDevOpsApiService, mockCosmosDbService) = CreateMocks(azureOpenAISettings);

        // Act
        var exception = Record.Exception(() => new AIService(mockOptions.Object, mockLogger.Object, mockHttpClientFactory.Object, mockLoggerFactory.Object, mockAzureDevOpsApiService.Object, mockCosmosDbService.Object));

        // Assert
        exception.Should().BeNull($"Constructor should succeed with TargetCount={targetCount} and ThresholdCount={thresholdCount}");
    }
}
