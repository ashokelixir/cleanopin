using Amazon.SQS;
using Amazon.SQS.Model;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Shared.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System.Collections.Concurrent;
using System.Text.Json;

namespace CleanArchTemplate.Infrastructure.Messaging;

/// <summary>
/// AWS SQS implementation of message consumer
/// </summary>
public class SqsMessageConsumer : IMessageConsumer, IHostedService
{
    private readonly IAmazonSQS _sqsClient;
    private readonly MessagingOptions _options;
    private readonly ILogger<SqsMessageConsumer> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly ConcurrentDictionary<string, ConsumerInfo> _consumers = new();
    private readonly ConcurrentDictionary<string, string> _queueUrlCache = new();
    private readonly SemaphoreSlim _queueUrlCacheLock = new(1, 1);
    private readonly CancellationTokenSource _globalCancellationTokenSource = new();

    public SqsMessageConsumer(
        IAmazonSQS sqsClient,
        IOptions<MessagingOptions> options,
        ILogger<SqsMessageConsumer> logger,
        ResiliencePipeline resiliencePipeline)
    {
        _sqsClient = sqsClient;
        _options = options.Value;
        _logger = logger;
        _resiliencePipeline = resiliencePipeline;
    }

    public async Task StartConsumingAsync<T>(string queueName, Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default) where T : class
    {
        if (_consumers.ContainsKey(queueName))
        {
            _logger.LogWarning("Consumer for queue {QueueName} is already running", queueName);
            return;
        }

        var queueUrl = await GetQueueUrlAsync(queueName, cancellationToken);
        var consumerCts = CancellationTokenSource.CreateLinkedTokenSource(_globalCancellationTokenSource.Token, cancellationToken);
        
        var consumerInfo = new ConsumerInfo
        {
            QueueName = queueName,
            QueueUrl = queueUrl,
            CancellationTokenSource = consumerCts,
            Task = StartConsumerLoop<T>(queueName, queueUrl, handler, consumerCts.Token)
        };

        _consumers.TryAdd(queueName, consumerInfo);
        _logger.LogInformation("Started consuming messages from queue {QueueName}", queueName);
    }

    public async Task StopConsumingAsync(string queueName)
    {
        if (_consumers.TryRemove(queueName, out var consumerInfo))
        {
            consumerInfo.CancellationTokenSource.Cancel();
            try
            {
                await consumerInfo.Task;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            finally
            {
                consumerInfo.CancellationTokenSource.Dispose();
            }

            _logger.LogInformation("Stopped consuming messages from queue {QueueName}", queueName);
        }
    }

    public async Task StopAllAsync()
    {
        _globalCancellationTokenSource.Cancel();

        var tasks = _consumers.Values.Select(c => c.Task).ToArray();
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }

        foreach (var consumer in _consumers.Values)
        {
            consumer.CancellationTokenSource.Dispose();
        }

        _consumers.Clear();
        _logger.LogInformation("Stopped all message consumers");
    }

    private async Task StartConsumerLoop<T>(string queueName, string queueUrl, Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken) where T : class
    {
        var consumerOptions = _options.Consumer;
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var receiveRequest = new ReceiveMessageRequest
                {
                    QueueUrl = queueUrl,
                    MaxNumberOfMessages = consumerOptions.MaxMessages,
                    WaitTimeSeconds = consumerOptions.WaitTimeSeconds,
                    MessageAttributeNames = new List<string> { "All" }
                };

                var response = await _resiliencePipeline.ExecuteAsync(async (ct) =>
                    await _sqsClient.ReceiveMessageAsync(receiveRequest, ct), cancellationToken);

                if (response.Messages.Any())
                {
                    var processingTasks = response.Messages.Select(message =>
                        ProcessMessageAsync(message, queueUrl, handler, cancellationToken));

                    await Task.WhenAll(processingTasks);
                }
                else
                {
                    // No messages received, wait before polling again
                    await Task.Delay(consumerOptions.PollingIntervalMs, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in consumer loop for queue {QueueName}", queueName);
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    private async Task ProcessMessageAsync<T>(Message message, string queueUrl, Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken) where T : class
    {
        try
        {
            _logger.LogDebug("Processing message {MessageId} from queue", message.MessageId);

            // Deserialize the message envelope
            var envelope = JsonSerializer.Deserialize<MessageEnvelope<T>>(message.Body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (envelope?.Payload == null)
            {
                _logger.LogWarning("Received message with null payload. MessageId: {MessageId}", message.MessageId);
                await DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);
                return;
            }

            // Process the message with retry policy
            await _resiliencePipeline.ExecuteAsync(async (ct) =>
            {
                await handler(envelope.Payload, ct);
            }, cancellationToken);

            // Delete the message after successful processing
            await DeleteMessageAsync(queueUrl, message.ReceiptHandle, cancellationToken);

            _logger.LogDebug("Successfully processed and deleted message {MessageId}", message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process message {MessageId}. Message will be retried or sent to DLQ", message.MessageId);
            
            // Don't delete the message - let it be retried or sent to DLQ based on queue configuration
            // The message will become visible again after the visibility timeout
        }
    }

    private async Task DeleteMessageAsync(string queueUrl, string receiptHandle, CancellationToken cancellationToken)
    {
        try
        {
            await _sqsClient.DeleteMessageAsync(queueUrl, receiptHandle, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete message with receipt handle {ReceiptHandle}", receiptHandle);
        }
    }

    private async Task<string> GetQueueUrlAsync(string queueName, CancellationToken cancellationToken)
    {
        await _queueUrlCacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_queueUrlCache.TryGetValue(queueName, out var cachedUrl))
            {
                return cachedUrl;
            }

            var response = await _sqsClient.GetQueueUrlAsync(queueName, cancellationToken);
            _queueUrlCache.TryAdd(queueName, response.QueueUrl);
            return response.QueueUrl;
        }
        finally
        {
            _queueUrlCacheLock.Release();
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SQS Message Consumer service started");
        await Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("SQS Message Consumer service stopping");
        await StopAllAsync();
    }

    public void Dispose()
    {
        _globalCancellationTokenSource?.Cancel();
        _globalCancellationTokenSource?.Dispose();
        
        foreach (var consumer in _consumers.Values)
        {
            consumer.CancellationTokenSource?.Dispose();
        }
    }

    private class ConsumerInfo
    {
        public string QueueName { get; set; } = string.Empty;
        public string QueueUrl { get; set; } = string.Empty;
        public CancellationTokenSource CancellationTokenSource { get; set; } = default!;
        public Task Task { get; set; } = default!;
    }
}