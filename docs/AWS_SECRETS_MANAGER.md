# AWS Secrets Manager Integration

This document describes how to use the AWS Secrets Manager integration in the Clean Architecture Template.

## Overview

The AWS Secrets Manager integration provides secure secret management with the following features:

- **Configuration Integration**: Seamlessly integrates with .NET's configuration system
- **Caching**: Reduces API calls with configurable in-memory caching
- **Resilience**: Built-in retry policies with exponential backoff
- **Environment-Specific**: Supports different secrets per environment
- **Automatic Rotation**: Detects and handles secret rotation
- **Local Development**: Supports local development without AWS dependencies

## Configuration

### appsettings.json

```json
{
  "SecretsManager": {
    "Region": "us-east-1",
    "Environment": "development",
    "CacheDurationMinutes": 15,
    "MaxRetryAttempts": 3,
    "BaseDelayMs": 1000,
    "EnableRotationDetection": true,
    "PreloadSecrets": [
      "database-credentials",
      "jwt-settings"
    ],
    "UseLocalDevelopment": true
  }
}
```

### Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `Region` | AWS region for Secrets Manager | `us-east-1` |
| `Environment` | Environment prefix for secret names | `development` |
| `CacheDurationMinutes` | How long to cache secrets in memory | `15` |
| `MaxRetryAttempts` | Maximum retry attempts for failed requests | `3` |
| `BaseDelayMs` | Base delay for exponential backoff | `1000` |
| `EnableRotationDetection` | Enable automatic rotation detection | `true` |
| `PreloadSecrets` | Secrets to load during startup | `[]` |
| `UseLocalDevelopment` | Use local development mode | `false` |

## Usage

### 1. Basic Secret Retrieval

```csharp
public class MyService
{
    private readonly ISecretsManagerService _secretsService;

    public MyService(ISecretsManagerService secretsService)
    {
        _secretsService = secretsService;
    }

    public async Task<string> GetDatabaseConnectionAsync()
    {
        return await _secretsService.GetSecretAsync("database-credentials");
    }
}
```

### 2. JSON Secret Deserialization

```csharp
public class DatabaseCredentials
{
    public string Host { get; set; }
    public int Port { get; set; }
    public string Database { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}

// Usage
var credentials = await _secretsService.GetSecretAsync<DatabaseCredentials>("database-credentials");
```

### 3. Batch Secret Retrieval

```csharp
var secretNames = new[] { "database-credentials", "jwt-settings", "api-keys" };
var secrets = await _secretsService.GetSecretsAsync(secretNames);

foreach (var secret in secrets)
{
    Console.WriteLine($"Secret: {secret.Key}, Length: {secret.Value.Length}");
}
```

### 4. Configuration Integration

```csharp
// In Program.cs
builder.Configuration.AddSecretsManager(
    new[] { "database-credentials", "jwt-settings" },
    region: "us-east-1",
    environment: "production",
    optional: true);

// Access through IConfiguration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
```

## Secret Structure in AWS

### Database Credentials

**Secret Name**: `{environment}/database-credentials`

```json
{
  "host": "your-rds-endpoint.region.rds.amazonaws.com",
  "port": 5432,
  "database": "cleanarch",
  "username": "app_user",
  "password": "secure-generated-password",
  "engine": "postgres"
}
```

### JWT Settings

**Secret Name**: `{environment}/jwt-settings`

```json
{
  "Jwt:SecretKey": "your-super-secret-jwt-signing-key",
  "Jwt:Issuer": "CleanArchTemplate",
  "Jwt:Audience": "CleanArchTemplate",
  "Jwt:AccessTokenExpirationMinutes": "60"
}
```

### External API Keys

**Secret Name**: `{environment}/external-api-keys`

```json
{
  "Datadog:ApiKey": "your-datadog-api-key",
  "SendGrid:ApiKey": "your-sendgrid-api-key",
  "Stripe:SecretKey": "your-stripe-secret-key"
}
```

## Environment Setup

### Development

In development, set `UseLocalDevelopment: true` to avoid AWS calls:

```json
{
  "SecretsManager": {
    "UseLocalDevelopment": true
  }
}
```

### Production

1. **Create Secrets in AWS Secrets Manager**:
   ```bash
   aws secretsmanager create-secret \
     --name "production/database-credentials" \
     --description "Database credentials for production" \
     --secret-string file://database-credentials.json
   ```

2. **Configure IAM Permissions**:
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
           "arn:aws:secretsmanager:us-east-1:*:secret:production/*"
         ]
       }
     ]
   }
   ```

3. **Set Environment Variables**:
   ```bash
   export AWS_REGION=us-east-1
   export AWS_ACCESS_KEY_ID=your-access-key
   export AWS_SECRET_ACCESS_KEY=your-secret-key
   ```

## Automatic Secret Rotation

The service supports automatic detection of rotated secrets:

### Database Credentials Rotation

```csharp
// Add to Program.cs for production
if (app.Environment.IsProduction())
{
    builder.Services.AddDatabaseCredentialsRotation();
}
```

### Manual Cache Invalidation

```csharp
// Invalidate specific secret
_secretsService.InvalidateCache("database-credentials");

