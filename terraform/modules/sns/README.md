# SNS Module

This module creates AWS SNS (Simple Notification Service) topics for SQS queue notifications and alerting in the Clean Architecture Template infrastructure.

## Overview

The SNS module provides:

- **SQS Alerts Topic**: For queue depth and message age alerts
- **SQS DLQ Alerts Topic**: For critical dead letter queue alerts
- **Infrastructure Alerts Topic**: For general infrastructure monitoring
- **Multiple Notification Channels**: Email, Slack, and SMS support
- **Security**: Server-side encryption and proper IAM policies
- **Delivery Policies**: Retry mechanisms for reliable delivery

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        SNS Topics                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌─────────────────┐    ┌─────────────────┐                    │
│  │   sqs-alerts    │    │ sqs-dlq-alerts  │                    │
│  │                 │    │   (Critical)    │                    │
│  └─────────────────┘    └─────────────────┘                    │
│           │                       │                            │
│           │              ┌─────────────────┐                   │
│           │              │ infrastructure- │                   │
│           │              │     alerts      │                   │
│           │              └─────────────────┘                   │
│           │                       │                            │
├─────────────────────────────────────────────────────────────────┤
│                    Notification Channels                       │
│                                                                 │
│  📧 Email          📱 Slack         📱 SMS                     │
│  • Primary         • Webhook        • DLQ Only                 │
│  • Additional      • All Topics     • Critical                 │
│  • All Topics      • Optional       • Optional                 │
└─────────────────────────────────────────────────────────────────┘
```

## Usage

### Basic Usage

```hcl
module "sns" {
  source = "./modules/sns"

  name_prefix = "cleanarch-dev"
  environment = "dev"

  # Email notifications
  enable_email_notifications = true
  notification_emails = [
    "admin@example.com",
    "devops@example.com"
  ]

  tags = {
    Environment = "dev"
    Project     = "cleanarch-template"
  }
}
```

### With Slack Integration

```hcl
module "sns" {
  source = "./modules/sns"

  name_prefix = "cleanarch-prod"
  environment = "prod"

  # Email notifications
  enable_email_notifications = true
  notification_emails = ["alerts@company.com"]

  # Slack notifications
  enable_slack_notifications = true
  slack_webhook_url = var.slack_webhook_url

  # SMS for critical alerts
  enable_sms_notifications = true
  notification_phone_numbers = ["+1234567890"]

  tags = local.common_tags
}
```

### Integration with SQS Module

```hcl
module "sns" {
  source = "./modules/sns"
  # ... configuration
}

module "sqs" {
  source = "./modules/sqs"

  # Use SNS topics for alarm actions
  alarm_actions = module.sns.all_alarm_action_arns

