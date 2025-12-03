using AzureDevOpsAI.Backend.Configuration;
using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AzureDevOpsAI.Backend.Tests.Services;

public class CosmosDbServiceTests
{
    [Fact]
    public void Constructor_ShouldThrowException_WhenManagedIdentityDisabledAndNoEndpoint()
    {
        // Arrange
        var settings = new CosmosDbSettings
        {
            Endpoint = "https://test.documents.azure.com:443/",
            DatabaseName = "TestDb",
            UseManagedIdentity = false // This should throw
        };

        var mockOptions = new Mock<IOptions<CosmosDbSettings>>();
        mockOptions.Setup(o => o.Value).Returns(settings);

        var mockLogger = new Mock<ILogger<CosmosDbService>>();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            new CosmosDbService(mockOptions.Object, mockLogger.Object));
    }

    [Fact]
    public void CosmosDbSettings_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var settings = new CosmosDbSettings();

        // Assert
        settings.DatabaseName.Should().Be("AzureDevOpsAIAgent");
        settings.ChatHistoryContainerName.Should().Be("chat-history");
        settings.ThoughtProcessContainerName.Should().Be("thought-process");
        settings.UseManagedIdentity.Should().BeTrue();
    }

    [Fact]
    public void ChatHistoryDocument_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var document = new ChatHistoryDocument
        {
            Id = "test-id",
            ConversationId = "conversation-123"
        };

        // Assert
        document.Messages.Should().NotBeNull();
        document.Messages.Should().BeEmpty();
        document.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        document.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ChatHistoryDocument_ShouldStoreMessages()
    {
        // Arrange
        var document = new ChatHistoryDocument
        {
            Id = "test-id",
            ConversationId = "conversation-123",
            SystemPrompt = "You are a helpful assistant"
        };

        // Act
        document.Messages.Add(new ChatMessageEntry
        {
            Role = "system",
            Content = "You are a helpful assistant"
        });
        document.Messages.Add(new ChatMessageEntry
        {
            Role = "user",
            Content = "Hello"
        });
        document.Messages.Add(new ChatMessageEntry
        {
            Role = "assistant",
            Content = "Hi there!"
        });

        // Assert
        document.Messages.Should().HaveCount(3);
        document.Messages[0].Role.Should().Be("system");
        document.Messages[1].Role.Should().Be("user");
        document.Messages[2].Role.Should().Be("assistant");
    }

    [Fact]
    public void ThoughtProcessDocument_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var document = new ThoughtProcessDocument
        {
            Id = "thought-123",
            ConversationId = "conversation-123",
            MessageId = "message-456"
        };

        // Assert
        document.Steps.Should().NotBeNull();
        document.Steps.Should().BeEmpty();
        document.ToolInvocations.Should().NotBeNull();
        document.ToolInvocations.Should().BeEmpty();
        document.StartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        document.EndTime.Should().BeNull();
        document.DurationMs.Should().BeNull();
    }

    [Fact]
    public void ThoughtProcessDocument_ShouldStoreSteps()
    {
        // Arrange
        var document = new ThoughtProcessDocument
        {
            Id = "thought-123",
            ConversationId = "conversation-123",
            MessageId = "message-456"
        };

        // Act
        document.Steps.Add(new ThoughtStepEntry
        {
            Id = "step-1",
            Description = "Analyzing user message",
            Type = "analysis",
            Details = new Dictionary<string, object>
            {
                ["message_length"] = 100
            }
        });

        document.EndTime = DateTime.UtcNow;
        document.DurationMs = 150;

        // Assert
        document.Steps.Should().HaveCount(1);
        document.Steps[0].Type.Should().Be("analysis");
        document.EndTime.Should().NotBeNull();
        document.DurationMs.Should().Be(150);
    }

    [Fact]
    public void ChatMessageEntry_ShouldInitializeWithDefaults()
    {
        // Arrange & Act
        var entry = new ChatMessageEntry
        {
            Role = "user",
            Content = "Hello world"
        };

        // Assert
        entry.Role.Should().Be("user");
        entry.Content.Should().Be("Hello world");
        entry.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ThoughtStepEntry_ShouldStoreDetails()
    {
        // Arrange & Act
        var step = new ThoughtStepEntry
        {
            Id = "step-1",
            Description = "Processing request",
            Type = "processing",
            Details = new Dictionary<string, object>
            {
                ["key1"] = "value1",
                ["key2"] = 42
            }
        };

        // Assert
        step.Details.Should().NotBeNull();
        step.Details.Should().ContainKey("key1");
        step.Details.Should().ContainKey("key2");
        step.Details!["key1"].Should().Be("value1");
        step.Details!["key2"].Should().Be(42);
    }

    [Fact]
    public void ToolInvocationEntry_ShouldStoreAllProperties()
    {
        // Arrange & Act
        var invocation = new ToolInvocationEntry
        {
            ToolName = "ProjectPlugin.GetProjects",
            Parameters = new Dictionary<string, object>
            {
                ["organizationUrl"] = "https://dev.azure.com/test"
            },
            Result = new { Count = 5 },
            Status = "success",
            ErrorMessage = null
        };

        // Assert
        invocation.ToolName.Should().Be("ProjectPlugin.GetProjects");
        invocation.Parameters.Should().NotBeNull();
        invocation.Status.Should().Be("success");
        invocation.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ToolInvocationEntry_ShouldHandleErrors()
    {
        // Arrange & Act
        var invocation = new ToolInvocationEntry
        {
            ToolName = "ProjectPlugin.GetProjects",
            Status = "error",
            ErrorMessage = "Connection refused"
        };

        // Assert
        invocation.Status.Should().Be("error");
        invocation.ErrorMessage.Should().Be("Connection refused");
    }
}
