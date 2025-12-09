using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Plugins;
using AzureDevOpsAI.Backend.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace AzureDevOpsAI.Backend.Tests.Plugins;

public class UsersPluginTests
{
    private readonly Mock<IAzureDevOpsApiService> _mockApiService;
    private readonly Mock<ILogger<UsersPlugin>> _mockLogger;
    private readonly UsersPlugin _plugin;

    public UsersPluginTests()
    {
        _mockApiService = new Mock<IAzureDevOpsApiService>();
        _mockLogger = new Mock<ILogger<UsersPlugin>>();
        _plugin = new UsersPlugin(_mockApiService.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializePlugin()
    {
        // Arrange & Act
        var plugin = new UsersPlugin(_mockApiService.Object, _mockLogger.Object);

        // Assert
        plugin.Should().NotBeNull();
    }

    [Fact]
    public async Task ListUsersAsync_ShouldReturnFormattedList_WhenUsersExist()
    {
        // Arrange
        var organization = "test-org";
        var userEntitlements = new UserEntitlementListResponse
        {
            Items = new List<UserEntitlement>
            {
                new UserEntitlement
                {
                    Id = "entitlement-1",
                    User = new User
                    {
                        Id = "user-guid-1",
                        DisplayName = "John Doe",
                        PrincipalName = "john.doe@contoso.com",
                        SubjectKind = "user"
                    },
                    AccessLevel = new AccessLevel
                    {
                        LicenseDisplayName = "Visual Studio Enterprise",
                        AccountLicenseType = "advanced",
                        Status = "Active"
                    },
                    LastAccessedDate = new DateTime(2024, 1, 15, 10, 30, 0),
                    DateCreated = new DateTime(2023, 6, 1)
                },
                new UserEntitlement
                {
                    Id = "entitlement-2",
                    User = new User
                    {
                        Id = "user-guid-2",
                        DisplayName = "Jane Smith",
                        PrincipalName = "jane.smith@contoso.com",
                        SubjectKind = "user"
                    },
                    AccessLevel = new AccessLevel
                    {
                        LicenseDisplayName = "Stakeholder",
                        AccountLicenseType = "stakeholder",
                        Status = "Active"
                    },
                    LastAccessedDate = new DateTime(2024, 2, 20, 14, 45, 0),
                    DateCreated = new DateTime(2023, 8, 15)
                }
            },
            TotalCount = 2
        };

        _mockApiService
            .Setup(x => x.GetAsync<UserEntitlementListResponse>(
                organization, 
                "https://vsaex.dev.azure.com/test-org/_apis/userentitlements", 
                "7.1", 
                default))
            .ReturnsAsync(userEntitlements);

        // Act
        var result = await _plugin.ListUsersAsync(organization);

        // Assert - verify JSON format
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("organization").GetString().Should().Be("test-org");
        jsonDoc.RootElement.GetProperty("totalUsers").GetInt32().Should().Be(2);
        
        var usersArray = jsonDoc.RootElement.GetProperty("users");
        usersArray.GetArrayLength().Should().Be(2);
        
        var firstUser = usersArray[0];
        firstUser.GetProperty("displayName").GetString().Should().Be("John Doe");
        firstUser.GetProperty("principalName").GetString().Should().Be("john.doe@contoso.com");
        firstUser.GetProperty("userId").GetString().Should().Be("user-guid-1");
        firstUser.GetProperty("license").GetString().Should().Be("Visual Studio Enterprise");
        firstUser.GetProperty("status").GetString().Should().Be("Active");
        
        var secondUser = usersArray[1];
        secondUser.GetProperty("displayName").GetString().Should().Be("Jane Smith");
        secondUser.GetProperty("principalName").GetString().Should().Be("jane.smith@contoso.com");
        secondUser.GetProperty("userId").GetString().Should().Be("user-guid-2");
        secondUser.GetProperty("license").GetString().Should().Be("Stakeholder");
    }

    [Fact]
    public async Task ListUsersAsync_ShouldReturnNoUsersMessage_WhenNoUsersExist()
    {
        // Arrange
        var organization = "empty-org";
        var emptyUsers = new UserEntitlementListResponse
        {
            Items = new List<UserEntitlement>(),
            TotalCount = 0
        };

        _mockApiService
            .Setup(x => x.GetAsync<UserEntitlementListResponse>(
                organization, 
                "https://vsaex.dev.azure.com/empty-org/_apis/userentitlements", 
                "7.1", 
                default))
            .ReturnsAsync(emptyUsers);

        // Act
        var result = await _plugin.ListUsersAsync(organization);

        // Assert - verify JSON format
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("message").GetString().Should().Be("No users found in this organization.");
        jsonDoc.RootElement.GetProperty("users").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task ListUsersAsync_ShouldReturnNoUsersMessage_WhenNullResponse()
    {
        // Arrange
        var organization = "null-org";

        _mockApiService
            .Setup(x => x.GetAsync<UserEntitlementListResponse>(
                organization, 
                "https://vsaex.dev.azure.com/null-org/_apis/userentitlements", 
                "7.1", 
                default))
            .ReturnsAsync((UserEntitlementListResponse?)null);

        // Act
        var result = await _plugin.ListUsersAsync(organization);

        // Assert - verify JSON format
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("message").GetString().Should().Be("No users found in this organization.");
        jsonDoc.RootElement.GetProperty("users").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task ListUsersAsync_ShouldReturnErrorMessage_WhenExceptionOccurs()
    {
        // Arrange
        var organization = "error-org";
        var exceptionMessage = "API call failed";

        _mockApiService
            .Setup(x => x.GetAsync<UserEntitlementListResponse>(
                organization, 
                It.IsAny<string>(), 
                "7.1", 
                default))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _plugin.ListUsersAsync(organization);

        // Assert - verify JSON format
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Be(exceptionMessage);
    }

    [Fact]
    public async Task ListUsersAsync_ShouldHandleUserWithoutAccessLevel()
    {
        // Arrange
        var organization = "test-org";
        var userEntitlements = new UserEntitlementListResponse
        {
            Items = new List<UserEntitlement>
            {
                new UserEntitlement
                {
                    Id = "entitlement-1",
                    User = new User
                    {
                        Id = "user-guid-1",
                        DisplayName = "John Doe",
                        PrincipalName = "john.doe@contoso.com"
                    },
                    AccessLevel = null
                }
            },
            TotalCount = 1
        };

        _mockApiService
            .Setup(x => x.GetAsync<UserEntitlementListResponse>(
                organization, 
                "https://vsaex.dev.azure.com/test-org/_apis/userentitlements", 
                "7.1", 
                default))
            .ReturnsAsync(userEntitlements);

        // Act
        var result = await _plugin.ListUsersAsync(organization);

        // Assert - verify JSON format
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("totalUsers").GetInt32().Should().Be(1);
        
        var usersArray = jsonDoc.RootElement.GetProperty("users");
        var firstUser = usersArray[0];
        firstUser.GetProperty("displayName").GetString().Should().Be("John Doe");
        firstUser.GetProperty("license").GetString().Should().Be("N/A");
    }

    [Fact]
    public async Task ListUsersAsync_ShouldLogInformationOnSuccess()
    {
        // Arrange
        var organization = "test-org";
        var userEntitlements = new UserEntitlementListResponse
        {
            Items = new List<UserEntitlement>
            {
                new UserEntitlement
                {
                    User = new User
                    {
                        DisplayName = "Test User",
                        PrincipalName = "test@contoso.com"
                    }
                }
            },
            TotalCount = 1
        };

        _mockApiService
            .Setup(x => x.GetAsync<UserEntitlementListResponse>(
                organization, 
                It.IsAny<string>(), 
                "7.1", 
                default))
            .ReturnsAsync(userEntitlements);

        // Act
        await _plugin.ListUsersAsync(organization);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Listing users for organization")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully retrieved")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task AddUserEntitlementAsync_ShouldAddUserWithoutProjectEntitlements_WhenNoneProvided()
    {
        // Arrange
        var organization = "test-org";
        var principalName = "newuser@contoso.com";
        var licenseType = "express";

        _mockApiService
            .Setup(x => x.PostAsync<object>(
                organization,
                "https://vsaex.dev.azure.com/test-org/_apis/userentitlements",
                It.IsAny<AddUserEntitlementRequest>(),
                "7.1",
                default))
            .ReturnsAsync(new { success = true });

        // Act
        var result = await _plugin.AddUserEntitlementAsync(organization, principalName, licenseType, null);

        // Assert - verify JSON format
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        jsonDoc.RootElement.GetProperty("user").GetString().Should().Be("newuser@contoso.com");
        jsonDoc.RootElement.GetProperty("license").GetString().Should().Be("express");
        jsonDoc.RootElement.GetProperty("hasProjectAccess").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task AddUserEntitlementAsync_ShouldAddUserWithProjectEntitlements_WhenValidDataProvided()
    {
        // Arrange
        var organization = "test-org";
        var principalName = "newuser@contoso.com";
        var licenseType = "express";
        var projectEntitlementsJson = "[{\"projectId\":\"proj-guid-1\",\"groupType\":\"projectContributor\"}]";

        _mockApiService
            .Setup(x => x.PostAsync<object>(
                organization,
                "https://vsaex.dev.azure.com/test-org/_apis/userentitlements",
                It.IsAny<AddUserEntitlementRequest>(),
                "7.1",
                default))
            .ReturnsAsync(new { success = true });

        // Act
        var result = await _plugin.AddUserEntitlementAsync(organization, principalName, licenseType, projectEntitlementsJson);

        // Assert - verify JSON format
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        jsonDoc.RootElement.GetProperty("user").GetString().Should().Be("newuser@contoso.com");
        jsonDoc.RootElement.GetProperty("license").GetString().Should().Be("express");
        jsonDoc.RootElement.GetProperty("hasProjectAccess").GetBoolean().Should().BeTrue();
        
        var projectEntitlements = jsonDoc.RootElement.GetProperty("projectEntitlements");
        projectEntitlements.GetArrayLength().Should().Be(1);
        projectEntitlements[0].GetProperty("projectId").GetString().Should().Be("proj-guid-1");
        projectEntitlements[0].GetProperty("groupType").GetString().Should().Be("projectContributor");
    }

    [Fact]
    public async Task AddUserEntitlementAsync_ShouldReturnError_WhenPrincipalNameIsEmpty()
    {
        // Arrange
        var organization = "test-org";
        var principalName = "";
        var licenseType = "express";

        // Act
        var result = await _plugin.AddUserEntitlementAsync(organization, principalName, licenseType, null);

        // Assert - verify JSON format
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Be("Principal name (email address) is required.");
    }

    [Fact]
    public async Task AddUserEntitlementAsync_ShouldReturnError_WhenLicenseTypeIsEmpty()
    {
        // Arrange
        var organization = "test-org";
        var principalName = "newuser@contoso.com";
        var licenseType = "";

        // Act
        var result = await _plugin.AddUserEntitlementAsync(organization, principalName, licenseType, null);

        // Assert - verify JSON format
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Account license type is required");
    }

    [Fact]
    public async Task AddUserEntitlementAsync_ShouldReturnError_WhenLicenseTypeIsInvalid()
    {
        // Arrange
        var organization = "test-org";
        var principalName = "newuser@contoso.com";
        var licenseType = "invalid-license";

        // Act
        var result = await _plugin.AddUserEntitlementAsync(organization, principalName, licenseType, null);

        // Assert - verify JSON format
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Invalid license type");
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Valid values: express, stakeholder, advanced, professional, earlyAdopter");
    }

    [Fact]
    public async Task AddUserEntitlementAsync_ShouldReturnError_WhenProjectEntitlementsJsonIsInvalid()
    {
        // Arrange
        var organization = "test-org";
        var principalName = "newuser@contoso.com";
        var licenseType = "express";
        var invalidJson = "not valid json";

        // Act
        var result = await _plugin.AddUserEntitlementAsync(organization, principalName, licenseType, invalidJson);

        // Assert - verify JSON format
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Invalid project entitlements JSON format");
    }

    [Fact]
    public async Task AddUserEntitlementAsync_ShouldNormalizeLicenseType()
    {
        // Arrange
        var organization = "test-org";
        var principalName = "newuser@contoso.com";
        var licenseType = "Express"; // uppercase
        var projectEntitlementsJson = "[{\"projectId\":\"proj-guid-1\",\"groupType\":\"projectContributor\"}]";

        AddUserEntitlementRequest? capturedRequest = null;
        _mockApiService
            .Setup(x => x.PostAsync<object>(
                organization,
                It.IsAny<string>(),
                It.IsAny<AddUserEntitlementRequest>(),
                "7.1",
                default))
            .Callback<string, string, object, string, CancellationToken>((org, url, req, ver, ct) =>
            {
                capturedRequest = req as AddUserEntitlementRequest;
            })
            .ReturnsAsync(new { success = true });

        // Act
        var result = await _plugin.AddUserEntitlementAsync(organization, principalName, licenseType, projectEntitlementsJson);

        // Assert - verify JSON format  
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        capturedRequest.Should().NotBeNull();
        capturedRequest!.AccessLevel.AccountLicenseType.Should().Be("express"); // normalized to lowercase
    }

    [Fact]
    public async Task AddUserEntitlementAsync_ShouldReturnError_WhenApiCallFails()
    {
        // Arrange
        var organization = "test-org";
        var principalName = "newuser@contoso.com";
        var licenseType = "express";
        var projectEntitlementsJson = "[{\"projectId\":\"proj-guid-1\",\"groupType\":\"projectContributor\"}]";

        _mockApiService
            .Setup(x => x.PostAsync<object>(
                organization,
                It.IsAny<string>(),
                It.IsAny<AddUserEntitlementRequest>(),
                "7.1",
                default))
            .ReturnsAsync((object?)null);

        // Act
        var result = await _plugin.AddUserEntitlementAsync(organization, principalName, licenseType, projectEntitlementsJson);

        // Assert - verify JSON format
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Failed to add user entitlement");
    }

    [Fact]
    public async Task AddUserEntitlementAsync_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        var organization = "test-org";
        var principalName = "newuser@contoso.com";
        var licenseType = "express";
        var projectEntitlementsJson = "[{\"projectId\":\"proj-guid-1\",\"groupType\":\"projectContributor\"}]";
        var exceptionMessage = "API call failed";

        _mockApiService
            .Setup(x => x.PostAsync<object>(
                organization,
                It.IsAny<string>(),
                It.IsAny<AddUserEntitlementRequest>(),
                "7.1",
                default))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _plugin.AddUserEntitlementAsync(organization, principalName, licenseType, projectEntitlementsJson);

        // Assert - verify JSON format
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Be(exceptionMessage);
    }

    [Fact]
    public async Task AddUserEntitlementAsync_ShouldHandleMultipleProjectEntitlements()
    {
        // Arrange
        var organization = "test-org";
        var principalName = "newuser@contoso.com";
        var licenseType = "advanced";
        var projectEntitlementsJson = "[{\"projectId\":\"proj-guid-1\",\"groupType\":\"projectContributor\"},{\"projectId\":\"proj-guid-2\",\"groupType\":\"projectReader\"}]";

        _mockApiService
            .Setup(x => x.PostAsync<object>(
                organization,
                It.IsAny<string>(),
                It.IsAny<AddUserEntitlementRequest>(),
                "7.1",
                default))
            .ReturnsAsync(new { success = true });

        // Act
        var result = await _plugin.AddUserEntitlementAsync(organization, principalName, licenseType, projectEntitlementsJson);

        // Assert - verify JSON format
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        
        var projectEntitlements = jsonDoc.RootElement.GetProperty("projectEntitlements");
        projectEntitlements.GetArrayLength().Should().Be(2);
        projectEntitlements[0].GetProperty("projectId").GetString().Should().Be("proj-guid-1");
        projectEntitlements[0].GetProperty("groupType").GetString().Should().Be("projectContributor");
        projectEntitlements[1].GetProperty("projectId").GetString().Should().Be("proj-guid-2");
        projectEntitlements[1].GetProperty("groupType").GetString().Should().Be("projectReader");
    }

    [Fact]
    public async Task AddUserEntitlementAsync_ShouldLogInformationOnSuccess()
    {
        // Arrange
        var organization = "test-org";
        var principalName = "newuser@contoso.com";
        var licenseType = "express";
        var projectEntitlementsJson = "[{\"projectId\":\"proj-guid-1\",\"groupType\":\"projectContributor\"}]";

        _mockApiService
            .Setup(x => x.PostAsync<object>(
                organization,
                It.IsAny<string>(),
                It.IsAny<AddUserEntitlementRequest>(),
                "7.1",
                default))
            .ReturnsAsync(new { success = true });

        // Act
        await _plugin.AddUserEntitlementAsync(organization, principalName, licenseType, projectEntitlementsJson);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Adding user entitlement")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully added user entitlement")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
