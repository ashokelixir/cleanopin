using CleanArchTemplate.Domain.Services;

namespace CleanArchTemplate.Application.Services;

/// <summary>
/// Application service interface for permission evaluation that handles repository interactions
/// </summary>
public interface IPermissionApplicationService
{
    /// <summary>
    /// Checks if a user has a specific permission by resource and action
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="resource">The resource name</param>
    /// <param name="action">The action name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user has the permission, false otherwise</returns>
    Task<bool> HasPermissionAsync(Guid userId, string resource, string action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has a specific permission by permission name
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="permissionName">The permission name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user has the permission, false otherwise</returns>
    Task<bool> HasPermissionAsync(Guid userId, string permissionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all effective permissions for a user (from roles and user-specific overrides)
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of permission names the user has</returns>
    Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has any of the specified permissions
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="permissions">List of permission names to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user has at least one of the permissions</returns>
    Task<bool> HasAnyPermissionAsync(Guid userId, IEnumerable<string> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a permission for a user and returns detailed information about the evaluation
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="permissionName">The permission name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Detailed permission evaluation result</returns>
    Task<PermissionEvaluationResult> EvaluatePermissionAsync(Guid userId, string permissionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has a permission including hierarchical permissions (inheritance)
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="permissionName">The permission name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user has the permission directly or through inheritance</returns>
    Task<bool> HasHierarchicalPermissionAsync(Guid userId, string permissionName, CancellationToken cancellationToken = default);
}