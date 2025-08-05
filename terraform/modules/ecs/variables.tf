# General Configuration
variable "name_prefix" {
  description = "Prefix for resource names"
  type        = string
}

variable "aws_region" {
  description = "AWS region"
  type        = string
}

variable "tags" {
  description = "Tags to apply to all resources"
  type        = map(string)
  default     = {}
}

# ECS Cluster Configuration
variable "cluster_name" {
  description = "Name of the ECS cluster"
  type        = string
}

# ECS Task Configuration
variable "task_family" {
  description = "Family name for the ECS task definition"
  type        = string
}

variable "task_cpu" {
  description = "CPU units for the ECS task (256, 512, 1024, 2048, 4096)"
  type        = number
  default     = 512

  validation {
    condition     = contains([256, 512, 1024, 2048, 4096], var.task_cpu)
    error_message = "Task CPU must be one of: 256, 512, 1024, 2048, 4096."
  }
}

variable "task_memory" {
  description = "Memory for the ECS task in MB"
  type        = number
  default     = 1024
}

# Container Configuration
variable "container_name" {
  description = "Name of the container"
  type        = string
}

variable "container_image" {
  description = "Docker image for the container"
  type        = string
}

variable "container_port" {
  description = "Port the container listens on"
  type        = number
  default     = 8080
}

variable "health_check_path" {
  description = "Health check endpoint path"
  type        = string
  default     = "/api/health"
}

# Environment Variables and Secrets
variable "environment_variables" {
  description = "Environment variables for the container"
  type = list(object({
    name  = string
    value = string
  }))
  default = []
}

variable "secrets" {
  description = "Secrets for the container from AWS Secrets Manager"
  type = list(object({
    name      = string
    valueFrom = string
  }))
  default = []
}

variable "secrets_arns" {
  description = "ARNs of secrets that the task can access"
  type        = list(string)
  default     = []
}

# ECS Service Configuration
variable "service_name" {
  description = "Name of the ECS service"
  type        = string
}

variable "desired_count" {
  description = "Desired number of tasks"
  type        = number
  default     = 2
}

# Network Configuration
variable "private_subnet_ids" {
  description = "List of private subnet IDs for ECS tasks"
  type        = list(string)
}

variable "ecs_security_group_id" {
  description = "Security group ID for ECS tasks"
  type        = string
}

# Load Balancer Configuration
variable "target_group_arn" {
  description = "ARN of the target group for the load balancer"
  type        = string
}

# Auto Scaling Configuration
variable "min_capacity" {
  description = "Minimum number of tasks"
  type        = number
  default     = 1
}

variable "max_capacity" {
  description = "Maximum number of tasks"
  type        = number
  default     = 10
}

variable "cpu_target_value" {
  description = "Target CPU utilization percentage for auto scaling"
  type        = number
  default     = 70
}

variable "memory_target_value" {
  description = "Target memory utilization percentage for auto scaling"
  type        = number
  default     = 80
}

# Logging Configuration
variable "log_retention_days" {
  description = "CloudWatch log retention in days"
  type        = number
  default     = 7
}

# SQS Configuration
variable "sqs_queue_arns" {
  description = "ARNs of SQS queues that the task can access"
  type        = list(string)
  default     = []
}

# IAM Role Configuration
variable "ecs_task_execution_role_arn" {
  description = "ARN of the ECS task execution role"
  type        = string
  default     = ""
}

variable "ecs_task_role_arn" {
  description = "ARN of the ECS task role"
  type        = string
  default     = ""
}