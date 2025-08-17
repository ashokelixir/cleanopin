using System.Diagnostics;
using System.Diagnostics.Metrics;
using CleanArchTemplate.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Infrastructure.Services;

/// <summary>
/// Implementation of telemetry service for OpenTelemetry integration
/// </summary>
public class TelemetryService : ITelemetryService, IDisposable
{
    private readonly ILogger<TelemetryService> _logger;
    private readonly ActivitySource _activitySource;
    private readonly Meter _meter;
    
    // Counters
    private readonly Counter<long> _requestCounter;
    private readonly Counter<long> _errorCounter;
    private readonly Counter<long> _databaseOperationCounter;
    private readonly Counter<long> _cacheOperationCounter;
    private readonly Counter<long> _externalServiceCallCounter;
    
    // Histograms
    private readonly Histogram<double> _requestDuration;
    private readonly Histogram<double> _databaseOperationDuration;
    private readonly Histogram<double> _cacheOperationDuration;
    private readonly Histogram<double> _externalServiceCallDuration;
    
    // Gauges (using UpDownCounter as .NET doesn't have native Gauge yet)
    private readonly UpDownCounter<long> _activeConnections;
    private readonly UpDownCounter<long> _cacheSize;

    public TelemetryService(ILogger<TelemetryService> logger)
    {
        _logger = logger;
        _activitySource = new ActivitySource("CleanArchTemplate");
        _meter = new Meter("CleanArchTemplate", "1.0.0");
        
        // Initialize counters
        _requestCounter = _meter.CreateCounter<long>(
            "cleanarch_requests_total",
            description: "Total number of requests processed");
            
        _errorCounter = _meter.CreateCounter<long>(
            "cleanarch_errors_total",
            description: "Total number of errors occurred");
            
        _databaseOperationCounter = _meter.CreateCounter<long>(
            "cleanarch_database_operations_total",
            description: "Total number of database operations");
            
        _cacheOperationCounter = _meter.CreateCounter<long>(
            "cleanarch_cache_operations_total",
            description: "Total number of cache operations");
            
        _externalServiceCallCounter = _meter.CreateCounter<long>(
            "cleanarch_external_service_calls_total",
            description: "Total number of external service calls");
        
        // Initialize histograms
        _requestDuration = _meter.CreateHistogram<double>(
            "cleanarch_request_duration_seconds",
            unit: "s",
            description: "Duration of HTTP requests in seconds");
            
        _databaseOperationDuration = _meter.CreateHistogram<double>(
            "cleanarch_database_operation_duration_seconds",
            unit: "s",
            description: "Duration of database operations in seconds");
            
        _cacheOperationDuration = _meter.CreateHistogram<double>(
            "cleanarch_cache_operation_duration_seconds",
            unit: "s",
            description: "Duration of cache operations in seconds");
            
        _externalServiceCallDuration = _meter.CreateHistogram<double>(
            "cleanarch_external_service_call_duration_seconds",
            unit: "s",
            description: "Duration of external service calls in seconds");
        
        // Initialize gauges
        _activeConnections = _meter.CreateUpDownCounter<long>(
            "cleanarch_active_connections",
            description: "Number of active database connections");
            
        _cacheSize = _meter.CreateUpDownCounter<long>(
            "cleanarch_cache_size_bytes",
            unit: "bytes",
            description: "Current cache size in bytes");
    }

    public Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        var activity = _activitySource.StartActivity(name, kind);
        
        if (activity != null)
        {
            _logger.LogDebug("Started activity: {ActivityName} with ID: {ActivityId}", name, activity.Id);
        }
        
