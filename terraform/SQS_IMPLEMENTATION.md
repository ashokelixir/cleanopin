# SQS Implementation Guide

This document describes the AWS SQS (Simple Queue Service) implementation for the Clean Architecture Template messaging infrastructure.

## Overview

The SQS implementation provides a complete messaging infrastructure with:

- **Standard Queues**: For user events and permission events
- **FIFO Queues**: For audit events requiring ordered processing
- **Dead Letter Queues**: Automatic failure handling for all main queues
- **CloudWatch Monitoring**: Comprehensive alarms and metrics
- **SNS Notifications**: Multi-channel alerting (Email, Slack, SMS)
- **IAM Security**: Least-privilege access for ECS tasks
- **Environment Isolation**: Separate queues per environment

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    SQS Messaging Infrastructure                 │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────┐    ┌─────────────────┐                    │
│  │   user-events   │───▶│ user-events-dlq │                    │
│  │   (Standard)    │    │   (Standard)    │                    │
│  └─────────────────┘    └─────────────────┘                    │
│                                                                 │
│  ┌─────────────────┐    ┌─────────────────┐                    │
│  │ permission-     │───▶│ permission-     │                    │
│  │   events        │    │ events-dlq      │                    │
│  │ (Standard)      │    │ (Standard)      │                    │
│  └─────────────────┘    └─────────────────┘                    │
│                                                                 │
│  ┌─────────────────┐    ┌─────────────────┐                    │
│  │  audit-events   │───▶│ audit-events-   │                    │
│  │    (FIFO)       │    │   dlq (FIFO)    │                    │
│  └─────────────────┘    └─────────────────┘                    │
│                                                                 │
├─────────────────────────────────────────────────────────────────┤
│                    CloudWatch Alarms                           │
│  • Queue Depth Monitoring                                      │
│  • Message Age Monitoring                                      │
│  • Dead Letter Queue Monitoring                                │
│                         │                                       │
│                         ▼                                       │
├─────────────────────────────────────────────────────────────────┤
│                     SNS Notifications                          │
│                                                                 │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   sqs-alerts    │  │ sqs-dlq-alerts  │  │ infrastructure- │ │
│  │                 │  │   (Critical)    │  │     alerts      │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
│           │                     │                     │         │
│           ▼                     ▼                     ▼         │
├─────────────────────────────────────────────────────────────────┤
│                  Notification Channels                         │
│                                                                 │
│  📧 Email          📱 Slack         📱 SMS                     │
│  • Primary         • Webhook        • DLQ Only                 │
│  • Additional      • All Topics     • Critical                 │
│  • All Topics      • Optional       • Optional                 │
└─────────────────────────────────────────────────────────────────┘
```

## Module Structure

```
terraform/modules/sqs/
├── main.tf          # SQS resources and CloudWatch alarms
├── variables.tf     # Input variables
├── outputs.tf       # Output values
└── README.md        # Module documentation
```

## Queue Configuration

### Standard Queues

#### user-events
- **Purpose**: User lifecycle events (created, updated, deleted)
- **Visibility Timeout**: 30 seconds
- **Max Receive Count**: 3
- **Message Retention**: 14 days
- **Long Polling**: 20 seconds

#### permission-events
- **Purpose**: Permission change events (assigned, removed, bulk operations)
- **Visibility Timeout**: 30 seconds
- **Max Receive Count**: 3
- **Message Retention**: 14 days
- **Long Polling**: 20 seconds

### FIFO Queues

#### audit-events.fifo
- **Purpose**: Audit events requiring ordered processing
- **Visibility Timeout**: 60 seconds
- **Max Receive Count**: 5
- **Message Retention**: 14 days
- **Content-Based Deduplication**: Enabled
- **Long Polling**: 20 seconds

### Dead Letter Queues

Each main queue has a corresponding DLQ:
- `user-events-dlq`
- `permission-events-dlq`
- `audit-events-dlq.fifo`

Messages are automatically moved to DLQ after exceeding the maximum receive count.

## Environment Configuration

### Development (dev)
```hcl
# Lower thresholds for development
sqs_queue_depth_alarm_threshold   = 50
sqs_message_age_alarm_threshold   = 600  # 10 minutes
sqs_enable_high_throughput        = false
```

### Staging (staging)
```hcl
# Production-like settings for testing
sqs_queue_depth_alarm_threshold   = 100
sqs_message_age_alarm_threshold   = 300  # 5 minutes
sqs_enable_high_throughput        = false
```

### Production (prod)
```hcl
# High availability and performance settings
sqs_queue_depth_alarm_threshold   = 200
sqs_message_age_alarm_threshold   = 180  # 3 minutes
sqs_enable_high_throughput        = true
sqs_deduplication_scope          = "messageGroup"
sqs_fifo_throughput_limit        = "perMessageGroupId"
```

## Security Features

### Server-Side Encryption
- **Default**: AWS managed keys (SQS-SSE)
- **Optional**: Customer managed KMS keys
- **In-Transit**: HTTPS encryption for all API calls

### IAM Permissions
ECS tasks have the following SQS permissions:
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
        "sqs:DeleteMessageBatch",
        "sqs:GetQueueAttributes",
        "sqs:GetQueueUrl",
        "sqs:ChangeMessageVisibility",
        "sqs:ChangeMessageVisibilityBatch",
        "sqs:PurgeQueue"
      ],
      "Resource": ["arn:aws:sqs:*:*:cleanarch-*"],
      "Condition": {
        "StringEquals": {
          "aws:SourceAccount": "${account_id}"
        }
      }
    }
  ]
}
```

