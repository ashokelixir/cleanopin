using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Enums;
using CleanArchTemplate.Domain.Services;
using CleanArchTemplate.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace CleanArchTemplate.UnitTests.Domain.Services;

public class PermissionEvaluationServiceTests
{
    private readonly PermissionEvaluationService _service;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _permissionId = Guid.NewGuid();
    private readonly Guid _roleId = Guid.NewGuid();

    public PermissionEvaluationServiceTests()
    {
        _service = new PermissionEvaluationService();
    }

    [Fact]
    public void HasPermission_WithResourceAndAction_ValidInput_ReturnsExpectedResult()
    {
        // Arrange
        var user = User.Create(new Email("test@example.com"), "John", "Doe", "hashedPassword");
        var resource = "Users";
        var action = "Create";
        var permission = new Permission(resource, action, "Create users", "User Management");
        var role = new Role("Admin", "Administrator role");
        role.AddPermission(permission);
        user.AddRole(role);
        
        var availablePermissions = new[] { permission };
        var userRoles = new[] { role };

        // Act
        var result = _service.HasPermission(user, resource, action, availablePermissions, userRoles);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void HasPermission_WithResourceAndAction_InvalidResource_ThrowsArgumentException(string resource)
    {
        // Arrange
        var user = User.Create(new Email("test@example.com"), "John", "Doe", "hashedPassword");
        var availablePermissions = Array.Empty<Permission>();
        var userRoles = Array.Empty<Role>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _service.HasPermission(user, resource, "Create", availablePermissions, userRoles));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void HasPermission_WithResourceAndAction_InvalidAction_ThrowsArgumentException(string action)
    {
        // Arrange
        var user = User.Create(new Email("test@example.com"), "John", "Doe", "hashedPassword");
        var availablePermissions = Array.Empty<Permission>();
        var userRoles = Array.Empty<Role>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _service.HasPermission(user, "Users", action, availablePermissions, userRoles));
    }

