#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Deploys the Secrets Manager infrastructure
.DESCRIPTION
    This script deploys the AWS Secrets Manager secrets for the CleanArchTemplate application
.PARAMETER Environment
    The environment to deploy to (dev, staging, prod)
.PARAMETER AutoApprove
    Skip interactive approval of terraform plan
.EXAMPLE
    ./deploy-secrets.ps1 -Environment dev
    ./deploy-secrets.ps1 -Environment staging -AutoApprove
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment,
    
    [switch]$AutoApprove
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$TerraformDir = Split-Path -Parent $ScriptDir

Write-Host "🚀 Deploying Secrets Manager infrastructure for $Environment environment..." -ForegroundColor Cyan

try {
    # Change to terraform directory
    Push-Location $TerraformDir

    # Ensure plans directory exists
    $PlansDir = Join-Path $TerraformDir "plans"
    if (-not (Test-Path $PlansDir)) {
        New-Item -ItemType Directory -Path $PlansDir -Force | Out-Null
    }

    # Initialize terraform
    Write-Host "📦 Initializing Terraform..." -ForegroundColor Yellow
    terraform init

    # Validate configuration
    Write-Host "✅ Validating Terraform configuration..." -ForegroundColor Green
    terraform validate

    # Create terraform plan
    Write-Host "📋 Creating Terraform plan..." -ForegroundColor Yellow
    $PlanFile = "plans/secrets-$Environment.tfplan"
    terraform plan -var-file="environments/$Environment.tfvars" -target=module.secrets -out=$PlanFile

    # Apply terraform plan
    if ($AutoApprove) {
        Write-Host "🔄 Applying Terraform plan (auto-approved)..." -ForegroundColor Yellow
        terraform apply -auto-approve $PlanFile
    } else {
        Write-Host "🔄 Applying Terraform plan..." -ForegroundColor Yellow
        terraform apply $PlanFile
    }

    # Get outputs
    Write-Host "📊 Getting deployment outputs..." -ForegroundColor Green
    $Outputs = terraform output -json | ConvertFrom-Json

    Write-Host ""
    Write-Host "🎉 Secrets Manager deployment completed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 Deployed Secrets:" -ForegroundColor Cyan
    
    if ($Outputs.jwt_settings_secret_name) {
        Write-Host "  • JWT Settings: $($Outputs.jwt_settings_secret_name.value)" -ForegroundColor White
    }
    
    if ($Outputs.external_api_keys_secret_name) {
        Write-Host "  • External API Keys: $($Outputs.external_api_keys_secret_name.value)" -ForegroundColor White
    }
    
    if ($Outputs.app_config_secret_name) {
        Write-Host "  • App Configuration: $($Outputs.app_config_secret_name.value)" -ForegroundColor White
    }

    Write-Host ""
    Write-Host "🔐 Security Notes:" -ForegroundColor Yellow
    Write-Host "  • All secrets are encrypted at rest with AWS KMS" -ForegroundColor White
    Write-Host "  • Secret values can be rotated through AWS console or API" -ForegroundColor White
    Write-Host "  • ECS tasks have been granted access to these secrets" -ForegroundColor White

} catch {
    Write-Host "❌ Deployment failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "📝 Next steps:" -ForegroundColor Cyan
Write-Host "1. Verify secrets in AWS Secrets Manager console" -ForegroundColor White
Write-Host "2. Update application configuration to use these secrets" -ForegroundColor White
Write-Host "3. Test secret retrieval in the application" -ForegroundColor White