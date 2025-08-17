# Docker Observability Setup

This document describes the comprehensive Docker-based observability stack for the CleanArch Template, including OpenTelemetry, Prometheus, Grafana, Jaeger, and Datadog integration.

## Overview

The observability stack provides:
- **Distributed Tracing** with Jaeger and OpenTelemetry
- **Metrics Collection** with Prometheus and OpenTelemetry
- **Visualization** with Grafana dashboards
- **Log Aggregation** with Seq
- **Production Monitoring** with Datadog (optional)
- **Health Monitoring** with comprehensive health checks

## Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Application   │───▶│ OpenTelemetry    │───▶│    Jaeger       │
│   (CleanArch)   │    │   Collector      │    │   (Traces)      │
└─────────────────┘    └──────────────────┘    └─────────────────┘
         │                       │                       │
         │                       ▼                       │
         │              ┌─────────────────┐              │
         │              │   Prometheus    │              │
         │              │   (Metrics)     │              │
         │              └─────────────────┘              │
         │                       │                       │
         │                       ▼                       │
         │              ┌─────────────────┐              │
         │              │    Grafana      │              │
         │              │ (Visualization) │              │
         │              └─────────────────┘              │
         │                                                │
         ▼                                                ▼
┌─────────────────┐                            ┌─────────────────┐
│      Seq        │                            │    Datadog      │
│    (Logs)       │                            │  (Production)   │
└─────────────────┘                            └─────────────────┘
```

## Docker Compose Files

### 1. `docker-compose.yml` (Base)
- PostgreSQL database
- Redis cache
- LocalStack (AWS services)
- Application container
- Seq logging

### 2. `docker-compose.observability.yml` (Development Observability)
- OpenTelemetry Collector
- Jaeger (distributed tracing)
- Prometheus (metrics)
- Grafana (visualization)
- Datadog Agent (optional)

### 3. `docker-compose.production.yml` (Production)
- Production-optimized application
- Datadog Agent with full monitoring
- NGINX reverse proxy
- Enhanced security and monitoring

## Quick Start

### Development with Basic Observability

```powershell
# Start development environment with observability
./scripts/docker-observability.ps1 -Action up -Environment observability

# View logs
./scripts/docker-observability.ps1 -Action logs -Environment observability

# Check status
./scripts/docker-observability.ps1 -Action status -Environment observability
```

### Development with Datadog

```powershell
# Copy and configure environment file
cp .env.observability.template .env.observability
# Edit .env.observability with your Datadog API key

# Start with Datadog profile
./scripts/docker-observability.ps1 -Action datadog-up
```

### Production Deployment

```powershell
# Configure production environment
cp .env.observability.template .env.production
# Edit .env.production with production values

