# Secrets Module

This module creates AWS Secrets Manager secrets for the CleanArchTemplate application.

## Secrets Created

1. **JWT Settings** (`{name_prefix}-jwt-settings`)
   - JWT signing secret key
   - Issuer and audience configuration
   - Token expiration settings

2. **External API Keys** (`{name_prefix}-external-api-keys`)
   - Datadog API key for monitoring
   - Seq API key for logging
   - AWS credentials for application services
   - Redis password for cache access

3. **Application Configuration** (`{name_prefix}-app-config`)
   - Application encryption key
   - CORS configuration
   - Environment-specific settings

## Usage

```hcl
module "secrets" {
  source = "./modules/secrets"

  name_prefix   = "cleanarch-dev"
  environment   = "development"
  
  # JWT Configuration
  jwt_secret_key = "your-super-secret-jwt-key-here"
  jwt_issuer     = "CleanArchTemplate"
  jwt_audience   = "CleanArchTemplate"
  
  # External API Keys
  datadog_api_key = "your-datadog-api-key"
  seq_api_key     = "your-seq-api-key"
  
  # Application Configuration
  encryption_key = "your-application-encryption-key"
  
  tags = {
    Environment = "development"
    Project     = "CleanArchTemplate"
  }
}
```

## Outputs

- `jwt_settings_secret_arn` - ARN of the JWT settings secret
- `external_api_keys_secret_arn` - ARN of the external API keys secret
- `app_config_secret_arn` - ARN of the application configuration secret
- `all_secret_arns` - List of all secret ARNs for IAM policy configuration

## Security Considerations

- All secrets are encrypted at rest using AWS KMS
- Secrets have a recovery window of 7 days by default
- Secret values use lifecycle ignore_changes to prevent accidental updates
- All sensitive variables are marked as sensitive in Terraform