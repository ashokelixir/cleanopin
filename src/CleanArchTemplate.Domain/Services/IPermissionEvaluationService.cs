using CleanArchTemplate.Domain.Entities;

namespace CleanArchTemplate.Domain.Services;

/// <summary>
/// Domain service interface for evaluating user permissions with loaded entities
/// This service works with already-loaded aggregates to maintain proper DDD boundaries
/// </summary>
public interface IPermissionEvaluationService
{
    /// <summary>
    /// Checks if a user has a specific permission by resource and action
    /// </summary>
    /// <param name="user">The user entity with loaded roles and permissions</param>
    /// <param name="resource">The resource name</param>
    /// <param name="action">The action name</param>
    /// <param name="availablePermissions">All available permissions in the system</param>
    /// <param name="userRoles">The user's roles with their permissions</param>
    /// <returns>True if the user has the permission, false otherwise</returns>
    bool HasPermission(User user, string resource, string action, IEnumerable<Permission> availablePermissions, IEnumerable<Role> userRoles);

    /// <summary>
    /// Checks if a user has a specific permission by permission name
    /// </summary>
    /// <param name="user">The user entity with loaded roles and permissions</param>
    /// <param name="permissionName">The permission name</param>
    /// <param name="availablePermissions">All available permissions in the system</param>
    /// <param name="userRoles">The user's roles with their permissions</param>
    /// <returns>True if the user has the permission, false otherwise</returns>
    bool HasPermission(User user, string permissionName, IEnumerable<Permission> availablePermissions, IEnumerable<Role> userRoles);

    /// <summary>
    /// Gets all effective permissions for a user
    /// </summary>
    /// <param name="user">The user entity with loaded roles and permissions</param>
    /// <param name="availablePermissions">All available permissions in the system</param>
    /// <param name="userRoles">The user's roles with their permissions</param>
    /// <returns>List of permission names the user has</returns>
    IEnumerable<string> GetUserPermissions(User user, IEnumerable<Permission> availablePermissions, IEnumerable<Role> userRoles);

    /// <summary>
    /// Checks if a user has any of the specified permissions
    /// </summary>
    /// <param name="user">The user entity with loaded roles and permissions</param>
    /// <param name="permissions">List of permission names to check</param>
    /// <param name="availablePermissions">All available permissions in the system</param>
    /// <param name="userRoles">The user's roles with their permissions</param>
    /// <returns>True if the user has at least one of the permissions</returns>
    bool HasAnyPermission(User user, IEnumerable<string> permissions, IEnumerable<Permission> availablePermissions, IEnumerable<Role> userRoles);

    /// <summary>
    /// Evaluates a permission for a user and returns detailed information about the evaluation
    /// </summary>
    /// <param name="user">The user entity with loaded roles and permissions</param>
    /// <param name="permissionName">The permission name</param>
    /// <param name="availablePermissions">All available permissions in the system</param>
    /// <param name="userRoles">The user's roles with their permissions</param>
    /// <returns>Detailed permission evaluation result</returns>
    PermissionEvaluationResult EvaluatePermission(User user, string permissionName, IEnumerable<Permission> availablePermissions, IEnumerable<Role> userRoles);

    /// <summary>
    /// Checks if a user has a permission including hierarchical permissions (inheritance)
    /// </summary>
    /// <param name="user">The user entity with loaded roles and permissions</param>
    /// <param name="permissionName">The permission name</param>
    /// <param name="availablePermissions">All available permissions in the system</param>
    /// <param name="userRoles">The user's roles with their permissions</param>
    /// <returns>True if the user has the permission directly or through inheritance</returns>
    bool HasHierarchicalPermission(User user, string permissionName, IEnumerable<Permission> availablePermissions, IEnumerable<Role> userRoles);
}