using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Plugins;
using AzureDevOpsAI.Backend.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;

namespace AzureDevOpsAI.Backend.Tests.Plugins;

public class UserEntitlementPluginTests
{
    private readonly Mock<IAzureDevOpsApiService> _mockApiService;
    private readonly Mock<ILogger<UserEntitlementPlugin>> _mockLogger;
    private readonly UserEntitlementPlugin _plugin;

    public UserEntitlementPluginTests()
    {
        _mockApiService = new Mock<IAzureDevOpsApiService>();
        _mockLogger = new Mock<ILogger<UserEntitlementPlugin>>();
        _plugin = new UserEntitlementPlugin(_mockApiService.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializePlugin()
    {
        // Arrange & Act
        var plugin = new UserEntitlementPlugin(_mockApiService.Object, _mockLogger.Object);

        // Assert
        plugin.Should().NotBeNull();
    }

    #region ListEntitlementsAsync Tests

    [Fact]
    public async Task ListEntitlementsAsync_ShouldReturnFormattedList_WhenEntitlementsExist()
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
                "5.1-preview.2",
                default))
            .ReturnsAsync(userEntitlements);

        // Act
        var result = await _plugin.ListEntitlementsAsync(organization);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("organization").GetString().Should().Be("test-org");
        jsonDoc.RootElement.GetProperty("pageSize").GetInt32().Should().Be(2);
        jsonDoc.RootElement.GetProperty("totalCount").GetInt32().Should().Be(2);
        jsonDoc.RootElement.GetProperty("hasMoreResults").GetBoolean().Should().BeFalse();

        var entitlementsArray = jsonDoc.RootElement.GetProperty("entitlements");
        entitlementsArray.GetArrayLength().Should().Be(2);

        var firstEntitlement = entitlementsArray[0];
        firstEntitlement.GetProperty("id").GetString().Should().Be("entitlement-1");
        firstEntitlement.GetProperty("displayName").GetString().Should().Be("John Doe");
        firstEntitlement.GetProperty("principalName").GetString().Should().Be("john.doe@contoso.com");
        firstEntitlement.GetProperty("userId").GetString().Should().Be("user-guid-1");
        firstEntitlement.GetProperty("license").GetString().Should().Be("Visual Studio Enterprise");
        firstEntitlement.GetProperty("status").GetString().Should().Be("Active");

