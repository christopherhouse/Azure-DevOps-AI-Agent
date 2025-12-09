using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Plugins;
using AzureDevOpsAI.Backend.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

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

        // Assert
        result.Should().Contain("Users in organization 'test-org':");
        result.Should().Contain("**John Doe** (john.doe@contoso.com)");
        result.Should().Contain("User ID: user-guid-1");
        result.Should().Contain("License: Visual Studio Enterprise");
        result.Should().Contain("Status: Active");
        result.Should().Contain("**Jane Smith** (jane.smith@contoso.com)");
        result.Should().Contain("User ID: user-guid-2");
        result.Should().Contain("License: Stakeholder");
        result.Should().Contain("Total: 2 user(s)");
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

        // Assert
        result.Should().Be("No users found in this organization.");
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

        // Assert
        result.Should().Be("No users found in this organization.");
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

        // Assert
        result.Should().StartWith("Error:");
        result.Should().Contain(exceptionMessage);
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

        // Assert
        result.Should().Contain("**John Doe** (john.doe@contoso.com)");
        result.Should().NotContain("License:");
        result.Should().Contain("Total: 1 user(s)");
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

        // Assert
        result.Should().Contain("✅ User entitlement added successfully!");
        result.Should().Contain("newuser@contoso.com");
        result.Should().Contain("express");
        result.Should().Contain("Organization-level access only");
        result.Should().Contain("no specific projects assigned");
        result.Should().Contain("To grant access to specific projects");
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

        // Assert
        result.Should().Contain("✅ User entitlement added successfully!");
        result.Should().Contain("newuser@contoso.com");
        result.Should().Contain("express");
        result.Should().Contain("1 project(s)");
        result.Should().Contain("Project ID: proj-guid-1");
        result.Should().Contain("Role: projectContributor");
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

        // Assert
        result.Should().Contain("Error: Principal name (email address) is required.");
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

        // Assert
        result.Should().Contain("Error: Account license type is required.");
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

        // Assert
        result.Should().Contain("Error: Invalid license type");
        result.Should().Contain("Valid values: express, stakeholder, advanced, professional, earlyAdopter");
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

        // Assert
        result.Should().Contain("Error: Invalid project entitlements JSON format");
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

        // Assert
        result.Should().Contain("✅ User entitlement added successfully!");
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

        // Assert
        result.Should().Contain("Error: Failed to add user entitlement");
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

        // Assert
        result.Should().Contain("Error:");
        result.Should().Contain(exceptionMessage);
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

        // Assert
        result.Should().Contain("✅ User entitlement added successfully!");
        result.Should().Contain("2 project(s)");
        result.Should().Contain("Project ID: proj-guid-1");
        result.Should().Contain("Role: projectContributor");
        result.Should().Contain("Project ID: proj-guid-2");
        result.Should().Contain("Role: projectReader");
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
