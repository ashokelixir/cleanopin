#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Docker Observability Management Script for CleanArch Template

.DESCRIPTION
    This script manages Docker containers with comprehensive observability stack including
    OpenTelemetry, Prometheus, Grafana, Jaeger, and optional Datadog integration.

.PARAMETER Action
    The action to perform: up, down, logs, clean, status, datadog-up, datadog-down, prod-up, prod-down

.PARAMETER Environment
    The environment to use: dev, observability, production

.PARAMETER Services
    Specific services to target (optional)

.EXAMPLE
    ./scripts/docker-observability.ps1 -Action up -Environment dev
    ./scripts/docker-observability.ps1 -Action up -Environment observability
    ./scripts/docker-observability.ps1 -Action datadog-up
    ./scripts/docker-observability.ps1 -Action prod-up
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("up", "down", "logs", "clean", "force-clean", "status", "datadog-up", "datadog-down", "prod-up", "prod-down", "restart")]
    [string]$Action,
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("dev", "observability", "production")]
    [string]$Environment = "dev",
    
    [Parameter(Mandatory = $false)]
    [string[]]$Services = @()
)

# Configuration
$ComposeFiles = @{
    "dev" = @("docker-compose.yml", "docker-compose.override.yml")
    "observability" = @("docker-compose.yml", "docker-compose.observability.yml")
    "production" = @("docker-compose.production.yml")
}

$DatadogProfile = "datadog"

function Write-Header {
    param([string]$Message)
    Write-Host "`n=== $Message ===" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Blue
}

function Get-ComposeCommand {
    param(
        [string]$Environment,
        [string[]]$AdditionalFiles = @(),
        [string]$Profile = ""
    )
    
    $files = $ComposeFiles[$Environment] + $AdditionalFiles
    $composeCmd = "docker-compose"
    
    foreach ($file in $files) {
        if (Test-Path $file) {
            $composeCmd += " -f $file"
        } else {
            Write-Warning "Compose file not found: $file"
        }
    }
    
    if ($Profile) {
        $composeCmd += " --profile $Profile"
    }
    
    return $composeCmd
}

function Start-Services {
    param(
        [string]$Environment,
        [string[]]$Services = @(),
        [string]$Profile = ""
    )
    
    Write-Header "Starting $Environment environment"
    
    # Create necessary directories
    $directories = @(
        "logs",
        "observability",
        "observability/grafana/provisioning/datasources",
        "observability/grafana/provisioning/dashboards",
        "observability/grafana/dashboards"
    )
    
    foreach ($dir in $directories) {
        if (!(Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
            Write-Info "Created directory: $dir"
        }
    }
    
    $composeCmd = Get-ComposeCommand -Environment $Environment -Profile $Profile
    
    if ($Services.Count -gt 0) {
        $serviceList = $Services -join " "
        $composeCmd += " up -d $serviceList"
    } else {
        $composeCmd += " up -d"
    }
    
    Write-Info "Executing: $composeCmd"
    Invoke-Expression $composeCmd
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Services started successfully"
        Show-ServiceStatus -Environment $Environment -Profile $Profile
        Show-ServiceUrls -Environment $Environment
    } else {
        Write-Error "Failed to start services"
        exit 1
    }
}

function Stop-Services {
    param(
        [string]$Environment,
        [string[]]$Services = @(),
        [string]$Profile = ""
    )
    
    Write-Header "Stopping $Environment environment"
    
    $composeCmd = Get-ComposeCommand -Environment $Environment -Profile $Profile
    
    if ($Services.Count -gt 0) {
        $serviceList = $Services -join " "
        $composeCmd += " down $serviceList"
    } else {
        $composeCmd += " down"
    }
    
    Write-Info "Executing: $composeCmd"
    Invoke-Expression $composeCmd
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Services stopped successfully"
    } else {
        Write-Error "Failed to stop services"
        exit 1
    }
}

function Show-Logs {
    param(
        [string]$Environment,
        [string[]]$Services = @(),
        [string]$Profile = ""
    )
    
    Write-Header "Showing logs for $Environment environment"
    
    $composeCmd = Get-ComposeCommand -Environment $Environment -Profile $Profile
    
    if ($Services.Count -gt 0) {
        $serviceList = $Services -join " "
        $composeCmd += " logs -f $serviceList"
    } else {
        $composeCmd += " logs -f"
    }
    
    Write-Info "Executing: $composeCmd"
    Invoke-Expression $composeCmd
}

function Show-ServiceStatus {
    param(
        [string]$Environment,
        [string]$Profile = ""
    )
    
    Write-Header "Service Status"
    
    $composeCmd = Get-ComposeCommand -Environment $Environment -Profile $Profile
    $composeCmd += " ps"
    
    Invoke-Expression $composeCmd
}

