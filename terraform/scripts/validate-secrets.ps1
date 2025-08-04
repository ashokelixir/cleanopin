#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Validates the Secrets Manager terraform configuration
.DESCRIPTION
    This script validates the terraform configuration for the secrets module
    and checks that all required variables are properly configured.
.PARAMETER Environment
    The environment to validate (dev, staging, prod)
.EXAMPLE
    ./validate-secrets.ps1 -Environment dev
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$TerraformDir = Split-Path -Parent $ScriptDir

Write-Host "🔍 Validating Secrets Manager terraform configuration for $Environment environment..." -ForegroundColor Cyan

try {
    # Change to terraform directory
    Push-Location $TerraformDir

    # Initialize terraform if needed
    if (-not (Test-Path ".terraform")) {
        Write-Host "📦 Initializing Terraform..." -ForegroundColor Yellow
        terraform init -backend=false
    }

    # Validate terraform configuration
    Write-Host "✅ Validating Terraform syntax..." -ForegroundColor Green
    terraform validate

    # Check if secrets module exists
    $SecretsModulePath = Join-Path $TerraformDir "modules/secrets"
    if (-not (Test-Path $SecretsModulePath)) {
        throw "Secrets module not found at: $SecretsModulePath"
    }

    Write-Host "✅ Secrets module found" -ForegroundColor Green

    # Validate secrets module specifically
    Push-Location $SecretsModulePath
    terraform validate
    Pop-Location

    Write-Host "✅ Secrets module validation passed" -ForegroundColor Green

    # Check environment-specific variables
    $EnvFile = Join-Path $TerraformDir "environments/$Environment.tfvars"
    if (-not (Test-Path $EnvFile)) {
        throw "Environment file not found: $EnvFile"
    }

    Write-Host "✅ Environment file found: $EnvFile" -ForegroundColor Green

    # Check for required secrets variables in the environment file
    $RequiredSecrets = @(
        "jwt_secret_key",
        "encryption_key",
        "cors_allowed_origins",
        "swagger_enabled"
    )

    $EnvContent = Get-Content $EnvFile -Raw
    $MissingSecrets = @()

    foreach ($Secret in $RequiredSecrets) {
        if ($EnvContent -notmatch "$Secret\s*=") {
            $MissingSecrets += $Secret
        }
    }

    if ($MissingSecrets.Count -gt 0) {
        Write-Host "❌ Missing required secrets in $Environment.tfvars:" -ForegroundColor Red
        $MissingSecrets | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
        throw "Missing required secrets configuration"
    }

    Write-Host "✅ All required secrets variables found" -ForegroundColor Green

    # Plan terraform to check for any issues
    Write-Host "📋 Running terraform plan to validate configuration..." -ForegroundColor Yellow
    terraform plan -var-file="environments/$Environment.tfvars" -target=module.secrets -out="plans/secrets-$Environment.tfplan"

    Write-Host "✅ Terraform plan completed successfully" -ForegroundColor Green
    Write-Host "🎉 Secrets Manager configuration validation completed successfully!" -ForegroundColor Green

} catch {
    Write-Host "❌ Validation failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "📝 Next steps:" -ForegroundColor Cyan
Write-Host "1. Review the terraform plan output above" -ForegroundColor White
Write-Host "2. Apply the changes with: terraform apply plans/secrets-$Environment.tfplan" -ForegroundColor White
Write-Host "3. Verify secrets are created in AWS Secrets Manager console" -ForegroundColor White