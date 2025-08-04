#!/usr/bin/env pwsh

Write-Host "Docker Compose Troubleshooting Script" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

# Check if Docker is running
Write-Host "`nChecking Docker status..." -ForegroundColor Yellow
try {
    docker version | Out-Null
    Write-Host "✓ Docker is running" -ForegroundColor Green
} catch {
    Write-Host "✗ Docker is not running or not accessible" -ForegroundColor Red
    exit 1
}

# Check individual services
Write-Host "`nChecking individual services..." -ForegroundColor Yellow

# Test PostgreSQL
Write-Host "Testing PostgreSQL..." -ForegroundColor Cyan
docker-compose up -d postgres
Start-Sleep 10
$pgHealth = docker-compose ps postgres --format json | ConvertFrom-Json
if ($pgHealth.Health -eq "healthy") {
    Write-Host "✓ PostgreSQL is healthy" -ForegroundColor Green
} else {
    Write-Host "✗ PostgreSQL health: $($pgHealth.Health)" -ForegroundColor Red
    Write-Host "PostgreSQL logs:" -ForegroundColor Yellow
    docker-compose logs postgres --tail 20
}

# Test Redis
Write-Host "`nTesting Redis..." -ForegroundColor Cyan
docker-compose up -d redis
Start-Sleep 5
$redisHealth = docker-compose ps redis --format json | ConvertFrom-Json
if ($redisHealth.Health -eq "healthy") {
    Write-Host "✓ Redis is healthy" -ForegroundColor Green
} else {
    Write-Host "✗ Redis health: $($redisHealth.Health)" -ForegroundColor Red
    Write-Host "Redis logs:" -ForegroundColor Yellow
    docker-compose logs redis --tail 20
}

# Test LocalStack (optional)
Write-Host "`nTesting LocalStack..." -ForegroundColor Cyan
docker-compose up -d localstack
Start-Sleep 30
try {
    $response = Invoke-WebRequest -Uri "http://localhost:4566/_localstack/health" -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "✓ LocalStack is healthy" -ForegroundColor Green
    } else {
        Write-Host "✗ LocalStack returned status: $($response.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "✗ LocalStack is not responding" -ForegroundColor Red
    Write-Host "LocalStack logs:" -ForegroundColor Yellow
    docker-compose logs localstack --tail 20
}

Write-Host "`nTroubleshooting complete!" -ForegroundColor Green
Write-Host "To start without LocalStack, use: docker-compose -f docker-compose.dev.yml up" -ForegroundColor Cyan