    [Fact]
    public void HasPermission_NullUser_ThrowsArgumentNullException()
    {
        // Arrange
        var availablePermissions = Array.Empty<Permission>();
        var userRoles = Array.Empty<Role>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _service.HasPermission(null!, "Users", "Create", availablePermissions, userRoles));
    }

    [Fact]
    public void HasPermission_WithPermissionName_ValidInput_ReturnsExpectedResult()
    {
        // Arrange
        var user = User.Create(new Email("test@example.com"), "John", "Doe", "hashedPassword");
        var permissionName = "Users.Create";
        var permission = new Permission("Users", "Create", "Create users", "User Management");
        var role = new Role("Admin", "Administrator role");
        role.AddPermission(permission);
        user.AddRole(role);
        
        var availablePermissions = new[] { permission };
        var userRoles = new[] { role };

        // Act
        var result = _service.HasPermission(user, permissionName, availablePermissions, userRoles);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EvaluatePermission_PermissionNotExists_ReturnsDeniedResult()
    {
        // Arrange
        var user = User.Create(new Email("test@example.com"), "John", "Doe", "hashedPassword");
        var permissionName = "NonExistent.Permission";
        var availablePermissions = Array.Empty<Permission>();
        var userRoles = Array.Empty<Role>();

        // Act
        var result = _service.EvaluatePermission(user, permissionName, availablePermissions, userRoles);

        // Assert
        result.HasPermission.Should().BeFalse();
        result.Source.Should().Be(PermissionSource.None);
        result.Reason.Should().Contain("does not exist");
    }

    [Fact]
    public void EvaluatePermission_PermissionInactive_ReturnsDeniedResult()
    {
        // Arrange
        var user = User.Create(new Email("test@example.com"), "John", "Doe", "hashedPassword");
        var permissionName = "Users.Create";
        var permission = new Permission("Users", "Create", "Create users", "User Management");
        permission.Deactivate();
        var availablePermissions = new[] { permission };
        var userRoles = Array.Empty<Role>();

        // Act
        var result = _service.EvaluatePermission(user, permissionName, availablePermissions, userRoles);

        // Assert
        result.HasPermission.Should().BeFalse();
        result.Source.Should().Be(PermissionSource.None);
        result.Reason.Should().Contain("not active");
    }

    [Fact]
    public void EvaluatePermission_UserOverrideGrant_ReturnsGrantedResult()
    {
        // Arrange
        var user = User.Create(new Email("test@example.com"), "John", "Doe", "hashedPassword");
        var permissionName = "Users.Create";
        var permission = new Permission("Users", "Create", "Create users", "User Management");
        var userPermission = new UserPermission(user.Id, permission.Id, PermissionState.Grant, "Special access");
        user.AddPermission(userPermission);
        var availablePermissions = new[] { permission };
        var userRoles = Array.Empty<Role>();

        // Act
        var result = _service.EvaluatePermission(user, permissionName, availablePermissions, userRoles);

        // Assert
        result.HasPermission.Should().BeTrue();
        result.Source.Should().Be(PermissionSource.UserOverride);
        result.UserOverride.Should().Be(userPermission);
        result.Reason.Should().Contain("Explicitly granted");
    }

    [Fact]
    public void EvaluatePermission_UserOverrideDeny_ReturnsDeniedResult()
    {
        // Arrange
        var user = User.Create(new Email("test@example.com"), "John", "Doe", "hashedPassword");
        var permissionName = "Users.Create";
        var permission = new Permission("Users", "Create", "Create users", "User Management");
        var userPermission = new UserPermission(user.Id, permission.Id, PermissionState.Deny, "Security restriction");
        user.AddPermission(userPermission);
        var availablePermissions = new[] { permission };
        var userRoles = Array.Empty<Role>();

        // Act
        var result = _service.EvaluatePermission(user, permissionName, availablePermissions, userRoles);

        // Assert
        result.HasPermission.Should().BeFalse();
        result.Source.Should().Be(PermissionSource.UserOverride);
        result.UserOverride.Should().Be(userPermission);
        result.Reason.Should().Contain("Explicitly denied");
    }

    [Fact]
    public void EvaluatePermission_RoleBasedPermission_ReturnsGrantedResult()
    {
        // Arrange
        var user = User.Create(new Email("test@example.com"), "John", "Doe", "hashedPassword");
        var permissionName = "Users.Create";
        var permission = new Permission("Users", "Create", "Create users", "User Management");
        var role = new Role("Admin", "Administrator role");
        role.AddPermission(permission);
        user.AddRole(role);
        var availablePermissions = new[] { permission };
        var userRoles = new[] { role };

        // Act
        var result = _service.EvaluatePermission(user, permissionName, availablePermissions, userRoles);

        // Assert
        result.HasPermission.Should().BeTrue();
        result.Source.Should().Be(PermissionSource.Role);
        result.GrantingRoles.Should().Contain("Admin");
        result.Reason.Should().Contain("Granted through roles");
    }

    [Fact]
    public void EvaluatePermission_NoPermissionSource_ReturnsDeniedResult()
    {
        // Arrange
        var user = User.Create(new Email("test@example.com"), "John", "Doe", "hashedPassword");
        var permissionName = "Users.Create";
        var permission = new Permission("Users", "Create", "Create users", "User Management");
        var availablePermissions = new[] { permission };
        var userRoles = Array.Empty<Role>();

        // Act
        var result = _service.EvaluatePermission(user, permissionName, availablePermissions, userRoles);

        // Assert
        result.HasPermission.Should().BeFalse();
        result.Source.Should().Be(PermissionSource.None);
        result.Reason.Should().Contain("does not have permission");
    }

    [Fact]
    public void GetUserPermissions_NullUser_ThrowsArgumentNullException()
    {
        // Arrange
        var availablePermissions = Array.Empty<Permission>();
        var userRoles = Array.Empty<Role>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _service.GetUserPermissions(null!, availablePermissions, userRoles));
    }

    [Fact]
    public void GetUserPermissions_WithRolePermissions_ReturnsCorrectPermissions()
    {
        // Arrange
        var user = User.Create(new Email("test@example.com"), "John", "Doe", "hashedPassword");
        var permission1 = new Permission("Users", "Create", "Create users", "User Management");
        var permission2 = new Permission("Users", "Read", "Read users", "User Management");
        var role = new Role("Admin", "Administrator role");
        role.AddPermission(permission1);
        role.AddPermission(permission2);
        user.AddRole(role);
        
        var availablePermissions = new[] { permission1, permission2 };
        var userRoles = new[] { role };

        // Act
        var result = _service.GetUserPermissions(user, availablePermissions, userRoles);

        // Assert
        result.Should().Contain(new[] { "Users.Create", "Users.Read" });
    }

    [Fact]
    public void GetUserPermissions_WithUserOverrides_AppliesOverridesCorrectly()
    {
        // Arrange
        var user = User.Create(new Email("test@example.com"), "John", "Doe", "hashedPassword");
        var permission1 = new Permission("Users", "Create", "Create users", "User Management");
        var permission2 = new Permission("Users", "Delete", "Delete users", "User Management");
        var role = new Role("Admin", "Administrator role");
        role.AddPermission(permission1);
        user.AddRole(role);

        var userPermissionGrant = new UserPermission(user.Id, permission2.Id, PermissionState.Grant, "Special access");
        var userPermissionDeny = new UserPermission(user.Id, permission1.Id, PermissionState.Deny, "Restricted");
        user.AddPermission(userPermissionGrant);
        user.AddPermission(userPermissionDeny);

        var availablePermissions = new[] { permission1, permission2 };
        var userRoles = new[] { role };

        // Act
        var result = _service.GetUserPermissions(user, availablePermissions, userRoles);

        // Assert
        result.Should().Contain("Users.Delete"); // Granted by user override
        result.Should().NotContain("Users.Create"); // Denied by user override
    }

    [Fact]
    public void HasAnyPermission_NullUser_ThrowsArgumentNullException()
    {
        // Arrange
        var availablePermissions = Array.Empty<Permission>();
        var userRoles = Array.Empty<Role>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _service.HasAnyPermission(null!, new[] { "Users.Create" }, availablePermissions, userRoles));
    }

    [Fact]
    public void HasAnyPermission_NullPermissions_ThrowsArgumentNullException()
    {
        // Arrange
        var user = User.Create(new Email("test@example.com"), "John", "Doe", "hashedPassword");
        var availablePermissions = Array.Empty<Permission>();
        var userRoles = Array.Empty<Role>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            _service.HasAnyPermission(user, null!, availablePermissions, userRoles));
    }

    [Fact]
    public void HasAnyPermission_EmptyPermissions_ReturnsFalse()
    {
        // Arrange
        var user = User.Create(new Email("test@example.com"), "John", "Doe", "hashedPassword");
        var availablePermissions = Array.Empty<Permission>();
        var userRoles = Array.Empty<Role>();

        // Act
        var result = _service.HasAnyPermission(user, Array.Empty<string>(), availablePermissions, userRoles);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasAnyPermission_UserHasOnePermission_ReturnsTrue()
    {
        // Arrange
        var user = User.Create(new Email("test@example.com"), "John", "Doe", "hashedPassword");
        var permission1 = new Permission("Users", "Create", "Create users", "User Management");
        var permission2 = new Permission("Users", "Delete", "Delete users", "User Management");
        var role = new Role("Admin", "Administrator role");
        role.AddPermission(permission1);
        user.AddRole(role);

        var availablePermissions = new[] { permission1, permission2 };
        var userRoles = new[] { role };

        // Act
        var result = _service.HasAnyPermission(user, new[] { "Users.Create", "Users.Delete" }, availablePermissions, userRoles);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void HasHierarchicalPermission_InvalidPermissionName_ThrowsArgumentException(string permissionName)
    {
        // Arrange
        var user = User.Create(new Email("test@example.com"), "John", "Doe", "hashedPassword");
        var availablePermissions = Array.Empty<Permission>();
        var userRoles = Array.Empty<Role>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            _service.HasHierarchicalPermission(user, permissionName, availablePermissions, userRoles));
    }

    [Fact]
    public void HasHierarchicalPermission_DirectPermission_ReturnsTrue()
    {
        // Arrange
        var user = User.Create(new Email("test@example.com"), "John", "Doe", "hashedPassword");
        var permissionName = "Users.Create";
        var permission = new Permission("Users", "Create", "Create users", "User Management");
        var role = new Role("Admin", "Administrator role");
        role.AddPermission(permission);
        user.AddRole(role);

        var availablePermissions = new[] { permission };
        var userRoles = new[] { role };

        // Act
        var result = _service.HasHierarchicalPermission(user, permissionName, availablePermissions, userRoles);

        // Assert
        result.Should().BeTrue();
    }
}