function Show-ServiceUrls {
    param([string]$Environment)
    
    Write-Header "Service URLs"
    
    switch ($Environment) {
        "dev" {
            Write-Info "Application: http://localhost:8080"
            Write-Info "Health Check: http://localhost:8080/health"
            Write-Info "Swagger: http://localhost:8080/swagger"
            Write-Info "Seq Logs: http://localhost:5341 (admin/admin123!)"
        }
        "observability" {
            Write-Info "Application: http://localhost:8080"
            Write-Info "Health Check: http://localhost:8080/health"
            Write-Info "Swagger: http://localhost:8080/swagger"
            Write-Info "Grafana: http://localhost:3000 (admin/admin123)"
            Write-Info "Prometheus: http://localhost:9090"
            Write-Info "Jaeger: http://localhost:16686"
            Write-Info "OpenTelemetry Collector: http://localhost:13133"
            Write-Info "Seq Logs: http://localhost:5341 (admin/admin123!)"
        }
        "production" {
            Write-Info "Application: http://localhost:8080"
            Write-Info "Health Check: http://localhost:8080/health"
            Write-Info "NGINX: http://localhost:80"
        }
    }
}

function Clean-Resources {
    Write-Header "Cleaning Docker resources"
    
    # Stop all compose services
    foreach ($env in $ComposeFiles.Keys) {
        Write-Info "Stopping $env services..."
        $composeCmd = Get-ComposeCommand -Environment $env
        Invoke-Expression "$composeCmd down" 2>$null
    }
    
    # Clean up Docker resources
    Write-Info "Removing unused containers..."
    docker container prune -f
    
    Write-Info "Removing unused images..."
    docker image prune -f
    
    Write-Info "Removing unused volumes..."
    docker volume prune -f
    
    Write-Info "Removing unused networks..."
    docker network prune -f
    
    Write-Success "Docker cleanup completed"
}

function Force-Clean-Resources {
    Write-Header "Force cleaning Docker resources (including stuck containers)"
    
    # Get all cleanarch containers
    Write-Info "Finding all cleanarch containers..."
    $containers = docker ps -a --filter "name=cleanarch-" --format "{{.Names}}" 2>$null
    
    if ($containers) {
        foreach ($container in $containers) {
            Write-Warning "Force removing container: $container"
            # Try graceful stop first
            docker stop $container --time 10 2>$null
            # Force remove
            docker rm -f $container 2>$null
        }
    }
    
    # Clean up volumes with cleanarch prefix
    Write-Info "Removing cleanarch volumes..."
    $volumes = docker volume ls --filter "name=cleanarch" --format "{{.Name}}" 2>$null
    if ($volumes) {
        foreach ($volume in $volumes) {
            Write-Info "Removing volume: $volume"
            docker volume rm $volume 2>$null
        }
    }
    
    # Clean up networks
    Write-Info "Removing cleanarch networks..."
    $networks = docker network ls --filter "name=cleanarch" --format "{{.Name}}" 2>$null
    if ($networks) {
        foreach ($network in $networks) {
            Write-Info "Removing network: $network"
            docker network rm $network 2>$null
        }
    }
    
    # General cleanup
    Write-Info "Running system cleanup..."
    docker system prune -f 2>$null
    
    Write-Success "Force cleanup completed"
}

function Restart-Services {
    param(
        [string]$Environment,
        [string[]]$Services = @(),
        [string]$Profile = ""
    )
    
    Write-Header "Restarting $Environment environment"
    
    Stop-Services -Environment $Environment -Services $Services -Profile $Profile
    Start-Sleep -Seconds 5
    Start-Services -Environment $Environment -Services $Services -Profile $Profile
}

function Test-Prerequisites {
    Write-Header "Checking prerequisites"
    
    # Check Docker
    try {
        $dockerVersion = docker --version
        Write-Success "Docker: $dockerVersion"
        
        # Test Docker daemon responsiveness
        $dockerInfo = docker info --format "{{.ServerVersion}}" 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Docker daemon may be unresponsive. Try restarting Docker Desktop."
        }
    } catch {
        Write-Error "Docker is not installed or not running"
        exit 1
    }
    
    # Check Docker Compose
    try {
        $composeVersion = docker-compose --version
        Write-Success "Docker Compose: $composeVersion"
    } catch {
        Write-Error "Docker Compose is not installed"
        exit 1
    }
    
    # Check required files
    $requiredFiles = @(
        "docker-compose.yml",
        "Dockerfile"
    )
    
    foreach ($file in $requiredFiles) {
        if (Test-Path $file) {
            Write-Success "Found: $file"
        } else {
            Write-Error "Missing required file: $file"
            exit 1
        }
    }
}

# Main execution
try {
    Test-Prerequisites
    
    switch ($Action) {
        "up" {
            Start-Services -Environment $Environment -Services $Services
        }
        "down" {
            Stop-Services -Environment $Environment -Services $Services
        }
        "logs" {
            Show-Logs -Environment $Environment -Services $Services
        }
        "status" {
            Show-ServiceStatus -Environment $Environment
            Show-ServiceUrls -Environment $Environment
        }
        "clean" {
            Clean-Resources
        }
        "force-clean" {
            Force-Clean-Resources
        }
        "datadog-up" {
            Start-Services -Environment "observability" -Services $Services -Profile $DatadogProfile
        }
        "datadog-down" {
            Stop-Services -Environment "observability" -Services $Services -Profile $DatadogProfile
        }
        "prod-up" {
            Start-Services -Environment "production" -Services $Services
        }
        "prod-down" {
            Stop-Services -Environment "production" -Services $Services
        }
        "restart" {
            Restart-Services -Environment $Environment -Services $Services
        }
    }
    
    Write-Success "Operation completed successfully"
} catch {
    Write-Error "An error occurred: $($_.Exception.Message)"
    exit 1
}