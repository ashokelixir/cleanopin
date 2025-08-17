using CleanArchTemplate.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CleanArchTemplate.Infrastructure.Caching;

/// <summary>
/// In-memory cache service implementation for development
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;

    public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_memoryCache.TryGetValue(key, out var value))
            {
                if (value is T typedValue)
                {
                    _logger.LogDebug("Cache hit for key: {Key}", key);
                    return Task.FromResult<T?>(typedValue);
                }
                
                if (value is string jsonValue)
                {
                    var deserializedValue = JsonSerializer.Deserialize<T>(jsonValue);
                    _logger.LogDebug("Cache hit for key: {Key} (deserialized)", key);
                    return Task.FromResult(deserializedValue);
                }
            }

            _logger.LogDebug("Cache miss for key: {Key}", key);
            return Task.FromResult<T?>(default);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting value from cache for key: {Key}", key);
            return Task.FromResult<T?>(default);
        }
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new MemoryCacheEntryOptions();
            
            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            else
            {
                // Default expiration of 1 hour
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            }

            // Store complex objects as JSON strings for consistency
            if (typeof(T) == typeof(string) || typeof(T).IsPrimitive)
            {
                _memoryCache.Set(key, value, options);
            }
            else
            {
                var jsonValue = JsonSerializer.Serialize(value);
                _memoryCache.Set(key, jsonValue, options);
            }

            _logger.LogDebug("Set cache value for key: {Key} with expiration: {Expiration}", key, expiration);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error setting value in cache for key: {Key}", key);
            return Task.CompletedTask;
        }
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _memoryCache.Remove(key);
            _logger.LogDebug("Removed cache value for key: {Key}", key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing value from cache for key: {Key}", key);
            return Task.CompletedTask;
        }
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            // Note: IMemoryCache doesn't support pattern-based removal natively
            // This is a limitation of the in-memory cache implementation
            // For production, use Redis which supports pattern-based operations
            _logger.LogWarning("Pattern-based cache removal is not supported by MemoryCache. Pattern: {Pattern}", pattern);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error removing values from cache by pattern: {Pattern}", pattern);
            return Task.CompletedTask;
        }
    }
}