namespace CleanArchTemplate.Application.Common.Models;

/// <summary>
/// Envelope for wrapping messages with metadata
/// </summary>
/// <typeparam name="T">The type of the message payload</typeparam>
public class MessageEnvelope<T> where T : class
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The type of the message
    /// </summary>
    public string MessageType { get; set; } = typeof(T).Name;

    /// <summary>
    /// Timestamp when the message was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The actual message payload
    /// </summary>
    public T Payload { get; set; } = default!;

    /// <summary>
    /// Correlation ID for tracking related messages
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// User ID of the user who triggered the message
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Additional metadata for the message
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Number of times this message has been processed (for retry tracking)
    /// </summary>
    public int ProcessingAttempts { get; set; } = 0;
}

/// <summary>
/// Base class for all messages
/// </summary>
public abstract class BaseMessage
{
    /// <summary>
    /// Unique identifier for the message
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Timestamp when the message was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Correlation ID for tracking related messages
    /// </summary>
    public string? CorrelationId { get; set; }
}