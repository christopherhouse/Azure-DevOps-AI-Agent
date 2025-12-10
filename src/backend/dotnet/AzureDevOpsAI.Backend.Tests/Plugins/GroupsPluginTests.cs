using AzureDevOpsAI.Backend.Models;
using AzureDevOpsAI.Backend.Plugins;
using AzureDevOpsAI.Backend.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace AzureDevOpsAI.Backend.Tests.Plugins;

public class GroupsPluginTests
{
    private readonly Mock<IAzureDevOpsApiService> _mockApiService;
    private readonly Mock<IAzureDevOpsDescriptorService> _mockDescriptorService;
    private readonly Mock<ILogger<GroupsPlugin>> _mockLogger;
    private readonly GroupsPlugin _plugin;

    public GroupsPluginTests()
    {
        _mockApiService = new Mock<IAzureDevOpsApiService>();
        _mockDescriptorService = new Mock<IAzureDevOpsDescriptorService>();
        _mockLogger = new Mock<ILogger<GroupsPlugin>>();
        _plugin = new GroupsPlugin(_mockApiService.Object, _mockDescriptorService.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializePlugin()
    {
        // Arrange & Act
        var plugin = new GroupsPlugin(_mockApiService.Object, _mockDescriptorService.Object, _mockLogger.Object);

        // Assert
        plugin.Should().NotBeNull();
    }

    [Fact]
    public async Task ListGroupsAsync_ShouldReturnGroupsList_WhenGroupsExist()
    {
        // Arrange
        var organization = "test-org";
        var groups = new GraphGroupListResponse
        {
            Value = new List<GraphGroup>
            {
                new GraphGroup
                {
                    Descriptor = "vssgp.Uy0xLTktMTU1MTM3NDI0NS0xMjA0NDAwOTY5",
                    DisplayName = "Project Administrators",
                    Description = "Administrators for the project",
                    PrincipalName = "[MyProject]\\Project Administrators",
                    Origin = "vsts",
                    SubjectKind = "group"
                },
                new GraphGroup
                {
                    Descriptor = "vssgp.Uy0xLTktMTU1MTM3NDI0NS0yMjA0NDAwOTY5",
                    DisplayName = "Contributors",
                    Description = "Contributors to the project",
                    PrincipalName = "[MyProject]\\Contributors",
                    Origin = "vsts",
                    SubjectKind = "group"
                }
            },
            Count = 2
        };

        _mockApiService
            .Setup(x => x.GetAsync<GraphGroupListResponse>(
                organization,
                It.Is<string>(s => s.Contains("graph/groups")),
                "7.1-preview.1",
                default))
            .ReturnsAsync(groups);

        // Act
        var result = await _plugin.ListGroupsAsync(organization);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SlimGroupListResponse>();
        result.Value.Should().HaveCount(2);
        result.Value[0].DisplayName.Should().Be("Project Administrators");
        result.Value[0].Descriptor.Should().Be("vssgp.Uy0xLTktMTU1MTM3NDI0NS0xMjA0NDAwOTY5");
        result.Value[0].Description.Should().Be("Administrators for the project");
        result.Value[0].PrincipalName.Should().Be("[MyProject]\\Project Administrators");
        result.Value[0].Origin.Should().Be("vsts");
        result.Value[1].DisplayName.Should().Be("Contributors");
        result.Value[1].Description.Should().Be("Contributors to the project");
    }

    [Fact]
    public async Task ListGroupsAsync_ShouldReturnEmptyList_WhenNoGroupsExist()
    {
        // Arrange
        var organization = "empty-org";
        var emptyGroups = new GraphGroupListResponse
        {
            Value = new List<GraphGroup>(),
            Count = 0
        };

        _mockApiService
            .Setup(x => x.GetAsync<GraphGroupListResponse>(
                organization,
                It.Is<string>(s => s.Contains("graph/groups")),
                "7.1-preview.1",
                default))
            .ReturnsAsync(emptyGroups);

        // Act
        var result = await _plugin.ListGroupsAsync(organization);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SlimGroupListResponse>();
        result.Value.Should().BeEmpty();
        result.Count.Should().Be(0);
    }

    [Fact]
    public async Task ListGroupsAsync_WithProjectId_ShouldCallDescriptorService()
    {
        // Arrange
        var organization = "test-org";
        var projectId = "project-123";
        var scopeDescriptor = "scp.ZGU4Y2YyYzYtZjBiZS00NTJjLWFhMjYtZmE3ZDI4OWJhMWQ2";

        _mockDescriptorService
            .Setup(x => x.GetScopeDescriptorAsync(organization, projectId, default))
            .ReturnsAsync(scopeDescriptor);

        var groups = new GraphGroupListResponse
        {
            Value = new List<GraphGroup>
            {
                new GraphGroup
                {
                    Descriptor = "vssgp.Test",
                    DisplayName = "Test Group",
                    Description = "Test Description",
                    PrincipalName = "[TestProject]\\Test Group",
                    Origin = "vsts"
                }
            },
            Count = 1
        };

        _mockApiService
            .Setup(x => x.GetAsync<GraphGroupListResponse>(
                organization,
                It.Is<string>(s => s.Contains("scopeDescriptor=" + scopeDescriptor)),
                "7.1-preview.1",
                default))
            .ReturnsAsync(groups);

        // Act
        var result = await _plugin.ListGroupsAsync(organization, projectId);

        // Assert
        _mockDescriptorService.Verify(x => x.GetScopeDescriptorAsync(organization, projectId, default), Times.Once);
        result.Should().NotBeNull();
        result.Should().BeOfType<SlimGroupListResponse>();
        result.Value.Should().HaveCount(1);
        result.Value[0].DisplayName.Should().Be("Test Group");
        result.Value[0].Descriptor.Should().Be("vssgp.Test");
    }

    [Fact]
    public async Task ListGroupMembersAsync_ShouldReturnMembersList_WhenMembersExist()
    {
        // Arrange
        var organization = "test-org";
        var groupDescriptor = "vssgp.Uy0xLTktMTU1MTM3NDI0NS0xMjA0NDAwOTY5";
        var members = new GraphMemberListResponse
        {
            Value = new List<GraphMember>
            {
                new GraphMember
                {
                    Descriptor = "aad.OWExOWNmYWMtYzI0Ny03NTYxLThhN2YtZGUyZDE1OWYxNWE5",
                    DisplayName = "John Doe",
                    PrincipalName = "john.doe@contoso.com",
                    MailAddress = "john.doe@contoso.com",
                    Origin = "aad",
                    SubjectKind = "user"
                },
                new GraphMember
                {
                    Descriptor = "aad.YWExOWNmYWMtYzI0Ny03NTYxLThhN2YtZGUyZDE1OWYxNWE5",
                    DisplayName = "Jane Smith",
                    PrincipalName = "jane.smith@contoso.com",
                    MailAddress = "jane.smith@contoso.com",
                    Origin = "aad",
                    SubjectKind = "user"
                }
            },
            Count = 2
        };

        _mockApiService
            .Setup(x => x.GetAsync<GraphMemberListResponse>(
                organization,
                It.Is<string>(s => s.Contains($"graph/groups/{groupDescriptor}/members")),
                "7.1-preview.1",
                default))
            .ReturnsAsync(members);

        // Act
        var result = await _plugin.ListGroupMembersAsync(organization, groupDescriptor);

        // Assert
        result.Should().NotBeNull();
        result.Value.Should().HaveCount(2);
        result.Value[0].DisplayName.Should().Be("John Doe");
        result.Value[0].PrincipalName.Should().Be("john.doe@contoso.com");
        result.Value[1].DisplayName.Should().Be("Jane Smith");
    }

    [Fact]
    public async Task CreateGroupAsync_ShouldReturnCreatedGroup_WhenGroupCreated()
    {
        // Arrange
        var organization = "test-org";
        var displayName = "New Test Group";
        var description = "A new test group";
        var createdGroup = new GraphGroup
        {
            Descriptor = "vssgp.NewGroupDescriptor",
            DisplayName = displayName,
            Description = description,
            PrincipalName = $"[{organization}]\\{displayName}"
        };

        _mockApiService
            .Setup(x => x.PostAsync<GraphGroup>(
                organization,
                It.Is<string>(s => s.Contains("graph/groups")),
                It.IsAny<GraphGroupCreationContext>(),
                "7.1-preview.1",
                default))
            .ReturnsAsync(createdGroup);

        // Act
        var result = await _plugin.CreateGroupAsync(organization, displayName, description);

        // Assert
        result.Should().NotBeNull();
        result.DisplayName.Should().Be(displayName);
        result.Description.Should().Be(description);
        result.Descriptor.Should().Be("vssgp.NewGroupDescriptor");
    }

    [Fact]
    public async Task CreateGroupAsync_WithProjectId_ShouldCallDescriptorService()
    {
        // Arrange
        var organization = "test-org";
        var displayName = "Project Group";
        var projectId = "project-123";
        var scopeDescriptor = "scp.ProjectScope";

        _mockDescriptorService
            .Setup(x => x.GetScopeDescriptorAsync(organization, projectId, default))
            .ReturnsAsync(scopeDescriptor);

        var createdGroup = new GraphGroup
        {
            Descriptor = "vssgp.ProjectGroupDescriptor",
            DisplayName = displayName
        };

        _mockApiService
            .Setup(x => x.PostAsync<GraphGroup>(
                organization,
                It.Is<string>(s => s.Contains("scopeDescriptor=" + scopeDescriptor)),
                It.IsAny<GraphGroupCreationContext>(),
                "7.1-preview.1",
                default))
            .ReturnsAsync(createdGroup);

        // Act
        var result = await _plugin.CreateGroupAsync(organization, displayName, null, projectId);

        // Assert
        _mockDescriptorService.Verify(x => x.GetScopeDescriptorAsync(organization, projectId, default), Times.Once);
        result.Should().NotBeNull();
        result.DisplayName.Should().Be(displayName);
        result.Descriptor.Should().Be("vssgp.ProjectGroupDescriptor");
    }

    [Fact]
    public async Task AddGroupMemberAsync_ShouldReturnMembershipState_WhenMemberAdded()
    {
        // Arrange
        var organization = "test-org";
        var groupDescriptor = "vssgp.GroupDescriptor";
        var memberDescriptor = "aad.MemberDescriptor";
        var membership = new GraphMembershipState
        {
            Active = true
        };

        _mockApiService
            .Setup(x => x.PostAsync<GraphMembershipState>(
                organization,
                It.Is<string>(s => s.Contains($"graph/memberships/{memberDescriptor}/{groupDescriptor}")),
                null,
                "7.1-preview.1",
                default))
            .ReturnsAsync(membership);

        // Act
        var result = await _plugin.AddGroupMemberAsync(organization, groupDescriptor, memberDescriptor);

        // Assert
        result.Should().NotBeNull();
        result.Active.Should().BeTrue();
    }

    [Fact]
    public async Task AddGroupMemberAsync_ShouldThrowArgumentException_WhenGroupDescriptorIsEmpty()
    {
        // Arrange
        var organization = "test-org";
        var groupDescriptor = "";
        var memberDescriptor = "aad.MemberDescriptor";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _plugin.AddGroupMemberAsync(organization, groupDescriptor, memberDescriptor));
    }

    [Fact]
    public async Task AddGroupMemberAsync_ShouldThrowArgumentException_WhenMemberDescriptorIsEmpty()
    {
        // Arrange
        var organization = "test-org";
        var groupDescriptor = "vssgp.GroupDescriptor";
        var memberDescriptor = "";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _plugin.AddGroupMemberAsync(organization, groupDescriptor, memberDescriptor));
    }

