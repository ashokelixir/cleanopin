# SQS Module Variables

variable "name_prefix" {
  description = "Prefix for resource names"
  type        = string
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default     = {}
}

# Queue Configuration
variable "message_retention_seconds" {
  description = "The number of seconds Amazon SQS retains a message"
  type        = number
  default     = 1209600 # 14 days
}

variable "dlq_message_retention_seconds" {
  description = "The number of seconds Amazon SQS retains a message in DLQ"
  type        = number
  default     = 1209600 # 14 days
}

variable "max_message_size" {
  description = "The limit of how many bytes a message can contain before Amazon SQS rejects it"
  type        = number
  default     = 262144 # 256 KB
}

variable "receive_wait_time_seconds" {
  description = "The time for which a ReceiveMessage call will wait for a message to arrive"
  type        = number
  default     = 20 # Enable long polling
}

variable "max_receive_count" {
  description = "The number of times a message is delivered to the source queue before being moved to the dead-letter queue"
  type        = number
  default     = 3
}

variable "audit_events_max_receive_count" {
  description = "The number of times a message is delivered to the audit events queue before being moved to the dead-letter queue"
  type        = number
  default     = 5
}

# Visibility Timeout Configuration
variable "user_events_visibility_timeout" {
  description = "The visibility timeout for the user events queue"
  type        = number
  default     = 30
}

variable "permission_events_visibility_timeout" {
  description = "The visibility timeout for the permission events queue"
  type        = number
  default     = 30
}

variable "audit_events_visibility_timeout" {
  description = "The visibility timeout for the audit events queue"
  type        = number
  default     = 60
}

# Security Configuration
variable "enable_sse" {
  description = "Enable server-side encryption for SQS queues"
  type        = bool
  default     = true
}

variable "kms_key_id" {
  description = "The ID of an AWS-managed customer master key (CMK) for Amazon SQS or a custom CMK"
  type        = string
  default     = null
}

# CloudWatch Alarms Configuration
variable "enable_cloudwatch_alarms" {
  description = "Enable CloudWatch alarms for SQS queues"
  type        = bool
  default     = true
}

variable "queue_depth_alarm_threshold" {
  description = "The threshold for queue depth alarm"
  type        = number
  default     = 100
}

variable "message_age_alarm_threshold" {
  description = "The threshold for message age alarm in seconds"
  type        = number
  default     = 300 # 5 minutes
}

variable "dlq_messages_alarm_threshold" {
  description = "The threshold for dead letter queue messages alarm"
  type        = number
  default     = 1
}

variable "alarm_actions" {
  description = "List of ARNs to notify when alarm triggers"
  type        = list(string)
  default     = []
}

# Environment-specific Configuration
variable "enable_high_throughput" {
  description = "Enable high throughput for FIFO queues (only for prod)"
  type        = bool
  default     = false
}

variable "deduplication_scope" {
  description = "Specifies whether message deduplication occurs at the message group or queue level"
  type        = string
  default     = "queue"
  validation {
    condition     = contains(["queue", "messageGroup"], var.deduplication_scope)
    error_message = "Deduplication scope must be either 'queue' or 'messageGroup'."
  }
}

variable "fifo_throughput_limit" {
  description = "Specifies whether the FIFO queue throughput quota applies to the entire queue or per message group"
  type        = string
  default     = "perQueue"
  validation {
    condition     = contains(["perQueue", "perMessageGroupId"], var.fifo_throughput_limit)
    error_message = "FIFO throughput limit must be either 'perQueue' or 'perMessageGroupId'."
  }
}