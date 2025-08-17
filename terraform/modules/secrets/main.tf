# Application secrets for CleanArchTemplate

# JWT Settings Secret
resource "aws_secretsmanager_secret" "jwt_settings" {
  name                    = "${var.name_prefix}-jwt-settings"
  description             = "JWT configuration settings for ${var.name_prefix}"
  recovery_window_in_days = var.recovery_window_in_days

  tags = merge(var.tags, {
    Name      = "${var.name_prefix}-jwt-settings"
    Type      = "application-secret"
    Component = "authentication"
  })
}

resource "aws_secretsmanager_secret_version" "jwt_settings" {
  secret_id = aws_secretsmanager_secret.jwt_settings.id
  secret_string = jsonencode({
    SecretKey                    = var.jwt_secret_key
    Issuer                       = var.jwt_issuer
    Audience                     = var.jwt_audience
    AccessTokenExpirationMinutes = var.jwt_access_token_expiration_minutes
    RefreshTokenExpirationDays   = var.jwt_refresh_token_expiration_days
  })

  lifecycle {
    ignore_changes = [secret_string]
  }
}

# External API Keys Secret
resource "aws_secretsmanager_secret" "external_api_keys" {
  name                    = "${var.name_prefix}-external-api-keys"
  description             = "External API keys and credentials for ${var.name_prefix}"
  recovery_window_in_days = var.recovery_window_in_days

  tags = merge(var.tags, {
    Name      = "${var.name_prefix}-external-api-keys"
    Type      = "application-secret"
    Component = "external-integrations"
  })
}

resource "aws_secretsmanager_secret_version" "external_api_keys" {
  secret_id = aws_secretsmanager_secret.external_api_keys.id
  secret_string = jsonencode({
    DatadogApiKey = var.datadog_api_key
    SeqApiKey     = var.seq_api_key
    AwsAccessKey  = var.aws_access_key
    AwsSecretKey  = var.aws_secret_key
    RedisPassword = var.redis_password
  })

  lifecycle {
    ignore_changes = [secret_string]
  }
}

# Application Configuration Secret (for sensitive app settings)
resource "aws_secretsmanager_secret" "app_config" {
  name                    = "${var.name_prefix}-app-config"
  description             = "Application configuration secrets for ${var.name_prefix}"
  recovery_window_in_days = var.recovery_window_in_days

  tags = merge(var.tags, {
    Name      = "${var.name_prefix}-app-config"
    Type      = "application-secret"
    Component = "configuration"
  })
}

resource "aws_secretsmanager_secret_version" "app_config" {
  secret_id = aws_secretsmanager_secret.app_config.id
  secret_string = jsonencode({
    EncryptionKey      = var.encryption_key
    CorsAllowedOrigins = var.cors_allowed_origins
    SwaggerEnabled     = var.swagger_enabled
    Environment        = var.environment
  })

  lifecycle {
    ignore_changes = [secret_string]
  }
}