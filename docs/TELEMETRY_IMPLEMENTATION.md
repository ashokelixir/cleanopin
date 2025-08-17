# OpenTelemetry and Datadog Observability Implementation

This document describes the comprehensive telemetry and observability implementation for the CleanArchTemplate project.

## Overview

The implementation provides enterprise-grade observability through OpenTelemetry instrumentation and Datadog integration, with comprehensive health checks and custom metrics collection.

## Components Implemented

### 1. Telemetry Service (`ITelemetryService`)

**Location**: `src/CleanArchTemplate.Infrastructure/Services/TelemetryService.cs`

**Features**:
- Activity creation and management for distributed tracing
- Custom metrics recording (counters, histograms, gauges)
- Exception tracking with detailed context
- Database operation metrics
- Cache operation metrics
- External service call metrics
- Event recording with tags

**Key Methods**:
- `StartActivity()` - Creates activities for distributed tracing
- `RecordMetric()` - Records custom metrics
- `RecordCounter()` - Records counter metrics
- `RecordHistogram()` - Records histogram metrics
- `RecordException()` - Records exceptions with context
- `RecordDatabaseOperation()` - Tracks database performance
- `RecordCacheOperation()` - Tracks cache performance
- `RecordExternalServiceCall()` - Tracks external API calls

### 2. OpenTelemetry Configuration

**Location**: `src/CleanArchTemplate.API/Extensions/OpenTelemetryExtensions.cs`

**Features**:
- ASP.NET Core instrumentation for HTTP requests
- HTTP client instrumentation for outbound calls
- Entity Framework Core instrumentation for database operations
- Redis instrumentation for cache operations
- Console exporter for development
- OTLP exporter for production environments
- Resource attributes configuration

**Instrumentation Includes**:
- HTTP request/response tracking
- Database query performance
- Cache hit/miss ratios
- External service calls
- Custom business metrics

### 3. Datadog Integration

**Location**: `src/CleanArchTemplate.API/Extensions/OpenTelemetryExtensions.cs`

**Features**:
- Automatic Datadog agent configuration
- Environment-based configuration
- Service mapping for dependencies
- Sampling rate configuration
- Runtime metrics collection
- Log injection for correlation

**Configuration**:
- Service name: `cleanarch-template`
- Environment-specific tagging
- Automatic instrumentation for supported libraries
- Custom service mapping for PostgreSQL, Redis, and SQS

### 4. Telemetry Middleware

**Location**: `src/CleanArchTemplate.API/Middleware/TelemetryMiddleware.cs`

**Features**:
- Automatic HTTP request tracking
- Request/response metrics collection
- User context enrichment
- Correlation ID tracking
- Business metrics for specific endpoints
- Exception tracking and correlation

**Metrics Collected**:
- HTTP request duration and count
- Authentication operation metrics
- User management operation metrics
- Role management operation metrics
- Permission operation metrics
- Health check metrics

### 5. Comprehensive Health Checks

**Location**: `src/CleanArchTemplate.API/Extensions/HealthCheckExtensions.cs`

**Health Check Types**:
- **Database**: PostgreSQL connectivity and performance
- **Memory**: Memory usage and garbage collection metrics
- **Disk Space**: Available disk space monitoring
- **Application**: Service registration and startup validation

**Health Check Endpoints**:
- `/health` - Basic health status
- `/health/detailed` - Detailed health information with metrics
- `/health/ready` - Kubernetes readiness probe
- `/health/live` - Kubernetes liveness probe

**Custom Health Checks**:
- `MemoryHealthCheck` - Monitors memory usage and GC statistics
- `DiskSpaceHealthCheck` - Monitors disk space across drives
- `ApplicationHealthCheck` - Validates critical service registration

### 6. Telemetry Decorators

**Cache Service Decorator**: `src/CleanArchTemplate.Infrastructure/Decorators/TelemetryCacheServiceDecorator.cs`
- Tracks cache operations (get, set, remove)
- Records cache hit/miss ratios
- Measures operation duration
- Provides cache performance insights

**Message Publisher Decorator**: `src/CleanArchTemplate.Infrastructure/Decorators/TelemetryMessagePublisherDecorator.cs`
- Tracks message publishing operations
- Records message processing metrics
- Monitors queue performance
- Tracks batch operations

## Configuration

### OpenTelemetry Settings (`appsettings.json`)

```json
{
  "OpenTelemetry": {
    "ServiceName": "CleanArchTemplate",
    "ServiceVersion": "1.0.0",
    "OtlpEndpoint": "",
    "EnableConsoleExporter": true,
    "EnableOtlpExporter": false
  }
}
```

### Datadog Settings (`appsettings.json`)

```json
{
  "Datadog": {
    "ApiKey": "",
    "AgentHost": "localhost",
    "AgentPort": "8126",
    "SampleRate": "1.0",
    "Environment": "development"
  }
}
```

## Metrics Collected

### HTTP Metrics
- `http_requests_total` - Total HTTP requests by method, status code, and success
- `http_request_duration_seconds` - HTTP request duration histogram

### Authentication Metrics
- `auth_operations_total` - Authentication operations by type and success
- `auth_operation_duration_seconds` - Authentication operation duration

