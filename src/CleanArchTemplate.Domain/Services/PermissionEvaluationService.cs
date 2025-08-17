using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Enums;

namespace CleanArchTemplate.Domain.Services;

/// <summary>
/// Domain service for evaluating user permissions by combining role permissions with user-specific overrides
/// This service works with already-loaded aggregates to maintain proper DDD boundaries
/// </summary>
public class PermissionEvaluationService : IPermissionEvaluationService
{
    public PermissionEvaluationService()
    {
    }

    /// <inheritdoc />
    public bool HasPermission(User user, string resource, string action, IEnumerable<Permission> availablePermissions, IEnumerable<Role> userRoles)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("Resource cannot be empty.", nameof(resource));

        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be empty.", nameof(action));

        var permissionName = $"{resource.Trim()}.{action.Trim()}";
        return HasPermission(user, permissionName, availablePermissions, userRoles);
    }

    /// <inheritdoc />
    public bool HasPermission(User user, string permissionName, IEnumerable<Permission> availablePermissions, IEnumerable<Role> userRoles)
    {
        var result = EvaluatePermission(user, permissionName, availablePermissions, userRoles);
        return result.HasPermission;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetUserPermissions(User user, IEnumerable<Permission> availablePermissions, IEnumerable<Role> userRoles)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (availablePermissions == null)
            throw new ArgumentNullException(nameof(availablePermissions));

        // Domain service should contain the core logic, not just delegate
        return user.GetEffectivePermissions(availablePermissions, userRoles);
    }

    /// <inheritdoc />
    public bool HasAnyPermission(User user, IEnumerable<string> permissions, IEnumerable<Permission> availablePermissions, IEnumerable<Role> userRoles)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (permissions == null)
            throw new ArgumentNullException(nameof(permissions));

        // Domain service should contain the core logic, not just delegate
        return user.HasAnyPermission(permissions, availablePermissions, userRoles);
    }

    /// <inheritdoc />
    public PermissionEvaluationResult EvaluatePermission(User user, string permissionName, IEnumerable<Permission> availablePermissions, IEnumerable<Role> userRoles)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (string.IsNullOrWhiteSpace(permissionName))
            throw new ArgumentException("Permission name cannot be empty.", nameof(permissionName));

        if (availablePermissions == null)
            throw new ArgumentNullException(nameof(availablePermissions));

        permissionName = permissionName.Trim();
        var permissions = availablePermissions.ToList();
        var roles = userRoles?.ToList() ?? new List<Role>();

        // Get the permission
        var permission = permissions.FirstOrDefault(p => p.Name == permissionName);
        if (permission == null)
        {
            return PermissionEvaluationResult.Denied($"Permission '{permissionName}' does not exist.");
        }

        if (!permission.IsActive)
        {
            return PermissionEvaluationResult.Denied($"Permission '{permissionName}' is not active.");
        }

        // Check for user-specific override first (highest priority)
        var userOverride = user.GetUserPermission(permission.Id);
        if (userOverride != null)
        {
            var reason = userOverride.State == PermissionState.Grant 
                ? $"Explicitly granted to user. Reason: {userOverride.Reason ?? "No reason provided"}"
                : $"Explicitly denied to user. Reason: {userOverride.Reason ?? "No reason provided"}";
            
            return PermissionEvaluationResult.FromUserOverride(userOverride, reason);
        }

        // Check role-based permissions
        var grantingRoles = new List<string>();

        foreach (var role in roles.Where(r => r.IsActive))
        {
            if (role.HasPermission(permission.Id))
            {
                grantingRoles.Add(role.Name);
            }
        }

        if (grantingRoles.Any())
        {
            return PermissionEvaluationResult.FromRoles(true, grantingRoles, 
                $"Granted through roles: {string.Join(", ", grantingRoles)}");
        }

        // Check hierarchical permissions (inheritance)
        var hierarchicalResult = CheckHierarchicalPermission(user, permission, permissions, roles);
        if (hierarchicalResult.HasPermission)
        {
            return hierarchicalResult;
        }

        return PermissionEvaluationResult.Denied($"User does not have permission '{permissionName}' through any source.");
    }

    /// <inheritdoc />
    public bool HasHierarchicalPermission(User user, string permissionName, IEnumerable<Permission> availablePermissions, IEnumerable<Role> userRoles)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (string.IsNullOrWhiteSpace(permissionName))
            throw new ArgumentException("Permission name cannot be empty.", nameof(permissionName));

        // First check direct permission
        if (HasPermission(user, permissionName, availablePermissions, userRoles))
            return true;

        var permissions = availablePermissions.ToList();
        var roles = userRoles?.ToList() ?? new List<Role>();
        
        // Get the permission to check its hierarchy
        var permission = permissions.FirstOrDefault(p => p.Name == permissionName.Trim());
        if (permission == null || !permission.IsActive)
            return false;

        // Check parent permissions recursively
        return CheckParentPermissions(user, permission, permissions, roles);
    }

    /// <summary>
    /// Checks hierarchical permissions by looking at parent permissions
    /// </summary>
    /// <param name="user">The user</param>
    /// <param name="permission">The permission to check hierarchy for</param>
    /// <param name="availablePermissions">All available permissions</param>
    /// <param name="userRoles">The user's roles</param>
    /// <returns>Permission evaluation result</returns>
    private PermissionEvaluationResult CheckHierarchicalPermission(User user, Permission permission, List<Permission> availablePermissions, List<Role> userRoles)
    {
        if (permission.ParentPermissionId == null)
        {
            return PermissionEvaluationResult.Denied("No parent permissions to inherit from.");
        }

        var parentPermission = availablePermissions.FirstOrDefault(p => p.Id == permission.ParentPermissionId.Value);
        if (parentPermission == null || !parentPermission.IsActive)
        {
            return PermissionEvaluationResult.Denied("Parent permission not found or inactive.");
        }

        // Check if user has the parent permission
        var parentResult = EvaluatePermission(user, parentPermission.Name, availablePermissions, userRoles);
        if (parentResult.HasPermission)
        {
            return PermissionEvaluationResult.FromInheritance(
                true, 
                parentPermission.Name, 
                parentResult.GrantingRoles,
                $"Inherited from parent permission '{parentPermission.Name}'. {parentResult.Reason}");
        }

        return PermissionEvaluationResult.Denied("No parent permissions grant access.");
    }

    /// <summary>
    /// Recursively checks parent permissions
    /// </summary>
    /// <param name="user">The user</param>
    /// <param name="permission">The permission to check parents for</param>
    /// <param name="availablePermissions">All available permissions</param>
    /// <param name="userRoles">The user's roles</param>
    /// <returns>True if any parent permission grants access</returns>
    private bool CheckParentPermissions(User user, Permission permission, List<Permission> availablePermissions, List<Role> userRoles)
    {
        if (permission.ParentPermissionId == null)
            return false;

        var parentPermission = availablePermissions.FirstOrDefault(p => p.Id == permission.ParentPermissionId.Value);
        if (parentPermission == null || !parentPermission.IsActive)
            return false;

        // Check if user has the parent permission directly
        if (HasPermission(user, parentPermission.Name, availablePermissions, userRoles))
            return true;

        // Recursively check parent's parents
        return CheckParentPermissions(user, parentPermission, availablePermissions, userRoles);
    }


}