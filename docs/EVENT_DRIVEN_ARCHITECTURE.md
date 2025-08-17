# Event-Driven Architecture

This document describes the complete event-driven architecture implementation in CleanArchTemplate, including domain events, event handlers, and SQS messaging integration.

## Architecture Overview

The system implements a comprehensive event-driven architecture with the following components:

1. **Domain Events** - Raised by domain entities when state changes occur
2. **Event Handlers** - Process domain events for cross-cutting concerns
3. **Message Publishing** - Publishes events to SQS queues for external systems
4. **Message Consumption** - Processes messages from SQS queues
5. **Message Handlers** - Handle specific message types from queues

## Domain Events

Domain events are raised by entities when important business events occur:

### User Events
- `UserCreatedEvent` - When a new user is created
- `UserProfileUpdatedEvent` - When user profile is updated
- `UserEmailUpdatedEvent` - When user email is changed
- `UserEmailVerifiedEvent` - When user email is verified
- `UserPasswordUpdatedEvent` - When user password is changed
- `UserActivatedEvent` - When user is activated
- `UserDeactivatedEvent` - When user is deactivated
- `UserRoleAssignedEvent` - When role is assigned to user
- `UserRoleRemovedEvent` - When role is removed from user

### Permission Events
- `PermissionCreatedEvent` - When a new permission is created
- `PermissionUpdatedEvent` - When permission details are updated
- `PermissionActivatedEvent` - When permission is activated
- `PermissionDeactivatedEvent` - When permission is deactivated
- `UserPermissionAssignedEvent` - When permission is assigned to user
- `UserPermissionRemovedEvent` - When permission is removed from user
- `UserPermissionUpdatedEvent` - When user permission is modified

### Role Events
- `RoleCreatedEvent` - When a new role is created
- `RoleUpdatedEvent` - When role details are updated
- `RoleActivatedEvent` - When role is activated
- `RoleDeactivatedEvent` - When role is deactivated
- `RolePermissionAssignedEvent` - When permission is assigned to role
- `RolePermissionRemovedEvent` - When permission is removed from role

## Event Handlers

The system has multiple types of event handlers for different concerns:

### Audit Event Handlers
- `PermissionAuditEventHandler` - Creates audit log entries for permission changes
- Located in: `src/CleanArchTemplate.Application/Features/Permissions/EventHandlers/`

### Messaging Event Handlers
- `PermissionMessagingEventHandler` - Publishes permission events to SQS
- `UserMessagingEventHandler` - Publishes user events to SQS
- `RoleMessagingEventHandler` - Publishes role events to SQS

## SQS Integration

### Queue Structure

The system uses separate SQS queues for different event types:

- `user-events` - User lifecycle events
- `permission-events` - Permission lifecycle events
- `user-permission-events` - User-permission relationship events
- `role-events` - Role lifecycle events
- `role-permission-events` - Role-permission relationship events
- `user-role-events` - User-role relationship events

### Message Types

Each queue processes specific message types:

#### User Events Queue
- `UserCreatedMessage`
- `UserProfileUpdatedMessage`
- `UserEmailUpdatedMessage`
- `UserEmailVerifiedMessage`
- `UserPasswordChangedMessage`
- `UserActivatedMessage`
- `UserDeactivatedMessage`

#### Permission Events Queue
- `PermissionCreatedMessage`
- `PermissionUpdatedMessage`
- `PermissionStateChangedMessage`

#### User Permission Events Queue
- `UserPermissionAssignedMessage`
- `UserPermissionRemovedMessage`
- `UserPermissionUpdatedMessage`

#### Role Events Queue
- `RoleCreatedMessage`
- `RoleUpdatedMessage`
- `RoleStateChangedMessage`

#### Role Permission Events Queue
- `RolePermissionAssignedMessage`
- `RolePermissionRemovedMessage`

#### User Role Events Queue
- `UserRoleAssignedMessage`
- `UserRoleRemovedMessage`

## Message Handlers

Message handlers process messages from SQS queues:

- `UserMessageHandler` - Processes user-related messages
- `PermissionMessageHandler` - Processes permission-related messages

## Configuration

### Messaging Configuration

Configure messaging in `appsettings.json`:

```json
{
  "Messaging": {
    "AwsRegion": "us-east-1",
    "LocalStackEndpoint": "http://localhost:4566",
    "QueueConfigurations": [
      {
        "QueueName": "user-events",
        "MessageRetentionPeriod": 1209600,
        "VisibilityTimeout": 30,
        "MaxReceiveCount": 3,
        "DeadLetterQueueName": "user-events-dlq"
      },
      {
        "QueueName": "permission-events",
        "MessageRetentionPeriod": 1209600,
        "VisibilityTimeout": 30,
        "MaxReceiveCount": 3,
        "DeadLetterQueueName": "permission-events-dlq"
      }
    ],
    "RetryPolicy": {
      "MaxRetryAttempts": 3,
      "InitialDelayMs": 1000,
      "MaxDelayMs": 30000
    }
  }
}
```

### Queue Management

The `SqsQueueManager` handles:
- Queue creation and configuration
- Dead letter queue setup
- Queue attribute management
- Health checks

## Event Flow

1. **Domain Event Raised**: Entity raises domain event when state changes
2. **Event Dispatching**: `DomainEventDispatcher` publishes event to MediatR
3. **Event Handling**: Multiple handlers process the same event:
   - Audit handler creates audit log entries
   - Messaging handler publishes message to SQS
4. **Message Processing**: Background service consumes messages from SQS
5. **Message Handling**: Specific message handlers process messages

## Error Handling

### Event Handler Resilience
- Event handlers catch exceptions and log errors
- Messaging failures don't break the main business flow
- Audit logging failures are logged but don't stop processing

### Message Processing Resilience
- Dead letter queues for failed messages
- Configurable retry policies
- Circuit breaker patterns for external dependencies

## Testing

### Unit Tests
- Event handler tests verify message publishing
- Mock dependencies for isolated testing
- Test error scenarios and resilience

### Integration Tests
- End-to-end event flow testing
- SQS integration testing with LocalStack
- Message processing verification

## Monitoring

### Logging
- Structured logging with Serilog
- Event processing metrics
- Error tracking and alerting

### Health Checks
- SQS queue health monitoring
- Message processing health checks
- Dead letter queue monitoring

## Best Practices

1. **Event Naming**: Use past tense for events (e.g., `UserCreated`, not `CreateUser`)
2. **Event Immutability**: Events should be immutable once created
3. **Event Versioning**: Consider versioning for message schema evolution
4. **Idempotency**: Message handlers should be idempotent
5. **Error Handling**: Always handle exceptions in event handlers
6. **Testing**: Test both success and failure scenarios
7. **Monitoring**: Monitor queue depths and processing times

## Future Enhancements

1. **Event Sourcing**: Store events as the source of truth
2. **CQRS Read Models**: Build read models from events
3. **Saga Pattern**: Implement distributed transactions
4. **Event Replay**: Ability to replay events for recovery
5. **Schema Registry**: Centralized schema management
6. **Message Encryption**: Encrypt sensitive message content