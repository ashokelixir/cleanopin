using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.ValueObjects;
using CleanArchTemplate.Domain.Enums;
using CleanArchTemplate.Infrastructure.Data.Contexts;
using CleanArchTemplate.Shared.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace CleanArchTemplate.Infrastructure.Data.Seed;

/// <summary>
/// Database seeder for initial data with comprehensive permission seeding
/// </summary>
public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;
    private readonly IConfiguration _configuration;

    public DatabaseSeeder(ApplicationDbContext context, ILogger<DatabaseSeeder> logger, IConfiguration configuration)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
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

            // Seed environment-specific permissions
            await SeedEnvironmentSpecificPermissionsAsync();
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
        _logger.LogInformation("Seeding permissions...");

        var existingPermissions = await _context.Permissions
            .Select(p => new { p.Resource, p.Action })
            .ToListAsync();

        var permissionsToAdd = GetDefaultPermissions()
            .Where(p => !existingPermissions.Any(ep => ep.Resource == p.Resource && ep.Action == p.Action))
            .ToList();

        if (permissionsToAdd.Any())
        {
            await _context.Permissions.AddRangeAsync(permissionsToAdd);
            _logger.LogInformation($"Added {permissionsToAdd.Count} new permissions.");
        }
        else
        {
            _logger.LogInformation("All default permissions already exist.");
        }
    }

    private List<Permission> GetDefaultPermissions()
    {
        return new List<Permission>
        {
            // User Management Permissions
            new Permission("Users", "Create", "Create new users", "User Management"),
            new Permission("Users", "Read", "View user information", "User Management"),
            new Permission("Users", "Update", "Update user information", "User Management"),
            new Permission("Users", "Delete", "Delete users", "User Management"),
            new Permission("Users", "ManageRoles", "Manage user roles", "User Management"),
            new Permission("Users", "ManageSessions", "Manage user sessions", "User Management"),

            // Role Management Permissions
            new Permission("Roles", "Create", "Create new roles", "Role Management"),
            new Permission("Roles", "Read", "View role information", "Role Management"),
            new Permission("Roles", "Update", "Update role information", "Role Management"),
            new Permission("Roles", "Delete", "Delete roles", "Role Management"),
            new Permission("Roles", "Assign", "Assign roles to users", "Role Management"),
            new Permission("Roles", "Remove", "Remove roles from users", "Role Management"),

            // Permission Management Permissions
            new Permission("Permissions", "Create", "Create new permissions", "Permission Management"),
            new Permission("Permissions", "Read", "View permission information", "Permission Management"),
            new Permission("Permissions", "Update", "Update permission information", "Permission Management"),
            new Permission("Permissions", "Delete", "Delete permissions", "Permission Management"),

            // User Permission Management
            new Permission("UserPermissions", "Create", "Create user permission overrides", "User Permission Management"),
            new Permission("UserPermissions", "Read", "View user permission overrides", "User Permission Management"),
            new Permission("UserPermissions", "Update", "Update user permission overrides", "User Permission Management"),
            new Permission("UserPermissions", "Delete", "Delete user permission overrides", "User Permission Management"),

            // Permission Matrix Management
            new Permission("PermissionMatrix", "Read", "View permission matrix", "Permission Matrix Management"),
            new Permission("PermissionMatrix", "Update", "Update permission matrix", "Permission Matrix Management"),
            new Permission("PermissionMatrix", "Export", "Export permission matrix", "Permission Matrix Management"),

            // Permission Audit Management
            new Permission("PermissionAudit", "Read", "View permission audit logs", "Permission Audit Management"),
            new Permission("PermissionAudit", "Export", "Export permission audit logs", "Permission Audit Management"),

            // Secrets Management Permissions
            new Permission("Secrets", "Read", "Read secrets information", "Secrets Management"),
            new Permission("Secrets", "Test", "Test secret retrieval", "Secrets Management"),
            new Permission("Secrets", "Manage", "Manage secrets", "Secrets Management"),
            new Permission("Secrets", "ManageCache", "Manage secrets cache", "Secrets Management"),

            // System Administration Permissions
            new Permission("System", "Admin", "Full system administration access", "System Administration"),
            new Permission("System", "Health", "View system health information", "System Administration"),
            new Permission("System", "Logs", "View system logs", "System Administration"),
            new Permission("System", "Settings", "Manage system settings", "System Administration"),

            // API Access Permissions
            new Permission("API", "Access", "Access API endpoints", "API Access"),
            new Permission("API", "Admin", "Administrative API access", "API Access")
        };
    }

    private async Task SeedRolesAsync()
    {
        _logger.LogInformation("Seeding roles...");

        var existingRoleNames = await _context.Roles
            .Select(r => r.Name)
            .ToListAsync();

        var rolesToAdd = GetDefaultRoles()
            .Where(r => !existingRoleNames.Contains(r.Name))
            .ToList();

        if (rolesToAdd.Any())
        {
            await _context.Roles.AddRangeAsync(rolesToAdd);
            _logger.LogInformation($"Added {rolesToAdd.Count} new roles.");
        }
        else
        {
            _logger.LogInformation("All default roles already exist.");
        }
    }

    private List<Role> GetDefaultRoles()
    {
        return new List<Role>
        {
            new Role("Administrator", "Full system administrator with all permissions"),
            new Role("User Manager", "Can manage users and their roles"),
            new Role("Role Manager", "Can manage roles and permissions"),
            new Role("Permission Manager", "Can manage permissions and permission matrix"),
            new Role("Audit Manager", "Can view and export audit logs"),
            new Role("Secrets Manager", "Can manage secrets and secret cache"),
            new Role("User", "Standard user with basic permissions"),
            new Role("Guest", "Guest user with read-only access")
        };
    }

    private async Task SeedAdminUserAsync()
    {
        var adminEmail = GetAdminEmail();
        
        if (await _context.Users.AnyAsync(u => u.Email.Value == adminEmail.Value))
        {
            _logger.LogInformation("Admin user already exists. Skipping admin user seeding.");
            return;
        }

        _logger.LogInformation("Seeding admin user...");

        var adminPassword = GetAdminPassword();
        var passwordHash = PasswordHelper.HashPassword(adminPassword);
        var adminUser = User.Create(adminEmail, "System", "Administrator", passwordHash);
        
        // Verify email immediately for admin user
        adminUser.GenerateEmailVerificationToken();
        adminUser.VerifyEmail(adminUser.EmailVerificationToken!);
        
        await _context.Users.AddAsync(adminUser);
        _logger.LogInformation("Added admin user.");
    }

    private Email GetAdminEmail()
    {
        var adminEmail = _configuration["DefaultAdmin:Email"] ?? "admin@cleanarch.com";
        return Email.Create(adminEmail);
    }

    private string GetAdminPassword()
    {
        return _configuration["DefaultAdmin:Password"] ?? "Admin123!";
    }

    private async Task AssignPermissionsToRolesAsync()
    {
        _logger.LogInformation("Assigning permissions to roles...");

        var roles = await _context.Roles.Include(r => r.RolePermissions).ToListAsync();
        var permissions = await _context.Permissions.ToListAsync();

        var rolePermissionAssignments = GetDefaultRolePermissionAssignments();

        foreach (var assignment in rolePermissionAssignments)
        {
            var role = roles.FirstOrDefault(r => r.Name == assignment.RoleName);
            if (role == null)
            {
                _logger.LogWarning($"Role '{assignment.RoleName}' not found. Skipping permission assignments.");
                continue;
            }

            var assignedCount = 0;
            foreach (var permissionKey in assignment.Permissions)
            {
                var permission = permissions.FirstOrDefault(p => 
                    $"{p.Resource}.{p.Action}" == permissionKey);
                
                if (permission == null)
                {
                    _logger.LogWarning($"Permission '{permissionKey}' not found for role '{assignment.RoleName}'.");
                    continue;
                }

                if (!role.RolePermissions.Any(rp => rp.PermissionId == permission.Id))
                {
                    role.AddPermission(permission);
                    assignedCount++;
                }
            }

            if (assignedCount > 0)
            {
                _logger.LogInformation($"Assigned {assignedCount} permissions to role '{assignment.RoleName}'.");
            }
        }
    }

    private List<RolePermissionAssignment> GetDefaultRolePermissionAssignments()
    {
        return new List<RolePermissionAssignment>
        {
            new RolePermissionAssignment
            {
                RoleName = "Administrator",
                Permissions = new[]
                {
                    // All permissions - Administrator has full access
                    "Users.Create", "Users.Read", "Users.Update", "Users.Delete", "Users.ManageRoles", "Users.ManageSessions",
                    "Roles.Create", "Roles.Read", "Roles.Update", "Roles.Delete", "Roles.Assign", "Roles.Remove",
                    "Permissions.Create", "Permissions.Read", "Permissions.Update", "Permissions.Delete",
                    "UserPermissions.Create", "UserPermissions.Read", "UserPermissions.Update", "UserPermissions.Delete",
                    "PermissionMatrix.Read", "PermissionMatrix.Update", "PermissionMatrix.Export",
                    "PermissionAudit.Read", "PermissionAudit.Export",
                    "Secrets.Read", "Secrets.Test", "Secrets.Manage", "Secrets.ManageCache",
                    "System.Admin", "System.Health", "System.Logs", "System.Settings",
                    "API.Access", "API.Admin"
                }
            },
            new RolePermissionAssignment
            {
                RoleName = "User Manager",
                Permissions = new[]
                {
                    "Users.Create", "Users.Read", "Users.Update", "Users.Delete", "Users.ManageRoles", "Users.ManageSessions",
                    "Roles.Read", "Roles.Assign", "Roles.Remove",
                    "UserPermissions.Create", "UserPermissions.Read", "UserPermissions.Update", "UserPermissions.Delete",
                    "PermissionMatrix.Read",
                    "PermissionAudit.Read",
                    "API.Access"
                }
            },
            new RolePermissionAssignment
            {
                RoleName = "Role Manager",
                Permissions = new[]
                {
                    "Roles.Create", "Roles.Read", "Roles.Update", "Roles.Delete", "Roles.Assign", "Roles.Remove",
                    "Permissions.Read",
                    "PermissionMatrix.Read", "PermissionMatrix.Update",
                    "PermissionAudit.Read",
                    "API.Access"
                }
            },
            new RolePermissionAssignment
            {
                RoleName = "Permission Manager",
                Permissions = new[]
                {
                    "Permissions.Create", "Permissions.Read", "Permissions.Update", "Permissions.Delete",
                    "UserPermissions.Create", "UserPermissions.Read", "UserPermissions.Update", "UserPermissions.Delete",
                    "PermissionMatrix.Read", "PermissionMatrix.Update", "PermissionMatrix.Export",
                    "PermissionAudit.Read",
                    "API.Access"
                }
            },
            new RolePermissionAssignment
            {
                RoleName = "Audit Manager",
                Permissions = new[]
                {
                    "PermissionAudit.Read", "PermissionAudit.Export",
                    "PermissionMatrix.Read", "PermissionMatrix.Export",
                    "Users.Read", "Roles.Read", "Permissions.Read",
                    "API.Access"
                }
            },
            new RolePermissionAssignment
            {
                RoleName = "Secrets Manager",
                Permissions = new[]
                {
                    "Secrets.Read", "Secrets.Test", "Secrets.Manage", "Secrets.ManageCache",
                    "System.Health",
                    "API.Access"
                }
            },
            new RolePermissionAssignment
            {
                RoleName = "User",
                Permissions = new[]
                {
                    "Users.Read",
                    "Roles.Read",
                    "API.Access"
                }
            },
            new RolePermissionAssignment
            {
                RoleName = "Guest",
                Permissions = new[]
                {
                    "Users.Read",
                    "Roles.Read",
                    "Permissions.Read"
                }
            }
        };
    }

    private class RolePermissionAssignment
    {
        public string RoleName { get; set; } = string.Empty;
        public string[] Permissions { get; set; } = Array.Empty<string>();
    }

    private async Task AssignRolesToAdminAsync()
    {
        var adminEmail = GetAdminEmail();
        var adminUser = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email.Value == adminEmail.Value);

        if (adminUser == null)
        {
            _logger.LogWarning("Admin user not found. Skipping role assignment.");
            return;
        }

        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator");
        if (adminRole == null)
        {
            _logger.LogWarning("Administrator role not found. Skipping role assignment.");
            return;
        }

        if (!adminUser.UserRoles.Any(ur => ur.RoleId == adminRole.Id))
        {
            adminUser.AddRole(adminRole);
            _logger.LogInformation("Assigned Administrator role to admin user.");
        }
        else
        {
            _logger.LogInformation("Admin user already has Administrator role.");
        }
    }

    /// <summary>
    /// Seeds environment-specific permissions based on configuration
    /// </summary>
    public async Task SeedEnvironmentSpecificPermissionsAsync()
    {
        var environment = _configuration["Environment"] ?? "Development";
        _logger.LogInformation($"Seeding environment-specific permissions for: {environment}");

        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            await SeedDevelopmentPermissionsAsync();
        }
        else if (environment.Equals("Staging", StringComparison.OrdinalIgnoreCase))
        {
            await SeedStagingPermissionsAsync();
        }
        else if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase))
        {
            await SeedProductionPermissionsAsync();
        }
    }

    private async Task SeedDevelopmentPermissionsAsync()
    {
        var devPermissions = new[]
        {
            new Permission("Development", "Debug", "Access debug endpoints", "Development"),
            new Permission("Development", "TestData", "Create test data", "Development"),
            new Permission("Development", "Reset", "Reset development data", "Development")
        };

        var existingDevPermissions = await _context.Permissions
            .Where(p => p.Category == "Development")
            .Select(p => new { p.Resource, p.Action })
            .ToListAsync();

        var newDevPermissions = devPermissions
            .Where(p => !existingDevPermissions.Any(ep => ep.Resource == p.Resource && ep.Action == p.Action))
            .ToList();

        if (newDevPermissions.Any())
        {
            await _context.Permissions.AddRangeAsync(newDevPermissions);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Added {newDevPermissions.Count} development permissions.");
        }
    }

    private async Task SeedStagingPermissionsAsync()
    {
        var stagingPermissions = new[]
        {
            new Permission("Staging", "Deploy", "Deploy to staging environment", "Staging"),
            new Permission("Staging", "TestData", "Manage staging test data", "Staging")
        };

        var existingStagingPermissions = await _context.Permissions
            .Where(p => p.Category == "Staging")
            .Select(p => new { p.Resource, p.Action })
            .ToListAsync();

        var newStagingPermissions = stagingPermissions
            .Where(p => !existingStagingPermissions.Any(ep => ep.Resource == p.Resource && ep.Action == p.Action))
            .ToList();

        if (newStagingPermissions.Any())
        {
            await _context.Permissions.AddRangeAsync(newStagingPermissions);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Added {newStagingPermissions.Count} staging permissions.");
        }
    }

    private async Task SeedProductionPermissionsAsync()
    {
        // Production typically doesn't need additional permissions beyond the defaults
        _logger.LogInformation("Production environment - using default permissions only.");
    }

    /// <summary>
    /// Cleans up orphaned permissions that are no longer used
    /// </summary>
    public async Task CleanupOrphanedPermissionsAsync()
    {
        _logger.LogInformation("Cleaning up orphaned permissions...");

        var orphanedPermissions = await _context.Permissions
            .Where(p => !_context.RolePermissions.Any(rp => rp.PermissionId == p.Id) &&
                       !_context.UserPermissions.Any(up => up.PermissionId == p.Id))
            .ToListAsync();

        if (orphanedPermissions.Any())
        {
            _context.Permissions.RemoveRange(orphanedPermissions);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Removed {orphanedPermissions.Count} orphaned permissions.");
        }
        else
        {
            _logger.LogInformation("No orphaned permissions found.");
        }
    }
}