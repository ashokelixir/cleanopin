using CleanArchTemplate.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CleanArchTemplate.Infrastructure.Messaging;

/// <summary>
/// No-operation implementation of IMessagePublisher for when messaging is disabled
/// </summary>
public class NoOpMessagePublisher : IMessagePublisher
{
    private readonly ILogger<NoOpMessagePublisher> _logger;

    public NoOpMessagePublisher(ILogger<NoOpMessagePublisher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<string> PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogDebug("Messaging is disabled. Skipping message publication to queue {QueueName} for message type {MessageType}", 
            queueName, typeof(T).Name);
        return Task.FromResult("noop-message-id");
    }

    public Task<IEnumerable<string>> PublishBatchAsync<T>(IEnumerable<T> messages, string queueName, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogDebug("Messaging is disabled. Skipping batch message publication to queue {QueueName} for {MessageCount} messages of type {MessageType}", 
            queueName, messages.Count(), typeof(T).Name);
        return Task.FromResult(Enumerable.Empty<string>());
    }

    public Task<string> PublishToFifoAsync<T>(T message, string queueName, string messageGroupId, string? deduplicationId = null, CancellationToken cancellationToken = default) where T : class
    {
        _logger.LogDebug("Messaging is disabled. Skipping FIFO message publication to queue {QueueName} for message type {MessageType}", 
            queueName, typeof(T).Name);
        return Task.FromResult("noop-fifo-message-id");
    }
}