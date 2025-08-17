using System.Diagnostics;

namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Service for managing telemetry data including activities, metrics, and exceptions
/// </summary>
public interface ITelemetryService
{
    /// <summary>
    /// Starts a new activity for distributed tracing
    /// </summary>
    /// <param name="name">The name of the activity</param>
    /// <param name="kind">The kind of activity</param>
    /// <returns>The started activity or null if not sampled</returns>
    Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal);

    /// <summary>
    /// Records a metric value with optional tags
    /// </summary>
    /// <param name="name">The name of the metric</param>
    /// <param name="value">The metric value</param>
    /// <param name="tags">Optional tags for the metric</param>
    void RecordMetric(string name, double value, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Records a counter metric
    /// </summary>
    /// <param name="name">The name of the counter</param>
    /// <param name="increment">The increment value (default: 1)</param>
    /// <param name="tags">Optional tags for the metric</param>
    void RecordCounter(string name, long increment = 1, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Records a histogram metric for measuring distributions
    /// </summary>
    /// <param name="name">The name of the histogram</param>
    /// <param name="value">The value to record</param>
    /// <param name="tags">Optional tags for the metric</param>
    void RecordHistogram(string name, double value, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Records an exception for telemetry
    /// </summary>
    /// <param name="exception">The exception to record</param>
    /// <param name="tags">Optional tags for the exception</param>
    void RecordException(Exception exception, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Adds a tag to the current activity
    /// </summary>
    /// <param name="key">The tag key</param>
    /// <param name="value">The tag value</param>
    void AddTag(string key, object? value);

    /// <summary>
    /// Adds an event to the current activity
    /// </summary>
    /// <param name="name">The event name</param>
    /// <param name="tags">Optional tags for the event</param>
    void AddEvent(string name, params KeyValuePair<string, object?>[] tags);

    /// <summary>
    /// Records database operation metrics
    /// </summary>
    /// <param name="operation">The database operation name</param>
    /// <param name="duration">The operation duration</param>
    /// <param name="success">Whether the operation was successful</param>
    /// <param name="tableName">The table name involved</param>
    void RecordDatabaseOperation(string operation, TimeSpan duration, bool success, string? tableName = null);

    /// <summary>
    /// Records cache operation metrics
    /// </summary>
    /// <param name="operation">The cache operation (get, set, remove)</param>
    /// <param name="hit">Whether it was a cache hit (for get operations)</param>
    /// <param name="duration">The operation duration</param>
    void RecordCacheOperation(string operation, bool? hit, TimeSpan duration);

    /// <summary>
    /// Records external service call metrics
    /// </summary>
    /// <param name="serviceName">The name of the external service</param>
    /// <param name="operation">The operation performed</param>
    /// <param name="duration">The call duration</param>
    /// <param name="success">Whether the call was successful</param>
    /// <param name="statusCode">The HTTP status code (if applicable)</param>
    void RecordExternalServiceCall(string serviceName, string operation, TimeSpan duration, bool success, int? statusCode = null);
}