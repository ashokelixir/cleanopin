# AWS Secrets Manager Implementation

This document describes the AWS Secrets Manager implementation for the CleanArchTemplate project.

## Overview

The Secrets Manager module creates and manages application secrets in AWS Secrets Manager, providing secure storage and retrieval of sensitive configuration data.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    AWS Secrets Manager                      │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │  JWT Settings   │  │ External APIs   │  │  App Config     │ │
│  │                 │  │                 │  │                 │ │
│  │ • SecretKey     │  │ • DatadogApiKey │  │ • EncryptionKey │ │
│  │ • Issuer        │  │ • SeqApiKey     │  │ • CORS Origins  │ │
│  │ • Audience      │  │ • AwsAccessKey  │  │ • Swagger       │ │
│  │ • Expiration    │  │ • AwsSecretKey  │  │ • Environment   │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────┐
│                      ECS Tasks                              │
├─────────────────────────────────────────────────────────────┤
│  • IAM permissions to read secrets                          │
│  • Secrets retrieved via AWS SDK                           │
│  • Cached locally for performance                          │
└─────────────────────────────────────────────────────────────┘
```

## Secrets Created

### 1. JWT Settings (`{name_prefix}-jwt-settings`)
Contains JWT authentication configuration:
- `SecretKey`: JWT signing secret key
- `Issuer`: JWT token issuer
- `Audience`: JWT token audience  
- `AccessTokenExpirationMinutes`: Access token expiration
- `RefreshTokenExpirationDays`: Refresh token expiration

### 2. External API Keys (`{name_prefix}-external-api-keys`)
Contains external service credentials:
- `DatadogApiKey`: Datadog monitoring API key
- `SeqApiKey`: Seq logging API key
- `AwsAccessKey`: AWS access key for application services
- `AwsSecretKey`: AWS secret key for application services
- `RedisPassword`: Redis cache password

### 3. Application Configuration (`{name_prefix}-app-config`)
Contains application-specific settings:
- `EncryptionKey`: Application data encryption key
- `CorsAllowedOrigins`: CORS allowed origins (JSON array)
- `SwaggerEnabled`: Whether Swagger UI is enabled
- `Environment`: Current environment name

## Terraform Module

### Usage

```hcl
module "secrets" {
  source = "./modules/secrets"

  name_prefix   = "cleanarch-dev"
  environment   = "development"
  
  # JWT Configuration
  jwt_secret_key = var.jwt_secret_key
  jwt_issuer     = "CleanArchTemplate"
  jwt_audience   = "CleanArchTemplate"
  
  # External API Keys
  datadog_api_key = var.datadog_api_key
  seq_api_key     = var.seq_api_key
  
  # Application Configuration
  encryption_key = var.encryption_key
  
  tags = local.common_tags
}
```

### Variables

| Variable | Type | Description | Required |
|----------|------|-------------|----------|
| `name_prefix` | string | Prefix for resource names | Yes |
| `environment` | string | Environment name | Yes |
| `jwt_secret_key` | string | JWT signing secret key | Yes |
| `encryption_key` | string | Application encryption key | Yes |
| `datadog_api_key` | string | Datadog API key | No |
| `seq_api_key` | string | Seq API key | No |
| `cors_allowed_origins` | string | CORS origins JSON | No |
| `swagger_enabled` | bool | Enable Swagger | No |

### Outputs

| Output | Description |
|--------|-------------|
| `jwt_settings_secret_arn` | ARN of JWT settings secret |
| `external_api_keys_secret_arn` | ARN of external API keys secret |
| `app_config_secret_arn` | ARN of app config secret |
| `all_secret_arns` | List of all secret ARNs |

## Environment Configuration

### Development (`dev.tfvars`)
```hcl
jwt_secret_key = "dev-super-secret-jwt-key-that-is-at-least-32-characters-long"
encryption_key = "dev-encryption-key-32-chars-long"
cors_allowed_origins = "[\"http://localhost:3000\", \"http://localhost:8080\"]"
swagger_enabled = true
```

### Staging (`staging.tfvars`)
```hcl
jwt_secret_key = "staging-super-secret-jwt-key-that-is-at-least-32-characters-long"
encryption_key = "staging-encryption-key-32-chars"
cors_allowed_origins = "[\"https://staging.cleanarch.com\"]"
swagger_enabled = false
datadog_api_key = "your-datadog-api-key-for-staging"
```

### Production (`prod.tfvars`)
```hcl
jwt_secret_key = "production-super-secret-jwt-key-that-is-at-least-32-characters-long"
encryption_key = "production-encryption-key-32-chars"
cors_allowed_origins = "[\"https://cleanarch.com\", \"https://www.cleanarch.com\"]"
swagger_enabled = false
datadog_api_key = "your-production-datadog-api-key"
```

## IAM Permissions

The ECS task role is automatically granted the following permissions:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": [
        "secretsmanager:GetSecretValue",
        "secretsmanager:DescribeSecret"
      ],
      "Resource": [
        "arn:aws:secretsmanager:region:account:secret:cleanarch-*"
      ]
    }
  ]
}
```