    [Fact]
    public async Task ListGroupsAsync_ShouldReturnSlimGroupsWithOnlyEssentialAttributes()
    {
        // Arrange
        var organization = "test-org";
        var groups = new GraphGroupListResponse
        {
            Value = new List<GraphGroup>
            {
                new GraphGroup
                {
                    Descriptor = "vssgp.Test123",
                    DisplayName = "Test Group",
                    Description = "Test Description",
                    PrincipalName = "[TestOrg]\\Test Group",
                    Origin = "vsts",
                    // These additional properties should NOT be included in SlimGroup
                    SubjectKind = "group",
                    Domain = "vstfs:///Classification/TeamProject/123",
                    MailAddress = "testgroup@contoso.com",
                    OriginId = "origin-123",
                    Url = "https://vssps.dev.azure.com/testorg/_apis/graph/groups/vssgp.Test123",
                    IsCrossProject = false,
                    IsDeleted = false,
                    IsRestrictedVisible = false
                }
            },
            Count = 1
        };

        _mockApiService
            .Setup(x => x.GetAsync<GraphGroupListResponse>(
                organization,
                It.Is<string>(s => s.Contains("graph/groups")),
                "7.1-preview.1",
                default))
            .ReturnsAsync(groups);

        // Act
        var result = await _plugin.ListGroupsAsync(organization);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<SlimGroupListResponse>();
        result.Value.Should().HaveCount(1);
        
        var slimGroup = result.Value[0];
        // Verify only the 5 essential attributes are set
        slimGroup.Description.Should().Be("Test Description");
        slimGroup.PrincipalName.Should().Be("[TestOrg]\\Test Group");
        slimGroup.Origin.Should().Be("vsts");
        slimGroup.DisplayName.Should().Be("Test Group");
        slimGroup.Descriptor.Should().Be("vssgp.Test123");
        
        // Verify the SlimGroup type has only the expected properties
        var slimGroupType = typeof(SlimGroup);
        var properties = slimGroupType.GetProperties();
        properties.Should().HaveCount(5, "SlimGroup should have exactly 5 properties");
        properties.Select(p => p.Name).Should().BeEquivalentTo(new[] 
        { 
            "Description", "PrincipalName", "Origin", "DisplayName", "Descriptor" 
        });
    }
}
