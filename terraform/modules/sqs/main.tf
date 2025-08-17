# SQS Module - AWS Simple Queue Service
# This module creates SQS queues for the Clean Architecture Template messaging infrastructure

terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

# Local values for queue configuration
locals {
  # Standard queues configuration
  standard_queues = {
    user-events = {
      name                       = "${var.name_prefix}-user-events"
      visibility_timeout_seconds = var.user_events_visibility_timeout
      message_retention_seconds  = var.message_retention_seconds
      max_receive_count          = var.max_receive_count
      delay_seconds              = 0
      receive_wait_time_seconds  = var.receive_wait_time_seconds
    }
    permission-events = {
      name                       = "${var.name_prefix}-permission-events"
      visibility_timeout_seconds = var.permission_events_visibility_timeout
      message_retention_seconds  = var.message_retention_seconds
      max_receive_count          = var.max_receive_count
      delay_seconds              = 0
      receive_wait_time_seconds  = var.receive_wait_time_seconds
    }
  }

  # FIFO queues configuration
  fifo_queues = {
    audit-events = {
      name                        = "${var.name_prefix}-audit-events.fifo"
      visibility_timeout_seconds  = var.audit_events_visibility_timeout
      message_retention_seconds   = var.message_retention_seconds
      max_receive_count           = var.audit_events_max_receive_count
      delay_seconds               = 0
      receive_wait_time_seconds   = var.receive_wait_time_seconds
      content_based_deduplication = true
    }
  }

  # Dead letter queues for standard queues
  standard_dlqs = {
    for queue_key, queue_config in local.standard_queues : "${queue_key}-dlq" => {
      name                       = "${queue_config.name}-dlq"
      visibility_timeout_seconds = queue_config.visibility_timeout_seconds
      message_retention_seconds  = var.dlq_message_retention_seconds
      delay_seconds              = 0
      receive_wait_time_seconds  = var.receive_wait_time_seconds
    }
  }

  # Dead letter queues for FIFO queues
  fifo_dlqs = {
    for queue_key, queue_config in local.fifo_queues : "${queue_key}-dlq" => {
      name                        = "${queue_config.name}-dlq.fifo"
      visibility_timeout_seconds  = queue_config.visibility_timeout_seconds
      message_retention_seconds   = var.dlq_message_retention_seconds
      delay_seconds               = 0
      receive_wait_time_seconds   = var.receive_wait_time_seconds
      content_based_deduplication = true
    }
  }

  # All queue ARNs for IAM policies
  all_queue_arns = concat(
    [for queue in aws_sqs_queue.standard_queues : queue.arn],
    [for queue in aws_sqs_queue.fifo_queues : queue.arn],
    [for queue in aws_sqs_queue.standard_dlqs : queue.arn],
    [for queue in aws_sqs_queue.fifo_dlqs : queue.arn]
  )
}

# Standard SQS Queues
resource "aws_sqs_queue" "standard_queues" {
  for_each = local.standard_queues

  name                       = each.value.name
  visibility_timeout_seconds = each.value.visibility_timeout_seconds
  message_retention_seconds  = each.value.message_retention_seconds
  delay_seconds              = each.value.delay_seconds
  receive_wait_time_seconds  = each.value.receive_wait_time_seconds
  max_message_size           = var.max_message_size

  # Enable server-side encryption
  sqs_managed_sse_enabled = var.enable_sse && var.kms_key_id == null
  kms_master_key_id       = var.kms_key_id

  # Redrive policy will be set after DLQ creation
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.standard_dlqs["${each.key}-dlq"].arn
    maxReceiveCount     = each.value.max_receive_count
  })

  tags = merge(var.tags, {
    Name        = each.value.name
    QueueType   = "Standard"
    Environment = var.environment
  })

  depends_on = [aws_sqs_queue.standard_dlqs]
}

# FIFO SQS Queues
resource "aws_sqs_queue" "fifo_queues" {
  for_each = local.fifo_queues

  name                       = each.value.name
  visibility_timeout_seconds = each.value.visibility_timeout_seconds
  message_retention_seconds  = each.value.message_retention_seconds
  delay_seconds              = each.value.delay_seconds
  receive_wait_time_seconds  = each.value.receive_wait_time_seconds
  max_message_size           = var.max_message_size

  # FIFO specific settings
  fifo_queue                  = true
  content_based_deduplication = each.value.content_based_deduplication
  deduplication_scope         = var.deduplication_scope
  fifo_throughput_limit       = var.fifo_throughput_limit

  # Enable server-side encryption
  sqs_managed_sse_enabled = var.enable_sse && var.kms_key_id == null
  kms_master_key_id       = var.kms_key_id

  # Redrive policy will be set after DLQ creation
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.fifo_dlqs["${each.key}-dlq"].arn
    maxReceiveCount     = each.value.max_receive_count
  })

  tags = merge(var.tags, {
    Name        = each.value.name
    QueueType   = "FIFO"
    Environment = var.environment
  })

  depends_on = [aws_sqs_queue.fifo_dlqs]
}