## CloudWatch Monitoring

### Metrics and Alarms

#### Queue Depth Alarms
- **Metric**: `ApproximateNumberOfVisibleMessages`
- **Threshold**: Environment-specific (50-200 messages)
- **Evaluation**: 2 periods of 5 minutes
- **Action**: SNS notification (configurable)

#### Message Age Alarms
- **Metric**: `ApproximateAgeOfOldestMessage`
- **Threshold**: Environment-specific (3-10 minutes)
- **Evaluation**: 2 periods of 5 minutes
- **Action**: SNS notification (configurable)

#### Dead Letter Queue Alarms
- **Metric**: `ApproximateNumberOfVisibleMessages` (DLQ)
- **Threshold**: 1 message
- **Evaluation**: 1 period of 5 minutes
- **Action**: Immediate SNS notification

### Dashboard Metrics
Available CloudWatch metrics:
- Number of messages sent/received
- Number of empty receives
- Message size statistics
- Queue depth over time
- Processing latency

## Deployment

### Prerequisites
1. AWS CLI configured with appropriate permissions
2. Terraform >= 1.5 installed
3. Backend S3 bucket and DynamoDB table configured

### Deploy SQS Infrastructure
```powershell
# Deploy to development
./scripts/deploy-sqs.ps1 -Environment dev -Region ap-south-1

# Deploy to production with auto-approval
./scripts/deploy-sqs.ps1 -Environment prod -Region us-east-1 -AutoApprove

# Plan only (no changes)
./scripts/deploy-sqs.ps1 -Environment staging -PlanOnly
```

### Destroy Infrastructure
```powershell
# Destroy development environment
./scripts/deploy-sqs.ps1 -Environment dev -DestroyMode
```

## Testing

### Automated Tests
```powershell
# Run all SQS tests
./tests/sqs.test.ps1 -Environment dev -Region ap-south-1

# Skip message publishing/consumption tests
./tests/sqs.test.ps1 -Environment prod -SkipMessageTests
```

### Test Coverage
- Queue creation and configuration
- Dead letter queue setup
- Server-side encryption
- CloudWatch alarms
- IAM permissions
- Message publishing/consumption
- FIFO queue functionality
- Naming conventions

### Manual Testing
```bash
# Send test message
aws sqs send-message \
  --queue-url https://sqs.region.amazonaws.com/account/cleanarch-dev-user-events \
  --message-body '{"test": "message"}' \
  --region ap-south-1

# Receive messages
aws sqs receive-message \
  --queue-url https://sqs.region.amazonaws.com/account/cleanarch-dev-user-events \
  --max-number-of-messages 1 \
  --region ap-south-1

# Send FIFO message
aws sqs send-message \
  --queue-url https://sqs.region.amazonaws.com/account/cleanarch-dev-audit-events.fifo \
  --message-body '{"audit": "event"}' \
  --message-group-id "test-group" \
  --message-deduplication-id "unique-id-123" \
  --region ap-south-1
```

## Integration with Application

