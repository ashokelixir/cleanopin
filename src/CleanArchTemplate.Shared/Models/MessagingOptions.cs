namespace CleanArchTemplate.Shared.Models;

/// <summary>
/// Configuration options for messaging infrastructure
/// </summary>
public class MessagingOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Messaging";

    /// <summary>
    /// Whether messaging is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// AWS region for SQS
    /// </summary>
    public string AwsRegion { get; set; } = "us-east-1";

    /// <summary>
    /// AWS access key (optional, can use IAM roles)
    /// </summary>
    public string? AwsAccessKey { get; set; }

    /// <summary>
    /// AWS secret key (optional, can use IAM roles)
    /// </summary>
    public string? AwsSecretKey { get; set; }

    /// <summary>
    /// LocalStack endpoint for development (optional)
    /// </summary>
    public string? LocalStackEndpoint { get; set; }

    /// <summary>
    /// Queue configurations
    /// </summary>
    public Dictionary<string, QueueOptions> Queues { get; set; } = new();

    /// <summary>
    /// Default retry policy configuration
    /// </summary>
    public RetryPolicyOptions RetryPolicy { get; set; } = new();

    /// <summary>
    /// Consumer configuration
    /// </summary>
    public ConsumerOptions Consumer { get; set; } = new();
}

/// <summary>
/// Configuration options for individual queues
/// </summary>
public class QueueOptions
{
    /// <summary>
    /// The queue name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a FIFO queue
    /// </summary>
    public bool IsFifo { get; set; } = false;

    /// <summary>
    /// Dead letter queue name (optional)
    /// </summary>
    public string? DeadLetterQueueName { get; set; }

    /// <summary>
    /// Maximum receive count before sending to DLQ
    /// </summary>
    public int MaxReceiveCount { get; set; } = 3;

    /// <summary>
    /// Visibility timeout in seconds
    /// </summary>
    public int VisibilityTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Message retention period in seconds
    /// </summary>
    public int MessageRetentionPeriodSeconds { get; set; } = 1209600; // 14 days

    /// <summary>
    /// Whether to enable content-based deduplication for FIFO queues
    /// </summary>
    public bool ContentBasedDeduplication { get; set; } = true;
}

/// <summary>
/// Retry policy configuration
/// </summary>
public class RetryPolicyOptions
{
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Initial retry delay in milliseconds
    /// </summary>
    public int InitialDelayMs { get; set; } = 1000;

    /// <summary>
    /// Maximum retry delay in milliseconds
    /// </summary>
    public int MaxDelayMs { get; set; } = 30000;

    /// <summary>
    /// Backoff multiplier for exponential backoff
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;
}

/// <summary>
/// Consumer configuration options
/// </summary>
public class ConsumerOptions
{
    /// <summary>
    /// Maximum number of messages to receive in a single batch
    /// </summary>
    public int MaxMessages { get; set; } = 10;

    /// <summary>
    /// Wait time for long polling in seconds
    /// </summary>
    public int WaitTimeSeconds { get; set; } = 20;

    /// <summary>
    /// Number of concurrent consumers per queue
    /// </summary>
    public int ConcurrentConsumers { get; set; } = 1;

    /// <summary>
    /// Consumer polling interval in milliseconds when no messages are received
    /// </summary>
    public int PollingIntervalMs { get; set; } = 5000;
}