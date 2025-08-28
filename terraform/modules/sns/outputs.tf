# SNS Module Outputs

# Topic ARNs
output "sqs_alerts_topic_arn" {
  description = "ARN of the SQS alerts SNS topic"
  value       = aws_sns_topic.topics["sqs-alerts"].arn
}

output "sqs_dlq_alerts_topic_arn" {
  description = "ARN of the SQS DLQ alerts SNS topic"
  value       = aws_sns_topic.topics["sqs-dlq-alerts"].arn
}

output "infrastructure_alerts_topic_arn" {
  description = "ARN of the infrastructure alerts SNS topic"
  value       = aws_sns_topic.topics["infrastructure-alerts"].arn
}

# All topic ARNs for convenience
output "all_topic_arns" {
  description = "List of all SNS topic ARNs"
  value       = [for topic in aws_sns_topic.topics : topic.arn]
}

# Topic ARNs by name
output "topic_arns_by_name" {
  description = "Map of topic names to ARNs"
  value = {
    for key, topic in aws_sns_topic.topics : key => topic.arn
  }
}

# Topic URLs
output "sqs_alerts_topic_url" {
  description = "URL of the SQS alerts SNS topic"
  value       = aws_sns_topic.topics["sqs-alerts"].id
}

output "sqs_dlq_alerts_topic_url" {
  description = "URL of the SQS DLQ alerts SNS topic"
  value       = aws_sns_topic.topics["sqs-dlq-alerts"].id
}

output "infrastructure_alerts_topic_url" {
  description = "URL of the infrastructure alerts SNS topic"
  value       = aws_sns_topic.topics["infrastructure-alerts"].id
}

# Topic names
output "sqs_alerts_topic_name" {
  description = "Name of the SQS alerts SNS topic"
  value       = aws_sns_topic.topics["sqs-alerts"].name
}

output "sqs_dlq_alerts_topic_name" {
  description = "Name of the SQS DLQ alerts SNS topic"
  value       = aws_sns_topic.topics["sqs-dlq-alerts"].name
}

output "infrastructure_alerts_topic_name" {
  description = "Name of the infrastructure alerts SNS topic"
  value       = aws_sns_topic.topics["infrastructure-alerts"].name
}

# Subscription information
output "email_subscriptions" {
  description = "Information about email subscriptions"
  value = var.enable_email_notifications ? {
    primary_email = length(var.notification_emails) > 0 ? var.notification_emails[0] : null
    total_emails  = length(var.notification_emails)
    topics_with_subscriptions = [
      for topic_key, topic in local.topics : topic_key
    ]
  } : null
}

output "slack_subscriptions" {
  description = "Information about Slack subscriptions"
  value = var.enable_slack_notifications && var.slack_webhook_url != null ? {
    enabled = true
    topics_with_subscriptions = [
      for topic_key, topic in local.topics : topic_key
    ]
  } : null
  sensitive = true
}

output "sms_subscriptions" {
  description = "Information about SMS subscriptions"
  value = var.enable_sms_notifications ? {
    enabled      = true
    phone_count  = length(var.notification_phone_numbers)
    dlq_only     = true
  } : null
}

# Configuration for SQS module
output "sqs_alarm_actions" {
  description = "SNS topic ARNs for SQS alarm actions"
  value = {
    queue_depth_alarms  = [aws_sns_topic.topics["sqs-alerts"].arn]
    message_age_alarms  = [aws_sns_topic.topics["sqs-alerts"].arn]
    dlq_message_alarms  = [aws_sns_topic.topics["sqs-dlq-alerts"].arn]
  }
}

# All alarm action ARNs
output "all_alarm_action_arns" {
  description = "All SNS topic ARNs that can be used for alarm actions"
  value = [
    aws_sns_topic.topics["sqs-alerts"].arn,
    aws_sns_topic.topics["sqs-dlq-alerts"].arn,
    aws_sns_topic.topics["infrastructure-alerts"].arn
  ]
}