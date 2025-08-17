using AutoMapper;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Enums;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Domain.Services;
using CleanArchTemplate.Domain.ValueObjects;
using CleanArchTemplate.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CleanArchTemplate.UnitTests.Infrastructure.Services;

public class PermissionMatrixServiceTests
{
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<IPermissionRepository> _mockPermissionRepository;
    private readonly Mock<IUserPermissionRepository> _mockUserPermissionRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<IPermissionEvaluationService> _mockPermissionEvaluationService;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<PermissionMatrixService>> _mockLogger;
    private readonly PermissionMatrixService _service;

    public PermissionMatrixServiceTests()
    {
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockPermissionRepository = new Mock<IPermissionRepository>();
        _mockUserPermissionRepository = new Mock<IUserPermissionRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPermissionEvaluationService = new Mock<IPermissionEvaluationService>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<PermissionMatrixService>>();

        _service = new PermissionMatrixService(
            _mockRoleRepository.Object,
            _mockPermissionRepository.Object,
            _mockUserPermissionRepository.Object,
            _mockUserRepository.Object,
            _mockPermissionEvaluationService.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task GetRolePermissionMatrixAsync_ShouldReturnMatrix_WhenDataExists()
    {
        // Arrange
        var roles = CreateTestRoles();
        var permissions = CreateTestPermissions();

        _mockRoleRepository.Setup(x => x.GetRolesWithPermissionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        _mockMapper.Setup(x => x.Map<IEnumerable<RoleDto>>(It.IsAny<IEnumerable<Role>>()))
            .Returns(new List<RoleDto>());
        _mockMapper.Setup(x => x.Map<IEnumerable<PermissionDto>>(It.IsAny<IEnumerable<Permission>>()))
            .Returns(new List<PermissionDto>());

        // Act
        var result = await _service.GetRolePermissionMatrixAsync();

        // Assert
        result.Should().NotBeNull();
        result.Metadata.Should().NotBeNull();
        result.Assignments.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRolePermissionMatrixByCategoryAsync_ShouldReturnFilteredMatrix_WhenCategoryExists()
    {
        // Arrange
        var category = "TestCategory";
        var roles = CreateTestRoles();
        var permissions = CreateTestPermissions().Where(p => p.Category == category);

        _mockRoleRepository.Setup(x => x.GetRolesWithPermissionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        _mockPermissionRepository.Setup(x => x.GetByCategoryAsync(category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        _mockMapper.Setup(x => x.Map<IEnumerable<RoleDto>>(It.IsAny<IEnumerable<Role>>()))
            .Returns(new List<RoleDto>());
        _mockMapper.Setup(x => x.Map<IEnumerable<PermissionDto>>(It.IsAny<IEnumerable<Permission>>()))
            .Returns(new List<PermissionDto>());

        // Act
        var result = await _service.GetRolePermissionMatrixByCategoryAsync(category);

        // Assert
        result.Should().NotBeNull();
        _mockPermissionRepository.Verify(x => x.GetByCategoryAsync(category, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRolePermissionMatrixByCategoryAsync_ShouldThrowArgumentException_WhenCategoryIsEmpty()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GetRolePermissionMatrixByCategoryAsync(string.Empty));
    }

    [Fact]
    public async Task GetUserPermissionMatrixAsync_ShouldReturnUserMatrix_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        var userRoles = CreateTestRoles();
        var permissions = CreateTestPermissions();
        var userOverrides = CreateTestUserPermissions(userId);

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockRoleRepository.Setup(x => x.GetUserRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userRoles);
        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockUserPermissionRepository.Setup(x => x.GetUserPermissionsWithDetailsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userOverrides);

        _mockMapper.Setup(x => x.Map<UserDto>(It.IsAny<User>()))
            .Returns(new UserDto());
        _mockMapper.Setup(x => x.Map<IEnumerable<PermissionDto>>(It.IsAny<IEnumerable<Permission>>()))
            .Returns(new List<PermissionDto>());
        _mockMapper.Setup(x => x.Map<IEnumerable<UserPermissionOverrideDto>>(It.IsAny<IEnumerable<UserPermission>>()))
            .Returns(new List<UserPermissionOverrideDto>());

        // Act
        var result = await _service.GetUserPermissionMatrixAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.User.Should().NotBeNull();
        result.Metadata.Should().NotBeNull();
    }

    [Fact]
    public async Task GetUserPermissionMatrixAsync_ShouldThrowArgumentException_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.GetUserPermissionMatrixAsync(userId));
    }

    [Fact]
    public async Task UpdateRolePermissionMatrixAsync_ShouldUpdateRole_WhenValidData()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var permissionIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var role = CreateTestRole(roleId);
        var permissions = CreateTestPermissionsWithIds(permissionIds);

        _mockRoleRepository.Setup(x => x.GetRoleWithPermissionsByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        _mockPermissionRepository.Setup(x => x.GetByIdsAsync(permissionIds, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        // Act
        await _service.UpdateRolePermissionMatrixAsync(roleId, permissionIds);

        // Assert
        _mockRoleRepository.Verify(x => x.UpdateAsync(role, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateRolePermissionMatrixAsync_ShouldThrowArgumentException_WhenRoleNotFound()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var permissionIds = new List<Guid> { Guid.NewGuid() };

        _mockRoleRepository.Setup(x => x.GetRoleWithPermissionsByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.UpdateRolePermissionMatrixAsync(roleId, permissionIds));
    }

    [Fact]
    public async Task BulkUpdateRolePermissionMatrixAsync_ShouldUpdateMultipleRoles_WhenValidData()
    {
        // Arrange
        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();
        var permissionId1 = Guid.NewGuid();
        var permissionId2 = Guid.NewGuid();

        var rolePermissionUpdates = new Dictionary<Guid, IEnumerable<Guid>>
        {
            { roleId1, new[] { permissionId1 } },
            { roleId2, new[] { permissionId2 } }
        };

        var roles = new List<Role> { CreateTestRole(roleId1), CreateTestRole(roleId2) };
        var permissions = CreateTestPermissionsWithIds(new[] { permissionId1, permissionId2 });

        _mockRoleRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        _mockPermissionRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);

        // Act
        await _service.BulkUpdateRolePermissionMatrixAsync(rolePermissionUpdates);

        // Assert
        _mockRoleRepository.Verify(x => x.UpdateAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task BulkUpdateRolePermissionMatrixAsync_ShouldThrowArgumentException_WhenUpdatesEmpty()
    {
        // Arrange
        var emptyUpdates = new Dictionary<Guid, IEnumerable<Guid>>();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _service.BulkUpdateRolePermissionMatrixAsync(emptyUpdates));
    }

    [Fact]
    public async Task UpdateUserPermissionOverridesAsync_ShouldUpdateOverrides_WhenValidData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var permissionOverrides = new Dictionary<Guid, PermissionState>
        {
            { permissionId, PermissionState.Grant }
        };

        var user = CreateTestUser(userId);
        var permissions = CreateTestPermissionsWithIds(new[] { permissionId });
        var existingOverrides = new List<UserPermission>();

        _mockUserRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _mockPermissionRepository.Setup(x => x.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockUserPermissionRepository.Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOverrides);

        // Act
        await _service.UpdateUserPermissionOverridesAsync(userId, permissionOverrides);

        // Assert
        _mockUserPermissionRepository.Verify(x => x.BulkAddAsync(It.IsAny<IEnumerable<UserPermission>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompareRolePermissionsAsync_ShouldReturnComparison_WhenBothRolesExist()
    {
        // Arrange
        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();
        var role1 = CreateTestRole(roleId1);
        var role2 = CreateTestRole(roleId2);

        _mockRoleRepository.Setup(x => x.GetRoleWithPermissionsByIdAsync(roleId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role1);
        _mockRoleRepository.Setup(x => x.GetRoleWithPermissionsByIdAsync(roleId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role2);

        _mockMapper.Setup(x => x.Map<IEnumerable<PermissionDto>>(It.IsAny<IEnumerable<Permission>>()))
            .Returns(new List<PermissionDto>());

        // Act
        var result = await _service.CompareRolePermissionsAsync(roleId1, roleId2);

        // Assert
        result.Should().NotBeNull();
        result.FirstEntity.Should().NotBeNull();
        result.SecondEntity.Should().NotBeNull();
        result.Statistics.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPermissionMatrixStatisticsAsync_ShouldReturnStatistics_WhenDataExists()
    {
        // Arrange
        var roles = CreateTestRoles();
        var permissions = CreateTestPermissions();
        var users = CreateTestUsers();
        var userOverrides = CreateTestUserPermissions(Guid.NewGuid());

        _mockRoleRepository.Setup(x => x.GetRolesWithPermissionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockUserRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);
        _mockUserPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(userOverrides);

        _mockMapper.Setup(x => x.Map<RoleDto>(It.IsAny<Role>()))
            .Returns(new RoleDto());
        _mockMapper.Setup(x => x.Map<PermissionDto>(It.IsAny<Permission>()))
            .Returns(new PermissionDto());

        // Act
        var result = await _service.GetPermissionMatrixStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.SystemStatistics.Should().NotBeNull();
        result.RoleStatistics.Should().NotBeNull();
        result.PermissionUsage.Should().NotBeNull();
        result.CategoryStatistics.Should().NotBeNull();
        result.UserOverrideStatistics.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidatePermissionMatrixAsync_ShouldReturnValidationResult_WhenDataExists()
    {
        // Arrange
        var roles = CreateTestRoles();
        var permissions = CreateTestPermissions();
        var users = CreateTestUsers();
        var userOverrides = CreateTestUserPermissions(Guid.NewGuid());

        _mockRoleRepository.Setup(x => x.GetRolesWithPermissionsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        _mockPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _mockUserRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);
        _mockUserPermissionRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(userOverrides);

        // Act
        var result = await _service.ValidatePermissionMatrixAsync();

        // Assert
        result.Should().NotBeNull();
        result.Summary.Should().NotBeNull();
        result.Errors.Should().NotBeNull();
        result.Warnings.Should().NotBeNull();
    }

    #region Helper Methods

    private List<Role> CreateTestRoles()
    {
        var role1 = CreateTestRole(Guid.NewGuid());
        var role2 = CreateTestRole(Guid.NewGuid());
        return new List<Role> { role1, role2 };
    }

    private Role CreateTestRole(Guid roleId)
    {
        var role = new Role("TestRole", "Test Role Description");
        
        // Use reflection to set the Id since it's protected
        var idProperty = typeof(Role).BaseType?.GetProperty("Id");
        idProperty?.SetValue(role, roleId);

        return role;
    }

    private List<Permission> CreateTestPermissions()
    {
        var permission1 = Permission.Create("Users", "Read", "Read users", "TestCategory");
        var permission2 = Permission.Create("Users", "Write", "Write users", "TestCategory");
        
        return new List<Permission> { permission1, permission2 };
    }

    private List<Permission> CreateTestPermissionsWithIds(IEnumerable<Guid> permissionIds)
    {
        var permissions = new List<Permission>();
        var idList = permissionIds.ToList();
        
        for (int i = 0; i < idList.Count; i++)
        {
            var permission = Permission.Create($"Resource{i}", $"Action{i}", $"Description{i}", "TestCategory");
            
            // Use reflection to set the Id since it's protected
            var idProperty = typeof(Permission).BaseType?.GetProperty("Id");
            idProperty?.SetValue(permission, idList[i]);
            
            permissions.Add(permission);
        }
        
        return permissions;
    }

    private User CreateTestUser(Guid userId)
    {
        var email = Email.Create("test@example.com");
        var user = User.Create(email, "Test", "User", "hashedpassword");
        
        // Use reflection to set the Id since it's protected
        var idProperty = typeof(User).BaseType?.GetProperty("Id");
        idProperty?.SetValue(user, userId);

        return user;
    }

    private List<User> CreateTestUsers()
    {
        var user1 = CreateTestUser(Guid.NewGuid());
        var user2 = CreateTestUser(Guid.NewGuid());
        return new List<User> { user1, user2 };
    }

    private List<UserPermission> CreateTestUserPermissions(Guid userId)
    {
        var permissionId = Guid.NewGuid();
        var userPermission = UserPermission.Create(userId, permissionId, PermissionState.Grant, "Test reason");
        return new List<UserPermission> { userPermission };
    }

    #endregion
}