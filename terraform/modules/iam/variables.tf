# General Configuration
variable "name_prefix" {
  description = "Prefix for resource names"
  type        = string
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default     = {}
}

# Secrets Manager Configuration
variable "secrets_manager_arns" {
  description = "ARNs of AWS Secrets Manager secrets that ECS tasks can access"
  type        = list(string)
  default     = []
}

# SQS Configuration
variable "sqs_queue_arns" {
  description = "ARNs of SQS queues that ECS tasks can access"
  type        = list(string)
  default     = []
}

# RDS Configuration
variable "enable_rds_access" {
  description = "Enable RDS access for ECS tasks (for RDS Proxy)"
  type        = bool
  default     = false
}

variable "rds_db_instance_id" {
  description = "RDS database instance identifier"
  type        = string
  default     = ""
}

variable "rds_db_username" {
  description = "RDS database username for IAM authentication"
  type        = string
  default     = ""
}

# X-Ray Configuration
variable "enable_xray" {
  description = "Enable AWS X-Ray tracing permissions"
  type        = bool
  default     = true
}

# CI/CD Cross-Account Configuration
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

# Lambda Configuration
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

# ElastiCache Configuration
variable "elasticache_cluster_arns" {
  description = "ARNs of ElastiCache clusters that ECS tasks can access"
  type        = list(string)
  default     = []
}

# Additional AWS Services Configuration
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

# Environment-specific Configuration
variable "environment" {
  description = "Environment name (dev, staging, prod)"
  type        = string
  default     = "dev"

  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "Environment must be one of: dev, staging, prod."
  }
}

# Security Configuration
variable "enable_resource_based_policies" {
  description = "Enable additional resource-based policy conditions"
  type        = bool
  default     = true
}

variable "allowed_ip_ranges" {
  description = "IP ranges allowed for certain operations (CIDR blocks)"
  type        = list(string)
  default     = []
}