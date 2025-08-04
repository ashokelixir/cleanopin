# PowerShell script for Docker development workflow

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("up", "down", "build", "logs", "clean", "test")]
    [string]$Command = "up"
)

$ErrorActionPreference = "Stop"

function Write-Info {
    param([string]$Message)
    Write-Host "INFO: $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "ERROR: $Message" -ForegroundColor Red
}

switch ($Command) {
    "up" {
        Write-Info "Starting development environment..."
        docker-compose up -d --build
        Write-Info "Environment started. API available at http://localhost:8080"
        Write-Info "Seq logs available at http://localhost:5341"
    }
    
    "down" {
        Write-Info "Stopping development environment..."
        docker-compose down
    }
    
    "build" {
        Write-Info "Building application..."
        docker-compose build --no-cache api
    }
    
    "logs" {
        Write-Info "Showing application logs..."
        docker-compose logs -f api
    }
    
    "clean" {
        Write-Info "Cleaning up Docker resources..."
        docker-compose down -v --remove-orphans
        docker system prune -f
        Write-Info "Cleanup completed"
    }
    
    "test" {
        Write-Info "Running tests in Docker..."
        docker-compose exec api dotnet test /src/tests/ --logger "console;verbosity=normal"
    }
    
    default {
        Write-Error "Unknown command: $Command"
        Write-Host "Available commands: up, down, build, logs, clean, test"
        exit 1
    }
}