        var secondEntitlement = entitlementsArray[1];
        secondEntitlement.GetProperty("id").GetString().Should().Be("entitlement-2");
        secondEntitlement.GetProperty("displayName").GetString().Should().Be("Jane Smith");
        secondEntitlement.GetProperty("principalName").GetString().Should().Be("jane.smith@contoso.com");
        secondEntitlement.GetProperty("userId").GetString().Should().Be("user-guid-2");
        secondEntitlement.GetProperty("license").GetString().Should().Be("Stakeholder");
    }

    [Fact]
    public async Task ListEntitlementsAsync_ShouldReturnNoEntitlementsMessage_WhenNoEntitlementsExist()
    {
        // Arrange
        var organization = "empty-org";
        var emptyEntitlements = new UserEntitlementListResponse
        {
            Items = new List<UserEntitlement>(),
            TotalCount = 0
        };

        _mockApiService
            .Setup(x => x.GetAsync<UserEntitlementListResponse>(
                organization,
                "https://vsaex.dev.azure.com/empty-org/_apis/userentitlements",
                "5.1-preview.2",
                default))
            .ReturnsAsync(emptyEntitlements);

        // Act
        var result = await _plugin.ListEntitlementsAsync(organization);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("message").GetString().Should().Be("No user entitlements found in this organization.");
        jsonDoc.RootElement.GetProperty("entitlements").GetArrayLength().Should().Be(0);
        jsonDoc.RootElement.GetProperty("hasMoreResults").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task ListEntitlementsAsync_ShouldReturnErrorMessage_WhenExceptionOccurs()
    {
        // Arrange
        var organization = "error-org";
        var exceptionMessage = "API call failed";

        _mockApiService
            .Setup(x => x.GetAsync<UserEntitlementListResponse>(
                organization,
                It.IsAny<string>(),
                "5.1-preview.2",
                default))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _plugin.ListEntitlementsAsync(organization);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Be(exceptionMessage);
    }

    [Fact]
    public async Task ListEntitlementsAsync_ShouldSupportPaging_WhenTopParameterProvided()
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
                    User = new User { Id = "user-1", DisplayName = "User 1", PrincipalName = "user1@test.com" },
                    AccessLevel = new AccessLevel { AccountLicenseType = "express", Status = "Active" }
                }
            },
            TotalCount = 150,
            ContinuationToken = "next-page-token-123"
        };

        _mockApiService
            .Setup(x => x.GetAsync<UserEntitlementListResponse>(
                organization,
                "https://vsaex.dev.azure.com/test-org/_apis/userentitlements?top=50",
                "5.1-preview.2",
                default))
            .ReturnsAsync(userEntitlements);

        // Act
        var result = await _plugin.ListEntitlementsAsync(organization, 50);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("hasMoreResults").GetBoolean().Should().BeTrue();
        jsonDoc.RootElement.GetProperty("continuationToken").GetString().Should().Be("next-page-token-123");
        jsonDoc.RootElement.GetProperty("totalCount").GetInt32().Should().Be(150);
        jsonDoc.RootElement.GetProperty("pageSize").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task ListEntitlementsAsync_ShouldSupportPaging_WhenContinuationTokenProvided()
    {
        // Arrange
        var organization = "test-org";
        var continuationToken = "next-page-token-123";
        var userEntitlements = new UserEntitlementListResponse
        {
            Items = new List<UserEntitlement>
            {
                new UserEntitlement
                {
                    Id = "entitlement-51",
                    User = new User { Id = "user-51", DisplayName = "User 51", PrincipalName = "user51@test.com" },
                    AccessLevel = new AccessLevel { AccountLicenseType = "stakeholder", Status = "Active" }
                }
            },
            TotalCount = 150,
            ContinuationToken = null // Last page
        };

        _mockApiService
            .Setup(x => x.GetAsync<UserEntitlementListResponse>(
                organization,
                It.Is<string>(s => s.Contains($"continuationToken={Uri.EscapeDataString(continuationToken)}")),
                "5.1-preview.2",
                default))
            .ReturnsAsync(userEntitlements);

        // Act
        var result = await _plugin.ListEntitlementsAsync(organization, null, continuationToken);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("hasMoreResults").GetBoolean().Should().BeFalse();
        jsonDoc.RootElement.TryGetProperty("continuationToken", out var token).Should().BeTrue();
        token.ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task ListEntitlementsAsync_ShouldReturnError_WhenTopParameterIsInvalid()
    {
        // Arrange
        var organization = "test-org";
        var invalidTop = 15000; // Exceeds max of 10000

        // Act
        var result = await _plugin.ListEntitlementsAsync(organization, invalidTop);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("must be between 1 and 10000");
    }

    #endregion

    #region CreateEntitlementAsync Tests

    [Fact]
    public async Task CreateEntitlementAsync_ShouldCreateEntitlementWithoutProjectEntitlements_WhenNoneProvided()
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
                "5.1-preview.2",
                default))
            .ReturnsAsync(new { success = true });

        // Act
        var result = await _plugin.CreateEntitlementAsync(organization, principalName, licenseType, null);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        jsonDoc.RootElement.GetProperty("user").GetString().Should().Be("newuser@contoso.com");
        jsonDoc.RootElement.GetProperty("license").GetString().Should().Be("express");
        jsonDoc.RootElement.GetProperty("hasProjectAccess").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task CreateEntitlementAsync_ShouldCreateEntitlementWithProjectEntitlements_WhenValidDataProvided()
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
                "5.1-preview.2",
                default))
            .ReturnsAsync(new { success = true });

        // Act
        var result = await _plugin.CreateEntitlementAsync(organization, principalName, licenseType, projectEntitlementsJson);

        // Assert
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
    public async Task CreateEntitlementAsync_ShouldReturnError_WhenPrincipalNameIsEmpty()
    {
        // Arrange
        var organization = "test-org";
        var principalName = "";
        var licenseType = "express";

        // Act
        var result = await _plugin.CreateEntitlementAsync(organization, principalName, licenseType, null);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Be("Principal name (email address) is required.");
    }

    [Fact]
    public async Task CreateEntitlementAsync_ShouldReturnError_WhenLicenseTypeIsEmpty()
    {
        // Arrange
        var organization = "test-org";
        var principalName = "newuser@contoso.com";
        var licenseType = "";

        // Act
        var result = await _plugin.CreateEntitlementAsync(organization, principalName, licenseType, null);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Account license type is required");
    }

    [Fact]
    public async Task CreateEntitlementAsync_ShouldReturnError_WhenLicenseTypeIsInvalid()
    {
        // Arrange
        var organization = "test-org";
        var principalName = "newuser@contoso.com";
        var licenseType = "invalid-license";

        // Act
        var result = await _plugin.CreateEntitlementAsync(organization, principalName, licenseType, null);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Invalid license type");
    }

    [Fact]
    public async Task CreateEntitlementAsync_ShouldReturnError_WhenProjectEntitlementsJsonIsInvalid()
    {
        // Arrange
        var organization = "test-org";
        var principalName = "newuser@contoso.com";
        var licenseType = "express";
        var invalidJson = "not valid json";

        // Act
        var result = await _plugin.CreateEntitlementAsync(organization, principalName, licenseType, invalidJson);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Invalid project entitlements JSON format");
    }

    #endregion

    #region UpdateEntitlementAsync Tests

    [Fact]
    public async Task UpdateEntitlementAsync_ShouldUpdateLicenseType_WhenProvided()
    {
        // Arrange
        var organization = "test-org";
        var userId = "user-guid-1";
        var newLicenseType = "advanced";

        _mockApiService
            .Setup(x => x.PatchAsync<object>(
                organization,
                "https://vsaex.dev.azure.com/test-org/_apis/userentitlements/user-guid-1",
                It.IsAny<object>(),
                "5.1-preview.2",
                default))
            .ReturnsAsync(new { success = true });

        // Act
        var result = await _plugin.UpdateEntitlementAsync(organization, userId, newLicenseType, null);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        jsonDoc.RootElement.GetProperty("userId").GetString().Should().Be("user-guid-1");
        jsonDoc.RootElement.GetProperty("updatedLicense").GetString().Should().Be("advanced");
    }

    [Fact]
    public async Task UpdateEntitlementAsync_ShouldUpdateProjectEntitlements_WhenProvided()
    {
        // Arrange
        var organization = "test-org";
        var userId = "user-guid-1";
        var projectEntitlementsJson = "[{\"projectId\":\"proj-guid-1\",\"groupType\":\"projectAdministrator\"}]";

        _mockApiService
            .Setup(x => x.PatchAsync<object>(
                organization,
                "https://vsaex.dev.azure.com/test-org/_apis/userentitlements/user-guid-1",
                It.IsAny<object>(),
                "5.1-preview.2",
                default))
            .ReturnsAsync(new { success = true });

        // Act
        var result = await _plugin.UpdateEntitlementAsync(organization, userId, null, projectEntitlementsJson);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        jsonDoc.RootElement.GetProperty("userId").GetString().Should().Be("user-guid-1");
        jsonDoc.RootElement.GetProperty("hasProjectAccess").GetBoolean().Should().BeTrue();

        var projectEntitlements = jsonDoc.RootElement.GetProperty("updatedProjectEntitlements");
        projectEntitlements.GetArrayLength().Should().Be(1);
        projectEntitlements[0].GetProperty("projectId").GetString().Should().Be("proj-guid-1");
        projectEntitlements[0].GetProperty("groupType").GetString().Should().Be("projectAdministrator");
    }

    [Fact]
    public async Task UpdateEntitlementAsync_ShouldUpdateBothLicenseAndProjects_WhenBothProvided()
    {
        // Arrange
        var organization = "test-org";
        var userId = "user-guid-1";
        var newLicenseType = "professional";
        var projectEntitlementsJson = "[{\"projectId\":\"proj-guid-1\",\"groupType\":\"projectReader\"}]";

        _mockApiService
            .Setup(x => x.PatchAsync<object>(
                organization,
                "https://vsaex.dev.azure.com/test-org/_apis/userentitlements/user-guid-1",
                It.IsAny<object>(),
                "5.1-preview.2",
                default))
            .ReturnsAsync(new { success = true });

        // Act
        var result = await _plugin.UpdateEntitlementAsync(organization, userId, newLicenseType, projectEntitlementsJson);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        jsonDoc.RootElement.GetProperty("userId").GetString().Should().Be("user-guid-1");
        jsonDoc.RootElement.GetProperty("updatedLicense").GetString().Should().Be("professional");
        jsonDoc.RootElement.GetProperty("hasProjectAccess").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task UpdateEntitlementAsync_ShouldReturnError_WhenUserIdIsEmpty()
    {
        // Arrange
        var organization = "test-org";
        var userId = "";
        var newLicenseType = "advanced";

        // Act
        var result = await _plugin.UpdateEntitlementAsync(organization, userId, newLicenseType, null);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("User ID is required");
    }

    [Fact]
    public async Task UpdateEntitlementAsync_ShouldReturnError_WhenNoFieldsProvided()
    {
        // Arrange
        var organization = "test-org";
        var userId = "user-guid-1";

        // Act
        var result = await _plugin.UpdateEntitlementAsync(organization, userId, null, null);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("At least one field");
    }

    [Fact]
    public async Task UpdateEntitlementAsync_ShouldReturnError_WhenLicenseTypeIsInvalid()
    {
        // Arrange
        var organization = "test-org";
        var userId = "user-guid-1";
        var invalidLicenseType = "invalid-license";

        // Act
        var result = await _plugin.UpdateEntitlementAsync(organization, userId, invalidLicenseType, null);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Invalid license type");
    }

    [Fact]
    public async Task UpdateEntitlementAsync_ShouldReturnError_WhenProjectEntitlementsJsonIsInvalid()
    {
        // Arrange
        var organization = "test-org";
        var userId = "user-guid-1";
        var invalidJson = "not valid json";

        // Act
        var result = await _plugin.UpdateEntitlementAsync(organization, userId, null, invalidJson);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Invalid project entitlements JSON format");
    }

    [Fact]
    public async Task UpdateEntitlementAsync_ShouldReturnError_WhenApiCallFails()
    {
        // Arrange
        var organization = "test-org";
        var userId = "user-guid-1";
        var newLicenseType = "advanced";

        _mockApiService
            .Setup(x => x.PatchAsync<object>(
                organization,
                It.IsAny<string>(),
                It.IsAny<object>(),
                "5.1-preview.2",
                default))
            .ReturnsAsync((object?)null);

        // Act
        var result = await _plugin.UpdateEntitlementAsync(organization, userId, newLicenseType, null);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Failed to update user entitlement");
    }

    #endregion

    #region DeleteEntitlementAsync Tests

    [Fact]
    public async Task DeleteEntitlementAsync_ShouldDeleteEntitlement_WhenValidUserIdProvided()
    {
        // Arrange
        var organization = "test-org";
        var userId = "user-guid-1";

        _mockApiService
            .Setup(x => x.DeleteAsync(
                organization,
                "https://vsaex.dev.azure.com/test-org/_apis/userentitlements/user-guid-1",
                "5.1-preview.2",
                default))
            .ReturnsAsync(true);

        // Act
        var result = await _plugin.DeleteEntitlementAsync(organization, userId);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
        jsonDoc.RootElement.GetProperty("userId").GetString().Should().Be("user-guid-1");
        jsonDoc.RootElement.GetProperty("message").GetString().Should().Be("User entitlement deleted successfully.");
    }

    [Fact]
    public async Task DeleteEntitlementAsync_ShouldReturnError_WhenUserIdIsEmpty()
    {
        // Arrange
        var organization = "test-org";
        var userId = "";

        // Act
        var result = await _plugin.DeleteEntitlementAsync(organization, userId);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("User ID is required");
    }

    [Fact]
    public async Task DeleteEntitlementAsync_ShouldReturnError_WhenApiCallFails()
    {
        // Arrange
        var organization = "test-org";
        var userId = "user-guid-1";

        _mockApiService
            .Setup(x => x.DeleteAsync(
                organization,
                It.IsAny<string>(),
                "5.1-preview.2",
                default))
            .ReturnsAsync(false);

        // Act
        var result = await _plugin.DeleteEntitlementAsync(organization, userId);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Contain("Failed to delete user entitlement");
    }

    [Fact]
    public async Task DeleteEntitlementAsync_ShouldReturnError_WhenExceptionOccurs()
    {
        // Arrange
        var organization = "test-org";
        var userId = "user-guid-1";
        var exceptionMessage = "API call failed";

        _mockApiService
            .Setup(x => x.DeleteAsync(
                organization,
                It.IsAny<string>(),
                "5.1-preview.2",
                default))
            .ThrowsAsync(new Exception(exceptionMessage));

        // Act
        var result = await _plugin.DeleteEntitlementAsync(organization, userId);

        // Assert
        var jsonDoc = JsonDocument.Parse(result);
        jsonDoc.RootElement.GetProperty("error").GetString().Should().Be(exceptionMessage);
    }

    #endregion
}
