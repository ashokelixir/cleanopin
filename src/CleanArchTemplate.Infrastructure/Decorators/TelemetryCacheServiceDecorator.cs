using CleanArchTemplate.Application.Common.Interfaces;
using System.Diagnostics;

namespace CleanArchTemplate.Infrastructure.Decorators;

/// <summary>
/// Decorator for cache service that adds telemetry tracking
/// </summary>
public class TelemetryCacheServiceDecorator : ICacheService
{
    private readonly ICacheService _cacheService;
    private readonly ITelemetryService _telemetryService;

    public TelemetryCacheServiceDecorator(ICacheService cacheService, ITelemetryService telemetryService)
    {
        _cacheService = cacheService;
        _telemetryService = telemetryService;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;
        T? result = default;
        bool hit = false;

        using var activity = _telemetryService.StartActivity("Cache.Get");
        _telemetryService.AddTag("cache.key", key);
        _telemetryService.AddTag("cache.operation", "get");
        _telemetryService.AddTag("cache.type", typeof(T).Name);

        try
        {
            result = await _cacheService.GetAsync<T>(key, cancellationToken);
            hit = result != null;
            _telemetryService.AddTag("cache.hit", hit);
            return result;
        }
        catch (Exception ex)
        {
            exception = ex;
            _telemetryService.RecordException(ex,
                new KeyValuePair<string, object?>("cache.key", key),
                new KeyValuePair<string, object?>("cache.operation", "get"));
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _telemetryService.RecordCacheOperation("get", hit, stopwatch.Elapsed);
            
            // Record cache hit/miss metrics
            _telemetryService.RecordCounter("cache_operations_total", 1,
                new KeyValuePair<string, object?>("operation", "get"),
                new KeyValuePair<string, object?>("result", hit ? "hit" : "miss"),
                new KeyValuePair<string, object?>("type", typeof(T).Name));
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;

        using var activity = _telemetryService.StartActivity("Cache.Set");
        _telemetryService.AddTag("cache.key", key);
        _telemetryService.AddTag("cache.operation", "set");
        _telemetryService.AddTag("cache.type", typeof(T).Name);
        _telemetryService.AddTag("cache.expiration", expiration?.TotalSeconds);

        try
        {
            await _cacheService.SetAsync(key, value, expiration, cancellationToken);
            _telemetryService.AddTag("cache.success", true);
        }
        catch (Exception ex)
        {
            exception = ex;
            _telemetryService.RecordException(ex,
                new KeyValuePair<string, object?>("cache.key", key),
                new KeyValuePair<string, object?>("cache.operation", "set"));
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _telemetryService.RecordCacheOperation("set", null, stopwatch.Elapsed);
            
            // Record cache set metrics
            _telemetryService.RecordCounter("cache_operations_total", 1,
                new KeyValuePair<string, object?>("operation", "set"),
                new KeyValuePair<string, object?>("success", exception == null),
                new KeyValuePair<string, object?>("type", typeof(T).Name));
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;

        using var activity = _telemetryService.StartActivity("Cache.Remove");
        _telemetryService.AddTag("cache.key", key);
        _telemetryService.AddTag("cache.operation", "remove");

        try
        {
            await _cacheService.RemoveAsync(key, cancellationToken);
            _telemetryService.AddTag("cache.success", true);
        }
        catch (Exception ex)
        {
            exception = ex;
            _telemetryService.RecordException(ex,
                new KeyValuePair<string, object?>("cache.key", key),
                new KeyValuePair<string, object?>("cache.operation", "remove"));
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _telemetryService.RecordCacheOperation("remove", null, stopwatch.Elapsed);
            
            // Record cache remove metrics
            _telemetryService.RecordCounter("cache_operations_total", 1,
                new KeyValuePair<string, object?>("operation", "remove"),
                new KeyValuePair<string, object?>("success", exception == null));
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;

        using var activity = _telemetryService.StartActivity("Cache.RemoveByPattern");
        _telemetryService.AddTag("cache.pattern", pattern);
        _telemetryService.AddTag("cache.operation", "remove_by_pattern");

        try
        {
            await _cacheService.RemoveByPatternAsync(pattern, cancellationToken);
            _telemetryService.AddTag("cache.success", true);
        }
        catch (Exception ex)
        {
            exception = ex;
            _telemetryService.RecordException(ex,
                new KeyValuePair<string, object?>("cache.pattern", pattern),
                new KeyValuePair<string, object?>("cache.operation", "remove_by_pattern"));
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _telemetryService.RecordCacheOperation("remove_by_pattern", null, stopwatch.Elapsed);
            
            // Record cache remove by pattern metrics
            _telemetryService.RecordCounter("cache_operations_total", 1,
                new KeyValuePair<string, object?>("operation", "remove_by_pattern"),
                new KeyValuePair<string, object?>("success", exception == null));
        }
    }
}