### Queue URLs
The application receives queue URLs through Terraform outputs:
```json
{
  "user_events": {
    "name": "cleanarch-dev-user-events",
    "url": "https://sqs.region.amazonaws.com/account/cleanarch-dev-user-events",
    "arn": "arn:aws:sqs:region:account:cleanarch-dev-user-events",
    "type": "standard"
  },
  "permission_events": {
    "name": "cleanarch-dev-permission-events",
    "url": "https://sqs.region.amazonaws.com/account/cleanarch-dev-permission-events",
    "arn": "arn:aws:sqs:region:account:cleanarch-dev-permission-events",
    "type": "standard"
  },
  "audit_events": {
    "name": "cleanarch-dev-audit-events.fifo",
    "url": "https://sqs.region.amazonaws.com/account/cleanarch-dev-audit-events.fifo",
    "arn": "arn:aws:sqs:region:account:cleanarch-dev-audit-events.fifo",
    "type": "fifo"
  }
}
```

### Environment Variables
ECS tasks receive queue configuration through environment variables:
```bash
MESSAGING__QUEUES__USER_EVENTS__NAME=cleanarch-dev-user-events
MESSAGING__QUEUES__USER_EVENTS__URL=https://sqs.region.amazonaws.com/account/cleanarch-dev-user-events
MESSAGING__QUEUES__PERMISSION_EVENTS__NAME=cleanarch-dev-permission-events
MESSAGING__QUEUES__PERMISSION_EVENTS__URL=https://sqs.region.amazonaws.com/account/cleanarch-dev-permission-events
MESSAGING__QUEUES__AUDIT_EVENTS__NAME=cleanarch-dev-audit-events.fifo
MESSAGING__QUEUES__AUDIT_EVENTS__URL=https://sqs.region.amazonaws.com/account/cleanarch-dev-audit-events.fifo
```

## Cost Optimization

### Strategies
1. **Long Polling**: Reduces empty receive charges
2. **Batch Operations**: Lower per-message costs
3. **Appropriate Retention**: Balance availability vs. storage costs
4. **Dead Letter Queues**: Prevent infinite processing costs
5. **Environment Sizing**: Different thresholds per environment

### Cost Monitoring
- CloudWatch billing alarms
- AWS Cost Explorer integration
- Queue metrics analysis
- Message volume tracking

## Troubleshooting

### Common Issues

#### Messages Not Being Processed
1. Check ECS task IAM permissions
2. Verify queue URLs in application configuration
3. Check consumer service health
4. Review CloudWatch logs

#### Messages Going to DLQ
1. Review application error logs
2. Check message format and validation
3. Verify visibility timeout settings
4. Analyze processing time vs. timeout

#### High Costs
1. Monitor message volume metrics
2. Check for message loops
3. Review polling intervals
4. Optimize batch sizes

#### FIFO Queue Issues
1. Verify message group IDs
2. Check deduplication IDs
3. Review throughput limits
4. Monitor ordering requirements

### Debugging Commands
```bash
# Check queue attributes
aws sqs get-queue-attributes --queue-url $QUEUE_URL --attribute-names All

# Monitor queue metrics
aws cloudwatch get-metric-statistics \
  --namespace AWS/SQS \
  --metric-name ApproximateNumberOfVisibleMessages \
  --dimensions Name=QueueName,Value=cleanarch-dev-user-events \
  --start-time 2024-01-01T00:00:00Z \
  --end-time 2024-01-01T01:00:00Z \
  --period 300 \
  --statistics Average

# Purge queue (development only)
aws sqs purge-queue --queue-url $QUEUE_URL
```

## Best Practices

### Message Design
1. Keep messages small and focused
2. Include correlation IDs for tracing
3. Use proper JSON schema validation
4. Implement idempotent processing

### Error Handling
1. Implement exponential backoff
2. Use dead letter queues appropriately
3. Log all processing errors
4. Monitor DLQ messages

### Performance
1. Use batch operations when possible
2. Configure appropriate visibility timeouts
3. Monitor queue depth and age
4. Scale consumers based on metrics

### Security
1. Use least-privilege IAM policies
2. Enable server-side encryption
3. Implement message validation
4. Monitor access patterns

## Maintenance

### Regular Tasks
1. Monitor CloudWatch alarms
2. Review DLQ messages
3. Analyze cost trends
4. Update security policies

### Scaling Considerations
1. Monitor queue depth trends
2. Adjust consumer capacity
3. Review timeout settings
4. Consider FIFO throughput limits

### Backup and Recovery
1. Messages are automatically replicated
2. DLQ provides message recovery
3. CloudWatch logs for audit trail
4. Infrastructure as Code for disaster recovery