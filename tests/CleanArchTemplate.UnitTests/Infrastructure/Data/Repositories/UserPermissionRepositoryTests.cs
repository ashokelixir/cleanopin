using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Enums;
using CleanArchTemplate.Domain.ValueObjects;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using CleanArchTemplate.Infrastructure.Data.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CleanArchTemplate.UnitTests.Infrastructure.Data.Repositories;

public class UserPermissionRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserPermissionRepository _repository;
    private readonly User _testUser;
    private readonly Permission _testPermission;

    public UserPermissionRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new UserPermissionRepository(_context);

        // Create test data
        _testUser = User.Create(Email.Create("test@example.com"), "Test", "User", "hashedpassword");
        _testPermission = Permission.Create("Users", "Create", "Create users", "User Management");

        _context.Users.Add(_testUser);
        _context.Permissions.Add(_testPermission);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByUserIdAsync_WithValidUserId_ReturnsUserPermissions()
    {
        // Arrange
        var userPermissions = new[]
        {
            UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Grant, "Test reason"),
            UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Deny, "Test deny reason")
        };

        await _context.UserPermissions.AddRangeAsync(userPermissions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(_testUser.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(up => up.UserId == _testUser.Id);
        result.Should().OnlyContain(up => up.Permission != null);
    }

    [Fact]
    public async Task GetByUserAndPermissionAsync_WithValidIds_ReturnsUserPermission()
    {
        // Arrange
        var userPermission = UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Grant, "Test reason");

        await _context.UserPermissions.AddAsync(userPermission);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserAndPermissionAsync(_testUser.Id, _testPermission.Id);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(_testUser.Id);
        result.PermissionId.Should().Be(_testPermission.Id);
        result.Permission.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByUserAndPermissionAsync_WithInvalidIds_ReturnsNull()
    {
        // Arrange
        var invalidUserId = Guid.NewGuid();
        var invalidPermissionId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByUserAndPermissionAsync(invalidUserId, invalidPermissionId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveByUserIdAsync_WithValidUserId_ReturnsOnlyActivePermissions()
    {
        // Arrange
        var activePermission = UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Grant, "Active permission");
        var expiredPermission = UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Grant, "Expired permission", DateTime.UtcNow.AddDays(1));
        // Manually set expiration to past date using reflection since the entity validates future dates
        var expiresAtField = typeof(UserPermission).GetField("<ExpiresAt>k__BackingField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        expiresAtField?.SetValue(expiredPermission, DateTime.UtcNow.AddDays(-1));

        await _context.UserPermissions.AddRangeAsync(new[] { activePermission, expiredPermission });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveByUserIdAsync(_testUser.Id);

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(up => up.IsActive());
    }

    [Fact]
    public async Task GetExpiringPermissionsAsync_WithExpiringPermissions_ReturnsExpiringPermissions()
    {
        // Arrange
        var expiringDate = DateTime.UtcNow.AddDays(1);
        var expiringPermission = UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Grant, "Expiring permission", expiringDate);
        var nonExpiringPermission = UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Grant, "Non-expiring permission");

        await _context.UserPermissions.AddRangeAsync(new[] { expiringPermission, nonExpiringPermission });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetExpiringPermissionsAsync(DateTime.UtcNow.AddDays(2));

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(up => up.ExpiresAt.HasValue && up.ExpiresAt.Value <= DateTime.UtcNow.AddDays(2));
    }

    [Fact]
    public async Task GetByUserIdAndStateAsync_WithValidUserIdAndState_ReturnsFilteredPermissions()
    {
        // Arrange
        var grantPermission = UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Grant, "Grant permission");
        var denyPermission = UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Deny, "Deny permission");

        await _context.UserPermissions.AddRangeAsync(new[] { grantPermission, denyPermission });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAndStateAsync(_testUser.Id, PermissionState.Grant);

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(up => up.State == PermissionState.Grant);
    }

    [Fact]
    public async Task GetByPermissionIdAsync_WithValidPermissionId_ReturnsUserPermissions()
    {
        // Arrange
        var userPermission = UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Grant, "Test permission");

        await _context.UserPermissions.AddAsync(userPermission);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByPermissionIdAsync(_testPermission.Id);

        // Assert
        result.Should().HaveCount(1);
        result.Should().OnlyContain(up => up.PermissionId == _testPermission.Id);
        result.Should().OnlyContain(up => up.Permission != null && up.User != null);
    }

    [Fact]
    public async Task BulkAddAsync_WithValidUserPermissions_AddsAllPermissions()
    {
        // Arrange
        var userPermissions = new[]
        {
            UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Grant, "Bulk permission 1"),
            UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Deny, "Bulk permission 2")
        };

        // Act
        await _repository.BulkAddAsync(userPermissions);
        await _context.SaveChangesAsync();

        // Assert
        var allUserPermissions = await _context.UserPermissions.ToListAsync();
        allUserPermissions.Should().HaveCount(2);
    }

    [Fact]
    public async Task BulkRemoveByUserAndPermissionsAsync_WithValidIds_RemovesPermissions()
    {
        // Arrange
        var userPermissions = new[]
        {
            UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Grant, "Permission to remove"),
            UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Deny, "Another permission to remove")
        };

        await _context.UserPermissions.AddRangeAsync(userPermissions);
        await _context.SaveChangesAsync();

        var permissionIds = new[] { _testPermission.Id };

        // Act
        await _repository.BulkRemoveByUserAndPermissionsAsync(_testUser.Id, permissionIds);
        await _context.SaveChangesAsync();

        // Assert
        var remainingPermissions = await _context.UserPermissions.ToListAsync();
        remainingPermissions.Should().BeEmpty();
    }

    [Fact]
    public async Task BulkUpdateStateAsync_WithValidIds_UpdatesState()
    {
        // Arrange
        var userPermissions = new[]
        {
            UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Grant, "Permission 1"),
            UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Grant, "Permission 2")
        };

        await _context.UserPermissions.AddRangeAsync(userPermissions);
        await _context.SaveChangesAsync();

        var userPermissionIds = userPermissions.Select(up => up.Id).ToArray();

        // Act
        await _repository.BulkUpdateStateAsync(userPermissionIds, PermissionState.Deny, "Bulk update reason");
        await _context.SaveChangesAsync();

        // Assert
        var updatedPermissions = await _context.UserPermissions.Where(up => userPermissionIds.Contains(up.Id)).ToListAsync();
        updatedPermissions.Should().OnlyContain(up => up.State == PermissionState.Deny);
        updatedPermissions.Should().OnlyContain(up => up.Reason == "Bulk update reason");
    }

    [Fact]
    public async Task BulkSetExpirationAsync_WithValidIds_UpdatesExpiration()
    {
        // Arrange
        var userPermissions = new[]
        {
            UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Grant, "Permission 1"),
            UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Grant, "Permission 2")
        };

        await _context.UserPermissions.AddRangeAsync(userPermissions);
        await _context.SaveChangesAsync();

        var userPermissionIds = userPermissions.Select(up => up.Id).ToArray();
        var expirationDate = DateTime.UtcNow.AddDays(30);

        // Act
        await _repository.BulkSetExpirationAsync(userPermissionIds, expirationDate);
        await _context.SaveChangesAsync();

        // Assert
        var updatedPermissions = await _context.UserPermissions.Where(up => userPermissionIds.Contains(up.Id)).ToListAsync();
        updatedPermissions.Should().OnlyContain(up => up.ExpiresAt.HasValue);
        updatedPermissions.Should().OnlyContain(up => up.ExpiresAt!.Value.Date == expirationDate.Date);
    }

    [Fact]
    public async Task GetByUserIdsAsync_WithValidUserIds_ReturnsUserPermissions()
    {
        // Arrange
        var anotherUser = User.Create(Email.Create("another@example.com"), "Another", "User", "hashedpassword");
        await _context.Users.AddAsync(anotherUser);
        await _context.SaveChangesAsync();

        var userPermissions = new[]
        {
            UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Grant, "User 1 permission"),
            UserPermission.Create(anotherUser.Id, _testPermission.Id, PermissionState.Grant, "User 2 permission")
        };

        await _context.UserPermissions.AddRangeAsync(userPermissions);
        await _context.SaveChangesAsync();

        var userIds = new[] { _testUser.Id, anotherUser.Id };

        // Act
        var result = await _repository.GetByUserIdsAsync(userIds);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(up => userIds.Contains(up.UserId));
        result.Should().OnlyContain(up => up.Permission != null && up.User != null);
    }

    [Fact]
    public async Task GetPagedAsync_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var userPermissions = new[]
        {
            UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Grant, "Grant permission"),
            UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Deny, "Deny permission")
        };

        await _context.UserPermissions.AddRangeAsync(userPermissions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetPagedAsync(1, 10, userId: _testUser.Id, state: PermissionState.Grant);

        // Assert
        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Items.Should().OnlyContain(up => up.UserId == _testUser.Id && up.State == PermissionState.Grant);
    }

    [Fact]
    public async Task CountByUserIdAsync_WithValidUserId_ReturnsCorrectCount()
    {
        // Arrange
        var userPermissions = new[]
        {
            UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Grant, "Permission 1"),
            UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Deny, "Permission 2")
        };

        await _context.UserPermissions.AddRangeAsync(userPermissions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CountByUserIdAsync(_testUser.Id);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task CountByPermissionIdAsync_WithValidPermissionId_ReturnsCorrectCount()
    {
        // Arrange
        var userPermissions = new[]
        {
            UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Grant, "Permission 1"),
            UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Deny, "Permission 2")
        };

        await _context.UserPermissions.AddRangeAsync(userPermissions);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CountByPermissionIdAsync(_testPermission.Id);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public async Task ExistsAsync_WithExistingUserPermission_ReturnsTrue()
    {
        // Arrange
        var userPermission = UserPermission.Create(_testUser.Id, _testPermission.Id, PermissionState.Grant, "Test permission");

        await _context.UserPermissions.AddAsync(userPermission);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.ExistsAsync(_testUser.Id, _testPermission.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WithNonExistingUserPermission_ReturnsFalse()
    {
        // Arrange
        var nonExistingUserId = Guid.NewGuid();
        var nonExistingPermissionId = Guid.NewGuid();

        // Act
        var result = await _repository.ExistsAsync(nonExistingUserId, nonExistingPermissionId);

        // Assert
        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}