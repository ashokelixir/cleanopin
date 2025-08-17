using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Domain.Interfaces;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using CleanArchTemplate.Infrastructure.Data.Seed;
using CleanArchTemplate.TestUtilities.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using FluentAssertions;
using Xunit.Abstractions;

namespace CleanArchTemplate.IntegrationTests.Infrastructure.Services;

[Collection("Database")]
public class PermissionSeedingIntegrationTests : BaseIntegrationTest
{
    private readonly IPermissionSeedingService _seedingService;
    private readonly DatabaseSeeder _databaseSeeder;

    public PermissionSeedingIntegrationTests(ITestOutputHelper output) : base(output)
    {
        using var scope = Factory.Services.CreateScope();
        _seedingService = scope.ServiceProvider.GetRequiredService<IPermissionSeedingService>();
        _databaseSeeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    }

    [Fact]
    public async Task SeedDefaultPermissionsAsync_ShouldCreateAllRequiredPermissions()
    {
        // Arrange
        await ResetDatabaseAsync();

        // Act
        using var scope = Factory.Services.CreateScope();
        var seedingService = scope.ServiceProvider.GetRequiredService<IPermissionSeedingService>();
        await seedingService.SeedDefaultPermissionsAsync();

        // Assert
        var permissions = await ExecuteDbContextAsync(async context => 
            await context.Permissions.ToListAsync());
        
        permissions.Should().NotBeEmpty();
        
        // Verify key permissions exist
        permissions.Should().Contain(p => p.Resource == "Users" && p.Action == "Create");
        permissions.Should().Contain(p => p.Resource == "Users" && p.Action == "Read");
        permissions.Should().Contain(p => p.Resource == "Users" && p.Action == "Update");
        permissions.Should().Contain(p => p.Resource == "Users" && p.Action == "Delete");
        
        permissions.Should().Contain(p => p.Resource == "Roles" && p.Action == "Create");
        permissions.Should().Contain(p => p.Resource == "Roles" && p.Action == "Read");
        
        permissions.Should().Contain(p => p.Resource == "Permissions" && p.Action == "Create");
        permissions.Should().Contain(p => p.Resource == "Permissions" && p.Action == "Read");
        
        permissions.Should().Contain(p => p.Resource == "Secrets" && p.Action == "Read");
        permissions.Should().Contain(p => p.Resource == "Secrets" && p.Action == "Manage");
    }

    [Fact]
    public async Task SeedEnvironmentPermissionsAsync_WithDevelopment_ShouldAddDevelopmentPermissions()
    {
        // Arrange
        await ResetDatabaseAsync();

        // Act
        using var scope = Factory.Services.CreateScope();
        var seedingService = scope.ServiceProvider.GetRequiredService<IPermissionSeedingService>();
        await seedingService.SeedEnvironmentPermissionsAsync("Development");

        // Assert
        var developmentPermissions = await ExecuteDbContextAsync(async context =>
            await context.Permissions
                .Where(p => p.Category == "Development")
                .ToListAsync());

        developmentPermissions.Should().NotBeEmpty();
        developmentPermissions.Should().Contain(p => p.Resource == "Development" && p.Action == "Debug");
        developmentPermissions.Should().Contain(p => p.Resource == "Development" && p.Action == "TestData");
        developmentPermissions.Should().Contain(p => p.Resource == "Development" && p.Action == "Reset");
    }

    [Fact]
    public async Task DatabaseSeeder_SeedAsync_ShouldCreateCompletePermissionSystem()
    {
        // Arrange
        await ResetDatabaseAsync();

        // Act
        using var scope = Factory.Services.CreateScope();
        var databaseSeeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await databaseSeeder.SeedAsync();

        // Assert
        var (permissions, roles, users, rolePermissions, userRoles) = await ExecuteDbContextAsync(async context =>
        {
            var perms = await context.Permissions.ToListAsync();
            var rolesList = await context.Roles.ToListAsync();
            var usersList = await context.Users.ToListAsync();
            var rolePerms = await context.RolePermissions.ToListAsync();
            var userRolesList = await context.UserRoles.ToListAsync();
            return (perms, rolesList, usersList, rolePerms, userRolesList);
        });

        // Verify permissions were created
        permissions.Should().NotBeEmpty();
        permissions.Count.Should().BeGreaterThan(20);

        // Verify roles were created
        roles.Should().NotBeEmpty();
        roles.Should().Contain(r => r.Name == "Administrator");
        roles.Should().Contain(r => r.Name == "User Manager");
        roles.Should().Contain(r => r.Name == "Role Manager");
        roles.Should().Contain(r => r.Name == "User");

        // Verify admin user was created
        users.Should().NotBeEmpty();
        users.Should().Contain(u => u.Email.Value.Contains("admin"));

        // Verify role-permission assignments
        rolePermissions.Should().NotBeEmpty();
        
        // Verify admin role has permissions
        var adminRole = roles.First(r => r.Name == "Administrator");
        var adminPermissions = rolePermissions.Where(rp => rp.RoleId == adminRole.Id).ToList();
        adminPermissions.Should().NotBeEmpty();

        // Verify user-role assignments
        userRoles.Should().NotBeEmpty();
        var adminUser = users.First(u => u.Email.Value.Contains("admin"));
        userRoles.Should().Contain(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id);
    }

