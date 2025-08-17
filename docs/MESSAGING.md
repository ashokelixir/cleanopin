# AWS SQS Messaging Infrastructure

This document describes the AWS SQS messaging infrastructure implementation in the Clean Architecture Template.

## Overview

The messaging infrastructure provides asynchronous message publishing and consumption capabilities using AWS SQS (Simple Queue Service). It supports both standard and FIFO queues, dead letter queues, retry mechanisms, and proper error handling.

## Architecture

### Components

1. **Message Publisher** (`IMessagePublisher`) - Publishes messages to SQS queues
2. **Message Consumer** (`IMessageConsumer`) - Consumes messages from SQS queues
3. **Queue Manager** (`SqsQueueManager`) - Manages queue creation and configuration
4. **Message Handlers** - Process specific message types
5. **Background Service** (`MessageConsumerService`) - Starts and manages message consumers

### Message Flow

```
Application → IMessagePublisher → SQS Queue → IMessageConsumer → Message Handler
```

## Configuration

### appsettings.json

```json
{
  "Messaging": {
    "AwsRegion": "us-east-1",
    "LocalStackEndpoint": "http://localhost:4566",
    "Queues": {
      "user-events": {
        "Name": "user-events",
        "IsFifo": false,
        "DeadLetterQueueName": "user-events-dlq",
        "MaxReceiveCount": 3,
        "VisibilityTimeoutSeconds": 30,
        "MessageRetentionPeriodSeconds": 1209600,
        "ContentBasedDeduplication": false
      },
      "permission-events": {
        "Name": "permission-events",
        "IsFifo": false,
        "DeadLetterQueueName": "permission-events-dlq",
        "MaxReceiveCount": 3,
        "VisibilityTimeoutSeconds": 30,
        "MessageRetentionPeriodSeconds": 1209600,
        "ContentBasedDeduplication": false
      },
      "audit-events": {
        "Name": "audit-events",
        "IsFifo": true,
        "DeadLetterQueueName": "audit-events-dlq",
        "MaxReceiveCount": 5,
        "VisibilityTimeoutSeconds": 60,
        "MessageRetentionPeriodSeconds": 1209600,
        "ContentBasedDeduplication": true
      }
    },
    "RetryPolicy": {
      "MaxRetryAttempts": 3,
      "InitialDelayMs": 1000,
      "MaxDelayMs": 30000,
      "BackoffMultiplier": 2.0
    },
    "Consumer": {
      "MaxMessages": 10,
      "WaitTimeSeconds": 20,
      "ConcurrentConsumers": 1,
      "PollingIntervalMs": 5000
    }
  }
}
```

## Usage

### Publishing Messages

```csharp
public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<UserDto>>
{
    private readonly IMessagePublisher _messagePublisher;

    public async Task<Result<UserDto>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // ... create user logic ...

        // Publish user created message
        var userCreatedMessage = new UserCreatedMessage
        {
            UserId = user.Id,
            Email = user.Email.Value,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsEmailVerified = user.IsEmailVerified,
            Roles = new List<string>()
        };

        await _messagePublisher.PublishAsync(userCreatedMessage, "user-events", cancellationToken);

        return Result<UserDto>.Success(userDto);
    }
}
```

### Batch Publishing

```csharp
var messages = new List<UserCreatedMessage> { /* ... */ };
var messageIds = await _messagePublisher.PublishBatchAsync(messages, "user-events");
```

### FIFO Publishing

```csharp
var messageId = await _messagePublisher.PublishToFifoAsync(
    message, 
    "audit-events", 
    messageGroupId: "user-operations", 
    deduplicationId: Guid.NewGuid().ToString());
```

### Message Handlers

```csharp
public class UserMessageHandler
{
    public async Task HandleUserCreatedAsync(UserCreatedMessage message, CancellationToken cancellationToken = default)
    {
        // Process user created event
        // - Send welcome email
        // - Create user profile in external systems
        // - Initialize user preferences
        // - Trigger analytics events
    }
}
```

