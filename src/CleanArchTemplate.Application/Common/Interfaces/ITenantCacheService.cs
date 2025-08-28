namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Interface for tenant-aware caching operations
/// </summary>
public interface ITenantCacheService : ICacheService
{
    /// <summary>
    /// Gets a cached value for the current tenant
    /// </summary>
    /// <typeparam name="T">The type of the cached value</typeparam>
    /// <param name="key">The cache key</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The cached value if found</returns>
    Task<T?> GetAsync<T>(string key, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a cached value for the current tenant
    /// </summary>
    /// <typeparam name="T">The type of the value to cache</typeparam>
    /// <param name="key">The cache key</param>
    /// <param name="value">The value to cache</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="expiration">Optional expiration time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetAsync<T>(string key, T value, Guid tenantId, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a cached value for the current tenant
    /// </summary>
    /// <param name="key">The cache key</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveAsync(string key, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cached values for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes cached values matching a pattern for a specific tenant
    /// </summary>
    /// <param name="pattern">The pattern to match</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveByPatternAsync(string pattern, Guid tenantId, CancellationToken cancellationToken = default);
}