using Amazon.SQS;
using Amazon.SQS.Model;
using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System.Text.Json;

namespace CleanArchTemplate.Infrastructure.Messaging;

/// <summary>
/// AWS SQS implementation of message publisher
/// </summary>
public class SqsMessagePublisher : IMessagePublisher
{
    private readonly IAmazonSQS _sqsClient;
    private readonly MessagingOptions _options;
    private readonly ILogger<SqsMessagePublisher> _logger;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly Dictionary<string, string> _queueUrlCache = new();
    private readonly SemaphoreSlim _queueUrlCacheLock = new(1, 1);

    public SqsMessagePublisher(
        IAmazonSQS sqsClient,
        IOptions<MessagingOptions> options,
        ILogger<SqsMessagePublisher> logger,
        ResiliencePipeline resiliencePipeline)
    {
        _sqsClient = sqsClient;
        _options = options.Value;
        _logger = logger;
        _resiliencePipeline = resiliencePipeline;
    }

    public async Task<string> PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var envelope = CreateMessageEnvelope(message);
            var queueUrl = await GetQueueUrlAsync(queueName, cancellationToken);
            
            var sendMessageRequest = new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = JsonSerializer.Serialize(envelope, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
                MessageAttributes = CreateMessageAttributes(envelope)
            };

            var response = await _resiliencePipeline.ExecuteAsync(async (ct) =>
                await _sqsClient.SendMessageAsync(sendMessageRequest, ct), cancellationToken);

            _logger.LogInformation("Message published successfully. MessageId: {MessageId}, Queue: {QueueName}",
                response.MessageId, queueName);

            return response.MessageId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to queue {QueueName}", queueName);
            throw;
        }
    }

    public async Task<IEnumerable<string>> PublishBatchAsync<T>(IEnumerable<T> messages, string queueName, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var messageList = messages.ToList();
            if (!messageList.Any())
            {
                return Enumerable.Empty<string>();
            }

            var queueUrl = await GetQueueUrlAsync(queueName, cancellationToken);
            var messageIds = new List<string>();

            // SQS batch size limit is 10 messages
            const int batchSize = 10;
            for (int i = 0; i < messageList.Count; i += batchSize)
            {
                var batch = messageList.Skip(i).Take(batchSize).ToList();
                var batchRequest = new SendMessageBatchRequest
                {
                    QueueUrl = queueUrl,
                    Entries = batch.Select((msg, index) =>
                    {
                        var envelope = CreateMessageEnvelope(msg);
                        return new SendMessageBatchRequestEntry
                        {
                            Id = $"msg_{i + index}",
                            MessageBody = JsonSerializer.Serialize(envelope, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            }),
                            MessageAttributes = CreateMessageAttributes(envelope)
                        };
                    }).ToList()
                };

                var response = await _resiliencePipeline.ExecuteAsync(async (ct) =>
                    await _sqsClient.SendMessageBatchAsync(batchRequest, ct), cancellationToken);

                messageIds.AddRange(response.Successful.Select(s => s.MessageId));

                if (response.Failed.Any())
                {
                    _logger.LogWarning("Some messages failed to publish in batch. Failed count: {FailedCount}",
                        response.Failed.Count);
                }
            }

            _logger.LogInformation("Batch published successfully. Total messages: {MessageCount}, Queue: {QueueName}",
                messageIds.Count, queueName);

            return messageIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message batch to queue {QueueName}", queueName);
            throw;
        }
    }

    public async Task<string> PublishToFifoAsync<T>(T message, string queueName, string messageGroupId, string? deduplicationId = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var envelope = CreateMessageEnvelope(message);
            var queueUrl = await GetQueueUrlAsync(queueName, cancellationToken);

            var sendMessageRequest = new SendMessageRequest
            {
                QueueUrl = queueUrl,
                MessageBody = JsonSerializer.Serialize(envelope, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
                MessageAttributes = CreateMessageAttributes(envelope),
                MessageGroupId = messageGroupId,
                MessageDeduplicationId = deduplicationId ?? envelope.MessageId
            };

            var response = await _resiliencePipeline.ExecuteAsync(async (ct) =>
                await _sqsClient.SendMessageAsync(sendMessageRequest, ct), cancellationToken);

            _logger.LogInformation("FIFO message published successfully. MessageId: {MessageId}, Queue: {QueueName}, GroupId: {GroupId}",
                response.MessageId, queueName, messageGroupId);

            return response.MessageId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish FIFO message to queue {QueueName}", queueName);
            throw;
        }
    }

    private MessageEnvelope<T> CreateMessageEnvelope<T>(T message) where T : class
    {
        return new MessageEnvelope<T>
        {
            Payload = message,
            MessageType = typeof(T).Name,
            CreatedAt = DateTime.UtcNow,
            MessageId = Guid.NewGuid().ToString()
        };
    }

    private Dictionary<string, MessageAttributeValue> CreateMessageAttributes<T>(MessageEnvelope<T> envelope) where T : class
    {
        var attributes = new Dictionary<string, MessageAttributeValue>
        {
            ["MessageType"] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = envelope.MessageType
            },
            ["CreatedAt"] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = envelope.CreatedAt.ToString("O")
            }
        };

        if (!string.IsNullOrEmpty(envelope.CorrelationId))
        {
            attributes["CorrelationId"] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = envelope.CorrelationId
            };
        }

        if (!string.IsNullOrEmpty(envelope.UserId))
        {
            attributes["UserId"] = new MessageAttributeValue
            {
                DataType = "String",
                StringValue = envelope.UserId
            };
        }

        return attributes;
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
            _queueUrlCache[queueName] = response.QueueUrl;
            return response.QueueUrl;
        }
        finally
        {
            _queueUrlCacheLock.Release();
        }
    }
}