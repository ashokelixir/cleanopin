using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Enums;
using CleanArchTemplate.Domain.Services;
using FluentAssertions;
using Xunit;

namespace CleanArchTemplate.UnitTests.Domain.Services;

public class PermissionEvaluationResultTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _permissionId = Guid.NewGuid();

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var hasPermission = true;
        var source = PermissionSource.Role;
        var reason = "Test reason";
        var grantingRoles = new[] { "Admin", "User" };

        // Act
        var result = new PermissionEvaluationResult(
            hasPermission,
            source,
            reason,
            grantingRoles: grantingRoles);

        // Assert
        result.HasPermission.Should().Be(hasPermission);
        result.Source.Should().Be(source);
        result.Reason.Should().Be(reason);
        result.GrantingRoles.Should().BeEquivalentTo(grantingRoles);
        result.UserOverride.Should().BeNull();
        result.IsInherited.Should().BeFalse();
        result.ParentPermissionName.Should().BeNull();
    }

    [Fact]
    public void Constructor_NullReason_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PermissionEvaluationResult(
            true,
            PermissionSource.Role,
            null!));
    }

    [Fact]
    public void Constructor_NullGrantingRoles_CreatesEmptyCollection()
    {
        // Act
        var result = new PermissionEvaluationResult(
            true,
            PermissionSource.Role,
            "Test reason",
            grantingRoles: null);

        // Assert
        result.GrantingRoles.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithInheritance_SetsInheritanceProperties()
    {
        // Arrange
        var parentPermissionName = "Users.Manage";

        // Act
        var result = new PermissionEvaluationResult(
            true,
            PermissionSource.Inheritance,
            "Inherited from parent",
            isInherited: true,
            parentPermissionName: parentPermissionName);

        // Assert
        result.IsInherited.Should().BeTrue();
        result.ParentPermissionName.Should().Be(parentPermissionName);
    }

    [Fact]
    public void FromUserOverride_GrantState_CreatesCorrectResult()
    {
        // Arrange
        var userPermission = new UserPermission(_userId, _permissionId, PermissionState.Grant, "Special access");
        var reason = "User override granted";

        // Act
        var result = PermissionEvaluationResult.FromUserOverride(userPermission, reason);

        // Assert
        result.HasPermission.Should().BeTrue();
        result.Source.Should().Be(PermissionSource.UserOverride);
        result.UserOverride.Should().Be(userPermission);
        result.Reason.Should().Be(reason);
        result.GrantingRoles.Should().BeEmpty();
        result.IsInherited.Should().BeFalse();
    }

    [Fact]
    public void FromUserOverride_DenyState_CreatesCorrectResult()
    {
        // Arrange
        var userPermission = new UserPermission(_userId, _permissionId, PermissionState.Deny, "Security restriction");
        var reason = "User override denied";

        // Act
        var result = PermissionEvaluationResult.FromUserOverride(userPermission, reason);

        // Assert
        result.HasPermission.Should().BeFalse();
        result.Source.Should().Be(PermissionSource.UserOverride);
        result.UserOverride.Should().Be(userPermission);
        result.Reason.Should().Be(reason);
    }

    [Fact]
    public void FromRoles_WithPermission_CreatesCorrectResult()
    {
        // Arrange
        var grantingRoles = new[] { "Admin", "Manager" };
        var reason = "Granted through roles";

        // Act
        var result = PermissionEvaluationResult.FromRoles(true, grantingRoles, reason);

        // Assert
        result.HasPermission.Should().BeTrue();
        result.Source.Should().Be(PermissionSource.Role);
        result.GrantingRoles.Should().BeEquivalentTo(grantingRoles);
        result.Reason.Should().Be(reason);
        result.UserOverride.Should().BeNull();
        result.IsInherited.Should().BeFalse();
    }

    [Fact]
    public void FromRoles_WithoutPermission_CreatesCorrectResult()
    {
        // Arrange
        var grantingRoles = Array.Empty<string>();
        var reason = "No roles grant this permission";

        // Act
        var result = PermissionEvaluationResult.FromRoles(false, grantingRoles, reason);

        // Assert
        result.HasPermission.Should().BeFalse();
        result.Source.Should().Be(PermissionSource.Role);
        result.GrantingRoles.Should().BeEmpty();
        result.Reason.Should().Be(reason);
    }

    [Fact]
    public void FromInheritance_CreatesCorrectResult()
    {
        // Arrange
        var parentPermissionName = "Users.Manage";
        var grantingRoles = new[] { "Admin" };
        var reason = "Inherited from parent permission";

        // Act
        var result = PermissionEvaluationResult.FromInheritance(true, parentPermissionName, grantingRoles, reason);

        // Assert
        result.HasPermission.Should().BeTrue();
        result.Source.Should().Be(PermissionSource.Inheritance);
        result.IsInherited.Should().BeTrue();
        result.ParentPermissionName.Should().Be(parentPermissionName);
        result.GrantingRoles.Should().BeEquivalentTo(grantingRoles);
        result.Reason.Should().Be(reason);
    }

    [Fact]
    public void Denied_CreatesCorrectResult()
    {
        // Arrange
        var reason = "Permission denied";

        // Act
        var result = PermissionEvaluationResult.Denied(reason);

        // Assert
        result.HasPermission.Should().BeFalse();
        result.Source.Should().Be(PermissionSource.None);
        result.Reason.Should().Be(reason);
        result.UserOverride.Should().BeNull();
        result.GrantingRoles.Should().BeEmpty();
        result.IsInherited.Should().BeFalse();
        result.ParentPermissionName.Should().BeNull();
    }

    [Theory]
    [InlineData(PermissionSource.None)]
    [InlineData(PermissionSource.UserOverride)]
    [InlineData(PermissionSource.Role)]
    [InlineData(PermissionSource.Inheritance)]
    public void PermissionSource_AllValues_AreValid(PermissionSource source)
    {
        // Act
        var result = new PermissionEvaluationResult(false, source, "Test reason");

        // Assert
        result.Source.Should().Be(source);
    }

    [Fact]
    public void GrantingRoles_IsReadOnly()
    {
        // Arrange
        var grantingRoles = new[] { "Admin", "User" };
        var result = new PermissionEvaluationResult(true, PermissionSource.Role, "Test", grantingRoles: grantingRoles);

        // Act & Assert
        result.GrantingRoles.Should().BeAssignableTo<IReadOnlyCollection<string>>();
        
        // Verify we can't modify the collection
        var collection = result.GrantingRoles as ICollection<string>;
        collection.Should().BeNull("GrantingRoles should be read-only");
    }
}