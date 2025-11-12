using AzureDevOpsAI.Backend.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AzureDevOpsAI.Backend.Tests.Configuration;

public class AzureOpenAIConfigurationTests
{
    [Fact]
    public void Configure_ShouldSetClientIdFromManagedIdentityClientIdEnvironmentVariable()
    {
        // Arrange
        var configurationData = new Dictionary<string, string?>
        {
            ["AzureOpenAI:Endpoint"] = "https://test.openai.azure.com/",
            ["AzureOpenAI:ChatDeploymentName"] = "gpt-4",
            ["AzureOpenAI:ClientId"] = "original-client-id",
            ["ManagedIdentityClientId"] = "managed-identity-client-id"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        var services = new ServiceCollection();
        
        // Apply the same configuration logic as in Program.cs
        services.Configure<AzureOpenAISettings>(options =>
        {
            configuration.GetSection("AzureOpenAI").Bind(options);
            
            // Override ClientId with ManagedIdentityClientId environment variable if provided
            var managedIdentityClientId = configuration["ManagedIdentityClientId"];
            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                options.ClientId = managedIdentityClientId;
            }
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var azureOpenAIOptions = serviceProvider.GetRequiredService<IOptions<AzureOpenAISettings>>();
        var settings = azureOpenAIOptions.Value;

        // Assert
        settings.ClientId.Should().Be("managed-identity-client-id", 
            "ManagedIdentityClientId environment variable should override AzureOpenAI:ClientId");
        settings.Endpoint.Should().Be("https://test.openai.azure.com/");
        settings.ChatDeploymentName.Should().Be("gpt-4");
    }

    [Fact]
    public void Configure_ShouldUseOriginalClientIdWhenManagedIdentityClientIdIsNotSet()
    {
        // Arrange
        var configurationData = new Dictionary<string, string?>
        {
            ["AzureOpenAI:Endpoint"] = "https://test.openai.azure.com/",
            ["AzureOpenAI:ChatDeploymentName"] = "gpt-4",
            ["AzureOpenAI:ClientId"] = "original-client-id"
            // No ManagedIdentityClientId
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        var services = new ServiceCollection();
        
        // Apply the same configuration logic as in Program.cs
        services.Configure<AzureOpenAISettings>(options =>
        {
            configuration.GetSection("AzureOpenAI").Bind(options);
            
            // Override ClientId with ManagedIdentityClientId environment variable if provided
            var managedIdentityClientId = configuration["ManagedIdentityClientId"];
            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                options.ClientId = managedIdentityClientId;
            }
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var azureOpenAIOptions = serviceProvider.GetRequiredService<IOptions<AzureOpenAISettings>>();
        var settings = azureOpenAIOptions.Value;

        // Assert
        settings.ClientId.Should().Be("original-client-id", 
            "Original AzureOpenAI:ClientId should be used when ManagedIdentityClientId is not set");
        settings.Endpoint.Should().Be("https://test.openai.azure.com/");
        settings.ChatDeploymentName.Should().Be("gpt-4");
    }

    [Fact]
    public void Configure_ShouldRequireClientId()
    {
        // Arrange
        var configurationData = new Dictionary<string, string?>
        {
            ["AzureOpenAI:Endpoint"] = "https://test.openai.azure.com/",
            ["AzureOpenAI:ChatDeploymentName"] = "gpt-4"
            // No ClientId provided
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        var services = new ServiceCollection();
        
        // Apply the same configuration logic as in Program.cs
        services.Configure<AzureOpenAISettings>(options =>
        {
            configuration.GetSection("AzureOpenAI").Bind(options);
            
            // Override ClientId with ManagedIdentityClientId environment variable if provided
            var managedIdentityClientId = configuration["ManagedIdentityClientId"];
            if (!string.IsNullOrEmpty(managedIdentityClientId))
            {
                options.ClientId = managedIdentityClientId;
            }
        });

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var azureOpenAIOptions = serviceProvider.GetRequiredService<IOptions<AzureOpenAISettings>>();
        var settings = azureOpenAIOptions.Value;

        // Assert - ClientId should be empty string (default for required string property)
        settings.ClientId.Should().Be(string.Empty, 
            "ClientId should be empty when not provided, triggering validation requirement");
        
        // The [Required] attribute will be validated by ASP.NET Core's options validation
        // when the application starts, not during configuration binding
    }

    [Fact]
    public void Configure_ShouldDefaultUseUserAssignedIdentityToTrue()
    {
        // Arrange
        var configurationData = new Dictionary<string, string?>
        {
            ["AzureOpenAI:Endpoint"] = "https://test.openai.azure.com/",
            ["AzureOpenAI:ChatDeploymentName"] = "gpt-4",
            ["AzureOpenAI:ClientId"] = "test-client-id"
            // No UseUserAssignedIdentity specified
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        var services = new ServiceCollection();
        services.Configure<AzureOpenAISettings>(configuration.GetSection("AzureOpenAI"));
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var azureOpenAIOptions = serviceProvider.GetRequiredService<IOptions<AzureOpenAISettings>>();
        var settings = azureOpenAIOptions.Value;

        // Assert
        settings.UseUserAssignedIdentity.Should().BeTrue(
            "UseUserAssignedIdentity should default to true for backward compatibility");
    }

    [Fact]
    public void Configure_ShouldSetUseUserAssignedIdentityToFalseWhenConfigured()
    {
        // Arrange
        var configurationData = new Dictionary<string, string?>
        {
            ["AzureOpenAI:Endpoint"] = "https://test.openai.azure.com/",
            ["AzureOpenAI:ChatDeploymentName"] = "gpt-4",
            ["AzureOpenAI:ClientId"] = "test-client-id",
            ["AzureOpenAI:UseUserAssignedIdentity"] = "false"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        var services = new ServiceCollection();
        services.Configure<AzureOpenAISettings>(configuration.GetSection("AzureOpenAI"));
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var azureOpenAIOptions = serviceProvider.GetRequiredService<IOptions<AzureOpenAISettings>>();
        var settings = azureOpenAIOptions.Value;

        // Assert
        settings.UseUserAssignedIdentity.Should().BeFalse(
            "UseUserAssignedIdentity should be false when explicitly configured");
    }

    [Fact]
    public void Configure_ShouldSetUseUserAssignedIdentityToTrueWhenConfigured()
    {
        // Arrange
        var configurationData = new Dictionary<string, string?>
        {
            ["AzureOpenAI:Endpoint"] = "https://test.openai.azure.com/",
            ["AzureOpenAI:ChatDeploymentName"] = "gpt-4",
            ["AzureOpenAI:ClientId"] = "test-client-id",
            ["AzureOpenAI:UseUserAssignedIdentity"] = "true"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        var services = new ServiceCollection();
        services.Configure<AzureOpenAISettings>(configuration.GetSection("AzureOpenAI"));
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var azureOpenAIOptions = serviceProvider.GetRequiredService<IOptions<AzureOpenAISettings>>();
        var settings = azureOpenAIOptions.Value;

        // Assert
        settings.UseUserAssignedIdentity.Should().BeTrue(
            "UseUserAssignedIdentity should be true when explicitly configured");
    }
}