    [Fact]
    public async Task ValidatePermissionIntegrityAsync_AfterSeeding_ShouldReturnTrue()
    {
        // Arrange
        await ResetDatabaseAsync();
        
        using var scope = Factory.Services.CreateScope();
        var seedingService = scope.ServiceProvider.GetRequiredService<IPermissionSeedingService>();
        await seedingService.SeedDefaultPermissionsAsync();

        // Act
        var result = await seedingService.ValidatePermissionIntegrityAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SeedDefaultRolePermissionsAsync_ShouldAssignPermissionsToRoles()
    {
        // Arrange
        await ResetDatabaseAsync();
        
        using var scope = Factory.Services.CreateScope();
        var seedingService = scope.ServiceProvider.GetRequiredService<IPermissionSeedingService>();
        var databaseSeeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        
        // Seed permissions and roles first using the full seeder
        await databaseSeeder.SeedAsync();

        // Act
        await seedingService.SeedDefaultRolePermissionsAsync();

        // Assert
        var rolePermissions = await ExecuteDbContextAsync(async context =>
            await context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .ToListAsync());

        rolePermissions.Should().NotBeEmpty();

        // Verify Administrator role has many permissions
        var adminRolePermissions = rolePermissions
            .Where(rp => rp.Role.Name == "Administrator")
            .ToList();
        adminRolePermissions.Should().NotBeEmpty();
        adminRolePermissions.Count.Should().BeGreaterThan(10);

        // Verify User role has basic permissions
        var userRolePermissions = rolePermissions
            .Where(rp => rp.Role.Name == "User")
            .ToList();
        userRolePermissions.Should().NotBeEmpty();
        userRolePermissions.Should().Contain(rp => 
            rp.Permission.Resource == "Users" && rp.Permission.Action == "Read");
    }

    [Fact]
    public async Task CleanupOrphanedPermissionsAsync_ShouldRemoveUnusedPermissions()
    {
        // Arrange
        await ResetDatabaseAsync();
        
        // Add an orphaned permission
        await ExecuteDbContextAsync(async context =>
        {
            var orphanedPermission = new Domain.Entities.Permission("Orphaned", "Test", "Test permission", "Test");
            context.Permissions.Add(orphanedPermission);
            await context.SaveChangesAsync();
        });

        var initialCount = await ExecuteDbContextAsync(async context => 
            await context.Permissions.CountAsync());

        // Act
        using var scope = Factory.Services.CreateScope();
        var seedingService = scope.ServiceProvider.GetRequiredService<IPermissionSeedingService>();
        await seedingService.CleanupOrphanedPermissionsAsync();

        // Assert
        var finalCount = await ExecuteDbContextAsync(async context => 
            await context.Permissions.CountAsync());
        finalCount.Should().BeLessThan(initialCount);

        var orphanedExists = await ExecuteDbContextAsync(async context =>
            await context.Permissions
                .AnyAsync(p => p.Resource == "Orphaned" && p.Action == "Test"));
        orphanedExists.Should().BeFalse();
    }

    [Fact]
    public async Task SeedingService_MultipleRuns_ShouldBeIdempotent()
    {
        // Arrange
        await ResetDatabaseAsync();

        using var scope = Factory.Services.CreateScope();
        var seedingService = scope.ServiceProvider.GetRequiredService<IPermissionSeedingService>();

        // Act - Run seeding multiple times
        await seedingService.SeedDefaultPermissionsAsync();
        var firstCount = await ExecuteDbContextAsync(async context => 
            await context.Permissions.CountAsync());

        await seedingService.SeedDefaultPermissionsAsync();
        var secondCount = await ExecuteDbContextAsync(async context => 
            await context.Permissions.CountAsync());

        await seedingService.SeedDefaultPermissionsAsync();
        var thirdCount = await ExecuteDbContextAsync(async context => 
            await context.Permissions.CountAsync());

        // Assert - Count should remain the same
        firstCount.Should().Be(secondCount);
        secondCount.Should().Be(thirdCount);
        firstCount.Should().BeGreaterThan(0);
    }
}