# JWT Settings Secret Outputs
output "jwt_settings_secret_arn" {
  description = "ARN of the JWT settings secret"
  value       = aws_secretsmanager_secret.jwt_settings.arn
}

output "jwt_settings_secret_name" {
  description = "Name of the JWT settings secret"
  value       = aws_secretsmanager_secret.jwt_settings.name
}

output "jwt_settings_secret_version_id" {
  description = "Version ID of the JWT settings secret"
  value       = aws_secretsmanager_secret_version.jwt_settings.version_id
}

# External API Keys Secret Outputs
output "external_api_keys_secret_arn" {
  description = "ARN of the external API keys secret"
  value       = aws_secretsmanager_secret.external_api_keys.arn
}

output "external_api_keys_secret_name" {
  description = "Name of the external API keys secret"
  value       = aws_secretsmanager_secret.external_api_keys.name
}

output "external_api_keys_secret_version_id" {
  description = "Version ID of the external API keys secret"
  value       = aws_secretsmanager_secret_version.external_api_keys.version_id
}

# Application Configuration Secret Outputs
output "app_config_secret_arn" {
  description = "ARN of the application configuration secret"
  value       = aws_secretsmanager_secret.app_config.arn
}

output "app_config_secret_name" {
  description = "Name of the application configuration secret"
  value       = aws_secretsmanager_secret.app_config.name
}

output "app_config_secret_version_id" {
  description = "Version ID of the application configuration secret"
  value       = aws_secretsmanager_secret_version.app_config.version_id
}

# All Secret ARNs (for IAM policies)
output "all_secret_arns" {
  description = "List of all secret ARNs created by this module"
  value = [
    aws_secretsmanager_secret.jwt_settings.arn,
    aws_secretsmanager_secret.external_api_keys.arn,
    aws_secretsmanager_secret.app_config.arn
  ]
}