# Dead Letter Queues for Standard Queues
resource "aws_sqs_queue" "standard_dlqs" {
  for_each = local.standard_dlqs

  name                       = each.value.name
  visibility_timeout_seconds = each.value.visibility_timeout_seconds
  message_retention_seconds  = each.value.message_retention_seconds
  delay_seconds              = each.value.delay_seconds
  receive_wait_time_seconds  = each.value.receive_wait_time_seconds
  max_message_size           = var.max_message_size

  # Enable server-side encryption
  sqs_managed_sse_enabled = var.enable_sse && var.kms_key_id == null
  kms_master_key_id       = var.kms_key_id

  tags = merge(var.tags, {
    Name        = each.value.name
    QueueType   = "Standard-DLQ"
    Environment = var.environment
  })
}

# Dead Letter Queues for FIFO Queues
resource "aws_sqs_queue" "fifo_dlqs" {
  for_each = local.fifo_dlqs

  name                       = each.value.name
  visibility_timeout_seconds = each.value.visibility_timeout_seconds
  message_retention_seconds  = each.value.message_retention_seconds
  delay_seconds              = each.value.delay_seconds
  receive_wait_time_seconds  = each.value.receive_wait_time_seconds
  max_message_size           = var.max_message_size

  # FIFO specific settings
  fifo_queue                  = true
  content_based_deduplication = each.value.content_based_deduplication
  deduplication_scope         = var.deduplication_scope
  fifo_throughput_limit       = var.fifo_throughput_limit

  # Enable server-side encryption
  sqs_managed_sse_enabled = var.enable_sse && var.kms_key_id == null
  kms_master_key_id       = var.kms_key_id

  tags = merge(var.tags, {
    Name        = each.value.name
    QueueType   = "FIFO-DLQ"
    Environment = var.environment
  })
}

# CloudWatch Alarms for Queue Depth
resource "aws_cloudwatch_metric_alarm" "queue_depth_alarm" {
  for_each = var.enable_cloudwatch_alarms ? merge(local.standard_queues, local.fifo_queues) : {}

  alarm_name          = "${each.value.name}-queue-depth"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "ApproximateNumberOfVisibleMessages"
  namespace           = "AWS/SQS"
  period              = "300"
  statistic           = "Average"
  threshold           = var.queue_depth_alarm_threshold
  alarm_description   = "This metric monitors ${each.value.name} queue depth"
  alarm_actions       = var.alarm_actions

  dimensions = {
    QueueName = each.value.name
  }

  tags = merge(var.tags, {
    Name        = "${each.value.name}-queue-depth-alarm"
    Environment = var.environment
  })

  depends_on = [
    aws_sqs_queue.standard_queues,
    aws_sqs_queue.fifo_queues
  ]
}

# CloudWatch Alarms for Message Age
resource "aws_cloudwatch_metric_alarm" "message_age_alarm" {
  for_each = var.enable_cloudwatch_alarms ? merge(local.standard_queues, local.fifo_queues) : {}

  alarm_name          = "${each.value.name}-message-age"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "ApproximateAgeOfOldestMessage"
  namespace           = "AWS/SQS"
  period              = "300"
  statistic           = "Maximum"
  threshold           = var.message_age_alarm_threshold
  alarm_description   = "This metric monitors ${each.value.name} message age"
  alarm_actions       = var.alarm_actions

  dimensions = {
    QueueName = each.value.name
  }

  tags = merge(var.tags, {
    Name        = "${each.value.name}-message-age-alarm"
    Environment = var.environment
  })

  depends_on = [
    aws_sqs_queue.standard_queues,
    aws_sqs_queue.fifo_queues
  ]
}

# CloudWatch Alarms for DLQ Messages
resource "aws_cloudwatch_metric_alarm" "dlq_messages_alarm" {
  for_each = var.enable_cloudwatch_alarms ? merge(local.standard_dlqs, local.fifo_dlqs) : {}

  alarm_name          = "${each.value.name}-dlq-messages"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "1"
  metric_name         = "ApproximateNumberOfVisibleMessages"
  namespace           = "AWS/SQS"
  period              = "300"
  statistic           = "Average"
  threshold           = var.dlq_messages_alarm_threshold
  alarm_description   = "This metric monitors ${each.value.name} dead letter queue messages"
  alarm_actions       = var.alarm_actions

  dimensions = {
    QueueName = each.value.name
  }

  tags = merge(var.tags, {
    Name        = "${each.value.name}-dlq-messages-alarm"
    Environment = var.environment
  })

  depends_on = [
    aws_sqs_queue.standard_dlqs,
    aws_sqs_queue.fifo_dlqs
  ]
}