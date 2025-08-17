namespace CleanArchTemplate.Application.Common.Models;

/// <summary>
/// Configuration for SQS queues
/// </summary>
public class QueueConfiguration
{
    /// <summary>
    /// The name of the queue
    /// </summary>
    public string QueueName { get; set; } = string.Empty;

    /// <summary>
    /// The URL of the queue
    /// </summary>
    public string QueueUrl { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a FIFO queue
    /// </summary>
    public bool IsFifo { get; set; } = false;

    /// <summary>
    /// The dead letter queue configuration
    /// </summary>
    public DeadLetterQueueConfiguration? DeadLetterQueue { get; set; }

    /// <summary>
    /// Maximum number of receive attempts before sending to DLQ
    /// </summary>
    public int MaxReceiveCount { get; set; } = 3;

    /// <summary>
    /// Visibility timeout in seconds
    /// </summary>
    public int VisibilityTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Message retention period in seconds (default 14 days)
    /// </summary>
    public int MessageRetentionPeriodSeconds { get; set; } = 1209600;

    /// <summary>
    /// Maximum message size in bytes (default 256KB)
    /// </summary>
    public int MaxMessageSizeBytes { get; set; } = 262144;

    /// <summary>
    /// Receive message wait time in seconds (long polling)
    /// </summary>
    public int ReceiveMessageWaitTimeSeconds { get; set; } = 20;
}

/// <summary>
/// Configuration for dead letter queues
/// </summary>
public class DeadLetterQueueConfiguration
{
    /// <summary>
    /// The name of the dead letter queue
    /// </summary>
    public string QueueName { get; set; } = string.Empty;

    /// <summary>
    /// The URL of the dead letter queue
    /// </summary>
    public string QueueUrl { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of times a message can be received before being sent to DLQ
    /// </summary>
    public int MaxReceiveCount { get; set; } = 3;
}