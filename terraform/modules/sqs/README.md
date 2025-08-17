# SQS Module

This Terraform module creates AWS SQS (Simple Queue Service) resources for the Clean Architecture Template messaging infrastructure.

## Features

- **Standard Queues**: For user events and permission events
- **FIFO Queues**: For audit events requiring ordered processing
- **Dead Letter Queues**: Automatic DLQ creation for all main queues
- **Server-Side Encryption**: Optional SSE with AWS managed keys or customer managed KMS keys
- **CloudWatch Alarms**: Monitoring for queue depth, message age, and DLQ messages
- **Long Polling**: Enabled by default for cost optimization
- **Configurable Timeouts**: Different visibility timeouts for different queue types

## Queue Architecture

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   user-events   │───▶│ user-events-dlq │    │ permission-     │
│   (Standard)    │    │   (Standard)    │    │   events        │
└─────────────────┘    └─────────────────┘    │ (Standard)      │
                                              └─────────────────┘
                                                       │
                                                       ▼
                                              ┌─────────────────┐
                                              │ permission-     │
                                              │ events-dlq      │
                                              │ (Standard)      │
                                              └─────────────────┘

┌─────────────────┐    ┌─────────────────┐
│  audit-events   │───▶│ audit-events-   │
│    (FIFO)       │    │   dlq (FIFO)    │
└─────────────────┘    └─────────────────┘
```

## Usage

```hcl
module "sqs" {
  source = "./modules/sqs"

  name_prefix = "cleanarch-dev"
  environment = "dev"

  # Queue Configuration
  message_retention_seconds     = 1209600  # 14 days
  dlq_message_retention_seconds = 1209600  # 14 days
  max_receive_count            = 3
  audit_events_max_receive_count = 5

  # Visibility Timeouts
  user_events_visibility_timeout       = 30
  permission_events_visibility_timeout = 30
  audit_events_visibility_timeout      = 60

  # Security
  enable_sse = true
  kms_key_id = null  # Use AWS managed keys

  # Monitoring
  enable_cloudwatch_alarms      = true
  queue_depth_alarm_threshold   = 100
  message_age_alarm_threshold   = 300  # 5 minutes
  dlq_messages_alarm_threshold  = 1
  alarm_actions                = []

  tags = {
    Environment = "dev"
    Project     = "cleanarch-template"
    Owner       = "platform-team"
  }
}
```

## Queue Types

### Standard Queues

1. **user-events**: Handles user lifecycle events
   - User created, updated, deleted messages
   - Default visibility timeout: 30 seconds
   - Max receive count: 3

2. **permission-events**: Handles permission change events
   - Permission assigned, removed, bulk operations
   - Default visibility timeout: 30 seconds
   - Max receive count: 3

### FIFO Queues

1. **audit-events**: Handles audit events requiring ordered processing
   - Audit log entries that must be processed in order
   - Default visibility timeout: 60 seconds
   - Max receive count: 5
   - Content-based deduplication enabled

## Dead Letter Queues

Each main queue has a corresponding dead letter queue:
- `user-events-dlq`
- `permission-events-dlq`
- `audit-events-dlq.fifo`

Messages are moved to DLQ after exceeding the maximum receive count.

## CloudWatch Alarms

The module creates the following CloudWatch alarms:

1. **Queue Depth Alarms**: Monitors `ApproximateNumberOfVisibleMessages`
2. **Message Age Alarms**: Monitors `ApproximateAgeOfOldestMessage`
3. **DLQ Message Alarms**: Monitors messages in dead letter queues

## Security

- Server-side encryption enabled by default using AWS managed keys
- Optional customer-managed KMS key support
- IAM policies can be applied using the output ARNs

## Monitoring

CloudWatch metrics are automatically available for:
- Queue depth
- Message age
- Number of messages sent/received
- Number of empty receives

## Environment-Specific Configuration

The module supports different configurations per environment:

- **Development**: Lower thresholds, shorter retention
- **Staging**: Production-like settings for testing
- **Production**: High availability, longer retention, stricter alarms

## Inputs

| Name | Description | Type | Default | Required |
|------|-------------|------|---------|:--------:|
| name_prefix | Prefix for resource names | `string` | n/a | yes |
| environment | Environment name | `string` | n/a | yes |
| message_retention_seconds | Message retention period | `number` | `1209600` | no |
| max_receive_count | Max receive count before DLQ | `number` | `3` | no |
| enable_sse | Enable server-side encryption | `bool` | `true` | no |
| enable_cloudwatch_alarms | Enable CloudWatch alarms | `bool` | `true` | no |

## Outputs

| Name | Description |
|------|-------------|
| all_queue_arns | List of all SQS queue ARNs |
| main_queue_arns | List of main queue ARNs |
| dlq_arns | List of dead letter queue ARNs |
| queue_configuration | Complete queue configuration |
| user_events_queue_arn | User events queue ARN |
| permission_events_queue_arn | Permission events queue ARN |
| audit_events_queue_arn | Audit events queue ARN |

## Integration with Application

The queue configuration output provides all necessary information for application configuration:

```json
{
  "user_events": {
    "name": "cleanarch-dev-user-events",
    "url": "https://sqs.region.amazonaws.com/account/cleanarch-dev-user-events",
    "arn": "arn:aws:sqs:region:account:cleanarch-dev-user-events",
    "type": "standard"
  }
}
```

## Cost Optimization

- Long polling enabled by default (20 seconds)
- Appropriate message retention periods
- DLQ prevents infinite processing of poison messages
- CloudWatch alarms help identify cost anomalies

## Best Practices

1. **Message Design**: Keep messages small and focused
2. **Error Handling**: Implement proper retry logic in consumers
3. **Monitoring**: Set up appropriate CloudWatch alarms
4. **Security**: Use IAM policies to restrict queue access
5. **Testing**: Test with LocalStack in development

## Troubleshooting

Common issues and solutions:

1. **Messages not being processed**: Check consumer configuration and IAM permissions
2. **Messages going to DLQ**: Review application error handling and visibility timeouts
3. **High costs**: Monitor message volume and optimize polling intervals
4. **FIFO ordering issues**: Ensure proper message group IDs are used