## Application Integration

The .NET application uses the `SecretsManagerService` to retrieve secrets:

```csharp
// Retrieve JWT settings
var jwtSettings = await _secretsManager.GetSecretAsync<JwtSettings>("jwt-settings");

// Retrieve external API keys
var apiKeys = await _secretsManager.GetSecretAsync<ExternalApiKeys>("external-api-keys");

// Retrieve app configuration
var appConfig = await _secretsManager.GetSecretAsync<AppConfig>("app-config");
```

## Security Features

1. **Encryption at Rest**: All secrets are encrypted using AWS KMS
2. **Access Control**: IAM policies restrict access to specific secrets
3. **Audit Logging**: All secret access is logged in CloudTrail
4. **Recovery Window**: Deleted secrets have a 7-day recovery window (30 days in prod)
5. **Versioning**: Secret versions are maintained for rollback capability

## Deployment

### Deploy Secrets Infrastructure

```bash
# Validate configuration
./scripts/validate-secrets.ps1 -Environment dev

# Deploy secrets
./scripts/deploy-secrets.ps1 -Environment dev

# Test deployment
./tests/secrets.test.ps1 -Environment dev
```

### Update Secret Values

```bash
# Update via AWS CLI
aws secretsmanager update-secret \
  --secret-id cleanarch-dev-jwt-settings \
  --secret-string '{"SecretKey":"new-secret-key",...}'

# Update via Terraform (not recommended for sensitive values)
terraform apply -var="jwt_secret_key=new-secret-key"
```

## Monitoring and Alerting

### CloudWatch Metrics
- Secret retrieval success/failure rates
- Secret access frequency
- Secret rotation status

### Recommended Alarms
- Failed secret retrievals
- Unusual access patterns
- Secrets nearing expiration

## Best Practices

1. **Secret Rotation**: Implement regular secret rotation for production
2. **Least Privilege**: Grant minimal required permissions
3. **Environment Separation**: Use different secrets for each environment
4. **Caching**: Cache secrets locally to reduce API calls
5. **Error Handling**: Implement proper error handling for secret retrieval failures

## Troubleshooting

### Common Issues

1. **Permission Denied**
   - Check IAM role permissions
   - Verify secret ARNs in IAM policy

2. **Secret Not Found**
   - Verify secret name and region
   - Check if secret was deleted

3. **Invalid Secret Format**
   - Ensure secret value is valid JSON
   - Check for required keys in secret

### Debug Commands

```bash
# List all secrets
aws secretsmanager list-secrets

# Describe specific secret
aws secretsmanager describe-secret --secret-id cleanarch-dev-jwt-settings

# Get secret value (be careful with sensitive data)
aws secretsmanager get-secret-value --secret-id cleanarch-dev-jwt-settings
```

## Cost Optimization

- Secrets Manager charges $0.40 per secret per month
- Additional charges for API calls ($0.05 per 10,000 requests)
- Use caching to minimize API calls
- Consider consolidating related secrets to reduce costs