// Clear all cached secrets
_secretsService.ClearCache();
```

## Error Handling

The service includes comprehensive error handling:

```csharp
try
{
    var secret = await _secretsService.GetSecretAsync("my-secret");
}
catch (AmazonSecretsManagerException ex)
{
    // Handle AWS-specific errors
    _logger.LogError(ex, "AWS Secrets Manager error");
}
catch (InvalidOperationException ex)
{
    // Handle deserialization errors
    _logger.LogError(ex, "Failed to deserialize secret");
}
```

## Monitoring and Logging

The service provides detailed logging:

- **Debug**: Cache hits/misses, secret retrieval attempts
- **Information**: Successful operations, cache invalidations
- **Warning**: Retry attempts, optional secret failures
- **Error**: Failed operations, deserialization errors

### Example Log Output

```
[10:30:00 INF] Successfully retrieved secret database-credentials
[10:30:01 DBG] Cached secret with key secret:production/database-credentials for 30 minutes
[10:35:00 DBG] Retrieved secret jwt-settings from cache
[10:40:00 WRN] Retry attempt 2 for secret retrieval after 2000ms. Exception: Request timeout
```

## Best Practices

1. **Use Environment Prefixes**: Always prefix secrets with environment names
2. **Enable Caching**: Use appropriate cache durations to reduce API calls
3. **Handle Failures Gracefully**: Make secrets optional where possible
4. **Monitor Usage**: Set up CloudWatch alarms for secret access
5. **Rotate Regularly**: Enable automatic rotation for sensitive secrets
6. **Least Privilege**: Grant minimal required IAM permissions
7. **Local Development**: Use local development mode to avoid AWS dependencies

## Testing

### Unit Tests

```csharp
[Test]
public async Task GetSecretAsync_ReturnsSecret_WhenSecretExists()
{
    // Arrange
    var mockSecretsManager = new Mock<IAmazonSecretsManager>();
    var mockCache = new Mock<IMemoryCache>();
    var mockLogger = new Mock<ILogger<SecretsManagerService>>();
    var settings = Options.Create(new SecretsManagerSettings());

    var service = new SecretsManagerService(
        mockSecretsManager.Object,
        mockCache.Object,
        mockLogger.Object,
        settings);

    // Act & Assert
    // ... test implementation
}
```

### Integration Tests

```csharp
[Test]
public async Task SecretsManager_Integration_Test()
{
    // Use TestContainers or LocalStack for integration testing
    var factory = new WebApplicationFactory<Program>();
    var client = factory.CreateClient();
    
    var response = await client.GetAsync("/api/v1/secrets/health");
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}
```

## Troubleshooting

### Common Issues

1. **Secret Not Found**: Check secret name and environment prefix
   ```bash
   # List all secrets for your environment
   aws secretsmanager list-secrets --query "SecretList[?starts_with(Name, 'production/')]"
   ```

2. **Access Denied**: Verify IAM permissions
   ```bash
   # Test if you can access a specific secret
   aws secretsmanager get-secret-value --secret-id "production/database-credentials" --query "SecretString" --output text
   ```

3. **Deserialization Errors**: Validate JSON structure in secret
   ```bash
   # Get and validate JSON structure
   aws secretsmanager get-secret-value --secret-id "production/database-credentials" --query "SecretString" --output text | jq .
   ```

4. **Connection Timeouts**: Check network connectivity and retry settings
   - Increase `MaxRetryAttempts` and `BaseDelayMs` in configuration
   - Check VPC endpoints if running in private subnets
   - Verify security group rules allow HTTPS outbound traffic

5. **Cache Issues**: Clear cache or restart application
   ```csharp
   // Clear specific secret from cache
   _secretsService.InvalidateCache("database-credentials");
   
   // Clear all cached secrets
   _secretsService.ClearCache();
   ```

6. **Local Development Issues**: 
   - Ensure `UseLocalDevelopment` is set to `true` in development
   - Check that AWS credentials are not required in local mode

### Debug Commands

```bash
# Test AWS credentials
aws sts get-caller-identity

# List all secrets
aws secretsmanager list-secrets

# List secrets for specific environment
aws secretsmanager list-secrets --query "SecretList[?starts_with(Name, 'production/')]"

# Get secret value
aws secretsmanager get-secret-value --secret-id "production/database-credentials"

# Get secret value as JSON and format it
aws secretsmanager get-secret-value --secret-id "production/database-credentials" --query "SecretString" --output text | jq .

# Check secret metadata
aws secretsmanager describe-secret --secret-id "production/database-credentials"

# Test connectivity to Secrets Manager
curl -I https://secretsmanager.us-east-1.amazonaws.com
```

### Performance Monitoring

Monitor your Secrets Manager usage with these CloudWatch metrics:

```bash
# Get API call metrics
aws cloudwatch get-metric-statistics \
  --namespace AWS/SecretsManager \
  --metric-name SuccessfulRequestLatency \
  --dimensions Name=SecretName,Value=production/database-credentials \
  --start-time 2024-01-01T00:00:00Z \
  --end-time 2024-01-02T00:00:00Z \
  --period 3600 \
  --statistics Average
```

### Logging Configuration

Enable detailed logging for troubleshooting:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Override": {
        "CleanArchTemplate.Infrastructure.Services.SecretsManagerService": "Debug"
      }
    }
  }
}
```

### Health Check Endpoint

Use the built-in health check endpoint to verify Secrets Manager connectivity:

```bash
# Check Secrets Manager health
curl -H "Authorization: Bearer YOUR_JWT_TOKEN" \
     https://your-api.com/api/v1/secrets/health
```