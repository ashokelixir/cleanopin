# SNS Module - AWS Simple Notification Service
# This module creates SNS topics for SQS queue notifications and alerting

terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

# Local values for topic configuration
locals {
  # SNS topics for different alert types
  topics = {
    sqs-alerts = {
      name         = "${var.name_prefix}-sqs-alerts"
      display_name = "SQS Queue Alerts"
      description  = "Notifications for SQS queue depth, message age, and DLQ alerts"
    }
    sqs-dlq-alerts = {
      name         = "${var.name_prefix}-sqs-dlq-alerts"
      display_name = "SQS Dead Letter Queue Alerts"
      description  = "Critical notifications for messages in dead letter queues"
    }
    infrastructure-alerts = {
      name         = "${var.name_prefix}-infrastructure-alerts"
      display_name = "Infrastructure Alerts"
      description  = "General infrastructure monitoring alerts"
    }
  }
}

# SNS Topics
resource "aws_sns_topic" "topics" {
  for_each = local.topics

  name         = each.value.name
  display_name = each.value.display_name

  # Enable server-side encryption
  kms_master_key_id = var.enable_encryption ? (var.kms_key_id != null ? var.kms_key_id : "alias/aws/sns") : null

  # Delivery policy for retries
  delivery_policy = jsonencode({
    "http" : {
      "defaultHealthyRetryPolicy" : {
        "minDelayTarget" : 20,
        "maxDelayTarget" : 20,
        "numRetries" : 3,
        "numMaxDelayRetries" : 0,
        "numMinDelayRetries" : 0,
        "numNoDelayRetries" : 0,
        "backoffFunction" : "linear"
      },
      "disableSubscriptionOverrides" : false
    }
  })

  tags = merge(var.tags, {
    Name        = each.value.name
    Description = each.value.description
    Environment = var.environment
    Purpose     = "Alerting"
  })
}

# SNS Topic Policies
resource "aws_sns_topic_policy" "topic_policies" {
  for_each = aws_sns_topic.topics

  arn = each.value.arn

  policy = jsonencode({
    Version = "2012-10-17"
    Id      = "${each.value.name}-policy"
    Statement = [
      {
        Sid    = "AllowCloudWatchAlarmsToPublish"
        Effect = "Allow"
        Principal = {
          Service = "cloudwatch.amazonaws.com"
        }
        Action = [
          "SNS:Publish"
        ]
        Resource = each.value.arn
        Condition = {
          StringEquals = {
            "aws:SourceAccount" = data.aws_caller_identity.current.account_id
          }
        }
      },
      {
        Sid    = "AllowECSTasksToPublish"
        Effect = "Allow"
        Principal = {
          AWS = var.ecs_task_role_arn != null ? var.ecs_task_role_arn : "*"
        }
        Action = [
          "SNS:Publish"
        ]
        Resource = each.value.arn
        Condition = {
          StringEquals = {
            "aws:SourceAccount" = data.aws_caller_identity.current.account_id
          }
        }
      }
    ]
  })
}

# Email subscriptions for critical alerts
resource "aws_sns_topic_subscription" "email_subscriptions" {
  for_each = var.enable_email_notifications ? {
    for topic_key, topic in local.topics : topic_key => topic
    if length(var.notification_emails) > 0
  } : {}

  topic_arn = aws_sns_topic.topics[each.key].arn
  protocol  = "email"
  endpoint  = var.notification_emails[0] # Primary email for all topics

  # Subscription attributes
  delivery_policy = jsonencode({
    "healthyRetryPolicy" : {
      "minDelayTarget" : 20,
      "maxDelayTarget" : 20,
      "numRetries" : 3,
      "numMaxDelayRetries" : 0,
      "numMinDelayRetries" : 0,
      "numNoDelayRetries" : 0,
      "backoffFunction" : "linear"
    }
  })
}

# Additional email subscriptions for multiple recipients
resource "aws_sns_topic_subscription" "additional_email_subscriptions" {
  for_each = var.enable_email_notifications ? {
    for idx, email in slice(var.notification_emails, 1, length(var.notification_emails)) :
    "${aws_sns_topic.topics["sqs-dlq-alerts"].name}-${idx}" => {
      topic_arn = aws_sns_topic.topics["sqs-dlq-alerts"].arn
      email     = email
    }
  } : {}

  topic_arn = each.value.topic_arn
  protocol  = "email"
  endpoint  = each.value.email

  delivery_policy = jsonencode({
    "healthyRetryPolicy" : {
      "minDelayTarget" : 20,
      "maxDelayTarget" : 20,
      "numRetries" : 3,
      "numMaxDelayRetries" : 0,
      "numMinDelayRetries" : 0,
      "numNoDelayRetries" : 0,
      "backoffFunction" : "linear"
    }
  })
}

# Slack webhook subscription (if configured)
resource "aws_sns_topic_subscription" "slack_subscriptions" {
  for_each = var.enable_slack_notifications && var.slack_webhook_url != null ? {
    for topic_key, topic in local.topics : topic_key => topic
  } : {}

  topic_arn = aws_sns_topic.topics[each.key].arn
  protocol  = "https"
  endpoint  = var.slack_webhook_url

  delivery_policy = jsonencode({
    "healthyRetryPolicy" : {
      "minDelayTarget" : 20,
      "maxDelayTarget" : 20,
      "numRetries" : 3,
      "numMaxDelayRetries" : 0,
      "numMinDelayRetries" : 0,
      "numNoDelayRetries" : 0,
      "backoffFunction" : "linear"
    }
  })
}

# SMS subscriptions for critical DLQ alerts (if configured)
resource "aws_sns_topic_subscription" "sms_subscriptions" {
  for_each = var.enable_sms_notifications && length(var.notification_phone_numbers) > 0 ? {
    for idx, phone in var.notification_phone_numbers :
    "dlq-sms-${idx}" => {
      topic_arn = aws_sns_topic.topics["sqs-dlq-alerts"].arn
      phone     = phone
    }
  } : {}

  topic_arn = each.value.topic_arn
  protocol  = "sms"
  endpoint  = each.value.phone

  delivery_policy = jsonencode({
    "healthyRetryPolicy" : {
      "minDelayTarget" : 20,
      "maxDelayTarget" : 20,
      "numRetries" : 3,
      "numMaxDelayRetries" : 0,
      "numMinDelayRetries" : 0,
      "numNoDelayRetries" : 0,
      "backoffFunction" : "linear"
    }
  })
}

# Data source for current AWS account
data "aws_caller_identity" "current" {}