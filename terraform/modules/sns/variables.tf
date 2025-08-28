# SNS Module Variables

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

# Security Configuration
variable "enable_encryption" {
  description = "Enable server-side encryption for SNS topics"
  type        = bool
  default     = true
}

variable "kms_key_id" {
  description = "The ID of an AWS-managed customer master key (CMK) for SNS or a custom CMK"
  type        = string
  default     = null
}

# IAM Configuration
variable "ecs_task_role_arn" {
  description = "ARN of the ECS task role that can publish to SNS topics"
  type        = string
  default     = null
}

# Email Notifications
variable "enable_email_notifications" {
  description = "Enable email notifications for SNS topics"
  type        = bool
  default     = true
}

variable "notification_emails" {
  description = "List of email addresses to receive notifications"
  type        = list(string)
  default     = []
  validation {
    condition = alltrue([
      for email in var.notification_emails : can(regex("^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$", email))
    ])
    error_message = "All notification emails must be valid email addresses."
  }
}

# Slack Notifications
variable "enable_slack_notifications" {
  description = "Enable Slack notifications for SNS topics"
  type        = bool
  default     = false
}

variable "slack_webhook_url" {
  description = "Slack webhook URL for notifications"
  type        = string
  default     = null
  sensitive   = true
}

# SMS Notifications
variable "enable_sms_notifications" {
  description = "Enable SMS notifications for critical alerts"
  type        = bool
  default     = false
}

variable "notification_phone_numbers" {
  description = "List of phone numbers to receive SMS notifications (E.164 format)"
  type        = list(string)
  default     = []
  validation {
    condition = alltrue([
      for phone in var.notification_phone_numbers : can(regex("^\\+[1-9]\\d{1,14}$", phone))
    ])
    error_message = "All phone numbers must be in E.164 format (e.g., +1234567890)."
  }
}

# Topic Configuration
variable "delivery_retry_attempts" {
  description = "Number of retry attempts for message delivery"
  type        = number
  default     = 3
  validation {
    condition     = var.delivery_retry_attempts >= 0 && var.delivery_retry_attempts <= 100
    error_message = "Delivery retry attempts must be between 0 and 100."
  }
}

variable "delivery_delay_seconds" {
  description = "Delay between retry attempts in seconds"
  type        = number
  default     = 20
  validation {
    condition     = var.delivery_delay_seconds >= 1 && var.delivery_delay_seconds <= 3600
    error_message = "Delivery delay must be between 1 and 3600 seconds."
  }
}