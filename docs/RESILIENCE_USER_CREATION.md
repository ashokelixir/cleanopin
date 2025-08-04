# Resilient User Creation Implementation

This document explains how the resilient user creation feature works in the Clean Architecture template.

## Overview

The resilient user creation demonstrates how to integrate Polly resilience patterns into application layer operations, providing fault tolerance for critical business operations like user registration.

## Architecture Flow

```
HTTP Request → UsersController → MediatR → CreateUserWithResilienceCommandHandler → ResilienceService → Database
```

## Key Components

### 1. UsersController
- **Location**: `src/CleanArchTemplate.API/Controllers/UsersController.cs`
- **Purpose**: HTTP endpoint for user creation with proper error handling
- **Features**:
  - RESTful API design
  - Comprehensive error handling with appropriate HTTP status codes
  - Structured logging for observability
  - Authorization requirements

### 2. CreateUserWithResilienceCommandHandler
- **Location**: `src/CleanArchTemplate.Application/Features/Users/Commands/CreateUser/CreateUserWithResilienceCommandHandler.cs`
- **Purpose**: Demonstrates resilience patterns in application layer
- **Resilience Layers**:
  - **Outer Layer**: Critical policy for entire operation
  - **Email Validation**: Database policy for existence checks
  - **Password Hashing**: Default policy for password operations
  - **Transaction Management**: Database/Critical policies for transaction operations
  - **DTO Mapping**: Default policy for mapping operations

### 3. ResilienceService
- **Location**: `src/CleanArchTemplate.Infrastructure/Services/ResilienceService.cs`
- **Purpose**: Polly-based resilience implementation
- **Strategies**:
  - **Timeout**: Prevents operations from hanging
  - **Retry**: Exponential backoff with jitter
  - **Circuit Breaker**: Prevents cascading failures

## Resilience Policies

### Policy Types
1. **Critical**: Highest level of protection for critical operations
2. **Database**: Optimized for database operations
3. **External-API**: Configured for external service calls
4. **Default**: General purpose resilience

### Configuration
Resilience settings are configured in `appsettings.json`:

```json
{
  "Resilience": {
    "Retry": {
      "MaxRetryAttempts": 3,
      "BaseDelayMs": 1000,
      "MaxDelayMs": 30000,
      "BackoffMultiplier": 2.0
    },
    "CircuitBreaker": {
      "FailureThreshold": 5,
      "DurationOfBreakSeconds": 60,
      "MinimumThroughput": 10,
      "SamplingDurationSeconds": 30
    },
    "Timeout": {
      "DefaultTimeoutSeconds": 30,
      "DatabaseTimeoutSeconds": 30,
      "ExternalApiTimeoutSeconds": 60
    }
  }
}
```

## Usage Example

### Creating a User with Resilience

```bash
POST /api/users
Content-Type: application/json

{
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "password": "SecurePassword123!"
}
```

### Response Examples

**Success (201 Created):**
```json
{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "email": "user@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "isEmailVerified": false,
  "isActive": true,
  "createdAt": "2024-01-15T10:30:00Z",
  "roles": []
}
```

**Conflict (409 Conflict):**
```json
{
  "error": "User with this email already exists"
}
```

**Validation Error (422 Unprocessable Entity):**
```json
{
  "error": "Validation failed",
  "details": ["Invalid email address format"]
}
```

## Resilience Benefits

### 1. Fault Tolerance
- Automatic retry on transient failures
- Circuit breaker prevents system overload
- Timeout protection prevents hanging operations

### 2. Observability
- Comprehensive logging at each resilience event
- Correlation IDs for request tracing
- Performance metrics collection

### 3. Configuration Flexibility
- Environment-specific resilience settings
- Policy-based configuration for different operation types
- Runtime configuration updates

## Comparison with Standard Handler

| Feature | Standard Handler | Resilient Handler |
|---------|------------------|-------------------|
| Retry Logic | ❌ None | ✅ Exponential backoff |
| Timeout Protection | ❌ None | ✅ Configurable timeouts |
| Circuit Breaker | ❌ None | ✅ Failure rate monitoring |
| Transaction Safety | ✅ Basic | ✅ Enhanced with resilience |
| Logging | ✅ Basic | ✅ Comprehensive |
| Performance | ✅ Fast | ✅ Resilient |

## Testing the Implementation

### 1. Normal Operation
```bash
curl -X POST "https://localhost:7001/api/users" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "email": "test@example.com",
    "firstName": "Test",
    "lastName": "User",
    "password": "SecurePassword123!"
  }'
```

### 2. Duplicate Email Test
```bash
# Create the same user twice to test conflict handling
curl -X POST "https://localhost:7001/api/users" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "email": "duplicate@example.com",
    "firstName": "Duplicate",
    "lastName": "User",
    "password": "SecurePassword123!"
  }'
```

### 3. Invalid Email Test
```bash
curl -X POST "https://localhost:7001/api/users" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "email": "invalid-email",
    "firstName": "Invalid",
    "lastName": "User",
    "password": "SecurePassword123!"
  }'
```

## Monitoring and Observability

### Log Examples

**Successful Creation:**
```
[INFO] Creating user with resilience patterns: test@example.com
[INFO] Successfully created user: 123e4567-e89b-12d3-a456-426614174000
[INFO] User created successfully with ID: 123e4567-e89b-12d3-a456-426614174000
```

**Retry Scenario:**
```
[WARN] Retry attempt 1 for policy: database, Exception: Connection timeout
[WARN] Retry attempt 2 for policy: database, Exception: Connection timeout
[INFO] Successfully created user: 123e4567-e89b-12d3-a456-426614174000
```

**Circuit Breaker:**
```
[WARN] Circuit breaker opened for policy: database
[ERROR] Operation failed after resilience policies for policy: database
```

## Best Practices

1. **Policy Selection**: Choose appropriate policies based on operation criticality
2. **Timeout Configuration**: Set realistic timeouts based on operation complexity
3. **Logging**: Include correlation IDs for distributed tracing
4. **Error Handling**: Provide meaningful error messages to clients
5. **Testing**: Test resilience scenarios in development environments

## Future Enhancements

1. **Metrics Collection**: Add custom metrics for resilience events
2. **Health Checks**: Include resilience status in health endpoints
3. **Configuration Hot Reload**: Support runtime configuration updates
4. **Fallback Strategies**: Implement fallback mechanisms for critical operations