# Start production environment
./scripts/docker-observability.ps1 -Action prod-up
```

## Service URLs

### Development Environment
- **Application**: http://localhost:8080
- **Health Checks**: http://localhost:8080/health
- **Detailed Health**: http://localhost:8080/health/detailed
- **Swagger UI**: http://localhost:8080/swagger
- **Seq Logs**: http://localhost:5341 (admin/admin123!)

### Observability Stack
- **Grafana**: http://localhost:3000 (admin/admin123)
- **Prometheus**: http://localhost:9090
- **Jaeger UI**: http://localhost:16686
- **OpenTelemetry Collector Health**: http://localhost:13133

## Configuration

### Environment Variables

Create `.env.observability` from the template:

```bash
cp .env.observability.template .env.observability
```

Key configurations:

#### OpenTelemetry
```env
OTEL_EXPORTER_OTLP_ENDPOINT=http://otel-collector:4317
OTEL_TRACES_EXPORTER=otlp
OTEL_METRICS_EXPORTER=otlp
```

#### Datadog (Production)
```env
DD_API_KEY=your-datadog-api-key
DD_SITE=datadoghq.com
DD_ENV=production
DD_SERVICE=cleanarch-template
```

### OpenTelemetry Collector Configuration

The collector is configured in `observability/otel-collector-config.yaml`:

- **Receivers**: OTLP (gRPC/HTTP), Prometheus scraping
- **Processors**: Batch processing, memory limiting, resource attribution
- **Exporters**: Jaeger (traces), Prometheus (metrics), Console (debugging)

### Prometheus Configuration

Configured in `observability/prometheus.yml`:

- Scrapes application metrics every 15s
- Collects OpenTelemetry Collector metrics
- Self-monitoring enabled
- 200h retention period

### Grafana Dashboards

Pre-configured dashboards in `observability/grafana/dashboards/`:

1. **CleanArch Overview** (`cleanarch-overview.json`)
   - HTTP request rate and latency
   - Database operation metrics
   - Cache hit rates
   - Authentication metrics
   - Error rates

## Metrics Collected

### HTTP Metrics
- `http_requests_total` - Request count by method, status
- `http_request_duration_seconds` - Request latency histogram

### Database Metrics
- `cleanarch_database_operations_total` - Database operation count
- `cleanarch_database_operation_duration_seconds` - Database latency

### Cache Metrics
- `cleanarch_cache_operations_total` - Cache operation count
- `cleanarch_cache_operation_duration_seconds` - Cache latency

### Business Metrics
- `auth_operations_total` - Authentication operations
- `user_operations_total` - User management operations
- `role_operations_total` - Role management operations
- `permission_operations_total` - Permission operations

### System Metrics
- `cleanarch_errors_total` - Application errors
- `cleanarch_active_connections` - Database connections
- `cleanarch_cache_size_bytes` - Cache size

## Health Checks

Comprehensive health checks available at:

- `/health` - Basic health status
- `/health/detailed` - Detailed system information
- `/health/ready` - Kubernetes readiness probe
- `/health/live` - Kubernetes liveness probe

Health checks monitor:
- Database connectivity
- Redis connectivity
- Memory usage
- Disk space
- Application services

## Distributed Tracing

### Automatic Instrumentation
- HTTP requests (incoming/outgoing)
- Database operations (Entity Framework)
- Cache operations (Redis)
- Message queue operations (SQS)

### Custom Tracing
```csharp
using var activity = _telemetryService.StartActivity("CustomOperation");
activity?.SetTag("custom.tag", "value");
// Your business logic
```

## Production Deployment

### Datadog Integration

For production monitoring with Datadog:

1. **Configure Environment Variables**:
   ```env
   DD_API_KEY=your-datadog-api-key
   DD_SITE=datadoghq.com
   DD_ENV=production
   ```

2. **Start with Datadog Profile**:
   ```powershell
   ./scripts/docker-observability.ps1 -Action prod-up
   ```

3. **Datadog Features Enabled**:
   - APM (Application Performance Monitoring)
   - Infrastructure monitoring
   - Log collection and correlation
   - Process monitoring
   - Network monitoring
   - Security monitoring

### Security Considerations

Production deployment includes:
- Non-root container execution
- Resource limits and health checks
- Secure secret management
- Network isolation
- Log sanitization

## Troubleshooting

### Common Issues

1. **Services Not Starting**
   ```powershell
   # Check Docker daemon
   docker info
   
   # Check compose file syntax
   docker-compose -f docker-compose.observability.yml config
   
   # View service logs
   ./scripts/docker-observability.ps1 -Action logs -Environment observability
   ```

2. **Missing Metrics**
   ```powershell
   # Check OpenTelemetry Collector health
   curl http://localhost:13133
   
   # Check Prometheus targets
   # Visit http://localhost:9090/targets
   
   # Check application health
   curl http://localhost:8080/health/detailed
   ```

3. **Tracing Issues**
   ```powershell
   # Check Jaeger UI
   # Visit http://localhost:16686
   
   # Verify OTLP endpoint connectivity
   docker-compose -f docker-compose.observability.yml logs otel-collector
   ```

### Performance Tuning

1. **OpenTelemetry Collector**
   - Adjust batch size in `otel-collector-config.yaml`
   - Configure memory limits
   - Tune sampling rates

2. **Prometheus**
   - Adjust scrape intervals
   - Configure retention policies
   - Optimize query performance

3. **Application**
   - Configure sampling rates
   - Optimize metric collection
   - Tune health check intervals

## Monitoring Best Practices

### Alerting Rules

Create Prometheus alerting rules for:
- High error rates (>5%)
- High latency (>1s p95)
- Database connection issues
- Cache miss rates (>20%)
- Memory usage (>80%)

### Dashboard Organization

Organize Grafana dashboards by:
- **Overview**: High-level application metrics
- **Infrastructure**: System and resource metrics
- **Business**: Domain-specific metrics
- **Debugging**: Detailed diagnostic information

### Log Correlation

Use structured logging with:
- Trace IDs for request correlation
- User IDs for user journey tracking
- Operation IDs for business process tracking
- Error correlation with traces

## Scaling Considerations

### Horizontal Scaling
- Configure OpenTelemetry Collector clustering
- Use Prometheus federation for multiple instances
- Implement distributed tracing across services

### Resource Management
- Set appropriate resource limits
- Monitor collector memory usage
- Configure data retention policies
- Implement log rotation

## Integration with CI/CD

### Build Pipeline
```yaml
# Example GitHub Actions integration
- name: Build and Test with Observability
  run: |
    ./scripts/docker-observability.ps1 -Action up -Environment observability
    # Run tests with telemetry validation
    ./scripts/docker-observability.ps1 -Action down -Environment observability
```

### Deployment Pipeline
```yaml
# Production deployment with monitoring
- name: Deploy with Monitoring
  run: |
    ./scripts/docker-observability.ps1 -Action prod-up
    # Verify health checks
    # Run smoke tests
```

This comprehensive observability setup provides enterprise-grade monitoring and debugging capabilities for the CleanArch Template application.