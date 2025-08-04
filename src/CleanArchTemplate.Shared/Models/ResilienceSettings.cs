using System;

namespace CleanArchTemplate.Shared.Models;

/// <summary>
/// Configuration settings for resilience policies
/// </summary>
public class ResilienceSettings
{
    public const string SectionName = "Resilience";

    /// <summary>
    /// Retry policy settings
    /// </summary>
    public RetrySettings Retry { get; set; } = new();

    /// <summary>
    /// Circuit breaker policy settings
    /// </summary>
    public CircuitBreakerSettings CircuitBreaker { get; set; } = new();

    /// <summary>
    /// Timeout policy settings
    /// </summary>
    public TimeoutSettings Timeout { get; set; } = new();

    /// <summary>
    /// Bulkhead policy settings
    /// </summary>
    public BulkheadSettings Bulkhead { get; set; } = new();
}

/// <summary>
/// Retry policy configuration
/// </summary>
public class RetrySettings
{
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay between retries in milliseconds
    /// </summary>
    public int BaseDelayMs { get; set; } = 1000;

    /// <summary>
    /// Maximum delay between retries in milliseconds
    /// </summary>
    public int MaxDelayMs { get; set; } = 30000;

    /// <summary>
    /// Backoff multiplier for exponential backoff
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;
}

/// <summary>
/// Circuit breaker policy configuration
/// </summary>
public class CircuitBreakerSettings
{
    /// <summary>
    /// Number of consecutive failures before opening the circuit
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Duration to keep the circuit open in seconds
    /// </summary>
    public int DurationOfBreakSeconds { get; set; } = 60;

    /// <summary>
    /// Minimum throughput before circuit breaker activates
    /// </summary>
    public int MinimumThroughput { get; set; } = 10;

    /// <summary>
    /// Sampling duration in seconds for failure rate calculation
    /// </summary>
    public int SamplingDurationSeconds { get; set; } = 30;
}

/// <summary>
/// Timeout policy configuration
/// </summary>
public class TimeoutSettings
{
    /// <summary>
    /// Default timeout in seconds for operations
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Timeout for database operations in seconds
    /// </summary>
    public int DatabaseTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Timeout for external API calls in seconds
    /// </summary>
    public int ExternalApiTimeoutSeconds { get; set; } = 60;
}

/// <summary>
/// Bulkhead policy configuration
/// </summary>
public class BulkheadSettings
{
    /// <summary>
    /// Maximum number of concurrent executions
    /// </summary>
    public int MaxConcurrentExecutions { get; set; } = 10;

    /// <summary>
    /// Maximum number of queued actions
    /// </summary>
    public int MaxQueuedActions { get; set; } = 20;
}