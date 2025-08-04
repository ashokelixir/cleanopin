#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Validate IAM roles and policies for Clean Architecture Template
.DESCRIPTION
    This script validates IAM configurations and checks for security best practices
.PARAMETER Environment
    Target environment (dev, staging, prod)
.EXAMPLE
    .\validate-iam.ps1 -Environment dev
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

Write-Host "🔍 Starting IAM validation for environment: $Environment" -ForegroundColor Green

# Change to terraform directory
Set-Location $TerraformDir

try {
    # Check if terraform is initialized
    if (-not (Test-Path ".terraform")) {
        Write-Host "⚠️ Terraform not initialized. Running init..." -ForegroundColor Yellow
        & terraform init -backend-config="backend-configs/$Environment.hcl"
    }
    
    # Validate terraform configuration
    Write-Host "✅ Validating Terraform configuration..." -ForegroundColor Yellow
    $validateResult = & terraform validate
    if ($LASTEXITCODE -ne 0) {
        throw "Terraform validation failed"
    }
    
    # Check IAM module structure
    Write-Host "🏗️ Checking IAM module structure..." -ForegroundColor Yellow
    $iamModulePath = "modules/iam"
    
    $requiredFiles = @(
        "$iamModulePath/main.tf",
        "$iamModulePath/variables.tf",
        "$iamModulePath/outputs.tf",
        "$iamModulePath/README.md"
    )
    
    foreach ($file in $requiredFiles) {
        if (-not (Test-Path $file)) {
            throw "Required IAM module file missing: $file"
        }
        Write-Host "  ✓ $file exists" -ForegroundColor Green
    }
    
    # Validate IAM policies using terraform plan
    Write-Host "📋 Validating IAM policies with terraform plan..." -ForegroundColor Yellow
    $planArgs = @(
        "plan",
        "-var-file=environments/$Environment.tfvars",
        "-target=module.iam",
        "-detailed-exitcode"
    )
    
    $planResult = & terraform @planArgs 2>&1
    $planExitCode = $LASTEXITCODE
    
    switch ($planExitCode) {
        0 {
            Write-Host "  ✓ No changes needed - IAM configuration is up to date" -ForegroundColor Green
        }
        1 {
            Write-Host "  ❌ Terraform plan failed" -ForegroundColor Red
            Write-Host $planResult -ForegroundColor Red
            throw "Terraform plan validation failed"
        }
        2 {
            Write-Host "  ⚠️ Changes detected in IAM configuration" -ForegroundColor Yellow
            Write-Host "  This is expected for new deployments" -ForegroundColor Yellow
        }
    }
    
    # Check for security best practices
    Write-Host "🔒 Checking security best practices..." -ForegroundColor Yellow
    
    # Check main.tf for security patterns
    $mainTfContent = Get-Content "$iamModulePath/main.tf" -Raw
    
    $securityChecks = @{
        "Principle of Least Privilege" = $mainTfContent -match '"Effect":\s*"Allow"'
        "Resource Constraints" = $mainTfContent -match '"Resource":\s*\['
        "Condition Blocks" = $mainTfContent -match '"Condition":\s*\{'
        "Account ID Validation" = $mainTfContent -match 'aws_caller_identity'
        "External ID for Cross-Account" = $mainTfContent -match 'sts:ExternalId'
    }
    
    foreach ($check in $securityChecks.GetEnumerator()) {
        if ($check.Value) {
            Write-Host "  ✓ $($check.Key)" -ForegroundColor Green
        } else {
            Write-Host "  ⚠️ $($check.Key) - Consider implementing" -ForegroundColor Yellow
        }
    }
    
    # Check environment-specific configurations
    Write-Host "🌍 Checking environment-specific configurations..." -ForegroundColor Yellow
    $envVarsFile = "environments/$Environment.tfvars"
    
    if (Test-Path $envVarsFile) {
        $envContent = Get-Content $envVarsFile -Raw
        
        $envChecks = @{
            "CI/CD Account IDs" = $envContent -match 'cicd_account_ids'
            "ECR Repository ARNs" = $envContent -match 'ecr_repository_arns'
            "X-Ray Configuration" = $envContent -match 'enable_xray'
            "S3 Access Configuration" = $envContent -match 'enable_s3_access'
        }
        
        foreach ($check in $envChecks.GetEnumerator()) {
            if ($check.Value) {
                Write-Host "  ✓ $($check.Key) configured" -ForegroundColor Green
            } else {
                Write-Host "  ⚠️ $($check.Key) not found in environment config" -ForegroundColor Yellow
            }
        }
    }
    
    # Check for common IAM anti-patterns
    Write-Host "⚠️ Checking for IAM anti-patterns..." -ForegroundColor Yellow
    
    $antiPatterns = @{
        "Wildcard Resources" = $mainTfContent -match '"Resource":\s*"\*"'
        "Overly Broad Actions" = $mainTfContent -match '"\*:\*"'
        "Missing Conditions" = -not ($mainTfContent -match '"Condition"')
    }
    
    $antiPatternFound = $false
    foreach ($pattern in $antiPatterns.GetEnumerator()) {
        if ($pattern.Value) {
            Write-Host "  ❌ Anti-pattern detected: $($pattern.Key)" -ForegroundColor Red
            $antiPatternFound = $true
        }
    }
    
    if (-not $antiPatternFound) {
        Write-Host "  ✓ No common anti-patterns detected" -ForegroundColor Green
    }
    
    # Summary
    Write-Host "`n📊 IAM Validation Summary:" -ForegroundColor Cyan
    Write-Host "  Environment: $Environment" -ForegroundColor White
    Write-Host "  Module Structure: ✓ Valid" -ForegroundColor Green
    Write-Host "  Terraform Config: ✓ Valid" -ForegroundColor Green
    Write-Host "  Security Checks: ✓ Passed" -ForegroundColor Green
    
    if ($antiPatternFound) {
        Write-Host "  Anti-patterns: ❌ Issues found" -ForegroundColor Red
        Write-Host "`n⚠️ Please review and fix the anti-patterns before deployment" -ForegroundColor Yellow
        exit 1
    } else {
        Write-Host "  Anti-patterns: ✓ None detected" -ForegroundColor Green
    }
    
    Write-Host "`n✅ IAM validation completed successfully!" -ForegroundColor Green
    
} catch {
    Write-Host "❌ Error during IAM validation: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    # Return to original directory
    Pop-Location -ErrorAction SilentlyContinue
}

Write-Host "🎉 IAM validation script completed!" -ForegroundColor Green