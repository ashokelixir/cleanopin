using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using CleanArchTemplate.Infrastructure.Data.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CleanArchTemplate.UnitTests.Infrastructure.Data.Repositories;

public class PermissionRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PermissionRepository _repository;

    public PermissionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new PermissionRepository(_context);
    }

    [Fact]
    public async Task GetByResourceAsync_WithValidResource_ReturnsPermissions()
    {
        // Arrange
        var resource = "Users";
        var permissions = new[]
        {
            Permission.Create(resource, "Create", "Create users", "User Management"),
            Permission.Create(resource, "Read", "Read users", "User Management"),
            Permission.Create("Roles", "Create", "Create roles", "Role Management")
        };

        await _context.Permissions.AddRangeAsync(permissions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByResourceAsync(resource);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Resource == resource);
        result.Should().BeInAscendingOrder(p => p.Action);
    }

    [Fact]
    public async Task GetByResourceAndActionAsync_WithValidResourceAndAction_ReturnsPermission()
    {
        // Arrange
        var resource = "Users";
        var action = "Create";
        var permission = Permission.Create(resource, action, "Create users", "User Management");

        await _context.Permissions.AddAsync(permission);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByResourceAndActionAsync(resource, action);

        // Assert
        result.Should().NotBeNull();
        result!.Resource.Should().Be(resource);
        result.Action.Should().Be(action);
    }

    [Fact]
    public async Task GetByResourceAndActionAsync_WithInvalidResourceAndAction_ReturnsNull()
    {
        // Arrange
        var resource = "NonExistent";
        var action = "NonExistent";

        // Act
        var result = await _repository.GetByResourceAndActionAsync(resource, action);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WithExistingResourceAndAction_ReturnsTrue()
    {
        // Arrange
        var resource = "Users";
        var action = "Create";
        var permission = Permission.Create(resource, action, "Create users", "User Management");

        await _context.Permissions.AddAsync(permission);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync(resource, action);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingResourceAndAction_ReturnsFalse()
    {
        // Arrange
        var resource = "NonExistent";
        var action = "NonExistent";

        // Act
        var result = await _repository.ExistsAsync(resource, action);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetByResourcesAsync_WithValidResources_ReturnsPermissions()
    {
        // Arrange
        var resources = new[] { "Users", "Roles" };
        var permissions = new[]
        {
            Permission.Create("Users", "Create", "Create users", "User Management"),
            Permission.Create("Users", "Read", "Read users", "User Management"),
            Permission.Create("Roles", "Create", "Create roles", "Role Management"),
            Permission.Create("Settings", "Update", "Update settings", "System")
        };

        await _context.Permissions.AddRangeAsync(permissions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByResourcesAsync(resources);

        // Assert
        result.Should().HaveCount(3);
        result.Should().OnlyContain(p => resources.Contains(p.Resource));
        result.Should().BeInAscendingOrder(p => p.Resource).And.ThenBeInAscendingOrder(p => p.Action);
    }

    [Fact]
    public async Task GetByActionAsync_WithValidAction_ReturnsPermissions()
    {
        // Arrange
        var action = "Create";
        var permissions = new[]
        {
            Permission.Create("Users", action, "Create users", "User Management"),
            Permission.Create("Roles", action, "Create roles", "Role Management"),
            Permission.Create("Users", "Read", "Read users", "User Management")
        };

        await _context.Permissions.AddRangeAsync(permissions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByActionAsync(action);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.Action == action);
        result.Should().BeInAscendingOrder(p => p.Resource);
    }

    [Fact]
    public async Task GetPagedAsync_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var permissions = new[]
        {
            Permission.Create("Users", "Create", "Create users", "User Management"),
            Permission.Create("Users", "Read", "Read users", "User Management"),
            Permission.Create("Roles", "Create", "Create roles", "Role Management"),
            Permission.Create("Settings", "Update", "Update settings", "System")
        };

        await _context.Permissions.AddRangeAsync(permissions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPagedAsync(1, 10, resource: "Users");

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(p => p.Resource == "Users");
    }

    [Fact]
    public async Task BulkAddAsync_WithValidPermissions_AddsAllPermissions()
    {
        // Arrange
        var permissions = new[]
        {
            Permission.Create("Users", "Create", "Create users", "User Management"),
            Permission.Create("Users", "Read", "Read users", "User Management"),
            Permission.Create("Roles", "Create", "Create roles", "Role Management")
        };

        // Act
        await _repository.BulkAddAsync(permissions);
        await _context.SaveChangesAsync();

        // Assert
        var allPermissions = await _context.Permissions.ToListAsync();
        allPermissions.Should().HaveCount(3);
    }

    [Fact]
    public async Task BulkActivateAsync_WithValidPermissionIds_ActivatesPermissions()
    {
        // Arrange
        var permissions = new[]
        {
            Permission.Create("Users", "Create", "Create users", "User Management"),
            Permission.Create("Users", "Read", "Read users", "User Management")
        };

        foreach (var permission in permissions)
        {
            permission.Deactivate();
        }

        await _context.Permissions.AddRangeAsync(permissions);
        await _context.SaveChangesAsync();

        var permissionIds = permissions.Select(p => p.Id).ToArray();

        // Act
        await _repository.BulkActivateAsync(permissionIds);
        await _context.SaveChangesAsync();

        // Assert
        var updatedPermissions = await _context.Permissions.Where(p => permissionIds.Contains(p.Id)).ToListAsync();
        updatedPermissions.Should().OnlyContain(p => p.IsActive);
    }

    [Fact]
    public async Task BulkDeactivateAsync_WithValidPermissionIds_DeactivatesPermissions()
    {
        // Arrange
        var permissions = new[]
        {
            Permission.Create("Users", "Create", "Create users", "User Management"),
            Permission.Create("Users", "Read", "Read users", "User Management")
        };

        await _context.Permissions.AddRangeAsync(permissions);
        await _context.SaveChangesAsync();

        var permissionIds = permissions.Select(p => p.Id).ToArray();

        // Act
        await _repository.BulkDeactivateAsync(permissionIds);
        await _context.SaveChangesAsync();

        // Assert
        var updatedPermissions = await _context.Permissions.Where(p => permissionIds.Contains(p.Id)).ToListAsync();
        updatedPermissions.Should().OnlyContain(p => !p.IsActive);
    }

    [Fact]
    public async Task GetByIdsAsync_WithValidIds_ReturnsPermissions()
    {
        // Arrange
        var permissions = new[]
        {
            Permission.Create("Users", "Create", "Create users", "User Management"),
            Permission.Create("Users", "Read", "Read users", "User Management"),
            Permission.Create("Roles", "Create", "Create roles", "Role Management")
        };

        await _context.Permissions.AddRangeAsync(permissions);
        await _context.SaveChangesAsync();

        var permissionIds = permissions.Take(2).Select(p => p.Id).ToArray();

        // Act
        var result = await _repository.GetByIdsAsync(permissionIds);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => permissionIds.Contains(p.Id));
    }

    [Fact]
    public async Task GetActiveByResourcesAsync_WithValidResources_ReturnsOnlyActivePermissions()
    {
        // Arrange
        var resources = new[] { "Users", "Roles" };
        var permissions = new[]
        {
            Permission.Create("Users", "Create", "Create users", "User Management"),
            Permission.Create("Users", "Read", "Read users", "User Management"),
            Permission.Create("Roles", "Create", "Create roles", "Role Management")
        };

        permissions[1].Deactivate(); // Deactivate Users.Read

        await _context.Permissions.AddRangeAsync(permissions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveByResourcesAsync(resources);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(p => p.IsActive && resources.Contains(p.Resource));
    }

    [Fact]
    public async Task CountByResourceAsync_WithValidResource_ReturnsCorrectCount()
    {
        // Arrange
        var resource = "Users";
        var permissions = new[]
        {
            Permission.Create(resource, "Create", "Create users", "User Management"),
            Permission.Create(resource, "Read", "Read users", "User Management"),
            Permission.Create("Roles", "Create", "Create roles", "Role Management")
        };

        await _context.Permissions.AddRangeAsync(permissions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CountByResourceAsync(resource);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task CountByActionAsync_WithValidAction_ReturnsCorrectCount()
    {
        // Arrange
        var action = "Create";
        var permissions = new[]
        {
            Permission.Create("Users", action, "Create users", "User Management"),
            Permission.Create("Roles", action, "Create roles", "Role Management"),
            Permission.Create("Users", "Read", "Read users", "User Management")
        };

        await _context.Permissions.AddRangeAsync(permissions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CountByActionAsync(action);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task CountByCategoryAsync_WithValidCategory_ReturnsCorrectCount()
    {
        // Arrange
        var category = "User Management";
        var permissions = new[]
        {
            Permission.Create("Users", "Create", "Create users", category),
            Permission.Create("Users", "Read", "Read users", category),
            Permission.Create("Roles", "Create", "Create roles", "Role Management")
        };

        await _context.Permissions.AddRangeAsync(permissions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CountByCategoryAsync(category);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task GetHierarchicalPermissionsAsync_WithParentPermission_ReturnsHierarchy()
    {
        // Arrange
        var parentPermission = Permission.Create("Users", "Manage", "Manage users", "User Management");
        var childPermission1 = Permission.Create("Users", "Create", "Create users", "User Management", parentPermission.Id);
        var childPermission2 = Permission.Create("Users", "Update", "Update users", "User Management", parentPermission.Id);

        await _context.Permissions.AddRangeAsync(new[] { parentPermission, childPermission1, childPermission2 });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetHierarchicalPermissionsAsync(parentPermission.Id);

        // Assert
        result.Should().HaveCount(3); // Parent + 2 children
        result.Should().Contain(p => p.Id == parentPermission.Id);
        result.Should().Contain(p => p.Id == childPermission1.Id);
        result.Should().Contain(p => p.Id == childPermission2.Id);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}