        return activity;
    }

    public void RecordMetric(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        try
        {
            // For custom metrics, we'll use a generic histogram
            var histogram = _meter.CreateHistogram<double>(name);
            histogram.Record(value, tags);
            
            _logger.LogDebug("Recorded metric: {MetricName} = {Value} with tags: {Tags}", 
                name, value, string.Join(", ", tags.Select(t => $"{t.Key}={t.Value}")));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record metric: {MetricName}", name);
        }
    }

    public void RecordCounter(string name, long increment = 1, params KeyValuePair<string, object?>[] tags)
    {
        try
        {
            var counter = _meter.CreateCounter<long>(name);
            counter.Add(increment, tags);
            
            _logger.LogDebug("Recorded counter: {CounterName} += {Increment} with tags: {Tags}", 
                name, increment, string.Join(", ", tags.Select(t => $"{t.Key}={t.Value}")));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record counter: {CounterName}", name);
        }
    }

    public void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags)
    {
        try
        {
            var histogram = _meter.CreateHistogram<double>(name);
            histogram.Record(value, tags);
            
            _logger.LogDebug("Recorded histogram: {HistogramName} = {Value} with tags: {Tags}", 
                name, value, string.Join(", ", tags.Select(t => $"{t.Key}={t.Value}")));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record histogram: {HistogramName}", name);
        }
    }

    public void RecordException(Exception exception, params KeyValuePair<string, object?>[] tags)
    {
        try
        {
            var activity = Activity.Current;
            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Error, exception.Message);
                
                // Add exception details as tags
                activity.SetTag("exception.type", exception.GetType().Name);
                activity.SetTag("exception.message", exception.Message);
                activity.SetTag("exception.stacktrace", exception.StackTrace);
                
                foreach (var tag in tags)
                {
                    activity.SetTag(tag.Key, tag.Value?.ToString());
                }
            }
            
            _errorCounter.Add(1, 
                new KeyValuePair<string, object?>("exception_type", exception.GetType().Name),
                new KeyValuePair<string, object?>("exception_message", exception.Message));
            
            _logger.LogError(exception, "Recorded exception in telemetry: {ExceptionType}", exception.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record exception in telemetry");
        }
    }

    public void AddTag(string key, object? value)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            activity.SetTag(key, value?.ToString());
            _logger.LogDebug("Added tag to current activity: {Key} = {Value}", key, value);
        }
    }

    public void AddEvent(string name, params KeyValuePair<string, object?>[] tags)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            var activityTags = new ActivityTagsCollection(tags.ToDictionary(t => t.Key, t => t.Value));
            activity.AddEvent(new ActivityEvent(name, DateTimeOffset.UtcNow, activityTags));
            
            _logger.LogDebug("Added event to current activity: {EventName} with tags: {Tags}", 
                name, string.Join(", ", tags.Select(t => $"{t.Key}={t.Value}")));
        }
    }

    public void RecordDatabaseOperation(string operation, TimeSpan duration, bool success, string? tableName = null)
    {
        try
        {
            var tags = new List<KeyValuePair<string, object?>>
            {
                new("operation", operation),
                new("success", success)
            };
            
            if (!string.IsNullOrEmpty(tableName))
            {
                tags.Add(new("table", tableName));
            }
            
            _databaseOperationCounter.Add(1, tags.ToArray());
            _databaseOperationDuration.Record(duration.TotalSeconds, tags.ToArray());
            
            _logger.LogDebug("Recorded database operation: {Operation} on {Table} - Duration: {Duration}ms, Success: {Success}", 
                operation, tableName ?? "unknown", duration.TotalMilliseconds, success);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record database operation metrics");
        }
    }

    public void RecordCacheOperation(string operation, bool? hit, TimeSpan duration)
    {
        try
        {
            var tags = new List<KeyValuePair<string, object?>>
            {
                new("operation", operation)
            };
            
            if (hit.HasValue)
            {
                tags.Add(new("hit", hit.Value));
            }
            
            _cacheOperationCounter.Add(1, tags.ToArray());
            _cacheOperationDuration.Record(duration.TotalSeconds, tags.ToArray());
            
            _logger.LogDebug("Recorded cache operation: {Operation} - Duration: {Duration}ms, Hit: {Hit}", 
                operation, duration.TotalMilliseconds, hit);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record cache operation metrics");
        }
    }

    public void RecordExternalServiceCall(string serviceName, string operation, TimeSpan duration, bool success, int? statusCode = null)
    {
        try
        {
            var tags = new List<KeyValuePair<string, object?>>
            {
                new("service", serviceName),
                new("operation", operation),
                new("success", success)
            };
            
            if (statusCode.HasValue)
            {
                tags.Add(new("status_code", statusCode.Value));
            }
            
            _externalServiceCallCounter.Add(1, tags.ToArray());
            _externalServiceCallDuration.Record(duration.TotalSeconds, tags.ToArray());
            
            _logger.LogDebug("Recorded external service call: {Service}.{Operation} - Duration: {Duration}ms, Success: {Success}, Status: {StatusCode}", 
                serviceName, operation, duration.TotalMilliseconds, success, statusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record external service call metrics");
        }
    }

    public void Dispose()
    {
        _activitySource?.Dispose();
        _meter?.Dispose();
    }
}