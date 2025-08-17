# Docker Setup Guide

This document provides comprehensive instructions for running the CleanArchTemplate application using Docker.

## Prerequisites

- Docker Desktop (Windows/Mac) or Docker Engine (Linux)
- Docker Compose v2.0+
- At least 4GB RAM available for containers
- Ports 5432, 6379, 8080, 4566, and 5341 available

## Quick Start

### Development Environment

1. **Start all services:**
   ```bash
   docker-compose up -d
   ```

2. **View logs:**
   ```bash
   docker-compose logs -f api
   ```

3. **Stop services:**
   ```bash
   docker-compose down
   ```

### Using PowerShell Script (Windows)

```powershell
# Start development environment
.\scripts\docker-dev.ps1 up

# View logs
.\scripts\docker-dev.ps1 logs

# Run tests
.\scripts\docker-dev.ps1 test

# Clean up
.\scripts\docker-dev.ps1 clean
```

## Services Overview

| Service | Port | Description |
|---------|------|-------------|
| API | 8080 | Main application |
| PostgreSQL | 5432 | Primary database |
| Redis | 6379 | Caching layer |
| LocalStack | 4566 | AWS services emulation (SQS, Secrets Manager, S3) |
| Seq | 5341 | Centralized logging |

## Environment Variables

### Application Configuration

```yaml
# Database
ConnectionStrings__DefaultConnection: "Host=postgres;Port=5432;Database=cleanarch_dev;Username=postgres;Password=WBn9uqfzyroot;"

# Redis
Redis__ConnectionString: "redis:6379"

# JWT
JwtSettings__SecretKey: "your-super-secret-key-that-is-at-least-256-bits-long-for-security"
JwtSettings__Issuer: "CleanArchTemplate"
JwtSettings__Audience: "CleanArchTemplate"
JwtSettings__ExpiryMinutes: 60

# AWS (LocalStack)
AWS__ServiceURL: "http://localstack:4566"
AWS__AccessKey: "test"
AWS__SecretKey: "test"
AWS__Region: "us-east-1"

# Messaging (SQS)
Messaging__AwsRegion: "us-east-1"
Messaging__LocalStackEndpoint: "http://localstack:4566"

# Logging
Serilog__MinimumLevel__Default: "Information"
```

## Development Workflow

### Hot Reload Development

The `docker-compose.override.yml` enables hot reload for development:

```bash
# Start with hot reload
docker-compose up -d

# The API will automatically reload when source files change
```

### Running Tests

```bash
# Run all tests
docker-compose exec api dotnet test /src/tests/

# Run specific test project
docker-compose exec api dotnet test /src/tests/CleanArchTemplate.UnitTests/

# Run with coverage
docker-compose exec api dotnet test /src/tests/ --collect:"XPlat Code Coverage"
```

### Database Operations

```bash
# Run EF Core migrations
docker-compose exec api dotnet ef database update

# Create new migration
docker-compose exec api dotnet ef migrations add MigrationName

# Reset database
docker-compose down -v
docker-compose up -d postgres
docker-compose exec api dotnet ef database update
```

## Production Deployment

### Building Production Image

```bash
# Build optimized production image
docker build -t cleanarchtemplate:latest .

# Run production container
docker run -d \
  --name cleanarch-api \
  -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="your-production-connection-string" \
  cleanarchtemplate:latest
```

### Security Considerations

The production Dockerfile implements several security best practices:

1. **Non-root user execution**
2. **Minimal attack surface** (Alpine Linux base)
3. **No unnecessary packages**
4. **Proper file permissions**
5. **Health checks**
6. **Signal handling**

## Troubleshooting

### Common Issues

1. **Port conflicts:**
   ```bash
   # Check what's using the port
   netstat -tulpn | grep :8080
   
   # Use different ports
   docker-compose up -d --scale api=0
   docker-compose run -p 8081:8080 api
   ```

2. **Database connection issues:**
   ```bash
   # Check PostgreSQL logs
   docker-compose logs postgres
   
   # Test connection
   docker-compose exec postgres psql -U postgres -d cleanarch_dev -c "SELECT 1;"
   ```

3. **Memory issues:**
   ```bash
   # Check container resource usage
   docker stats
   
   # Increase Docker Desktop memory limit
   # Docker Desktop -> Settings -> Resources -> Memory
   ```

### Health Checks

All services include health checks:

```bash
# Check service health
docker-compose ps

# View health check logs
docker inspect cleanarch-api --format='{{json .State.Health}}'
```

### Logs and Monitoring

```bash
# View all logs
docker-compose logs

# Follow specific service logs
docker-compose logs -f api

# View Seq logs (centralized logging)
# Open http://localhost:5341 in browser
```

## Performance Optimization

### Build Optimization

The Dockerfile uses multi-stage builds to minimize image size:

- **Build stage:** Full SDK for compilation
- **Publish stage:** Optimized publish output
- **Runtime stage:** Minimal runtime with security hardening

### Resource Limits

Configure resource limits in production:

```yaml
services:
  api:
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 512M
        reservations:
          cpus: '0.5'
          memory: 256M
```

## Monitoring and Observability

### Health Endpoints

- **Application Health:** `http://localhost:8080/health`
- **Database Health:** `http://localhost:8080/health/database`
- **Redis Health:** `http://localhost:8080/health/redis`
- **SQS Health:** `http://localhost:8080/health/sqs`

### Event-Driven Architecture Testing

Test the complete event-driven messaging system:

```powershell
# Run messaging test script
.\scripts\test-messaging.ps1

# Check SQS queue status
curl http://localhost:4566/_localstack/health

# View queue messages in LocalStack
awslocal sqs list-queues --endpoint-url http://localhost:4566
```

### SQS Queues

The application uses the following SQS queues for event-driven architecture:

- **user-events** - User lifecycle events (create, update, activate, etc.)
- **permission-events** - Permission lifecycle events
- **user-permission-events** - User-permission relationship events
- **role-events** - Role lifecycle events
- **role-permission-events** - Role-permission relationship events
- **user-role-events** - User-role relationship events
- **audit-events.fifo** - Audit trail events (FIFO for ordering)

Each queue has a corresponding dead letter queue (DLQ) for failed message handling.

### Metrics Collection

The application exposes metrics for monitoring:

- Response times
- Request counts
- Database query performance
- Cache hit ratios
- Custom business metrics

### Centralized Logging

Seq provides centralized log aggregation:

1. Open http://localhost:5341
2. View structured logs with correlation IDs
3. Create custom queries and dashboards
4. Set up alerts for error conditions

## Cleanup

### Remove All Resources

```bash
# Stop and remove containers, networks, and volumes
docker-compose down -v --remove-orphans

# Remove unused Docker resources
docker system prune -a -f
```

### Selective Cleanup

```bash
# Remove only volumes (keeps images)
docker-compose down -v

# Remove only containers (keeps volumes and images)
docker-compose down
```