namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Interface for cache service operations
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a value from the cache
    /// </summary>
    /// <typeparam name="T">The type of the cached value</typeparam>
    /// <param name="key">The cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The cached value or null if not found</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a value in the cache
    /// </summary>
    /// <typeparam name="T">The type of the value to cache</typeparam>
    /// <param name="key">The cache key</param>
    /// <param name="value">The value to cache</param>
    /// <param name="expiration">Optional expiration time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a value from the cache
    /// </summary>
    /// <param name="key">The cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes values from the cache by pattern
    /// </summary>
    /// <param name="pattern">The pattern to match keys</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
}