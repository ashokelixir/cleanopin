using System.Security.Claims;
using CleanArchTemplate.Application.Common.Models;

namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Service interface for permission-based authorization
/// </summary>
public interface IPermissionAuthorizationService
{
    /// <summary>
    /// Authorizes a user for a specific resource and action
    /// </summary>
    /// <param name="user">The claims principal representing the user</param>
    /// <param name="resource">The resource name</param>
    /// <param name="action">The action name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authorization result</returns>
    Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, string resource, string action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authorizes a user for a specific permission
    /// </summary>
    /// <param name="user">The claims principal representing the user</param>
    /// <param name="permission">The permission name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authorization result</returns>
    Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authorizes a user by user ID for a specific resource and action
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="resource">The resource name</param>
    /// <param name="action">The action name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authorization result</returns>
    Task<AuthorizationResult> AuthorizeAsync(Guid userId, string resource, string action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authorizes a user by user ID for a specific permission
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="permission">The permission name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authorization result</returns>
    Task<AuthorizationResult> AuthorizeAsync(Guid userId, string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has any of the specified permissions
    /// </summary>
    /// <param name="user">The claims principal representing the user</param>
    /// <param name="permissions">The permissions to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authorization result indicating if user has any of the permissions</returns>
    Task<AuthorizationResult> AuthorizeAnyAsync(ClaimsPrincipal user, IEnumerable<string> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has all of the specified permissions
    /// </summary>
    /// <param name="user">The claims principal representing the user</param>
    /// <param name="permissions">The permissions to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authorization result indicating if user has all permissions</returns>
    Task<AuthorizationResult> AuthorizeAllAsync(ClaimsPrincipal user, IEnumerable<string> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs bulk authorization check for multiple permissions
    /// </summary>
    /// <param name="user">The claims principal representing the user</param>
    /// <param name="permissions">The permissions to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of permission to authorization result</returns>
    Task<Dictionary<string, AuthorizationResult>> BulkAuthorizeAsync(ClaimsPrincipal user, IEnumerable<string> permissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions for a user
    /// </summary>
    /// <param name="user">The claims principal representing the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user permissions</returns>
    Task<IEnumerable<string>> GetUserPermissionsAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all permissions for a user by user ID
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user permissions</returns>
    Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets effective permissions for a user (role permissions + user overrides)
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Effective user permissions with details</returns>
    Task<EffectivePermissionsDto> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has permission to access a specific resource instance
    /// </summary>
    /// <param name="user">The claims principal representing the user</param>
    /// <param name="resource">The resource name</param>
    /// <param name="action">The action name</param>
    /// <param name="resourceId">The specific resource instance ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authorization result</returns>
    Task<AuthorizationResult> AuthorizeResourceAsync(ClaimsPrincipal user, string resource, string action, Guid resourceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets authorization context for a user including roles and permissions
    /// </summary>
    /// <param name="user">The claims principal representing the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authorization context</returns>
    Task<AuthorizationContextDto> GetAuthorizationContextAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if a permission exists and is active
    /// </summary>
    /// <param name="permission">The permission name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if permission exists and is active</returns>
    Task<bool> IsValidPermissionAsync(string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets authorization requirements for a specific resource and action
    /// </summary>
    /// <param name="resource">The resource name</param>
    /// <param name="action">The action name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Authorization requirements</returns>
    Task<AuthorizationRequirementsDto> GetAuthorizationRequirementsAsync(string resource, string action, CancellationToken cancellationToken = default);
}