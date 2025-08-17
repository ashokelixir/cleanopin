# Global variables
variable "aws_region" {
  description = "AWS region for resources"
  type        = string
  default     = "ap-south-1"
}

variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be one of: dev, staging, prod."
  }
}

variable "project_name" {
  description = "Name of the project"
  type        = string
  default     = "cleanarch-template"
}

variable "owner" {
  description = "Owner of the resources"
  type        = string
  default     = "platform-team"
}

# VPC Configuration
variable "vpc_cidr" {
  description = "CIDR block for VPC"
  type        = string
  default     = "10.0.0.0/16"
}

variable "availability_zones" {
  description = "List of availability zones"
  type        = list(string)
  default     = ["ap-south-1a", "ap-south-1b", "ap-south-1c"]
}

# Database Configuration
variable "db_instance_class" {
  description = "RDS instance class"
  type        = string
  default     = "db.t3.micro"
}

variable "db_allocated_storage" {
  description = "RDS allocated storage in GB"
  type        = number
  default     = 20
}

variable "db_max_allocated_storage" {
  description = "RDS maximum allocated storage in GB"
  type        = number
  default     = 100
}

variable "postgres_version" {
  description = "PostgreSQL engine version"
  type        = string
  default     = "15.13"
}

variable "db_backup_retention_period" {
  description = "Database backup retention period in days"
  type        = number
  default     = 7
}

variable "db_backup_window" {
  description = "Database backup window (UTC)"
  type        = string
  default     = "03:00-04:00"
}

variable "db_maintenance_window" {
  description = "Database maintenance window (UTC)"
  type        = string
  default     = "sun:04:00-sun:05:00"
}

variable "enable_performance_insights" {
  description = "Enable RDS Performance Insights"
  type        = bool
  default     = true
}

variable "performance_insights_retention_period" {
  description = "Performance Insights retention period in days"
  type        = number
  default     = 7
}

# Cache Configuration
variable "redis_node_type" {
  description = "ElastiCache Redis node type"
  type        = string
  default     = "cache.t3.micro"
}

variable "redis_num_cache_nodes" {
  description = "Number of cache nodes"
  type        = number
  default     = 1
}

# ECS Configuration
variable "ecs_task_cpu" {
  description = "ECS task CPU units"
  type        = number
  default     = 512
}

variable "ecs_task_memory" {
  description = "ECS task memory in MB"
  type        = number
  default     = 1024
}

variable "ecs_desired_count" {
  description = "Desired number of ECS tasks"
  type        = number
  default     = 2
}

variable "ecs_min_capacity" {
  description = "Minimum number of ECS tasks for auto scaling"
  type        = number
  default     = 1
}

variable "ecs_max_capacity" {
  description = "Maximum number of ECS tasks for auto scaling"
  type        = number
  default     = 10
}

variable "ecs_cpu_target_value" {
  description = "Target CPU utilization percentage for ECS auto scaling"
  type        = number
  default     = 70
}

variable "ecs_memory_target_value" {
  description = "Target memory utilization percentage for ECS auto scaling"
  type        = number
  default     = 80
}

variable "ecs_log_retention_days" {
  description = "ECS CloudWatch log retention in days"
  type        = number
  default     = 7
}

variable "container_image" {
  description = "Docker container image for the application"
  type        = string
  default     = "nginx:latest" # Placeholder - should be replaced with actual application image
}

# ALB Configuration
variable "certificate_arn" {
  description = "ARN of the SSL certificate for HTTPS listener"
  type        = string
  default     = ""
}

variable "alb_log_retention_days" {
  description = "ALB access log retention in days"
  type        = number
  default     = 30
}

# Application Configuration
variable "app_port" {
  description = "Application port"
  type        = number
  default     = 8080
}

variable "health_check_path" {
  description = "Health check endpoint path"
  type        = string
  default     = "/health"
}

# Security Configuration
variable "allowed_ssh_cidr_blocks" {
  description = "CIDR blocks allowed to SSH to bastion host"
  type        = list(string)
  default     = ["10.0.0.0/8"] # Default to private networks only
}

# IAM Configuration
variable "cicd_account_ids" {
  description = "List of AWS account IDs that can assume the CI/CD role"
  type        = list(string)
  default     = []
}

variable "cicd_external_id" {
  description = "External ID for CI/CD cross-account role assumption"
  type        = string
  default     = ""
  sensitive   = true
}

variable "ecr_repository_arns" {
  description = "ARNs of ECR repositories that CI/CD can access"
  type        = list(string)
  default     = []
}

variable "enable_xray" {
  description = "Enable AWS X-Ray tracing permissions"
  type        = bool
  default     = true
}

variable "enable_s3_access" {
  description = "Enable S3 access for ECS tasks"
  type        = bool
  default     = false
}

variable "s3_bucket_arns" {
  description = "ARNs of S3 buckets that ECS tasks can access"
  type        = list(string)
  default     = []
}

variable "enable_sns_access" {
  description = "Enable SNS access for ECS tasks"
  type        = bool
  default     = false
}

variable "sns_topic_arns" {
  description = "ARNs of SNS topics that ECS tasks can access"
  type        = list(string)
  default     = []
}

