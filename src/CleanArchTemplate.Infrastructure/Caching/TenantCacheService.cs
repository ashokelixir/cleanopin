using CleanArchTemplate.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Infrastructure.Caching;

/// <summary>
/// Tenant-aware cache service that prefixes cache keys with tenant ID
/// </summary>
public class TenantCacheService : ITenantCacheService
{
    private readonly ICacheService _cacheService;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantCacheService> _logger;

    public TenantCacheService(
        ICacheService cacheService,
        ITenantContext tenantContext,
        ILogger<TenantCacheService> logger)
    {
        _cacheService = cacheService;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    /// <summary>
    /// Gets a cached value using the current tenant context
    /// </summary>
    /// <typeparam name="T">The type of the cached value</typeparam>
    /// <param name="key">The cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The cached value if found</returns>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.GetCurrentTenantId();
        if (!tenantId.HasValue)
        {
            _logger.LogWarning("No tenant context available for cache key: {Key}", key);
            return default(T);
        }

        return await GetAsync<T>(key, tenantId.Value, cancellationToken);
    }

    /// <summary>
    /// Gets a cached value for a specific tenant
    /// </summary>
    /// <typeparam name="T">The type of the cached value</typeparam>
    /// <param name="key">The cache key</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The cached value if found</returns>
    public async Task<T?> GetAsync<T>(string key, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenantKey = GetTenantKey(key, tenantId);
        return await _cacheService.GetAsync<T>(tenantKey, cancellationToken);
    }

    /// <summary>
    /// Sets a cached value using the current tenant context
    /// </summary>
    /// <typeparam name="T">The type of the value to cache</typeparam>
    /// <param name="key">The cache key</param>
    /// <param name="value">The value to cache</param>
    /// <param name="expiration">Optional expiration time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.GetCurrentTenantId();
        if (!tenantId.HasValue)
        {
            _logger.LogWarning("No tenant context available for cache key: {Key}", key);
            return;
        }

        await SetAsync(key, value, tenantId.Value, expiration, cancellationToken);
    }

    /// <summary>
    /// Sets a cached value for a specific tenant
    /// </summary>
    /// <typeparam name="T">The type of the value to cache</typeparam>
    /// <param name="key">The cache key</param>
    /// <param name="value">The value to cache</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="expiration">Optional expiration time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task SetAsync<T>(string key, T value, Guid tenantId, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var tenantKey = GetTenantKey(key, tenantId);
        await _cacheService.SetAsync(tenantKey, value, expiration, cancellationToken);
    }

    /// <summary>
    /// Removes a cached value using the current tenant context
    /// </summary>
    /// <param name="key">The cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.GetCurrentTenantId();
        if (!tenantId.HasValue)
        {
            _logger.LogWarning("No tenant context available for cache key: {Key}", key);
            return;
        }

        await RemoveAsync(key, tenantId.Value, cancellationToken);
    }

    /// <summary>
    /// Removes a cached value for a specific tenant
    /// </summary>
    /// <param name="key">The cache key</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RemoveAsync(string key, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenantKey = GetTenantKey(key, tenantId);
        await _cacheService.RemoveAsync(tenantKey, cancellationToken);
    }

    /// <summary>
    /// Removes cached values matching a pattern using the current tenant context
    /// </summary>
    /// <param name="pattern">The pattern to match</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.GetCurrentTenantId();
        if (!tenantId.HasValue)
        {
            _logger.LogWarning("No tenant context available for cache pattern: {Pattern}", pattern);
            return;
        }

        await RemoveByPatternAsync(pattern, tenantId.Value, cancellationToken);
    }

    /// <summary>
    /// Removes all cached values for a specific tenant
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RemoveByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenantPattern = $"tenant:{tenantId}:*";
        await _cacheService.RemoveByPatternAsync(tenantPattern, cancellationToken);
    }

    /// <summary>
    /// Removes cached values matching a pattern for a specific tenant
    /// </summary>
    /// <param name="pattern">The pattern to match</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task RemoveByPatternAsync(string pattern, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenantPattern = GetTenantKey(pattern, tenantId);
        await _cacheService.RemoveByPatternAsync(tenantPattern, cancellationToken);
    }

    /// <summary>
    /// Creates a tenant-scoped cache key
    /// </summary>
    /// <param name="key">The original cache key</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>The tenant-scoped cache key</returns>
    private static string GetTenantKey(string key, Guid tenantId)
    {
        return $"tenant:{tenantId}:{key}";
    }
}