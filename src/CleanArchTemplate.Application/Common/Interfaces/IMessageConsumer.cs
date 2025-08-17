namespace CleanArchTemplate.Application.Common.Interfaces;

/// <summary>
/// Interface for consuming messages from message queues
/// </summary>
public interface IMessageConsumer
{
    /// <summary>
    /// Starts consuming messages from the specified queue
    /// </summary>
    /// <typeparam name="T">The type of message to consume</typeparam>
    /// <param name="queueName">The name of the queue to consume from</param>
    /// <param name="handler">The handler function to process messages</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StartConsumingAsync<T>(string queueName, Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Stops consuming messages from the specified queue
    /// </summary>
    /// <param name="queueName">The name of the queue to stop consuming from</param>
    Task StopConsumingAsync(string queueName);

    /// <summary>
    /// Stops all message consumption
    /// </summary>
    Task StopAllAsync();
}