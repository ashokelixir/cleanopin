# Polly Resilience Framework Integration

This document describes how the Polly resilience framework is integrated into the Clean Architecture template and how to use it effectively.

## Overview

The resilience framework provides fault tolerance and stability to the application through various patterns:

- **Retry Policy**: Automatically retries failed operations with exponential backoff
- **Circuit Breaker**: Prevents cascading failures by temporarily stopping calls to failing services
- **Timeout Policy**: Ensures operations don't hang indefinitely
- **Bulkhead Isolation**: Limits resource consumption to prevent resource exhaustion

## Configuration

Resilience policies are configured in `appsettings.json`:

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
    },
    "Bulkhead": {
      "MaxConcurrentExecutions": 10,
      "MaxQueuedActions": 20
    }
  }
}
```

## Policy Names

The following predefined policy names are available:

- `ApplicationConstants.ResiliencePolicies.Database` - For database operations
- `ApplicationConstants.ResiliencePolicies.ExternalApi` - For external API calls
- `ApplicationConstants.ResiliencePolicies.Critical` - For critical operations with enhanced resilience
- `ApplicationConstants.ResiliencePolicies.NonCritical` - For non-critical operations
- `ApplicationConstants.ResiliencePolicies.Default` - Default policy for general operations

## Usage Examples

### Basic Usage in Services

```csharp
public class MyService
{
    private readonly IResilienceService _resilienceService;

    public MyService(IResilienceService resilienceService)
    {
        _resilienceService = resilienceService;
    }

    public async Task<string> GetDataAsync()
    {
        return await _resilienceService.ExecuteAsync(
            async () =>
            {
                // Your operation here
                return await SomeExternalApiCall();
            },
            ApplicationConstants.ResiliencePolicies.ExternalApi);
    }
}
```

### Using Fallback Mechanisms

```csharp
public async Task<User> GetUserWithFallbackAsync(Guid userId)
{
    return await _resilienceService.ExecuteWithFallbackAsync(
        // Primary operation
        async () => await _userRepository.GetByIdAsync(userId),
        // Fallback operation
        async () => await GetUserFromCacheAsync(userId),
        ApplicationConstants.ResiliencePolicies.Database);
}
```

### Database Operations with Resilience

```csharp
public class ResilientUserService
{
    public async Task<User> CreateUserAsync(User user)
    {
        return await _resilienceService.ExecuteAsync(
            async () =>
            {
                await _unitOfWork.Users.AddAsync(user);
                await _unitOfWork.SaveChangesAsync();
                return user;
            },
            ApplicationConstants.ResiliencePolicies.Critical);
    }
}
```

### HTTP Client with Resilience

```csharp
public class ExternalApiService
{
    private readonly ResilientHttpService _httpService;

    public async Task<ApiResponse> CallExternalApiAsync(string endpoint)
    {
        return await _httpService.GetAsync<ApiResponse>(endpoint);
    }

    public async Task<ApiResponse> CallWithFallbackAsync(string endpoint)
    {
        return await _httpService.GetWithFallbackAsync(
            endpoint, 
            new ApiResponse { Status = "Unavailable" });
    }
}
```

## Advanced Usage

### Custom Policy Configuration

You can create custom policies by extending the `ResilienceService`:

```csharp
public class CustomResilienceService : ResilienceService
{
    protected override void ConfigurePipeline<T>(ResiliencePipelineBuilderBase<T> builder, string policyName)
    {
        if (policyName == "custom-policy")
        {
            // Configure custom policy
            builder.AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 5,
                // Custom configuration
            });
        }
        else
        {
            base.ConfigurePipeline(builder, policyName);
        }
    }
}
```

### Transaction Management with Resilience

```csharp
public class ResilientUnitOfWork
{
    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
    {
        return await _resilienceService.ExecuteAsync(
            async () =>
            {
                await BeginTransactionAsync();
                try
                {
                    var result = await operation();
                    await CommitTransactionAsync();
                    return result;
                }
                catch
                {
                    await RollbackTransactionAsync();
                    throw;
                }
            },
            ApplicationConstants.ResiliencePolicies.Critical);
    }
}
```

## Health Checks with Resilience

The template includes resilient health checks:

```csharp
// GET /api/health/resilient
// Returns comprehensive system health with resilience patterns

// GET /api/health/critical
// Returns critical operations health check
```

## Best Practices

### 1. Choose Appropriate Policies

- Use `Critical` for operations that must succeed (user creation, payments)
- Use `Database` for database operations
- Use `ExternalApi` for third-party API calls
- Use `NonCritical` for operations that can fail gracefully

### 2. Implement Proper Fallbacks

```csharp
// Good: Meaningful fallback
return await _resilienceService.ExecuteWithFallbackAsync(
    () => GetFromDatabase(),
    () => GetFromCache(),
    "database");

// Bad: Empty fallback
return await _resilienceService.ExecuteWithFallbackAsync(
    () => GetFromDatabase(),
    () => Task.FromResult<User>(null),
    "database");
```

### 3. Log Appropriately

The resilience service automatically logs retry attempts, circuit breaker state changes, and timeouts. Ensure your operations also log relevant business context.

### 4. Monitor Circuit Breaker State

Monitor circuit breaker metrics to understand system health:

- Open circuits indicate failing dependencies
- Half-open circuits show recovery attempts
- Closed circuits indicate healthy operations

### 5. Configure Timeouts Appropriately

- Database operations: 30 seconds (default)
- External APIs: 60 seconds (default)
- Critical operations: Use shorter timeouts with more retries

## Testing Resilience

### Unit Testing

```csharp
[Test]
public async Task Should_Retry_On_Transient_Failure()
{
    // Arrange
    var mockService = new Mock<IExternalService>();
    mockService.SetupSequence(x => x.CallAsync())
        .ThrowsAsync(new HttpRequestException())
        .ThrowsAsync(new HttpRequestException())
        .ReturnsAsync("Success");

    // Act & Assert
    var result = await _resilienceService.ExecuteAsync(
        () => mockService.Object.CallAsync(),
        ApplicationConstants.ResiliencePolicies.ExternalApi);

    Assert.AreEqual("Success", result);
    mockService.Verify(x => x.CallAsync(), Times.Exactly(3));
}
```

### Integration Testing

Test resilience behavior with actual dependencies using TestContainers or similar tools.

## Monitoring and Observability

The resilience framework integrates with the application's logging and telemetry:

- Retry attempts are logged with correlation IDs
- Circuit breaker state changes are logged
- Timeout events are logged with operation context
- Custom metrics can be added for monitoring

## Performance Considerations

- Resilience policies add overhead - use appropriately
- Circuit breakers prevent resource waste on failing services
- Bulkhead isolation prevents resource exhaustion
- Monitor policy effectiveness and adjust configuration as needed

## Troubleshooting

### Common Issues

1. **Too Many Retries**: Reduce `MaxRetryAttempts` or increase `BaseDelayMs`
2. **Circuit Breaker Opening Too Often**: Increase `FailureThreshold` or `SamplingDurationSeconds`
3. **Operations Timing Out**: Increase timeout values or optimize operations
4. **Resource Exhaustion**: Configure bulkhead policies appropriately

### Debugging

Enable debug logging to see resilience policy execution:

```json
{
  "Logging": {
    "LogLevel": {
      "CleanArchTemplate.Infrastructure.Services.ResilienceService": "Debug"
    }
  }
}
```

This will log all policy executions, retries, and state changes for debugging purposes.