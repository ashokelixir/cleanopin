# Common variables
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

variable "recovery_window_in_days" {
  description = "Number of days to retain secret after deletion"
  type        = number
  default     = 7
}

# JWT Configuration
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

# External API Keys
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

# Application Configuration
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