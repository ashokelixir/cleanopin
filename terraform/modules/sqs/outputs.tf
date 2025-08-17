# SQS Module Outputs

# Standard Queue Outputs
output "user_events_queue_arn" {
  description = "ARN of the user events queue"
  value       = aws_sqs_queue.standard_queues["user-events"].arn
}

output "user_events_queue_url" {
  description = "URL of the user events queue"
  value       = aws_sqs_queue.standard_queues["user-events"].url
}

output "user_events_queue_name" {
  description = "Name of the user events queue"
  value       = aws_sqs_queue.standard_queues["user-events"].name
}

output "permission_events_queue_arn" {
  description = "ARN of the permission events queue"
  value       = aws_sqs_queue.standard_queues["permission-events"].arn
}

output "permission_events_queue_url" {
  description = "URL of the permission events queue"
  value       = aws_sqs_queue.standard_queues["permission-events"].url
}

output "permission_events_queue_name" {
  description = "Name of the permission events queue"
  value       = aws_sqs_queue.standard_queues["permission-events"].name
}

# FIFO Queue Outputs
output "audit_events_queue_arn" {
  description = "ARN of the audit events FIFO queue"
  value       = aws_sqs_queue.fifo_queues["audit-events"].arn
}

output "audit_events_queue_url" {
  description = "URL of the audit events FIFO queue"
  value       = aws_sqs_queue.fifo_queues["audit-events"].url
}

output "audit_events_queue_name" {
  description = "Name of the audit events FIFO queue"
  value       = aws_sqs_queue.fifo_queues["audit-events"].name
}

# Dead Letter Queue Outputs
output "user_events_dlq_arn" {
  description = "ARN of the user events dead letter queue"
  value       = aws_sqs_queue.standard_dlqs["user-events-dlq"].arn
}

output "user_events_dlq_url" {
  description = "URL of the user events dead letter queue"
  value       = aws_sqs_queue.standard_dlqs["user-events-dlq"].url
}

output "permission_events_dlq_arn" {
  description = "ARN of the permission events dead letter queue"
  value       = aws_sqs_queue.standard_dlqs["permission-events-dlq"].arn
}

output "permission_events_dlq_url" {
  description = "URL of the permission events dead letter queue"
  value       = aws_sqs_queue.standard_dlqs["permission-events-dlq"].url
}

output "audit_events_dlq_arn" {
  description = "ARN of the audit events dead letter queue"
  value       = aws_sqs_queue.fifo_dlqs["audit-events-dlq"].arn
}

output "audit_events_dlq_url" {
  description = "URL of the audit events dead letter queue"
  value       = aws_sqs_queue.fifo_dlqs["audit-events-dlq"].url
}

# All Queue ARNs for IAM Policies
output "all_queue_arns" {
  description = "List of all SQS queue ARNs (including DLQs)"
  value       = local.all_queue_arns
}

output "main_queue_arns" {
  description = "List of main SQS queue ARNs (excluding DLQs)"
  value = concat(
    [for queue in aws_sqs_queue.standard_queues : queue.arn],
    [for queue in aws_sqs_queue.fifo_queues : queue.arn]
  )
}

output "dlq_arns" {
  description = "List of dead letter queue ARNs"
  value = concat(
    [for queue in aws_sqs_queue.standard_dlqs : queue.arn],
    [for queue in aws_sqs_queue.fifo_dlqs : queue.arn]
  )
}

# Queue Configuration for Application
output "queue_configuration" {
  description = "Queue configuration for application use"
  value = {
    user_events = {
      name = aws_sqs_queue.standard_queues["user-events"].name
      url  = aws_sqs_queue.standard_queues["user-events"].url
      arn  = aws_sqs_queue.standard_queues["user-events"].arn
      type = "standard"
      dlq = {
        name = aws_sqs_queue.standard_dlqs["user-events-dlq"].name
        url  = aws_sqs_queue.standard_dlqs["user-events-dlq"].url
        arn  = aws_sqs_queue.standard_dlqs["user-events-dlq"].arn
      }
    }
    permission_events = {
      name = aws_sqs_queue.standard_queues["permission-events"].name
      url  = aws_sqs_queue.standard_queues["permission-events"].url
      arn  = aws_sqs_queue.standard_queues["permission-events"].arn
      type = "standard"
      dlq = {
        name = aws_sqs_queue.standard_dlqs["permission-events-dlq"].name
        url  = aws_sqs_queue.standard_dlqs["permission-events-dlq"].url
        arn  = aws_sqs_queue.standard_dlqs["permission-events-dlq"].arn
      }
    }
    audit_events = {
      name = aws_sqs_queue.fifo_queues["audit-events"].name
      url  = aws_sqs_queue.fifo_queues["audit-events"].url
      arn  = aws_sqs_queue.fifo_queues["audit-events"].arn
      type = "fifo"
      dlq = {
        name = aws_sqs_queue.fifo_dlqs["audit-events-dlq"].name
        url  = aws_sqs_queue.fifo_dlqs["audit-events-dlq"].url
        arn  = aws_sqs_queue.fifo_dlqs["audit-events-dlq"].arn
      }
    }
  }
}

# CloudWatch Alarm ARNs
output "cloudwatch_alarm_arns" {
  description = "List of CloudWatch alarm ARNs for SQS queues"
  value = var.enable_cloudwatch_alarms ? concat(
    [for alarm in aws_cloudwatch_metric_alarm.queue_depth_alarm : alarm.arn],
    [for alarm in aws_cloudwatch_metric_alarm.message_age_alarm : alarm.arn],
    [for alarm in aws_cloudwatch_metric_alarm.dlq_messages_alarm : alarm.arn]
  ) : []
}