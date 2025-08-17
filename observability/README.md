# CleanArch Template - Observability Stack

This directory contains the configuration files for the comprehensive observability stack including OpenTelemetry, Prometheus, Grafana, and Jaeger.

## Quick Start

### 1. Start Development Environment with Observability

```powershell
# From project root
./scripts/docker-observability.ps1 -Action up -Environment observability
```

### 2. Access Services

- **Application**: http://localhost:8080
- **Health Checks**: http://localhost:8080/health/detailed
- **Grafana**: http://localhost:3000 (admin/admin123)
- **Prometheus**: http://localhost:9090
- **Jaeger**: http://localhost:16686
- **Seq Logs**: http://localhost:5341 (admin/admin123!)

### 3. View Metrics and Traces

1. **Grafana Dashboard**: Navigate to http://localhost:3000 and view the "CleanArch Template - Overview" dashboard
2. **Jaeger Traces**: Visit http://localhost:16686 to view distributed traces
3. **Prometheus Metrics**: Check http://localhost:9090 for raw metrics
4. **Application Health**: Monitor http://localhost:8080/health/detailed

## Configuration Files

- `otel-collector-config.yaml` - OpenTelemetry Collector configuration
- `prometheus.yml` - Prometheus scraping configuration
- `grafana/provisioning/` - Grafana datasources and dashboard provisioning
- `grafana/dashboards/` - Pre-built Grafana dashboards

## Metrics Available

### HTTP Metrics
- Request rate and latency
- Status code distribution
- Error rates

### Database Metrics
- Query performance
- Connection pool status
- Operation success rates

### Cache Metrics
- Hit/miss ratios
- Operation latency
- Cache size

### Business Metrics
- Authentication operations
- User management activities
- Permission changes
- Role assignments

## Customization

### Adding Custom Metrics

1. Update the application to emit custom metrics using `ITelemetryService`
2. Metrics will automatically be collected by OpenTelemetry
3. Add new panels to Grafana dashboards as needed

### Adding Custom Dashboards

1. Create new dashboard JSON files in `grafana/dashboards/`
2. Restart Grafana container to load new dashboards
3. Or import dashboards through the Grafana UI

### Configuring Alerts

1. Add alerting rules to `prometheus.yml`
2. Configure notification channels in Grafana
3. Set up alert conditions based on your SLIs/SLOs

For detailed documentation, see [DOCKER_OBSERVABILITY.md](../docs/DOCKER_OBSERVABILITY.md)