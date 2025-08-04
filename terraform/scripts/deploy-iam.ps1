#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Deploy IAM roles and policies for Clean Architecture Template
.DESCRIPTION
    This script deploys the IAM module with proper validation and error handling
.PARAMETER Environment
    Target environment (dev, staging, prod)
.PARAMETER Action
    Action to perform (plan, apply, destroy)
.PARAMETER AutoApprove
    Skip interactive approval for apply/destroy
.EXAMPLE
    .\deploy-iam.ps1 -Environment dev -Action plan
    .\deploy-iam.ps1 -Environment prod -Action apply -AutoApprove
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment,
    
    [Parameter(Mandatory = $true)]
    [ValidateSet("plan", "apply", "destroy")]
    [string]$Action,
    
    [switch]$AutoApprove
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Get script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$TerraformDir = Split-Path -Parent $ScriptDir

Write-Host "🚀 Starting IAM deployment for environment: $Environment" -ForegroundColor Green
Write-Host "📁 Working directory: $TerraformDir" -ForegroundColor Blue

# Change to terraform directory
Set-Location $TerraformDir

try {
    # Initialize Terraform with backend configuration
    Write-Host "🔧 Initializing Terraform..." -ForegroundColor Yellow
    $initArgs = @(
        "init",
        "-backend-config=backend-configs/$Environment.hcl",
        "-reconfigure"
    )
    
    $initResult = & terraform @initArgs
    if ($LASTEXITCODE -ne 0) {
        throw "Terraform init failed with exit code $LASTEXITCODE"
    }
    
    # Validate Terraform configuration
    Write-Host "✅ Validating Terraform configuration..." -ForegroundColor Yellow
    $validateResult = & terraform validate
    if ($LASTEXITCODE -ne 0) {
        throw "Terraform validation failed with exit code $LASTEXITCODE"
    }
    
    # Select workspace
    Write-Host "🏗️ Selecting workspace: $Environment" -ForegroundColor Yellow
    & terraform workspace select $Environment 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Creating new workspace: $Environment" -ForegroundColor Yellow
        & terraform workspace new $Environment
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create workspace $Environment"
        }
    }
    
    # Prepare terraform command arguments
    $tfArgs = @()
    $tfVarsFile = "environments/$Environment.tfvars"
    
    if (-not (Test-Path $tfVarsFile)) {
        throw "Environment variables file not found: $tfVarsFile"
    }
    
    switch ($Action) {
        "plan" {
            Write-Host "📋 Creating execution plan..." -ForegroundColor Yellow
            $tfArgs = @(
                "plan",
                "-var-file=$tfVarsFile",
                "-target=module.iam",
                "-out=iam-$Environment.tfplan"
            )
        }
        "apply" {
            Write-Host "🚀 Applying IAM configuration..." -ForegroundColor Yellow
            $tfArgs = @(
                "apply",
                "-var-file=$tfVarsFile",
                "-target=module.iam"
            )
            
            if ($AutoApprove) {
                $tfArgs += "-auto-approve"
            }
        }
        "destroy" {
            Write-Host "💥 Destroying IAM resources..." -ForegroundColor Red
            $tfArgs = @(
                "destroy",
                "-var-file=$tfVarsFile",
                "-target=module.iam"
            )
            
            if ($AutoApprove) {
                $tfArgs += "-auto-approve"
            }
        }
    }
    
    # Execute terraform command
    Write-Host "⚡ Executing: terraform $($tfArgs -join ' ')" -ForegroundColor Cyan
    $result = & terraform @tfArgs
    
    if ($LASTEXITCODE -ne 0) {
        throw "Terraform $Action failed with exit code $LASTEXITCODE"
    }
    
    # Show outputs for successful apply
    if ($Action -eq "apply") {
        Write-Host "📊 IAM Resources Created:" -ForegroundColor Green
        & terraform output -json | ConvertFrom-Json | ForEach-Object {
            $_.PSObject.Properties | Where-Object { $_.Name -like "*iam*" -or $_.Name -like "*role*" } | ForEach-Object {
                Write-Host "  $($_.Name): $($_.Value.value)" -ForegroundColor White
            }
        }
    }
    
    Write-Host "✅ IAM deployment completed successfully!" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Error during IAM deployment: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    # Return to original directory
    Pop-Location -ErrorAction SilentlyContinue
}

Write-Host "🎉 IAM deployment script completed!" -ForegroundColor Green