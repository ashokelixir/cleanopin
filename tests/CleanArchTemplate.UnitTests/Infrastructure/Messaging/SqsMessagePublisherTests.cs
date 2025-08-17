using Amazon.SQS;
using Amazon.SQS.Model;
using CleanArchTemplate.Application.Common.Messages;
using CleanArchTemplate.Infrastructure.Messaging;
using CleanArchTemplate.Shared.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Polly;
using System.Text.Json;

namespace CleanArchTemplate.UnitTests.Infrastructure.Messaging;

public class SqsMessagePublisherTests
{
    private readonly Mock<IAmazonSQS> _mockSqsClient;
    private readonly Mock<IOptions<MessagingOptions>> _mockOptions;
    private readonly Mock<ILogger<SqsMessagePublisher>> _mockLogger;
    private readonly ResiliencePipeline _resiliencePipeline;
    private readonly SqsMessagePublisher _publisher;
    private readonly MessagingOptions _messagingOptions;

    public SqsMessagePublisherTests()
    {
        _mockSqsClient = new Mock<IAmazonSQS>();
        _mockOptions = new Mock<IOptions<MessagingOptions>>();
        _mockLogger = new Mock<ILogger<SqsMessagePublisher>>();
        
        _resiliencePipeline = new ResiliencePipelineBuilder()
            .AddRetry(new Polly.Retry.RetryStrategyOptions
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromMilliseconds(100)
            })
            .Build();

        _messagingOptions = new MessagingOptions
        {
            AwsRegion = "us-east-1",
            Queues = new Dictionary<string, QueueOptions>
            {
                ["test-queue"] = new QueueOptions { Name = "test-queue" }
            }
        };

        _mockOptions.Setup(x => x.Value).Returns(_messagingOptions);

        _publisher = new SqsMessagePublisher(
            _mockSqsClient.Object,
            _mockOptions.Object,
            _mockLogger.Object,
            _resiliencePipeline);
    }

    [Fact]
    public async Task PublishAsync_ShouldPublishMessage_WhenValidMessageProvided()
    {
        // Arrange
        var message = new UserCreatedMessage
        {
            UserId = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };

        var queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue";
        var messageId = "test-message-id";

        _mockSqsClient.Setup(x => x.GetQueueUrlAsync("test-queue", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueUrlResponse { QueueUrl = queueUrl });

        _mockSqsClient.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse { MessageId = messageId });

        // Act
        var result = await _publisher.PublishAsync(message, "test-queue");

        // Assert
        result.Should().Be(messageId);

        _mockSqsClient.Verify(x => x.SendMessageAsync(
            It.Is<SendMessageRequest>(req => 
                req.QueueUrl == queueUrl &&
                req.MessageBody.Contains(message.Email) &&
                req.MessageAttributes.ContainsKey("MessageType")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishBatchAsync_ShouldPublishMessages_WhenValidMessagesProvided()
    {
        // Arrange
        var messages = new List<UserCreatedMessage>
        {
            new() { UserId = Guid.NewGuid(), Email = "test1@example.com", FirstName = "Test1", LastName = "User" },
            new() { UserId = Guid.NewGuid(), Email = "test2@example.com", FirstName = "Test2", LastName = "User" }
        };

        var queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue";
        var messageIds = new List<string> { "msg-1", "msg-2" };

        _mockSqsClient.Setup(x => x.GetQueueUrlAsync("test-queue", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueUrlResponse { QueueUrl = queueUrl });

        _mockSqsClient.Setup(x => x.SendMessageBatchAsync(It.IsAny<SendMessageBatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageBatchResponse
            {
                Successful = messageIds.Select((id, index) => new SendMessageBatchResultEntry
                {
                    Id = $"msg_{index}",
                    MessageId = id
                }).ToList(),
                Failed = new List<BatchResultErrorEntry>()
            });

        // Act
        var result = await _publisher.PublishBatchAsync(messages, "test-queue");

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(messageIds);

        _mockSqsClient.Verify(x => x.SendMessageBatchAsync(
            It.Is<SendMessageBatchRequest>(req => 
                req.QueueUrl == queueUrl &&
                req.Entries.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishToFifoAsync_ShouldPublishFifoMessage_WhenValidMessageProvided()
    {
        // Arrange
        var message = new UserCreatedMessage
        {
            UserId = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };

        var queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue.fifo";
        var messageId = "test-message-id";
        var messageGroupId = "test-group";
        var deduplicationId = "test-dedup-id";

        _mockSqsClient.Setup(x => x.GetQueueUrlAsync("test-queue", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueUrlResponse { QueueUrl = queueUrl });

        _mockSqsClient.Setup(x => x.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageResponse { MessageId = messageId });

        // Act
        var result = await _publisher.PublishToFifoAsync(message, "test-queue", messageGroupId, deduplicationId);

        // Assert
        result.Should().Be(messageId);

        _mockSqsClient.Verify(x => x.SendMessageAsync(
            It.Is<SendMessageRequest>(req => 
                req.QueueUrl == queueUrl &&
                req.MessageGroupId == messageGroupId &&
                req.MessageDeduplicationId == deduplicationId &&
                req.MessageBody.Contains(message.Email)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_ShouldThrowException_WhenSqsClientFails()
    {
        // Arrange
        var message = new UserCreatedMessage
        {
            UserId = Guid.NewGuid(),
            Email = "test@example.com"
        };

        _mockSqsClient.Setup(x => x.GetQueueUrlAsync("test-queue", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonSQSException("Queue not found"));

        // Act & Assert
        await _publisher.Invoking(p => p.PublishAsync(message, "test-queue"))
            .Should().ThrowAsync<AmazonSQSException>()
            .WithMessage("Queue not found");
    }

    [Fact]
    public async Task PublishBatchAsync_ShouldReturnEmpty_WhenNoMessagesProvided()
    {
        // Arrange
        var messages = new List<UserCreatedMessage>();

        // Act
        var result = await _publisher.PublishBatchAsync(messages, "test-queue");

        // Assert
        result.Should().BeEmpty();

        _mockSqsClient.Verify(x => x.SendMessageBatchAsync(
            It.IsAny<SendMessageBatchRequest>(), 
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PublishBatchAsync_ShouldHandleLargeBatches_WhenMoreThan10MessagesProvided()
    {
        // Arrange
        var messages = Enumerable.Range(1, 25)
            .Select(i => new UserCreatedMessage
            {
                UserId = Guid.NewGuid(),
                Email = $"test{i}@example.com",
                FirstName = $"Test{i}",
                LastName = "User"
            })
            .ToList();

        var queueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue";

        _mockSqsClient.Setup(x => x.GetQueueUrlAsync("test-queue", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetQueueUrlResponse { QueueUrl = queueUrl });

        _mockSqsClient.Setup(x => x.SendMessageBatchAsync(It.IsAny<SendMessageBatchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SendMessageBatchRequest req, CancellationToken ct) => new SendMessageBatchResponse
            {
                Successful = req.Entries.Select(entry => new SendMessageBatchResultEntry
                {
                    Id = entry.Id,
                    MessageId = $"msg-{entry.Id}"
                }).ToList(),
                Failed = new List<BatchResultErrorEntry>()
            });

        // Act
        var result = await _publisher.PublishBatchAsync(messages, "test-queue");

        // Assert
        result.Should().HaveCount(25);

        // Should make 3 batch calls (10 + 10 + 5)
        _mockSqsClient.Verify(x => x.SendMessageBatchAsync(
            It.IsAny<SendMessageBatchRequest>(), 
            It.IsAny<CancellationToken>()), Times.Exactly(3));
    }
}