# Security Groups Module Variables
variable "name_prefix" {
  description = "Name prefix for security groups"
  type        = string
}

variable "vpc_id" {
  description = "ID of the VPC"
  type        = string
}

variable "vpc_cidr_block" {
  description = "CIDR block of the VPC"
  type        = string
}

variable "app_port" {
  description = "Application port"
  type        = number
  default     = 8080
}

variable "allowed_ssh_cidr_blocks" {
  description = "CIDR blocks allowed to SSH to bastion host"
  type        = list(string)
  default     = ["10.0.0.0/8"] # Default to private networks only
}

variable "tags" {
  description = "Tags to apply to resources"
  type        = map(string)
  default     = {}
}