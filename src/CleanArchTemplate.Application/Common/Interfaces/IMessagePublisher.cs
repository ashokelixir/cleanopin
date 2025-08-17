using CleanArchTemplate.Application.Common.Models;

namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Interface for publishing messages to message queues
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Publishes a single message to the specified queue
    /// </summary>
    /// <typeparam name="T">The type of message to publish</typeparam>
    /// <param name="message">The message to publish</param>
    /// <param name="queueName">The name of the queue to publish to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The message ID of the published message</returns>
    Task<string> PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Publishes a batch of messages to the specified queue
    /// </summary>
    /// <typeparam name="T">The type of messages to publish</typeparam>
    /// <param name="messages">The messages to publish</param>
    /// <param name="queueName">The name of the queue to publish to</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A collection of message IDs for the published messages</returns>
    Task<IEnumerable<string>> PublishBatchAsync<T>(IEnumerable<T> messages, string queueName, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Publishes a message to a FIFO queue with message group ID and deduplication ID
    /// </summary>
    /// <typeparam name="T">The type of message to publish</typeparam>
    /// <param name="message">The message to publish</param>
    /// <param name="queueName">The name of the FIFO queue to publish to</param>
    /// <param name="messageGroupId">The message group ID for FIFO ordering</param>
    /// <param name="deduplicationId">The deduplication ID to prevent duplicate messages</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The message ID of the published message</returns>
    Task<string> PublishToFifoAsync<T>(T message, string queueName, string messageGroupId, string? deduplicationId = null, CancellationToken cancellationToken = default) where T : class;
}