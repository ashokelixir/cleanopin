# Docker Messaging Setup

This document explains the Docker configuration for the event-driven messaging system using LocalStack SQS.

## Overview

The application uses AWS SQS for event-driven messaging, with LocalStack providing SQS emulation for local development. When domain events occur (user creation, permission changes, etc.), they are automatically published to appropriate SQS queues.

## SQS Queue Structure

### Event Queues

| Queue Name | Purpose | Message Types |
|------------|---------|---------------|
| `user-events` | User lifecycle events | UserCreated, UserUpdated, UserActivated, etc. |
| `permission-events` | Permission lifecycle events | PermissionCreated, PermissionUpdated, etc. |
| `user-permission-events` | User-permission relationships | UserPermissionAssigned, UserPermissionRemoved |
| `role-events` | Role lifecycle events | RoleCreated, RoleUpdated, RoleActivated, etc. |
| `role-permission-events` | Role-permission relationships | RolePermissionAssigned, RolePermissionRemoved |
| `user-role-events` | User-role relationships | UserRoleAssigned, UserRoleRemoved |
| `audit-events.fifo` | Audit trail events | Ordered audit events for compliance |

### Dead Letter Queues

Each main queue has a corresponding DLQ for failed message processing:
- `user-events-dlq`
- `permission-events-dlq`
- `user-permission-events-dlq`
- `role-events-dlq`
- `role-permission-events-dlq`
- `user-role-events-dlq`
- `audit-events-dlq.fifo`

## LocalStack Configuration

### Services Enabled
```yaml
environment:
  - SERVICES=sqs,secretsmanager,s3
  - DEBUG=0
  - DATA_DIR=/tmp/localstack/data
  - PERSISTENCE=1
```

### Initialization Script
The `scripts/localstack-init.sh` script automatically:
1. Creates all required SQS queues
2. Sets up dead letter queue policies
3. Configures queue attributes (retention, visibility timeout, etc.)
4. Creates S3 buckets and Secrets Manager secrets

## Application Configuration

### Environment Variables
```yaml
# In docker-compose.yml
environment:
  - Messaging__AwsRegion=us-east-1
  - Messaging__LocalStackEndpoint=http://localstack:4566
```

### Queue Configuration
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
        "MessageRetentionPeriodSeconds": 1209600
      }
      // ... other queues
    }
  }
}
```

## Event Flow

1. **Domain Event Raised** - Entity raises event (e.g., `UserCreatedEvent`)
2. **Event Handler Triggered** - MediatR dispatches to multiple handlers:
   - `PermissionAuditEventHandler` - Creates audit logs
   - `PermissionMessagingEventHandler` - Publishes to SQS
3. **Message Published** - Event converted to message and sent to appropriate queue
4. **Background Processing** - `MessageConsumerService` processes messages
5. **Message Handling** - Specific handlers process different message types

## Testing the System

### Using the Test Script
```powershell
# Run comprehensive messaging test
.\scripts\test-messaging.ps1

# Check specific queue
awslocal sqs get-queue-attributes --queue-url http://localhost:4566/000000000000/user-events --attribute-names All
```

### Manual Testing
```bash
# List all queues
awslocal sqs list-queues --endpoint-url http://localhost:4566

# Check queue message count
awslocal sqs get-queue-attributes \
  --queue-url http://localhost:4566/000000000000/user-events \
  --attribute-names ApproximateNumberOfMessages

# Receive messages (for debugging)
awslocal sqs receive-message \
  --queue-url http://localhost:4566/000000000000/user-events \
  --max-number-of-messages 10
```

## Monitoring

### Health Checks
- LocalStack health: `http://localhost:4566/_localstack/health`
- Application health: `http://localhost:8080/health`
- SQS-specific health: `http://localhost:8080/health/sqs`

### Logging
- Application logs show event handler execution
- Seq (http://localhost:5341) provides structured log analysis
- LocalStack logs show SQS operations

### Queue Monitoring
```bash
# Monitor queue depths
watch -n 5 'awslocal sqs get-queue-attributes --queue-url http://localhost:4566/000000000000/user-events --attribute-names ApproximateNumberOfMessages'

# Check dead letter queues
awslocal sqs get-queue-attributes \
  --queue-url http://localhost:4566/000000000000/user-events-dlq \
  --attribute-names ApproximateNumberOfMessages
```

## Troubleshooting

### Common Issues

1. **LocalStack not starting**
   ```bash
   # Check LocalStack logs
   docker-compose logs localstack
   
   # Restart LocalStack
   docker-compose restart localstack
   ```

2. **Queues not created**
   ```bash
   # Check initialization script execution
   docker-compose logs localstack | grep "init-aws.sh"
   
   # Manually run initialization
   docker-compose exec localstack /etc/localstack/init/ready.d/init-aws.sh
   ```

3. **Messages not being processed**
   ```bash
   # Check consumer service logs
   docker-compose logs api | grep MessageConsumer
   
   # Verify queue configuration
   awslocal sqs get-queue-attributes --queue-url http://localhost:4566/000000000000/user-events --attribute-names All
   ```

4. **Connection issues**
   ```bash
   # Test LocalStack connectivity
   curl http://localhost:4566/_localstack/health
   
   # Check network connectivity from container
   docker-compose exec api ping localstack
   ```

### Performance Tuning

1. **Consumer Configuration**
   ```json
   {
     "Consumer": {
       "MaxMessages": 10,
       "WaitTimeSeconds": 20,
       "ConcurrentConsumers": 2,
       "PollingIntervalMs": 2000
     }
   }
   ```

2. **Queue Attributes**
   - Increase `VisibilityTimeoutSeconds` for long-running message processing
   - Adjust `MaxReceiveCount` based on retry requirements
   - Use FIFO queues for ordered processing when needed

## Production Considerations

When deploying to production:

1. **Replace LocalStack with AWS SQS**
   - Remove `LocalStackEndpoint` configuration
   - Configure AWS credentials (IAM roles recommended)
   - Update queue URLs to use AWS SQS format

2. **Security**
   - Use IAM roles instead of access keys
   - Enable SQS encryption at rest
   - Configure VPC endpoints for private communication

3. **Monitoring**
   - Set up CloudWatch alarms for queue depths
   - Monitor dead letter queues
   - Track message processing latency

4. **Scaling**
   - Increase consumer concurrency based on load
   - Use multiple application instances
   - Consider SQS FIFO queues for ordering requirements