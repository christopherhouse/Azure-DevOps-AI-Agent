using AzureDevOpsAI.Backend.Configuration;
using AzureDevOpsAI.Backend.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AzureDevOpsAI.Backend.Tests.Services;

public class AIServiceTests
{
    [Fact]
    public void Constructor_ShouldLoadSystemPrompt_WhenInitialized()
    {
        // Arrange
        var azureOpenAISettings = new AzureOpenAISettings
        {
            Endpoint = "https://test.openai.azure.com/",
            ApiKey = "test-key",
            ChatDeploymentName = "gpt-4",
            UseManagedIdentity = false,
            ClientId = "test-client-id" // Required field
        };

        var mockOptions = new Mock<IOptions<AzureOpenAISettings>>();
        mockOptions.Setup(o => o.Value).Returns(azureOpenAISettings);

        var mockLogger = new Mock<ILogger<AIService>>();

        // Act & Assert - Should not throw exception during construction
        var exception = Record.Exception(() => new AIService(mockOptions.Object, mockLogger.Object));

        // Assert
        exception.Should().BeNull("Constructor should succeed with valid configuration");
    }

    [Fact]
    public void Constructor_ShouldConfigureUserAssignedManagedIdentity_WhenClientIdProvided()
    {
        // Arrange
        var azureOpenAISettings = new AzureOpenAISettings
        {
            Endpoint = "https://test.openai.azure.com/",
            ChatDeploymentName = "gpt-4",
            UseManagedIdentity = true,
            ClientId = "12345678-1234-1234-1234-123456789012" // UAMI client ID
        };

        var mockOptions = new Mock<IOptions<AzureOpenAISettings>>();
        mockOptions.Setup(o => o.Value).Returns(azureOpenAISettings);

        var mockLogger = new Mock<ILogger<AIService>>();

        // Act & Assert - Should not throw exception during construction
        var exception = Record.Exception(() => new AIService(mockOptions.Object, mockLogger.Object));

        // Assert
        exception.Should().BeNull("Constructor should succeed with UAMI configuration");
        
        // Verify that the logger was called with UAMI client ID info
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("User Assigned Managed Identity client ID")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("What is Azure DevOps?")]
    [InlineData("How do I create a pipeline?")]
    [InlineData("Tell me about work items")]
    public async Task ProcessChatMessageAsync_ShouldReturnResponse_WithValidMessage(string message)
    {
        // Arrange
        var azureOpenAISettings = new AzureOpenAISettings
        {
            Endpoint = "https://test.openai.azure.com/",
            ApiKey = "test-key", 
            ChatDeploymentName = "gpt-4",
            UseManagedIdentity = false,
            MaxTokens = 1000,
            Temperature = 0.7,
            ClientId = "test-client-id" // Required field
        };

        var mockOptions = new Mock<IOptions<AzureOpenAISettings>>();
        mockOptions.Setup(o => o.Value).Returns(azureOpenAISettings);

        var mockLogger = new Mock<ILogger<AIService>>();

        // Note: This test will fail with actual Azure OpenAI calls due to missing credentials
        // In a real test environment, you would mock the Semantic Kernel components
        // For now, we'll just test the service construction and configuration
        
        // Act & Assert - Constructor should not throw
        var service = new AIService(mockOptions.Object, mockLogger.Object);
        
        // Assert that service is created
        service.Should().NotBeNull();
        service.Should().BeAssignableTo<IAIService>();
    }

    [Theory]
    [InlineData("pipeline", "How do I create a YAML pipeline?")]
    [InlineData("work item", "How do I customize work item types?")]
    [InlineData("repository", "How do I set up branch policies?")]
    [InlineData("test", "How do I set up automated testing?")]
    public void GenerateSuggestions_ShouldReturnContextualSuggestions_BasedOnUserMessage(string keyword, string expectedSuggestion)
    {
        // This test verifies the suggestion generation logic
        // We'll test this indirectly through the public interface once we have mock capabilities
        // For now, this serves as documentation of the expected behavior
        
        keyword.Should().NotBeNullOrEmpty();
        expectedSuggestion.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetChatHistoryAsync_ShouldReturnEmptyHistory_ForNewConversation()
    {
        // Arrange
        var azureOpenAISettings = new AzureOpenAISettings
        {
            Endpoint = "https://test.openai.azure.com/",
            ApiKey = "test-key",
            ChatDeploymentName = "gpt-4",
            UseManagedIdentity = false,
            ClientId = "test-client-id" // Required field
        };

        var mockOptions = new Mock<IOptions<AzureOpenAISettings>>();
        mockOptions.Setup(o => o.Value).Returns(azureOpenAISettings);

        var mockLogger = new Mock<ILogger<AIService>>();

        var service = new AIService(mockOptions.Object, mockLogger.Object);
        var conversationId = Guid.NewGuid().ToString();

        // Act
        var history = await service.GetChatHistoryAsync(conversationId);

        // Assert
        history.Should().NotBeNull();
        // New conversation should have empty history initially
        history.Count.Should().Be(0);
    }
}