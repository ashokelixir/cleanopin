namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Service interface for caching permission-related data
/// </summary>
public interface IPermissionCacheService
{
    /// <summary>
    /// Gets cached user permissions
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cached user permissions or null if not cached</returns>
    Task<IEnumerable<string>?> GetUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets user permissions in cache
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="permissions">The permissions to cache</param>
    /// <param name="expiry">Optional cache expiry time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SetUserPermissionsAsync(Guid userId, IEnumerable<string> permissions, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cached role permissions
    /// </summary>
    /// <param name="roleId">The role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cached role permissions or null if not cached</returns>
    Task<IEnumerable<string>?> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets role permissions in cache
    /// </summary>
    /// <param name="roleId">The role ID</param>
    /// <param name="permissions">The permissions to cache</param>
    /// <param name="expiry">Optional cache expiry time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SetRolePermissionsAsync(Guid roleId, IEnumerable<string> permissions, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cached permission evaluation result
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="permission">The permission to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cached evaluation result or null if not cached</returns>
    Task<bool?> GetPermissionEvaluationAsync(Guid userId, string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets permission evaluation result in cache
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="permission">The permission that was evaluated</param>
    /// <param name="hasPermission">The evaluation result</param>
    /// <param name="expiry">Optional cache expiry time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SetPermissionEvaluationAsync(Guid userId, string permission, bool hasPermission, TimeSpan? expiry = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all cached permissions for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task InvalidateUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all cached permissions for a role
    /// </summary>
    /// <param name="roleId">The role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task InvalidateRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cached permissions for all users with a specific role
    /// </summary>
    /// <param name="roleId">The role ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task InvalidateUsersWithRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates cached permission evaluation for a specific user and permission
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="permission">The permission</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task InvalidatePermissionEvaluationAsync(Guid userId, string permission, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all cached permission evaluations for a user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task InvalidateUserPermissionEvaluationsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Warms up the cache with frequently accessed permissions
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task WarmUpUserPermissionsAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets cache statistics for monitoring
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cache statistics</returns>
    Task<PermissionCacheStatistics> GetCacheStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all permission-related cache entries
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task ClearAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Cache statistics for permission caching
/// </summary>
public class PermissionCacheStatistics
{
    /// <summary>
    /// Total number of cache hits
    /// </summary>
    public long HitCount { get; set; }

    /// <summary>
    /// Total number of cache misses
    /// </summary>
    public long MissCount { get; set; }

    /// <summary>
    /// Cache hit ratio (0.0 to 1.0)
    /// </summary>
    public double HitRatio => HitCount + MissCount > 0 ? (double)HitCount / (HitCount + MissCount) : 0.0;

    /// <summary>
    /// Number of cached user permission entries
    /// </summary>
    public int UserPermissionEntries { get; set; }

    /// <summary>
    /// Number of cached role permission entries
    /// </summary>
    public int RolePermissionEntries { get; set; }

    /// <summary>
    /// Number of cached permission evaluation entries
    /// </summary>
    public int PermissionEvaluationEntries { get; set; }

    /// <summary>
    /// Total cache memory usage in bytes
    /// </summary>
    public long MemoryUsageBytes { get; set; }
}