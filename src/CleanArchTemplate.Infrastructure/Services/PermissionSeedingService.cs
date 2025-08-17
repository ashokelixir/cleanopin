using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Infrastructure.Services;

/// <summary>
/// Service for seeding and managing default permissions
/// </summary>
public class PermissionSeedingService : IPermissionSeedingService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserPermissionRepository _userPermissionRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PermissionSeedingService> _logger;

    public PermissionSeedingService(
        IPermissionRepository permissionRepository,
        IRoleRepository roleRepository,
        IUserPermissionRepository userPermissionRepository,
        IConfiguration configuration,
        ILogger<PermissionSeedingService> logger)
    {
        _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _userPermissionRepository = userPermissionRepository ?? throw new ArgumentNullException(nameof(userPermissionRepository));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedDefaultPermissionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding default permissions...");

        var defaultPermissions = await GetDefaultPermissionsAsync(cancellationToken);
        var existingPermissions = await _permissionRepository.GetAllAsync(cancellationToken);
        
        var existingKeys = existingPermissions.Select(p => $"{p.Resource}.{p.Action}").ToHashSet();
        var permissionsToAdd = defaultPermissions
            .Where(p => !existingKeys.Contains($"{p.Resource}.{p.Action}"))
            .ToList();

        if (permissionsToAdd.Any())
        {
            foreach (var permission in permissionsToAdd)
            {
                await _permissionRepository.AddAsync(permission, cancellationToken);
            }
            
            _logger.LogInformation($"Added {permissionsToAdd.Count} new default permissions.");
        }
        else
        {
            _logger.LogInformation("All default permissions already exist.");
        }
    }

    public async Task SeedEnvironmentPermissionsAsync(string environment, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Seeding environment-specific permissions for: {environment}");

        var environmentPermissions = GetEnvironmentSpecificPermissions(environment);
        if (!environmentPermissions.Any())
        {
            _logger.LogInformation($"No environment-specific permissions defined for: {environment}");
            return;
        }

        var existingPermissions = await _permissionRepository.GetAllAsync(cancellationToken);
        var existingKeys = existingPermissions.Select(p => $"{p.Resource}.{p.Action}").ToHashSet();
        
        var permissionsToAdd = environmentPermissions
            .Where(p => !existingKeys.Contains($"{p.Resource}.{p.Action}"))
            .ToList();

        if (permissionsToAdd.Any())
        {
            foreach (var permission in permissionsToAdd)
            {
                await _permissionRepository.AddAsync(permission, cancellationToken);
            }
            
            _logger.LogInformation($"Added {permissionsToAdd.Count} environment-specific permissions for {environment}.");
        }
    }

    public async Task SeedDefaultRolePermissionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding default role-permission assignments...");

        var roles = await _roleRepository.GetAllAsync(cancellationToken);
        var permissions = await _permissionRepository.GetAllAsync(cancellationToken);
        var permissionLookup = permissions.ToDictionary(p => $"{p.Resource}.{p.Action}", p => p);

        var rolePermissionAssignments = GetDefaultRolePermissionAssignments();
        var assignmentCount = 0;

        foreach (var assignment in rolePermissionAssignments)
        {
            var role = roles.FirstOrDefault(r => r.Name == assignment.RoleName);
            if (role == null)
            {
                _logger.LogWarning($"Role '{assignment.RoleName}' not found. Skipping permission assignments.");
                continue;
            }

            var roleWithPermissions = await _roleRepository.GetByIdWithPermissionsAsync(role.Id, cancellationToken);
            if (roleWithPermissions == null) continue;

            var existingPermissionIds = roleWithPermissions.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();

            foreach (var permissionKey in assignment.Permissions)
            {
                if (permissionLookup.TryGetValue(permissionKey, out var permission))
                {
                    if (!existingPermissionIds.Contains(permission.Id))
                    {
                        roleWithPermissions.AddPermission(permission);
                        assignmentCount++;
                    }
                }
                else
                {
                    _logger.LogWarning($"Permission '{permissionKey}' not found for role '{assignment.RoleName}'.");
                }
            }

            await _roleRepository.UpdateAsync(roleWithPermissions, cancellationToken);
        }

        _logger.LogInformation($"Assigned {assignmentCount} role-permission relationships.");
    }

    public async Task<IEnumerable<Permission>> GetDefaultPermissionsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(GetDefaultPermissions());
    }

    public async Task<bool> ValidatePermissionIntegrityAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating permission integrity...");

        var defaultPermissions = await GetDefaultPermissionsAsync(cancellationToken);
        var existingPermissions = await _permissionRepository.GetAllAsync(cancellationToken);
        
        var defaultKeys = defaultPermissions.Select(p => $"{p.Resource}.{p.Action}").ToHashSet();
        var existingKeys = existingPermissions.Select(p => $"{p.Resource}.{p.Action}").ToHashSet();

        var missingPermissions = defaultKeys.Except(existingKeys).ToList();
        
        if (missingPermissions.Any())
        {
            _logger.LogWarning($"Missing {missingPermissions.Count} required permissions: {string.Join(", ", missingPermissions)}");
            return false;
        }

        _logger.LogInformation("Permission integrity validation passed.");
        return true;
    }

    public async Task CleanupOrphanedPermissionsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cleaning up orphaned permissions...");

        var allPermissions = await _permissionRepository.GetAllAsync(cancellationToken);
        var orphanedPermissions = new List<Permission>();

        foreach (var permission in allPermissions)
        {
            var hasRoleAssignments = await _roleRepository.HasPermissionAsync(permission.Id, cancellationToken);
            var hasUserAssignments = await _userPermissionRepository.HasPermissionAsync(permission.Id, cancellationToken);

            if (!hasRoleAssignments && !hasUserAssignments)
            {
                orphanedPermissions.Add(permission);
            }
        }

        if (orphanedPermissions.Any())
        {
            foreach (var permission in orphanedPermissions)
            {
                await _permissionRepository.DeleteAsync(permission.Id, cancellationToken);
            }
            
            _logger.LogInformation($"Removed {orphanedPermissions.Count} orphaned permissions.");
        }
        else
        {
            _logger.LogInformation("No orphaned permissions found.");
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

    private List<Permission> GetEnvironmentSpecificPermissions(string environment)
    {
        return environment.ToLowerInvariant() switch
        {
            "development" => new List<Permission>
            {
                new Permission("Development", "Debug", "Access debug endpoints", "Development"),
                new Permission("Development", "TestData", "Create test data", "Development"),
                new Permission("Development", "Reset", "Reset development data", "Development")
            },
            "staging" => new List<Permission>
            {
                new Permission("Staging", "Deploy", "Deploy to staging environment", "Staging"),
                new Permission("Staging", "TestData", "Manage staging test data", "Staging")
            },
            "production" => new List<Permission>(), // Production uses default permissions only
            _ => new List<Permission>()
        };
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
}