  # ... other configuration
}
```

## Inputs

| Name | Description | Type | Default | Required |
|------|-------------|------|---------|:--------:|
| name_prefix | Prefix for resource names | `string` | n/a | yes |
| environment | Environment name (dev, staging, prod) | `string` | n/a | yes |
| tags | Tags to apply to all resources | `map(string)` | `{}` | no |
| enable_encryption | Enable server-side encryption for SNS topics | `bool` | `true` | no |
| kms_key_id | KMS key ID for encryption | `string` | `null` | no |
| ecs_task_role_arn | ECS task role ARN for publishing | `string` | `null` | no |
| enable_email_notifications | Enable email notifications | `bool` | `true` | no |
| notification_emails | List of email addresses | `list(string)` | `[]` | no |
| enable_slack_notifications | Enable Slack notifications | `bool` | `false` | no |
| slack_webhook_url | Slack webhook URL | `string` | `null` | no |
| enable_sms_notifications | Enable SMS notifications | `bool` | `false` | no |
| notification_phone_numbers | List of phone numbers (E.164 format) | `list(string)` | `[]` | no |

## Outputs

| Name | Description |
|------|-------------|
| sqs_alerts_topic_arn | ARN of the SQS alerts SNS topic |
| sqs_dlq_alerts_topic_arn | ARN of the SQS DLQ alerts SNS topic |
| infrastructure_alerts_topic_arn | ARN of the infrastructure alerts SNS topic |
| all_topic_arns | List of all SNS topic ARNs |
| sqs_alarm_actions | SNS topic ARNs for SQS alarm actions |

## Topics

### sqs-alerts
- **Purpose**: Queue depth and message age alerts
- **Severity**: Warning/Info
- **Subscriptions**: Email, Slack (optional)
- **Usage**: Connected to SQS CloudWatch alarms

### sqs-dlq-alerts
- **Purpose**: Dead letter queue message alerts
- **Severity**: Critical
- **Subscriptions**: Email, Slack, SMS (optional)
- **Usage**: Immediate notification for failed messages

### infrastructure-alerts
- **Purpose**: General infrastructure monitoring
- **Severity**: Variable
- **Subscriptions**: Email, Slack (optional)
- **Usage**: Future expansion for other AWS services

## Security Features

### Encryption
- Server-side encryption enabled by default
- Uses AWS managed keys or custom KMS keys
- Encryption in transit for all communications

### IAM Policies
- CloudWatch can publish to topics
- ECS tasks can publish (if role provided)
- Account-scoped access controls
- Least privilege principle

### Access Control
```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Service": "cloudwatch.amazonaws.com"
      },
      "Action": "SNS:Publish",
      "Resource": "arn:aws:sns:*:*:cleanarch-*",
      "Condition": {
        "StringEquals": {
          "aws:SourceAccount": "123456789012"
        }
      }
    }
  ]
}
```

## Notification Channels

### Email Notifications
- **Primary Email**: Receives all alerts
- **Additional Emails**: Receive DLQ alerts only
- **Format**: JSON payload with alarm details
- **Delivery**: Reliable with retry policy

### Slack Notifications
- **Webhook Integration**: HTTPS endpoint
- **All Topics**: Receives all alert types
- **Format**: JSON payload (can be formatted with Lambda)
- **Optional**: Disabled by default

### SMS Notifications
- **Critical Only**: DLQ alerts only
- **Format**: E.164 phone numbers
- **Cost**: Per-message charges apply
- **Optional**: Disabled by default

## Delivery Policies

All subscriptions include retry policies:

```json
{
  "healthyRetryPolicy": {
    "minDelayTarget": 20,
    "maxDelayTarget": 20,
    "numRetries": 3,
    "numMaxDelayRetries": 0,
    "numMinDelayRetries": 0,
    "numNoDelayRetries": 0,
    "backoffFunction": "linear"
  }
}
```

## Environment Configuration

### Development
```hcl
enable_email_notifications = true
notification_emails = ["dev-team@company.com"]
enable_slack_notifications = true
enable_sms_notifications = false
```

### Staging
```hcl
enable_email_notifications = true
notification_emails = ["staging-alerts@company.com"]
enable_slack_notifications = true
enable_sms_notifications = false
```

### Production
```hcl
enable_email_notifications = true
notification_emails = [
  "prod-alerts@company.com",
  "oncall@company.com"
]
enable_slack_notifications = true
enable_sms_notifications = true
notification_phone_numbers = ["+1234567890"]
```

## Cost Considerations

### Pricing Factors
- **Requests**: $0.50 per 1 million requests
- **Email**: $2.00 per 100,000 notifications
- **SMS**: $0.75 per 100 notifications (US)
- **HTTPS**: $0.60 per 1 million notifications

### Cost Optimization
1. Use appropriate alarm thresholds
2. Consolidate similar alerts
3. Consider Slack over SMS for non-critical alerts
4. Monitor notification volume

## Monitoring

### CloudWatch Metrics
- Number of messages published
- Number of messages delivered
- Number of failed deliveries
- Subscription confirmation status

### Recommended Alarms
- Failed delivery rate > 5%
- Unconfirmed subscriptions
- High message volume (cost control)

## Troubleshooting

### Common Issues

#### Email Not Received
1. Check spam/junk folders
2. Verify email address format
3. Confirm subscription in AWS console
4. Check delivery status in CloudWatch

#### Slack Not Working
1. Verify webhook URL is correct
2. Check Slack app permissions
3. Test webhook manually
4. Review CloudWatch logs

#### SMS Not Delivered
1. Verify phone number format (E.164)
2. Check SMS opt-out status
3. Verify AWS SMS permissions
4. Check delivery status

### Debugging Commands

```bash
# List topics
aws sns list-topics

# Get topic attributes
aws sns get-topic-attributes --topic-arn $TOPIC_ARN

# List subscriptions
aws sns list-subscriptions-by-topic --topic-arn $TOPIC_ARN

# Test publish
aws sns publish \
  --topic-arn $TOPIC_ARN \
  --message "Test message" \
  --subject "Test Alert"
```

## Best Practices

### Topic Design
1. Separate topics by severity/purpose
2. Use descriptive names and descriptions
3. Enable encryption for sensitive data
4. Set appropriate delivery policies

### Subscription Management
1. Confirm all subscriptions
2. Regularly review subscriber lists
3. Use distribution lists for email
4. Test notification channels regularly

### Security
1. Use least privilege IAM policies
2. Enable encryption in transit and at rest
3. Monitor access patterns
4. Rotate webhook URLs periodically

### Cost Management
1. Monitor notification volume
2. Set billing alarms
3. Use appropriate channels for severity
4. Consolidate similar alerts

## Integration Examples

### With CloudWatch Alarms
```hcl
resource "aws_cloudwatch_metric_alarm" "example" {
  alarm_name          = "example-alarm"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "CPUUtilization"
  namespace           = "AWS/EC2"
  period              = "120"
  statistic           = "Average"
  threshold           = "80"
  alarm_description   = "This metric monitors ec2 cpu utilization"
  alarm_actions       = [module.sns.infrastructure_alerts_topic_arn]
}
```

### With Lambda Functions
```hcl
resource "aws_sns_topic_subscription" "lambda" {
  topic_arn = module.sns.sqs_alerts_topic_arn
  protocol  = "lambda"
  endpoint  = aws_lambda_function.alert_processor.arn
}
```

### With SQS Queues
```hcl
resource "aws_sns_topic_subscription" "sqs" {
  topic_arn = module.sns.infrastructure_alerts_topic_arn
  protocol  = "sqs"
  endpoint  = aws_sqs_queue.alert_queue.arn
}
```