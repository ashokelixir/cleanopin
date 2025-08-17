using System.Reflection;
using System.Security.Claims;
using CleanArchTemplate.Application.Common.Interfaces;
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

public class PermissionAuthorizationServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IPermissionRepository> _permissionRepositoryMock;
    private readonly Mock<IUserPermissionRepository> _userPermissionRepositoryMock;
    private readonly Mock<IPermissionEvaluationService> _permissionEvaluationServiceMock;
    private readonly Mock<IPermissionCacheService> _permissionCacheServiceMock;
    private readonly Mock<ILogger<PermissionAuthorizationService>> _loggerMock;
    private readonly PermissionAuthorizationService _service;

    public PermissionAuthorizationServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _permissionRepositoryMock = new Mock<IPermissionRepository>();
        _userPermissionRepositoryMock = new Mock<IUserPermissionRepository>();
        _permissionEvaluationServiceMock = new Mock<IPermissionEvaluationService>();
        _permissionCacheServiceMock = new Mock<IPermissionCacheService>();
        _loggerMock = new Mock<ILogger<PermissionAuthorizationService>>();

        _service = new PermissionAuthorizationService(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _permissionRepositoryMock.Object,
            _userPermissionRepositoryMock.Object,
            _permissionEvaluationServiceMock.Object,
            _permissionCacheServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task AuthorizeAsync_WithValidUserAndPermission_ShouldReturnSuccessResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permission = "Users.Read";
        var user = CreateTestUser(userId);
        var permissions = new List<Permission> { CreateTestPermission("Users.Read") };
        var roles = new List<Role> { CreateTestRole("Admin") };
        var userPermissions = new List<string> { "Users.Read", "Users.Write" };

        var evaluationResult = PermissionEvaluationResult.FromRoles(true, new[] { "Admin" }, "Granted through role");

        _userRepositoryMock.Setup(x => x.GetUserWithRolesAndPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _permissionRepositoryMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _roleRepositoryMock.Setup(x => x.GetUserRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        _permissionEvaluationServiceMock.Setup(x => x.EvaluatePermission(user, permission, permissions, roles))
            .Returns(evaluationResult);
        _permissionCacheServiceMock.Setup(x => x.GetUserPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPermissions);

        // Act
        var result = await _service.AuthorizeAsync(userId, permission);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeTrue();
        result.RequiredPermission.Should().Be(permission);
        result.UserPermissions.Should().BeEquivalentTo(userPermissions);
    }

    [Fact]
    public async Task AuthorizeAsync_WithInvalidUser_ShouldReturnFailureResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permission = "Users.Read";

        _userRepositoryMock.Setup(x => x.GetUserWithRolesAndPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _service.AuthorizeAsync(userId, permission);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.FailureReason.Should().Be("User not found");
        result.RequiredPermission.Should().Be(permission);
    }

    [Fact]
    public async Task AuthorizeAsync_WithInactiveUser_ShouldReturnFailureResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permission = "Users.Read";
        var user = CreateTestUser(userId, isActive: false);

        _userRepositoryMock.Setup(x => x.GetUserWithRolesAndPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _service.AuthorizeAsync(userId, permission);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.FailureReason.Should().Be("User account is not active");
        result.RequiredPermission.Should().Be(permission);
    }

    [Fact]
    public async Task AuthorizeAsync_WithClaimsPrincipal_ShouldExtractUserIdAndAuthorize()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permission = "Users.Read";
        var user = CreateTestUser(userId);
        var permissions = new List<Permission> { CreateTestPermission("Users.Read") };
        var roles = new List<Role> { CreateTestRole("Admin") };
        var userPermissions = new List<string> { "Users.Read" };

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }));

        var evaluationResult = PermissionEvaluationResult.FromRoles(true, new[] { "Admin" }, "Granted through role");

        _userRepositoryMock.Setup(x => x.GetUserWithRolesAndPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _permissionRepositoryMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _roleRepositoryMock.Setup(x => x.GetUserRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        _permissionEvaluationServiceMock.Setup(x => x.EvaluatePermission(user, permission, permissions, roles))
            .Returns(evaluationResult);
        _permissionCacheServiceMock.Setup(x => x.GetUserPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPermissions);

        // Act
        var result = await _service.AuthorizeAsync(claimsPrincipal, permission);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeTrue();
        result.RequiredPermission.Should().Be(permission);
    }

    [Fact]
    public async Task AuthorizeAsync_WithResourceAndAction_ShouldCombineAndAuthorize()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var resource = "Users";
        var action = "Read";
        var expectedPermission = "Users.Read";
        var user = CreateTestUser(userId);
        var permissions = new List<Permission> { CreateTestPermission(expectedPermission) };
        var roles = new List<Role> { CreateTestRole("Admin") };
        var userPermissions = new List<string> { expectedPermission };

        var evaluationResult = PermissionEvaluationResult.FromRoles(true, new[] { "Admin" }, "Granted through role");

        _userRepositoryMock.Setup(x => x.GetUserWithRolesAndPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _permissionRepositoryMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(permissions);
        _roleRepositoryMock.Setup(x => x.GetUserRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        _permissionEvaluationServiceMock.Setup(x => x.EvaluatePermission(user, expectedPermission, permissions, roles))
            .Returns(evaluationResult);
        _permissionCacheServiceMock.Setup(x => x.GetUserPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPermissions);

        // Act
        var result = await _service.AuthorizeAsync(userId, resource, action);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeTrue();
        result.RequiredPermission.Should().Be(expectedPermission);
    }

    [Fact]
    public async Task AuthorizeAnyAsync_WithValidPermissions_ShouldReturnSuccessWhenUserHasAny()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new[] { "Users.Read", "Users.Write", "Users.Delete" };
        var user = CreateTestUser(userId);
        var availablePermissions = permissions.Select(CreateTestPermission).ToList();
        var roles = new List<Role> { CreateTestRole("Admin") };
        var userPermissions = new List<string> { "Users.Read" };

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }));

        _userRepositoryMock.Setup(x => x.GetUserWithRolesAndPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _permissionRepositoryMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(availablePermissions);
        _roleRepositoryMock.Setup(x => x.GetUserRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        _permissionEvaluationServiceMock.Setup(x => x.HasAnyPermission(user, permissions, availablePermissions, roles))
            .Returns(true);
        _permissionCacheServiceMock.Setup(x => x.GetUserPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPermissions);

        // Act
        var result = await _service.AuthorizeAnyAsync(claimsPrincipal, permissions);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeTrue();
        result.UserPermissions.Should().BeEquivalentTo(userPermissions);
    }

    [Fact]
    public async Task AuthorizeAllAsync_WithValidPermissions_ShouldReturnSuccessWhenUserHasAll()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new[] { "Users.Read", "Users.Write" };
        var user = CreateTestUser(userId);
        var availablePermissions = permissions.Select(CreateTestPermission).ToList();
        var roles = new List<Role> { CreateTestRole("Admin") };
        var userPermissions = new List<string> { "Users.Read", "Users.Write" };

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }));

        // Setup evaluation to return true for all permissions
        foreach (var permission in permissions)
        {
            var evaluationResult = PermissionEvaluationResult.FromRoles(true, new[] { "Admin" }, "Granted through role");
            _permissionEvaluationServiceMock.Setup(x => x.EvaluatePermission(user, permission, availablePermissions, roles))
                .Returns(evaluationResult);
        }

        _userRepositoryMock.Setup(x => x.GetUserWithRolesAndPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _permissionRepositoryMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(availablePermissions);
        _roleRepositoryMock.Setup(x => x.GetUserRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        _permissionCacheServiceMock.Setup(x => x.GetUserPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPermissions);

        // Act
        var result = await _service.AuthorizeAllAsync(claimsPrincipal, permissions);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeTrue();
        result.UserPermissions.Should().BeEquivalentTo(userPermissions);
    }

    [Fact]
    public async Task AuthorizeAllAsync_WithMissingPermissions_ShouldReturnFailureWithMissingPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new[] { "Users.Read", "Users.Write", "Users.Delete" };
        var user = CreateTestUser(userId);
        var availablePermissions = permissions.Select(CreateTestPermission).ToList();
        var roles = new List<Role> { CreateTestRole("User") };
        var userPermissions = new List<string> { "Users.Read" };

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }));

        // Setup evaluation - user has Read but not Write or Delete
        _permissionEvaluationServiceMock.Setup(x => x.EvaluatePermission(user, "Users.Read", availablePermissions, roles))
            .Returns(PermissionEvaluationResult.FromRoles(true, new[] { "User" }, "Granted through role"));
        _permissionEvaluationServiceMock.Setup(x => x.EvaluatePermission(user, "Users.Write", availablePermissions, roles))
            .Returns(PermissionEvaluationResult.Denied("User does not have permission"));
        _permissionEvaluationServiceMock.Setup(x => x.EvaluatePermission(user, "Users.Delete", availablePermissions, roles))
            .Returns(PermissionEvaluationResult.Denied("User does not have permission"));

        _userRepositoryMock.Setup(x => x.GetUserWithRolesAndPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _permissionRepositoryMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(availablePermissions);
        _roleRepositoryMock.Setup(x => x.GetUserRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        _permissionCacheServiceMock.Setup(x => x.GetUserPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPermissions);

        // Act
        var result = await _service.AuthorizeAllAsync(claimsPrincipal, permissions);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.FailureReason.Should().Contain("Users.Write, Users.Delete");
    }

    [Fact]
    public async Task BulkAuthorizeAsync_WithMultiplePermissions_ShouldReturnResultsForEach()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var permissions = new[] { "Users.Read", "Users.Write", "Users.Delete" };
        var user = CreateTestUser(userId);
        var availablePermissions = permissions.Select(CreateTestPermission).ToList();
        var roles = new List<Role> { CreateTestRole("User") };
        var userPermissions = new List<string> { "Users.Read", "Users.Write" };

        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        }));

        // Setup evaluation - user has Read and Write but not Delete
        _permissionEvaluationServiceMock.Setup(x => x.EvaluatePermission(user, "Users.Read", availablePermissions, roles))
            .Returns(PermissionEvaluationResult.FromRoles(true, new[] { "User" }, "Granted through role"));
        _permissionEvaluationServiceMock.Setup(x => x.EvaluatePermission(user, "Users.Write", availablePermissions, roles))
            .Returns(PermissionEvaluationResult.FromRoles(true, new[] { "User" }, "Granted through role"));
        _permissionEvaluationServiceMock.Setup(x => x.EvaluatePermission(user, "Users.Delete", availablePermissions, roles))
            .Returns(PermissionEvaluationResult.Denied("User does not have permission"));

        _userRepositoryMock.Setup(x => x.GetUserWithRolesAndPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _permissionRepositoryMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(availablePermissions);
        _roleRepositoryMock.Setup(x => x.GetUserRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);
        _permissionCacheServiceMock.Setup(x => x.GetUserPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userPermissions);

        // Act
        var results = await _service.BulkAuthorizeAsync(claimsPrincipal, permissions);

        // Assert
        results.Should().HaveCount(3);
        results["Users.Read"].IsAuthorized.Should().BeTrue();
        results["Users.Write"].IsAuthorized.Should().BeTrue();
        results["Users.Delete"].IsAuthorized.Should().BeFalse();
    }

    [Fact]
    public async Task GetUserPermissionsAsync_WithValidUser_ShouldReturnCachedPermissions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var cachedPermissions = new List<string> { "Users.Read", "Users.Write" };

        _permissionCacheServiceMock.Setup(x => x.GetUserPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedPermissions);

        // Act
        var result = await _service.GetUserPermissionsAsync(userId);

        // Assert
        result.Should().BeEquivalentTo(cachedPermissions);
        _userRepositoryMock.Verify(x => x.GetUserWithRolesAndPermissionsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_WithNoCachedPermissions_ShouldFetchFromDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateTestUser(userId);
        var availablePermissions = new List<Permission> { CreateTestPermission("Users.Read") };
        var roles = new List<Role> { CreateTestRole("User") };
        var expectedPermissions = new List<string> { "Users.Read" };

        _permissionCacheServiceMock.Setup(x => x.GetUserPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<string>?)null);
        _userRepositoryMock.Setup(x => x.GetUserWithRolesAndPermissionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _permissionRepositoryMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(availablePermissions);
        _roleRepositoryMock.Setup(x => x.GetUserRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        // Mock the user entity method
        user.GetEffectivePermissions(availablePermissions, roles);

        // Act
        var result = await _service.GetUserPermissionsAsync(userId);

        // Assert
        _permissionCacheServiceMock.Verify(x => x.SetUserPermissionsAsync(userId, It.IsAny<IEnumerable<string>>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsValidPermissionAsync_WithValidPermission_ShouldReturnTrue()
    {
        // Arrange
        var permissionName = "Users.Read";
        var permission = CreateTestPermission(permissionName);

        _permissionRepositoryMock.Setup(x => x.GetByNameAsync(permissionName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(permission);

        // Act
        var result = await _service.IsValidPermissionAsync(permissionName);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsValidPermissionAsync_WithInvalidPermission_ShouldReturnFalse()
    {
        // Arrange
        var permissionName = "Invalid.Permission";

        _permissionRepositoryMock.Setup(x => x.GetByNameAsync(permissionName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Permission?)null);

        // Act
        var result = await _service.IsValidPermissionAsync(permissionName);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AuthorizeAsync_WithInvalidPermission_ShouldReturnFailure(string? permission)
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _service.AuthorizeAsync(userId, permission!);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.FailureReason.Should().Be("Permission cannot be null or empty");
    }

    [Fact]
    public async Task AuthorizeAsync_WithNullClaimsPrincipal_ShouldReturnFailure()
    {
        // Arrange
        ClaimsPrincipal? user = null;
        var permission = "Users.Read";

        // Act
        var result = await _service.AuthorizeAsync(user!, permission);

        // Assert
        result.Should().NotBeNull();
        result.IsAuthorized.Should().BeFalse();
        result.FailureReason.Should().Be("User principal is null");
    }

    private static User CreateTestUser(Guid id, bool isActive = true)
    {
        var user = User.Create(
            Email.Create($"test{id:N}@example.com"),
            "Test",
            "User",
            "hashedpassword");
        
        // Set the ID using reflection since it's likely protected
        var idProperty = typeof(User).GetProperty("Id");
        idProperty?.SetValue(user, id);
        
        if (!isActive)
        {
            // Deactivate user if needed - this might need to be done through a method
            user.Deactivate();
        }
        
        return user;
    }

    private static Permission CreateTestPermission(string name)
    {
        var parts = name.Split('.');
        var resource = parts.Length > 1 ? parts[0] : "Unknown";
        var action = parts.Length > 1 ? parts[1] : name;
        
        return Permission.Create(resource, action, $"Permission to {action} {resource}", "Test");
    }

    private static Role CreateTestRole(string name)
    {
        return new Role(name, $"{name} role description");
    }
}