### Business Metrics
- `user_operations_total` - User management operations
- `role_operations_total` - Role management operations
- `permission_operations_total` - Permission operations
- `health_check_requests_total` - Health check requests

### Database Metrics
- `cleanarch_database_operations_total` - Database operations by type and success
- `cleanarch_database_operation_duration_seconds` - Database operation duration

### Cache Metrics
- `cleanarch_cache_operations_total` - Cache operations by type and result
- `cleanarch_cache_operation_duration_seconds` - Cache operation duration

### Messaging Metrics
- `messaging_operations_total` - Message publishing operations
- `messaging_operation_duration_seconds` - Message operation duration
- `messaging_batch_size` - Message batch size histogram

### System Metrics
- `cleanarch_active_connections` - Active database connections
- `cleanarch_cache_size_bytes` - Current cache size
- `cleanarch_errors_total` - Total errors by type

## Testing

### Unit Tests
**Location**: `tests/CleanArchTemplate.UnitTests/Infrastructure/Services/TelemetryServiceTests.cs`

**Coverage**:
- Activity creation and management
- Metrics recording functionality
- Exception handling
- Tag and event management
- Database operation tracking
- Cache operation tracking
- External service call tracking

### Integration Tests
**Location**: `tests/CleanArchTemplate.IntegrationTests/API/HealthChecks/HealthCheckIntegrationTests.cs`

**Coverage**:
- Health check endpoint functionality
- Detailed health information
- Content type validation
- Performance metrics inclusion
- Individual health check validation

### Middleware Tests
**Location**: `tests/CleanArchTemplate.UnitTests/API/Middleware/TelemetryMiddlewareTests.cs`

**Coverage**:
- HTTP request tracking
- User context enrichment
- Correlation ID handling
- Exception recording
- Business metrics collection

## Usage Examples

### Recording Custom Metrics

```csharp
// Inject ITelemetryService
public class MyService
{
    private readonly ITelemetryService _telemetryService;
    
    public MyService(ITelemetryService telemetryService)
    {
        _telemetryService = telemetryService;
    }
    
    public async Task ProcessDataAsync()
    {
        using var activity = _telemetryService.StartActivity("ProcessData");
        
        try
        {
            // Business logic
            _telemetryService.RecordCounter("data_processed_total", 1,
                new KeyValuePair<string, object?>("type", "user_data"));
        }
        catch (Exception ex)
        {
            _telemetryService.RecordException(ex);
            throw;
        }
    }
}
```

### Database Operation Tracking

```csharp
// Automatically tracked through EF Core instrumentation
// Additional custom tracking available through decorators
```

### Cache Operation Tracking

```csharp
// Automatically tracked through cache service decorator
var data = await _cacheService.GetAsync<UserData>("user:123");
// Metrics automatically recorded: cache hit/miss, duration
```

## Production Deployment

### Environment Variables

```bash
# Datadog Configuration
DD_API_KEY=your-datadog-api-key
DD_ENV=production
DD_SERVICE=cleanarch-template
DD_VERSION=1.0.0
DD_TRACE_ENABLED=true
DD_RUNTIME_METRICS_ENABLED=true
DD_LOGS_INJECTION=true

# OpenTelemetry Configuration
OTEL_SERVICE_NAME=cleanarch-template
OTEL_SERVICE_VERSION=1.0.0
OTEL_EXPORTER_OTLP_ENDPOINT=https://your-otlp-endpoint
```

### Docker Configuration

The telemetry configuration is automatically applied when the application starts. Health checks are available immediately at the configured endpoints.

## Monitoring and Alerting

### Key Metrics to Monitor

1. **HTTP Request Rate and Latency**
   - Monitor `http_requests_total` and `http_request_duration_seconds`
   - Alert on high error rates or increased latency

2. **Database Performance**
   - Monitor `cleanarch_database_operation_duration_seconds`
   - Alert on slow queries or connection issues

3. **Cache Performance**
   - Monitor cache hit ratios from `cleanarch_cache_operations_total`
   - Alert on low hit rates or cache failures

4. **Health Check Status**
   - Monitor health check endpoints
   - Alert on unhealthy status

5. **Error Rates**
   - Monitor `cleanarch_errors_total`
   - Alert on increased error rates

### Recommended Dashboards

1. **Application Overview**
   - Request rate, latency, error rate
   - Health check status
   - Active users and sessions

2. **Database Performance**
   - Query duration and count
   - Connection pool metrics
   - Slow query identification

3. **Cache Performance**
   - Hit/miss ratios
   - Cache size and eviction rates
   - Operation latencies

4. **Business Metrics**
   - User registration rates
   - Authentication success/failure rates
   - Permission changes and audits

## Benefits

1. **Comprehensive Observability**: Full visibility into application performance and behavior
2. **Proactive Monitoring**: Early detection of issues through metrics and health checks
3. **Performance Optimization**: Detailed insights for identifying bottlenecks
4. **Business Intelligence**: Custom metrics for business KPIs
5. **Debugging Support**: Distributed tracing for complex request flows
6. **Production Readiness**: Enterprise-grade monitoring and alerting capabilities

## Next Steps

1. Configure Datadog dashboards and alerts
2. Set up log aggregation and correlation
3. Implement custom business metrics based on requirements
4. Configure alerting thresholds based on baseline performance
5. Set up automated performance testing with telemetry validation