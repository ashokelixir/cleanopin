using CleanArchTemplate.Application.Common.Interfaces;
using System.Diagnostics;

namespace CleanArchTemplate.Infrastructure.Decorators;

/// <summary>
/// Decorator for message publisher that adds telemetry tracking
/// </summary>
public class TelemetryMessagePublisherDecorator : IMessagePublisher
{
    private readonly IMessagePublisher _messagePublisher;
    private readonly ITelemetryService _telemetryService;

    public TelemetryMessagePublisherDecorator(IMessagePublisher messagePublisher, ITelemetryService telemetryService)
    {
        _messagePublisher = messagePublisher;
        _telemetryService = telemetryService;
    }

    public async Task<string> PublishAsync<T>(T message, string queueName, CancellationToken cancellationToken = default) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;
        string messageId = string.Empty;

        using var activity = _telemetryService.StartActivity("MessagePublisher.Publish", ActivityKind.Producer);
        _telemetryService.AddTag("messaging.operation", "publish");
        _telemetryService.AddTag("messaging.destination", queueName);
        _telemetryService.AddTag("messaging.message_type", typeof(T).Name);
        _telemetryService.AddTag("messaging.system", "sqs");

        try
        {
            messageId = await _messagePublisher.PublishAsync(message, queueName, cancellationToken);
            _telemetryService.AddTag("messaging.success", true);
            _telemetryService.AddTag("messaging.message_id", messageId);
            
            _telemetryService.AddEvent("message.published",
                new KeyValuePair<string, object?>("queue", queueName),
                new KeyValuePair<string, object?>("message_type", typeof(T).Name),
                new KeyValuePair<string, object?>("message_id", messageId));
                
            return messageId;
        }
        catch (Exception ex)
        {
            exception = ex;
            _telemetryService.AddTag("messaging.success", false);
            _telemetryService.RecordException(ex,
                new KeyValuePair<string, object?>("messaging.queue", queueName),
                new KeyValuePair<string, object?>("messaging.operation", "publish"),
                new KeyValuePair<string, object?>("messaging.message_type", typeof(T).Name));
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _telemetryService.RecordExternalServiceCall(
                "sqs",
                $"publish.{queueName}",
                stopwatch.Elapsed,
                exception == null);
                
            // Record messaging metrics
            _telemetryService.RecordCounter("messaging_operations_total", 1,
                new KeyValuePair<string, object?>("operation", "publish"),
                new KeyValuePair<string, object?>("queue", queueName),
                new KeyValuePair<string, object?>("message_type", typeof(T).Name),
                new KeyValuePair<string, object?>("success", exception == null));
                
            _telemetryService.RecordHistogram("messaging_operation_duration_seconds", stopwatch.Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("operation", "publish"),
                new KeyValuePair<string, object?>("queue", queueName));
        }
    }

    public async Task<IEnumerable<string>> PublishBatchAsync<T>(IEnumerable<T> messages, string queueName, CancellationToken cancellationToken = default) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;
        var messageCount = messages.Count();
        IEnumerable<string> messageIds = Enumerable.Empty<string>();

        using var activity = _telemetryService.StartActivity("MessagePublisher.PublishBatch", ActivityKind.Producer);
        _telemetryService.AddTag("messaging.operation", "publish_batch");
        _telemetryService.AddTag("messaging.destination", queueName);
        _telemetryService.AddTag("messaging.message_type", typeof(T).Name);
        _telemetryService.AddTag("messaging.batch_size", messageCount);
        _telemetryService.AddTag("messaging.system", "sqs");

        try
        {
            messageIds = await _messagePublisher.PublishBatchAsync(messages, queueName, cancellationToken);
            _telemetryService.AddTag("messaging.success", true);
            _telemetryService.AddTag("messaging.message_ids_count", messageIds.Count());
            
            _telemetryService.AddEvent("messages.published_batch",
                new KeyValuePair<string, object?>("queue", queueName),
                new KeyValuePair<string, object?>("message_type", typeof(T).Name),
                new KeyValuePair<string, object?>("batch_size", messageCount),
                new KeyValuePair<string, object?>("message_ids_count", messageIds.Count()));
                
            return messageIds;
        }
        catch (Exception ex)
        {
            exception = ex;
            _telemetryService.AddTag("messaging.success", false);
            _telemetryService.RecordException(ex,
                new KeyValuePair<string, object?>("messaging.queue", queueName),
                new KeyValuePair<string, object?>("messaging.operation", "publish_batch"),
                new KeyValuePair<string, object?>("messaging.message_type", typeof(T).Name),
                new KeyValuePair<string, object?>("messaging.batch_size", messageCount));
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _telemetryService.RecordExternalServiceCall(
                "sqs",
                $"publish_batch.{queueName}",
                stopwatch.Elapsed,
                exception == null);
                
            // Record messaging metrics
            _telemetryService.RecordCounter("messaging_operations_total", messageCount,
                new KeyValuePair<string, object?>("operation", "publish_batch"),
                new KeyValuePair<string, object?>("queue", queueName),
                new KeyValuePair<string, object?>("message_type", typeof(T).Name),
                new KeyValuePair<string, object?>("success", exception == null));
                
            _telemetryService.RecordHistogram("messaging_operation_duration_seconds", stopwatch.Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("operation", "publish_batch"),
                new KeyValuePair<string, object?>("queue", queueName));
                
            _telemetryService.RecordHistogram("messaging_batch_size", messageCount,
                new KeyValuePair<string, object?>("operation", "publish_batch"),
                new KeyValuePair<string, object?>("queue", queueName));
        }
    }

    public async Task<string> PublishToFifoAsync<T>(T message, string queueName, string messageGroupId, string? deduplicationId = null, CancellationToken cancellationToken = default) where T : class
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;
        string messageId = string.Empty;

        using var activity = _telemetryService.StartActivity("MessagePublisher.PublishToFifo", ActivityKind.Producer);
        _telemetryService.AddTag("messaging.operation", "publish_fifo");
        _telemetryService.AddTag("messaging.destination", queueName);
        _telemetryService.AddTag("messaging.message_type", typeof(T).Name);
        _telemetryService.AddTag("messaging.message_group_id", messageGroupId);
        _telemetryService.AddTag("messaging.deduplication_id", deduplicationId);
        _telemetryService.AddTag("messaging.system", "sqs");

        try
        {
            messageId = await _messagePublisher.PublishToFifoAsync(message, queueName, messageGroupId, deduplicationId, cancellationToken);
            _telemetryService.AddTag("messaging.success", true);
            _telemetryService.AddTag("messaging.message_id", messageId);
            
            _telemetryService.AddEvent("message.published_fifo",
                new KeyValuePair<string, object?>("queue", queueName),
                new KeyValuePair<string, object?>("message_type", typeof(T).Name),
                new KeyValuePair<string, object?>("message_id", messageId),
                new KeyValuePair<string, object?>("message_group_id", messageGroupId));
                
            return messageId;
        }
        catch (Exception ex)
        {
            exception = ex;
            _telemetryService.AddTag("messaging.success", false);
            _telemetryService.RecordException(ex,
                new KeyValuePair<string, object?>("messaging.queue", queueName),
                new KeyValuePair<string, object?>("messaging.operation", "publish_fifo"),
                new KeyValuePair<string, object?>("messaging.message_type", typeof(T).Name),
                new KeyValuePair<string, object?>("messaging.message_group_id", messageGroupId));
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _telemetryService.RecordExternalServiceCall(
                "sqs",
                $"publish_fifo.{queueName}",
                stopwatch.Elapsed,
                exception == null);
                
            // Record messaging metrics
            _telemetryService.RecordCounter("messaging_operations_total", 1,
                new KeyValuePair<string, object?>("operation", "publish_fifo"),
                new KeyValuePair<string, object?>("queue", queueName),
                new KeyValuePair<string, object?>("message_type", typeof(T).Name),
                new KeyValuePair<string, object?>("success", exception == null));
                
            _telemetryService.RecordHistogram("messaging_operation_duration_seconds", stopwatch.Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("operation", "publish_fifo"),
                new KeyValuePair<string, object?>("queue", queueName));
        }
    }
}