variable "enable_rds_access" {
  description = "Enable RDS access for ECS tasks (for RDS Proxy)"
  type        = bool
  default     = false
}

variable "create_lambda_role" {
  description = "Create IAM role for Lambda functions"
  type        = bool
  default     = false
}

variable "lambda_vpc_access" {
  description = "Enable VPC access for Lambda functions"
  type        = bool
  default     = false
}

# Secrets Manager Configuration
variable "jwt_secret_key" {
  description = "JWT signing secret key"
  type        = string
  sensitive   = true
}

variable "jwt_issuer" {
  description = "JWT issuer"
  type        = string
  default     = "CleanArchTemplate"
}

variable "jwt_audience" {
  description = "JWT audience"
  type        = string
  default     = "CleanArchTemplate"
}

variable "jwt_access_token_expiration_minutes" {
  description = "JWT access token expiration in minutes"
  type        = number
  default     = 60
}

variable "jwt_refresh_token_expiration_days" {
  description = "JWT refresh token expiration in days"
  type        = number
  default     = 7
}

variable "datadog_api_key" {
  description = "Datadog API key for monitoring"
  type        = string
  sensitive   = true
  default     = ""
}

variable "seq_api_key" {
  description = "Seq API key for logging"
  type        = string
  sensitive   = true
  default     = ""
}

variable "aws_access_key" {
  description = "AWS access key for application services"
  type        = string
  sensitive   = true
  default     = ""
}

variable "aws_secret_key" {
  description = "AWS secret key for application services"
  type        = string
  sensitive   = true
  default     = ""
}

variable "redis_password" {
  description = "Redis password for cache access"
  type        = string
  sensitive   = true
  default     = ""
}

variable "encryption_key" {
  description = "Application encryption key"
  type        = string
  sensitive   = true
}

variable "cors_allowed_origins" {
  description = "CORS allowed origins as JSON array string"
  type        = string
  default     = "[\"*\"]"
}

variable "swagger_enabled" {
  description = "Whether Swagger is enabled"
  type        = bool
  default     = false
}

# SQS Configuration
variable "sqs_message_retention_seconds" {
  description = "The number of seconds Amazon SQS retains a message"
  type        = number
  default     = 1209600 # 14 days
}

variable "sqs_dlq_message_retention_seconds" {
  description = "The number of seconds Amazon SQS retains a message in DLQ"
  type        = number
  default     = 1209600 # 14 days
}

variable "sqs_max_receive_count" {
  description = "The number of times a message is delivered to the source queue before being moved to the dead-letter queue"
  type        = number
  default     = 3
}

variable "sqs_audit_events_max_receive_count" {
  description = "The number of times a message is delivered to the audit events queue before being moved to the dead-letter queue"
  type        = number
  default     = 5
}

variable "sqs_user_events_visibility_timeout" {
  description = "The visibility timeout for the user events queue"
  type        = number
  default     = 30
}

variable "sqs_permission_events_visibility_timeout" {
  description = "The visibility timeout for the permission events queue"
  type        = number
  default     = 30
}

variable "sqs_audit_events_visibility_timeout" {
  description = "The visibility timeout for the audit events queue"
  type        = number
  default     = 60
}

variable "sqs_enable_sse" {
  description = "Enable server-side encryption for SQS queues"
  type        = bool
  default     = true
}

variable "sqs_kms_key_id" {
  description = "The ID of an AWS-managed customer master key (CMK) for Amazon SQS or a custom CMK"
  type        = string
  default     = null
}

variable "sqs_enable_cloudwatch_alarms" {
  description = "Enable CloudWatch alarms for SQS queues"
  type        = bool
  default     = true
}

variable "sqs_queue_depth_alarm_threshold" {
  description = "The threshold for queue depth alarm"
  type        = number
  default     = 100
}

variable "sqs_message_age_alarm_threshold" {
  description = "The threshold for message age alarm in seconds"
  type        = number
  default     = 300 # 5 minutes
}

variable "sqs_dlq_messages_alarm_threshold" {
  description = "The threshold for dead letter queue messages alarm"
  type        = number
  default     = 1
}

variable "sqs_alarm_actions" {
  description = "List of ARNs to notify when SQS alarm triggers"
  type        = list(string)
  default     = []
}

variable "sqs_enable_high_throughput" {
  description = "Enable high throughput for FIFO queues (only for prod)"
  type        = bool
  default     = false
}

variable "sqs_deduplication_scope" {
  description = "Specifies whether message deduplication occurs at the message group or queue level"
  type        = string
  default     = "queue"
  validation {
    condition     = contains(["queue", "messageGroup"], var.sqs_deduplication_scope)
    error_message = "Deduplication scope must be either 'queue' or 'messageGroup'."
  }
}

variable "sqs_fifo_throughput_limit" {
  description = "Specifies whether the FIFO queue throughput quota applies to the entire queue or per message group"
  type        = string
  default     = "perQueue"
  validation {
    condition     = contains(["perQueue", "perMessageGroupId"], var.sqs_fifo_throughput_limit)
    error_message = "FIFO throughput limit must be either 'perQueue' or 'perMessageGroupId'."
  }
}