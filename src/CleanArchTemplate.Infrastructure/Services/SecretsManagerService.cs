using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Shared.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Polly;

namespace CleanArchTemplate.Infrastructure.Services;

/// <summary>
/// AWS Secrets Manager service implementation with caching and resilience
/// </summary>
public class SecretsManagerService : ISecretsManagerService, IDisposable
{
    private readonly IAmazonSecretsManager _secretsManagerClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SecretsManagerService> _logger;
    private readonly SecretsManagerSettings _settings;
    private readonly IAsyncPolicy _retryPolicy;
    private bool _disposed;

    public SecretsManagerService(
        IAmazonSecretsManager secretsManagerClient,
        IMemoryCache cache,
        ILogger<SecretsManagerService> logger,
        IOptions<SecretsManagerSettings> settings)
    {
        _secretsManagerClient = secretsManagerClient ?? throw new ArgumentNullException(nameof(secretsManagerClient));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings.Value ?? throw new ArgumentNullException(nameof(settings));

        // Configure retry policy with exponential backoff
        _retryPolicy = Policy
            .Handle<AmazonSecretsManagerException>()
            .Or<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: _settings.MaxRetryAttempts,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(
                    _settings.BaseDelayMs * Math.Pow(2, retryAttempt - 1)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retry attempt {RetryCount} for secret retrieval after {Delay}ms. Exception: {Exception}",
                        retryCount, timespan.TotalMilliseconds, outcome.Message);
                });
    }

    public async Task<string> GetRawSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving raw secret {SecretName} from AWS Secrets Manager", secretName);

        _logger.LogInformation("Retrieving secret: {SecretName} from region: {Region}", secretName, _secretsManagerClient.Config.RegionEndpoint?.SystemName ?? "Unknown");

        var request = new GetSecretValueRequest
        {
            SecretId = GetFullSecretName(secretName)
        };

        var response = await _secretsManagerClient.GetSecretValueAsync(request, cancellationToken).ConfigureAwait(false);

        return response.SecretString;
    }

    /// <inheritdoc />
    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(secretName))
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));

        var cacheKey = GetCacheKey(secretName);

        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out string? cachedValue) && cachedValue != null)
        {
            _logger.LogDebug("Retrieved secret {SecretName} from cache", secretName);
            return cachedValue;
        }

        // If in local development mode, return a placeholder
        //if (_settings.UseLocalDevelopment)
        //{
        //    var localValue = $"local-{secretName}-value";
        //    _logger.LogInformation("Using local development value for secret {SecretName}", secretName);
        //    CacheSecret(cacheKey, localValue);
        //    return localValue;
        //}

        try
        {
            var secretValue = await _retryPolicy.ExecuteAsync(async () =>
            {
                _logger.LogDebug("Retrieving secret {SecretName} from AWS Secrets Manager", secretName);

                var request = new GetSecretValueRequest
                {
                    SecretId = GetFullSecretName(secretName)
                };

                var response = await _secretsManagerClient.GetSecretValueAsync(request, cancellationToken);
                
                _logger.LogInformation("Successfully retrieved secret {SecretName}", secretName);
                return response.SecretString;
            });

            // Cache the secret
            CacheSecret(cacheKey, secretValue);

            return secretValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve secret {SecretName} after {MaxRetries} attempts", 
                secretName, _settings.MaxRetryAttempts);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<T?> GetSecretAsync<T>(string secretName, CancellationToken cancellationToken = default)
    where T : class
    {
        var secretValue = await GetSecretAsync(secretName, cancellationToken);

        // If T is string, return the raw value without JSON deserialization
        if (typeof(T) == typeof(string))
        {
            return (T)(object)secretValue;
        }

        try
        {
            var deserializedValue = JsonSerializer.Deserialize<T>(secretValue, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            _logger.LogDebug("Successfully deserialized secret {SecretName} to type {Type}",
                secretName, typeof(T).Name);

            return deserializedValue;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize secret {SecretName} to type {Type}",
                secretName, typeof(T).Name);
            throw new InvalidOperationException(
                $"Failed to deserialize secret '{secretName}' to type '{typeof(T).Name}'", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, string>> GetSecretsAsync(
        IEnumerable<string> secretNames, 
        CancellationToken cancellationToken = default)
    {
        if (secretNames == null)
            throw new ArgumentNullException(nameof(secretNames));

        var secretNamesList = secretNames.ToList();
        if (!secretNamesList.Any())
            return new Dictionary<string, string>();

        var results = new Dictionary<string, string>();
        var tasks = secretNamesList.Select(async secretName =>
        {
            try
            {
                var value = await GetSecretAsync(secretName, cancellationToken);
                return new KeyValuePair<string, string>(secretName, value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve secret {SecretName} in batch operation", secretName);
                throw;
            }
        });

        var completedTasks = await Task.WhenAll(tasks);
        
        foreach (var result in completedTasks)
        {
            results[result.Key] = result.Value;
        }

        _logger.LogInformation("Successfully retrieved {Count} secrets in batch operation", results.Count);
        return results;
    }

    /// <inheritdoc />
    public void InvalidateCache(string secretName)
    {
        if (string.IsNullOrWhiteSpace(secretName))
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));

        var cacheKey = GetCacheKey(secretName);
        _cache.Remove(cacheKey);
        
        _logger.LogInformation("Invalidated cache for secret {SecretName}", secretName);
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Clear();
            _logger.LogInformation("Cleared all secrets from cache");
        }
        else
        {
            _logger.LogWarning("Unable to clear cache - cache implementation does not support clearing");
        }
    }

    /// <summary>
    /// Gets the full secret name with environment prefix
    /// </summary>
    /// <param name="secretName">The base secret name</param>
    /// <returns>The full secret name</returns>
    private string GetFullSecretName(string secretName)
    {
        return string.IsNullOrWhiteSpace(_settings.Environment) 
            ? secretName 
            : $"{_settings.Environment}/{secretName}";
    }

    /// <summary>
    /// Gets the cache key for a secret
    /// </summary>
    /// <param name="secretName">The secret name</param>
    /// <returns>The cache key</returns>
    private string GetCacheKey(string secretName)
    {
        return $"secret:{GetFullSecretName(secretName)}";
    }

    /// <summary>
    /// Caches a secret value
    /// </summary>
    /// <param name="cacheKey">The cache key</param>
    /// <param name="secretValue">The secret value</param>
    private void CacheSecret(string cacheKey, string secretValue)
    {
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_settings.CacheDurationMinutes),
            Priority = CacheItemPriority.High,
            Size = secretValue.Length
        };

        _cache.Set(cacheKey, secretValue, cacheOptions);
        _logger.LogDebug("Cached secret with key {CacheKey} for {Duration} minutes", 
            cacheKey, _settings.CacheDurationMinutes);
    }

    /// <summary>
    /// Preloads specified secrets into cache
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    public async Task PreloadSecretsAsync(CancellationToken cancellationToken = default)
    {
        if (!_settings.PreloadSecrets.Any())
        {
            _logger.LogDebug("No secrets configured for preloading");
            return;
        }

        _logger.LogInformation("Preloading {Count} secrets into cache", _settings.PreloadSecrets.Count);

        var preloadTasks = _settings.PreloadSecrets.Select(async secretName =>
        {
            try
            {
                await GetSecretAsync(secretName, cancellationToken);
                _logger.LogDebug("Successfully preloaded secret {SecretName}", secretName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to preload secret {SecretName}", secretName);
            }
        });

        await Task.WhenAll(preloadTasks);
        _logger.LogInformation("Completed preloading secrets");
    }

    /// <summary>
    /// Disposes the service and its resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _secretsManagerClient?.Dispose();
            _disposed = true;
        }
    }
}