## Message Types

### User Messages

- `UserCreatedMessage` - Published when a user is created
- `UserUpdatedMessage` - Published when a user is updated
- `UserDeletedMessage` - Published when a user is deleted

### Permission Messages

- `PermissionAssignedMessage` - Published when a permission is assigned to a user
- `PermissionRemovedMessage` - Published when a permission is removed from a user
- `BulkPermissionsAssignedMessage` - Published when permissions are bulk assigned

## Queue Types

### Standard Queues

- **user-events** - User lifecycle events
- **permission-events** - Permission change events

### FIFO Queues

- **audit-events** - Audit events requiring ordered processing

## Dead Letter Queues

All queues are configured with dead letter queues (DLQ) to handle failed messages:

- Messages are sent to DLQ after exceeding the maximum receive count
- DLQ messages can be inspected and reprocessed manually
- Separate DLQ for each main queue

## Error Handling

### Retry Mechanism

- Exponential backoff retry policy
- Configurable retry attempts and delays
- Circuit breaker pattern for external dependencies

### Message Visibility

- Messages become invisible during processing
- Visibility timeout prevents duplicate processing
- Failed messages become visible again for retry

### Resilience

- Polly resilience framework integration
- Timeout policies for message operations
- Graceful degradation on failures

## Development Setup

### LocalStack

For local development, the template uses LocalStack to simulate AWS services:

```bash
# Start LocalStack with Docker Compose
docker-compose up localstack

# Queues are automatically created via initialization script
# scripts/localstack-init.sh
```

### Queue Management

```csharp
// Create all configured queues
var queueManager = serviceProvider.GetRequiredService<SqsQueueManager>();
var queueConfigurations = await queueManager.CreateAllQueuesAsync();
```

## Testing

### Unit Tests

- Message publisher tests with mocked SQS client
- Message handler tests with mocked dependencies
- Queue manager tests for configuration validation

### Integration Tests

- End-to-end message publishing and consumption
- Queue creation and configuration
- Error handling and retry scenarios

## Monitoring

### Logging

- Structured logging with Serilog
- Correlation IDs for message tracing
- Performance metrics for message processing

### Health Checks

- SQS connectivity health checks
- Queue depth monitoring
- Message age alerting

## Security

### IAM Permissions

Required IAM permissions for SQS operations:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "sqs:SendMessage",
        "sqs:SendMessageBatch",
        "sqs:ReceiveMessage",
        "sqs:DeleteMessage",
        "sqs:GetQueueAttributes",
        "sqs:GetQueueUrl",
        "sqs:CreateQueue",
        "sqs:SetQueueAttributes"
      ],
      "Resource": "arn:aws:sqs:*:*:*"
    }
  ]
}
```

### Message Encryption

- Server-side encryption with AWS KMS (configurable)
- In-transit encryption with HTTPS
- Message attribute encryption for sensitive data

## Best Practices

1. **Message Design**
   - Keep messages small and focused
   - Include correlation IDs for tracing
   - Use versioning for message schema evolution

2. **Error Handling**
   - Implement idempotent message handlers
   - Use dead letter queues for poison messages
   - Log all message processing errors

3. **Performance**
   - Use batch operations when possible
   - Configure appropriate visibility timeouts
   - Monitor queue depth and processing times

4. **Reliability**
   - Implement proper retry mechanisms
   - Use FIFO queues for ordered processing
   - Handle duplicate messages gracefully

## Troubleshooting

### Common Issues

1. **Messages not being consumed**
   - Check consumer service is running
   - Verify queue configuration
   - Check IAM permissions

2. **Messages going to DLQ**
   - Review message handler exceptions
   - Check visibility timeout settings
   - Verify message format

3. **High message latency**
   - Monitor queue depth
   - Check consumer concurrency settings
   - Review message processing time

### Debugging

- Enable debug logging for messaging components
- Use AWS CloudWatch for queue metrics
- Monitor application performance counters