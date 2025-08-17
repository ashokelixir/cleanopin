using Amazon.SQS;
using Amazon.SQS.Model;
using CleanArchTemplate.Application.Common.Models;
using CleanArchTemplate.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CleanArchTemplate.Infrastructure.Messaging;

/// <summary>
/// Service for managing SQS queues (creation, configuration, etc.)
/// </summary>
public class SqsQueueManager
{
    private readonly IAmazonSQS _sqsClient;
    private readonly MessagingOptions _options;
    private readonly ILogger<SqsQueueManager> _logger;

    public SqsQueueManager(
        IAmazonSQS sqsClient,
        IOptions<MessagingOptions> options,
        ILogger<SqsQueueManager> logger)
    {
        _sqsClient = sqsClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Creates a queue if it doesn't exist
    /// </summary>
    /// <param name="queueName">Name of the queue to create</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The queue configuration</returns>
    public async Task<QueueConfiguration> CreateQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if queue configuration exists in options
            if (!_options.Queues.TryGetValue(queueName, out var queueOptions))
            {
                throw new InvalidOperationException($"Queue configuration for '{queueName}' not found in options");
            }

            var queueName_actual = queueOptions.IsFifo ? $"{queueOptions.Name}.fifo" : queueOptions.Name;

            // Check if queue already exists
            try
            {
                var getQueueUrlResponse = await _sqsClient.GetQueueUrlAsync(queueName_actual, cancellationToken);
                _logger.LogInformation("Queue {QueueName} already exists with URL: {QueueUrl}", queueName_actual, getQueueUrlResponse.QueueUrl);
                
                return new QueueConfiguration
                {
                    QueueName = queueName_actual,
                    QueueUrl = getQueueUrlResponse.QueueUrl,
                    IsFifo = queueOptions.IsFifo,
                    MaxReceiveCount = queueOptions.MaxReceiveCount,
                    VisibilityTimeoutSeconds = queueOptions.VisibilityTimeoutSeconds,
                    MessageRetentionPeriodSeconds = queueOptions.MessageRetentionPeriodSeconds
                };
            }
            catch (QueueDoesNotExistException)
            {
                // Queue doesn't exist, create it
            }

            // Create the queue
            var createQueueRequest = new CreateQueueRequest
            {
                QueueName = queueName_actual,
                Attributes = CreateQueueAttributes(queueOptions)
            };

            var createQueueResponse = await _sqsClient.CreateQueueAsync(createQueueRequest, cancellationToken);
            _logger.LogInformation("Created queue {QueueName} with URL: {QueueUrl}", queueName_actual, createQueueResponse.QueueUrl);

            var queueConfig = new QueueConfiguration
            {
                QueueName = queueName_actual,
                QueueUrl = createQueueResponse.QueueUrl,
                IsFifo = queueOptions.IsFifo,
                MaxReceiveCount = queueOptions.MaxReceiveCount,
                VisibilityTimeoutSeconds = queueOptions.VisibilityTimeoutSeconds,
                MessageRetentionPeriodSeconds = queueOptions.MessageRetentionPeriodSeconds
            };

            // Create dead letter queue if configured
            if (!string.IsNullOrEmpty(queueOptions.DeadLetterQueueName))
            {
                var dlqConfig = await CreateDeadLetterQueueAsync(queueOptions.DeadLetterQueueName, queueOptions.IsFifo, cancellationToken);
                queueConfig.DeadLetterQueue = new DeadLetterQueueConfiguration
                {
                    QueueName = dlqConfig.QueueName,
                    QueueUrl = dlqConfig.QueueUrl,
                    MaxReceiveCount = queueOptions.MaxReceiveCount
                };

                // Update the main queue with DLQ configuration
                await ConfigureDeadLetterQueueAsync(createQueueResponse.QueueUrl, dlqConfig.QueueUrl, queueOptions.MaxReceiveCount, cancellationToken);
            }

            return queueConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create queue {QueueName}", queueName);
            throw;
        }
    }

    /// <summary>
    /// Creates all configured queues
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary of queue configurations</returns>
    public async Task<Dictionary<string, QueueConfiguration>> CreateAllQueuesAsync(CancellationToken cancellationToken = default)
    {
        var queueConfigurations = new Dictionary<string, QueueConfiguration>();

        foreach (var queueName in _options.Queues.Keys)
        {
            try
            {
                var config = await CreateQueueAsync(queueName, cancellationToken);
                queueConfigurations[queueName] = config;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create queue {QueueName}", queueName);
                throw;
            }
        }

        return queueConfigurations;
    }

    /// <summary>
    /// Deletes a queue
    /// </summary>
    /// <param name="queueName">Name of the queue to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DeleteQueueAsync(string queueName, CancellationToken cancellationToken = default)
    {
        try
        {
            var getQueueUrlResponse = await _sqsClient.GetQueueUrlAsync(queueName, cancellationToken);
            await _sqsClient.DeleteQueueAsync(getQueueUrlResponse.QueueUrl, cancellationToken);
            _logger.LogInformation("Deleted queue {QueueName}", queueName);
        }
        catch (QueueDoesNotExistException)
        {
            _logger.LogWarning("Queue {QueueName} does not exist, cannot delete", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete queue {QueueName}", queueName);
            throw;
        }
    }

    private async Task<QueueConfiguration> CreateDeadLetterQueueAsync(string dlqName, bool isFifo, CancellationToken cancellationToken)
    {
        var dlqName_actual = isFifo ? $"{dlqName}.fifo" : dlqName;

        try
        {
            var getQueueUrlResponse = await _sqsClient.GetQueueUrlAsync(dlqName_actual, cancellationToken);
            return new QueueConfiguration
            {
                QueueName = dlqName_actual,
                QueueUrl = getQueueUrlResponse.QueueUrl,
                IsFifo = isFifo
            };
        }
        catch (QueueDoesNotExistException)
        {
            // Create the DLQ
            var createDlqRequest = new CreateQueueRequest
            {
                QueueName = dlqName_actual,
                Attributes = new Dictionary<string, string>
                {
                    [QueueAttributeName.MessageRetentionPeriod] = "1209600", // 14 days
                    [QueueAttributeName.VisibilityTimeout] = "30"
                }
            };

            if (isFifo)
            {
                createDlqRequest.Attributes[QueueAttributeName.FifoQueue] = "true";
                createDlqRequest.Attributes[QueueAttributeName.ContentBasedDeduplication] = "true";
            }

            var createDlqResponse = await _sqsClient.CreateQueueAsync(createDlqRequest, cancellationToken);
            _logger.LogInformation("Created dead letter queue {QueueName} with URL: {QueueUrl}", dlqName_actual, createDlqResponse.QueueUrl);

            return new QueueConfiguration
            {
                QueueName = dlqName_actual,
                QueueUrl = createDlqResponse.QueueUrl,
                IsFifo = isFifo
            };
        }
    }

    private async Task ConfigureDeadLetterQueueAsync(string queueUrl, string dlqUrl, int maxReceiveCount, CancellationToken cancellationToken)
    {
        // Get DLQ ARN
        var getDlqAttributesResponse = await _sqsClient.GetQueueAttributesAsync(new GetQueueAttributesRequest
        {
            QueueUrl = dlqUrl,
            AttributeNames = new List<string> { QueueAttributeName.QueueArn }
        }, cancellationToken);

        var dlqArn = getDlqAttributesResponse.Attributes[QueueAttributeName.QueueArn];

        // Configure redrive policy
        var redrivePolicy = System.Text.Json.JsonSerializer.Serialize(new
        {
            deadLetterTargetArn = dlqArn,
            maxReceiveCount = maxReceiveCount
        });

        await _sqsClient.SetQueueAttributesAsync(new SetQueueAttributesRequest
        {
            QueueUrl = queueUrl,
            Attributes = new Dictionary<string, string>
            {
                [QueueAttributeName.RedrivePolicy] = redrivePolicy
            }
        }, cancellationToken);

        _logger.LogInformation("Configured dead letter queue for queue {QueueUrl} with DLQ {DlqUrl}", queueUrl, dlqUrl);
    }

    private Dictionary<string, string> CreateQueueAttributes(QueueOptions queueOptions)
    {
        var attributes = new Dictionary<string, string>
        {
            [QueueAttributeName.VisibilityTimeout] = queueOptions.VisibilityTimeoutSeconds.ToString(),
            [QueueAttributeName.MessageRetentionPeriod] = queueOptions.MessageRetentionPeriodSeconds.ToString(),
            [QueueAttributeName.ReceiveMessageWaitTimeSeconds] = "20" // Enable long polling
        };

        if (queueOptions.IsFifo)
        {
            attributes[QueueAttributeName.FifoQueue] = "true";
            if (queueOptions.ContentBasedDeduplication)
            {
                attributes[QueueAttributeName.ContentBasedDeduplication] = "true";
            }
        }

        return attributes;
    }
}