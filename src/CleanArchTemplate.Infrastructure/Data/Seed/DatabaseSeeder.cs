using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.ValueObjects;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using CleanArchTemplate.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Infrastructure.Data.Seed;

/// <summary>
/// Database seeder for initial data
/// </summary>
public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(ApplicationDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Seeds the database with initial data
    /// </summary>
    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting database seeding...");

            // Seed permissions first
            await SeedPermissionsAsync();
            await _context.SaveChangesAsync();

            // Seed roles
            await SeedRolesAsync();
            await _context.SaveChangesAsync();

            // Seed admin user
            await SeedAdminUserAsync();
            await _context.SaveChangesAsync();

            // Assign permissions to roles
            await AssignPermissionsToRolesAsync();
            await _context.SaveChangesAsync();

            // Assign roles to admin user
            await AssignRolesToAdminAsync();
            await _context.SaveChangesAsync();

            _logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task SeedPermissionsAsync()
    {
        if (await _context.Permissions.AnyAsync())
        {
            _logger.LogInformation("Permissions already exist. Skipping permission seeding.");
            return;
        }

        _logger.LogInformation("Seeding permissions...");

        var permissions = new[]
        {
            // User Management
            new Permission("users.create", "Create new users", "User Management"),
            new Permission("users.read", "View user information", "User Management"),
            new Permission("users.update", "Update user information", "User Management"),
            new Permission("users.delete", "Delete users", "User Management"),
            new Permission("users.manage_roles", "Manage user roles", "User Management"),

            // Role Management
            new Permission("roles.create", "Create new roles", "Role Management"),
            new Permission("roles.read", "View role information", "Role Management"),
            new Permission("roles.update", "Update role information", "Role Management"),
            new Permission("roles.delete", "Delete roles", "Role Management"),
            new Permission("roles.manage_permissions", "Manage role permissions", "Role Management"),

            // Permission Management
            new Permission("permissions.create", "Create new permissions", "Permission Management"),
            new Permission("permissions.read", "View permission information", "Permission Management"),
            new Permission("permissions.update", "Update permission information", "Permission Management"),
            new Permission("permissions.delete", "Delete permissions", "Permission Management"),

            // System Administration
            new Permission("system.admin", "Full system administration access", "System Administration"),
            new Permission("system.health", "View system health information", "System Administration"),
            new Permission("system.logs", "View system logs", "System Administration"),
            new Permission("system.settings", "Manage system settings", "System Administration"),

            // API Access
            new Permission("api.access", "Access API endpoints", "API Access"),
            new Permission("api.admin", "Administrative API access", "API Access")
        };

        await _context.Permissions.AddRangeAsync(permissions);
        _logger.LogInformation($"Added {permissions.Length} permissions.");
    }

    private async Task SeedRolesAsync()
    {
        if (await _context.Roles.AnyAsync())
        {
            _logger.LogInformation("Roles already exist. Skipping role seeding.");
            return;
        }

        _logger.LogInformation("Seeding roles...");

        var roles = new[]
        {
            new Role("Administrator", "Full system administrator with all permissions"),
            new Role("User Manager", "Can manage users and their roles"),
            new Role("Role Manager", "Can manage roles and permissions"),
            new Role("User", "Standard user with basic permissions"),
            new Role("Guest", "Guest user with read-only access")
        };

        await _context.Roles.AddRangeAsync(roles);
        _logger.LogInformation($"Added {roles.Length} roles.");
    }

    private async Task SeedAdminUserAsync()
    {
        var adminEmail = Email.Create("admin@cleanarch.com");
        
        if (await _context.Users.AnyAsync(u => u.Email.Value == adminEmail.Value))
        {
            _logger.LogInformation("Admin user already exists. Skipping admin user seeding.");
            return;
        }

        _logger.LogInformation("Seeding admin user...");

        var passwordHash = PasswordHelper.HashPassword("Admin123!");
        var adminUser = User.Create(adminEmail, "System", "Administrator", passwordHash);
        
        // Verify email immediately for admin user
        adminUser.GenerateEmailVerificationToken();
        adminUser.VerifyEmail(adminUser.EmailVerificationToken!);
        
        await _context.Users.AddAsync(adminUser);
        _logger.LogInformation("Added admin user.");
    }

    private async Task AssignPermissionsToRolesAsync()
    {
        // Check if role permissions already exist
        if (await _context.RolePermissions.AnyAsync())
        {
            _logger.LogInformation("Role permissions already exist. Skipping role permission assignment.");
            return;
        }

        var roles = await _context.Roles.ToListAsync();
        var permissions = await _context.Permissions.ToListAsync();

        var adminRole = roles.First(r => r.Name == "Administrator");
        var userManagerRole = roles.First(r => r.Name == "User Manager");
        var roleManagerRole = roles.First(r => r.Name == "Role Manager");
        var userRole = roles.First(r => r.Name == "User");
        var guestRole = roles.First(r => r.Name == "Guest");

        // Administrator gets all permissions
        foreach (var permission in permissions)
        {
            adminRole.AddPermission(permission);
        }

        // User Manager gets user management permissions
        var userManagementPermissions = permissions.Where(p => p.Category == "User Management").ToList();
        userManagementPermissions.Add(permissions.First(p => p.Name == "api.access"));
        foreach (var permission in userManagementPermissions)
        {
            userManagerRole.AddPermission(permission);
        }

        // Role Manager gets role and permission management permissions
        var roleManagementPermissions = permissions.Where(p => 
            p.Category == "Role Management" || p.Category == "Permission Management").ToList();
        roleManagementPermissions.Add(permissions.First(p => p.Name == "api.access"));
        foreach (var permission in roleManagementPermissions)
        {
            roleManagerRole.AddPermission(permission);
        }

        // User gets basic permissions
        var basicPermissions = new[]
        {
            permissions.First(p => p.Name == "users.read"),
            permissions.First(p => p.Name == "api.access")
        };
        foreach (var permission in basicPermissions)
        {
            userRole.AddPermission(permission);
        }

        // Guest gets read-only permissions
        var readOnlyPermissions = new[]
        {
            permissions.First(p => p.Name == "users.read"),
            permissions.First(p => p.Name == "roles.read"),
            permissions.First(p => p.Name == "permissions.read")
        };
        foreach (var permission in readOnlyPermissions)
        {
            guestRole.AddPermission(permission);
        }

        _logger.LogInformation("Assigned permissions to roles.");
    }

    private async Task AssignRolesToAdminAsync()
    {
        // Check if user roles already exist
        if (await _context.UserRoles.AnyAsync())
        {
            _logger.LogInformation("User roles already exist. Skipping user role assignment.");
            return;
        }

        var adminEmail = Email.Create("admin@cleanarch.com");
        var adminUser = await _context.Users.FirstAsync(u => u.Email.Value == adminEmail.Value);
        var adminRole = await _context.Roles.FirstAsync(r => r.Name == "Administrator");

        adminUser.AddRole(adminRole);
        
        _logger.LogInformation("Assigned Administrator role to admin user.");
    }
}