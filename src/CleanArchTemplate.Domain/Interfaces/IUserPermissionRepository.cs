using CleanArchTemplate.Domain.Entities;
using CleanArchTemplate.Domain.Enums;

namespace CleanArchTemplate.Domain.Interfaces;

/// <summary>
/// Domain repository interface for UserPermission entity extending base repository
/// </summary>
public interface IUserPermissionRepository : IRepository<UserPermission>
{
    /// <summary>
    /// Gets all user permission overrides for a specific user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User's permission overrides</returns>
    Task<IEnumerable<UserPermission>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific user permission override
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="permissionId">The permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The user permission override if found, null otherwise</returns>
    Task<UserPermission?> GetByUserAndPermissionAsync(Guid userId, Guid permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active user permission overrides for a specific user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Active user permission overrides</returns>
    Task<IEnumerable<UserPermission>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user permission overrides with their associated permissions
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User permissions with permission details</returns>
    Task<IEnumerable<UserPermission>> GetUserPermissionsWithDetailsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user permission overrides that are expiring before the specified date
    /// </summary>
    /// <param name="before">The date to check expiration against</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Expiring user permissions</returns>
    Task<IEnumerable<UserPermission>> GetExpiringPermissionsAsync(DateTime before, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user permissions by permission state (Grant/Deny)
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="state">The permission state to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User permissions with the specified state</returns>
    Task<IEnumerable<UserPermission>> GetByUserIdAndStateAsync(Guid userId, PermissionState state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users who have a specific permission override
    /// </summary>
    /// <param name="permissionId">The permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User permissions for the specified permission</returns>
    Task<IEnumerable<UserPermission>> GetByPermissionIdAsync(Guid permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk adds user permissions
    /// </summary>
    /// <param name="userPermissions">The user permissions to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task BulkAddAsync(IEnumerable<UserPermission> userPermissions, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk removes user permissions for a specific user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="permissionIds">The permission IDs to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task BulkRemoveByUserAndPermissionsAsync(Guid userId, IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk updates the state of multiple user permissions
    /// </summary>
    /// <param name="userPermissionIds">The user permission IDs to update</param>
    /// <param name="state">The new permission state</param>
    /// <param name="reason">Optional reason for the change</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task BulkUpdateStateAsync(IEnumerable<Guid> userPermissionIds, PermissionState state, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk sets expiration date for multiple user permissions
    /// </summary>
    /// <param name="userPermissionIds">The user permission IDs to update</param>
    /// <param name="expiresAt">The new expiration date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task BulkSetExpirationAsync(IEnumerable<Guid> userPermissionIds, DateTime? expiresAt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user permissions for multiple users
    /// </summary>
    /// <param name="userIds">The user IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User permissions for the specified users</returns>
    Task<IEnumerable<UserPermission>> GetByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user permissions for multiple permissions
    /// </summary>
    /// <param name="permissionIds">The permission IDs</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User permissions for the specified permissions</returns>
    Task<IEnumerable<UserPermission>> GetByPermissionIdsAsync(IEnumerable<Guid> permissionIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user permissions with pagination and filtering
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="userId">Optional user ID filter</param>
    /// <param name="permissionId">Optional permission ID filter</param>
    /// <param name="state">Optional permission state filter</param>
    /// <param name="includeExpired">Optional flag to include expired permissions</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated user permissions</returns>
    Task<(IEnumerable<UserPermission> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, 
        int pageSize, 
        Guid? userId = null,
        Guid? permissionId = null,
        PermissionState? state = null,
        bool? includeExpired = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts user permissions for a specific user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of user permissions</returns>
    Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts user permissions for a specific permission
    /// </summary>
    /// <param name="permissionId">The permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Count of user permissions</returns>
    Task<int> CountByPermissionIdAsync(Guid permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user permission override exists
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="permissionId">The permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the user permission exists</returns>
    Task<bool> ExistsAsync(Guid userId, Guid permissionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if any user has a specific permission override
    /// </summary>
    /// <param name="permissionId">The permission ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if any user has the permission override, false otherwise</returns>
    Task<bool> HasPermissionAsync(Guid permissionId, CancellationToken cancellationToken = default);
}