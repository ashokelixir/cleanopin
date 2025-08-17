using CleanArchTemplate.Application.Common.Interfaces;
using CleanArchTemplate.Application.Common.Messages;
using CleanArchTemplate.Infrastructure.Messaging;
using CleanArchTemplate.Shared.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CleanArchTemplate.IntegrationTests.Infrastructure.Messaging;

[Collection("Integration")]
public class MessagingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly IServiceScope _scope;

    public MessagingIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _scope = _factory.Services.CreateScope();
    }

    [Fact]
    public async Task QueueManager_ShouldCreateQueues_WhenConfigured()
    {
        // Arrange
        var queueManager = _scope.ServiceProvider.GetRequiredService<SqsQueueManager>();

        // Act
        var queueConfigurations = await queueManager.CreateAllQueuesAsync();

        // Assert
        queueConfigurations.Should().NotBeEmpty();
        queueConfigurations.Should().ContainKey("user-events");
        queueConfigurations.Should().ContainKey("permission-events");
        queueConfigurations.Should().ContainKey("audit-events");

        // Verify FIFO queue configuration
        var auditQueue = queueConfigurations["audit-events"];
        auditQueue.IsFifo.Should().BeTrue();
        auditQueue.QueueName.Should().EndWith(".fifo");
    }

    [Fact]
    public async Task MessagePublisher_ShouldPublishMessage_WhenValidMessageProvided()
    {
        // Arrange
        var publisher = _scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
        var queueManager = _scope.ServiceProvider.GetRequiredService<SqsQueueManager>();

        // Ensure queues exist
        await queueManager.CreateAllQueuesAsync();

        var message = new UserCreatedMessage
        {
            UserId = Guid.NewGuid(),
            Email = "integration-test@example.com",
            FirstName = "Integration",
            LastName = "Test",
            IsEmailVerified = false,
            Roles = new List<string> { "User" }
        };

        // Act
        var messageId = await publisher.PublishAsync(message, "user-events");

        // Assert
        messageId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task MessagePublisher_ShouldPublishBatchMessages_WhenValidMessagesProvided()
    {
        // Arrange
        var publisher = _scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
        var queueManager = _scope.ServiceProvider.GetRequiredService<SqsQueueManager>();

        // Ensure queues exist
        await queueManager.CreateAllQueuesAsync();

        var messages = new List<UserCreatedMessage>
        {
            new()
            {
                UserId = Guid.NewGuid(),
                Email = "batch1@example.com",
                FirstName = "Batch1",
                LastName = "Test"
            },
            new()
            {
                UserId = Guid.NewGuid(),
                Email = "batch2@example.com",
                FirstName = "Batch2",
                LastName = "Test"
            }
        };

        // Act
        var messageIds = await publisher.PublishBatchAsync(messages, "user-events");

        // Assert
        messageIds.Should().HaveCount(2);
        messageIds.Should().AllSatisfy(id => id.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task MessagePublisher_ShouldPublishFifoMessage_WhenValidMessageProvided()
    {
        // Arrange
        var publisher = _scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
        var queueManager = _scope.ServiceProvider.GetRequiredService<SqsQueueManager>();

        // Ensure queues exist
        await queueManager.CreateAllQueuesAsync();

        var message = new UserCreatedMessage
        {
            UserId = Guid.NewGuid(),
            Email = "fifo-test@example.com",
            FirstName = "FIFO",
            LastName = "Test",
            IsEmailVerified = true,
            Roles = new List<string> { "User" }
        };

        var messageGroupId = "integration-test-group";
        var deduplicationId = Guid.NewGuid().ToString();

        // Act
        var messageId = await publisher.PublishToFifoAsync(message, "audit-events", messageGroupId, deduplicationId);

        // Assert
        messageId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task MessageConsumer_ShouldConsumeMessages_WhenMessagesArePublished()
    {
        // Arrange
        var publisher = _scope.ServiceProvider.GetRequiredService<IMessagePublisher>();
        var consumer = _scope.ServiceProvider.GetRequiredService<IMessageConsumer>();
        var queueManager = _scope.ServiceProvider.GetRequiredService<SqsQueueManager>();

        // Ensure queues exist
        await queueManager.CreateAllQueuesAsync();

        var receivedMessages = new List<UserCreatedMessage>();
        var messageReceivedEvent = new ManualResetEventSlim(false);

        // Start consumer
        await consumer.StartConsumingAsync<UserCreatedMessage>("user-events", async (message, ct) =>
        {
            receivedMessages.Add(message);
            messageReceivedEvent.Set();
            await Task.CompletedTask;
        });

        var testMessage = new UserCreatedMessage
        {
            UserId = Guid.NewGuid(),
            Email = "consumer-test@example.com",
            FirstName = "Consumer",
            LastName = "Test",
            IsEmailVerified = false,
            Roles = new List<string> { "User" }
        };

        // Act
        await publisher.PublishAsync(testMessage, "user-events");

        // Wait for message to be consumed (with timeout)
        var messageReceived = messageReceivedEvent.Wait(TimeSpan.FromSeconds(30));

        // Cleanup
        await consumer.StopConsumingAsync("user-events");

        // Assert
        messageReceived.Should().BeTrue("Message should be consumed within timeout period");
        receivedMessages.Should().HaveCount(1);
        receivedMessages[0].Email.Should().Be(testMessage.Email);
        receivedMessages[0].UserId.Should().Be(testMessage.UserId);
    }

    [Fact]
    public void MessagingOptions_ShouldBeConfiguredCorrectly()
    {
        // Arrange & Act
        var options = _scope.ServiceProvider.GetRequiredService<IOptions<MessagingOptions>>();

        // Assert
        options.Value.Should().NotBeNull();
        options.Value.AwsRegion.Should().NotBeNullOrEmpty();
        options.Value.Queues.Should().NotBeEmpty();
        options.Value.Queues.Should().ContainKey("user-events");
        options.Value.Queues.Should().ContainKey("permission-events");
        options.Value.Queues.Should().ContainKey("audit-events");

        // Verify FIFO queue configuration
        var auditQueue = options.Value.Queues["audit-events"];
        auditQueue.IsFifo.Should().BeTrue();
        auditQueue.ContentBasedDeduplication.Should().BeTrue();
    }

    public void Dispose()
    {
        _scope?.Dispose();
    }
}