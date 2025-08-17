using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace CleanArchTemplate.UnitTests.Infrastructure.Services;

public class PermissionSeedingServiceTests
{
    private readonly Mock<IPermissionRepository> _mockPermissionRepository;
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<IUserPermissionRepository> _mockUserPermissionRepository;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<PermissionSeedingService>> _mockLogger;
    private readonly PermissionSeedingService _service;

    public PermissionSeedingServiceTests()
    {
        _mockPermissionRepository = new Mock<IPermissionRepository>();
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockUserPermissionRepository = new Mock<IUserPermissionRepository>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<PermissionSeedingService>>();

        _service = new PermissionSeedingService(
            _mockPermissionRepository.Object,
            _mockRoleRepository.Object,
            _mockUserPermissionRepository.Object,
            _mockConfiguration.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task SeedDefaultPermissionsAsync_WhenNoPermissionsExist_ShouldAddAllDefaultPermissions()
    {
        // Arrange
        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Permission>());

        // Act
        await _service.SeedDefaultPermissionsAsync();

        // Assert
        _mockPermissionRepository.Verify(x => x.AddAsync(It.IsAny<Permission>(), It.IsAny<CancellationToken>()), 
            Times.AtLeast(20)); // Should add at least 20 default permissions
    }

    [Fact]
    public async Task SeedDefaultPermissionsAsync_WhenSomePermissionsExist_ShouldOnlyAddMissingPermissions()
    {
        // Arrange
        var existingPermissions = new List<Permission>
        {
            new Permission("Users", "Create", "Create users", "User Management"),
            new Permission("Users", "Read", "Read users", "User Management")
        };

        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPermissions);

        // Act
        await _service.SeedDefaultPermissionsAsync();

        // Assert
        _mockPermissionRepository.Verify(x => x.AddAsync(It.IsAny<Permission>(), It.IsAny<CancellationToken>()), 
            Times.AtLeast(1)); // Should add remaining permissions
    }

    [Fact]
    public async Task SeedEnvironmentPermissionsAsync_WithDevelopmentEnvironment_ShouldAddDevelopmentPermissions()
    {
        // Arrange
        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Permission>());

        // Act
        await _service.SeedEnvironmentPermissionsAsync("Development");

        // Assert
        _mockPermissionRepository.Verify(x => x.AddAsync(
            It.Is<Permission>(p => p.Resource == "Development"), 
            It.IsAny<CancellationToken>()), Times.AtLeast(1));
    }

    [Fact]
    public async Task SeedEnvironmentPermissionsAsync_WithProductionEnvironment_ShouldNotAddAnyPermissions()
    {
        // Arrange
        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Permission>());

        // Act
        await _service.SeedEnvironmentPermissionsAsync("Production");

        // Assert
        _mockPermissionRepository.Verify(x => x.AddAsync(It.IsAny<Permission>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task ValidatePermissionIntegrityAsync_WhenAllPermissionsExist_ShouldReturnTrue()
    {
        // Arrange
        var defaultPermissions = await _service.GetDefaultPermissionsAsync();
        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultPermissions);

        // Act
        var result = await _service.ValidatePermissionIntegrityAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePermissionIntegrityAsync_WhenSomePermissionsMissing_ShouldReturnFalse()
    {
        // Arrange
        var incompletePermissions = new List<Permission>
        {
            new Permission("Users", "Create", "Create users", "User Management")
        };

        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(incompletePermissions);

        // Act
        var result = await _service.ValidatePermissionIntegrityAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CleanupOrphanedPermissionsAsync_WhenOrphanedPermissionsExist_ShouldRemoveThem()
    {
        // Arrange
        var orphanedPermission = new Permission("Orphaned", "Action", "Description", "Category");
        var permissions = new List<Permission> { orphanedPermission };

        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockRoleRepository.Setup(x => x.HasPermissionAsync(orphanedPermission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockUserPermissionRepository.Setup(x => x.HasPermissionAsync(orphanedPermission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _service.CleanupOrphanedPermissionsAsync();

        // Assert
        _mockPermissionRepository.Verify(x => x.DeleteAsync(orphanedPermission.Id, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task CleanupOrphanedPermissionsAsync_WhenNoOrphanedPermissions_ShouldNotRemoveAny()
    {
        // Arrange
        var permission = new Permission("Users", "Create", "Create users", "User Management");
        var permissions = new List<Permission> { permission };

        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockRoleRepository.Setup(x => x.HasPermissionAsync(permission.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.CleanupOrphanedPermissionsAsync();

        // Assert
        _mockPermissionRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task GetDefaultPermissionsAsync_ShouldReturnAllDefaultPermissions()
    {
        // Act
        var result = await _service.GetDefaultPermissionsAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(p => p.Resource == "Users" && p.Action == "Create");
        result.Should().Contain(p => p.Resource == "Roles" && p.Action == "Read");
        result.Should().Contain(p => p.Resource == "Permissions" && p.Action == "Update");
        result.Should().Contain(p => p.Resource == "Secrets" && p.Action == "Manage");
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    [InlineData("Production")]
    [InlineData("Unknown")]
    public async Task SeedEnvironmentPermissionsAsync_WithDifferentEnvironments_ShouldHandleGracefully(string environment)
    {
        // Arrange
        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Permission>());

        // Act & Assert
        var act = async () => await _service.SeedEnvironmentPermissionsAsync(environment);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SeedDefaultRolePermissionsAsync_WhenRolesExist_ShouldAssignPermissions()
    {
        // Arrange
        var adminRole = new Role("Administrator", "Admin role");
        var userRole = new Role("User", "User role");
        var roles = new List<Role> { adminRole, userRole };

        var permissions = new List<Permission>
        {
            new Permission("Users", "Create", "Create users", "User Management"),
            new Permission("Users", "Read", "Read users", "User Management")
        };

        _mockRoleRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockRoleRepository.Setup(x => x.GetByIdWithPermissionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => roles.FirstOrDefault(r => r.Id == id));

        // Act
        await _service.SeedDefaultRolePermissionsAsync();

        // Assert
        _mockRoleRepository.Verify(x => x.UpdateAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()), 
            Times.AtLeast(1));
    }
}