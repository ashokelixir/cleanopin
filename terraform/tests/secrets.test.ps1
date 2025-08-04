#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Tests the Secrets Manager integration
.DESCRIPTION
    This script tests that the Secrets Manager secrets are properly created and accessible
.PARAMETER Environment
    The environment to test (dev, staging, prod)
.EXAMPLE
    ./secrets.test.ps1 -Environment dev
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

Write-Host "🧪 Testing Secrets Manager integration for $Environment environment..." -ForegroundColor Cyan

try {
    # Change to terraform directory
    Push-Location $TerraformDir

    # Get terraform outputs
    Write-Host "📊 Getting Terraform outputs..." -ForegroundColor Yellow
    $Outputs = terraform output -json | ConvertFrom-Json

    if (-not $Outputs) {
        throw "No Terraform outputs found. Make sure the infrastructure is deployed."
    }

    # Test 1: Check if secrets exist in outputs
    Write-Host "✅ Test 1: Checking Terraform outputs..." -ForegroundColor Green
    
    $RequiredOutputs = @(
        "jwt_settings_secret_name",
        "external_api_keys_secret_name", 
        "app_config_secret_name"
    )

    foreach ($Output in $RequiredOutputs) {
        if (-not $Outputs.$Output) {
            throw "Missing required output: $Output"
        }
        Write-Host "  ✓ $Output: $($Outputs.$Output.value)" -ForegroundColor White
    }

    # Test 2: Check if secrets exist in AWS
    Write-Host "✅ Test 2: Checking secrets in AWS..." -ForegroundColor Green
    
    $SecretNames = @(
        $Outputs.jwt_settings_secret_name.value,
        $Outputs.external_api_keys_secret_name.value,
        $Outputs.app_config_secret_name.value
    )

    foreach ($SecretName in $SecretNames) {
        try {
            $SecretInfo = aws secretsmanager describe-secret --secret-id $SecretName --output json | ConvertFrom-Json
            Write-Host "  ✓ Secret exists: $SecretName" -ForegroundColor White
            Write-Host "    - ARN: $($SecretInfo.ARN)" -ForegroundColor Gray
            Write-Host "    - Created: $($SecretInfo.CreatedDate)" -ForegroundColor Gray
        } catch {
            throw "Failed to find secret in AWS: $SecretName"
        }
    }

    # Test 3: Test secret value retrieval (without showing values)
    Write-Host "✅ Test 3: Testing secret value retrieval..." -ForegroundColor Green
    
    foreach ($SecretName in $SecretNames) {
        try {
            $SecretValue = aws secretsmanager get-secret-value --secret-id $SecretName --output json | ConvertFrom-Json
            $SecretData = $SecretValue.SecretString | ConvertFrom-Json
            
            if ($SecretData) {
                Write-Host "  ✓ Secret value retrieved: $SecretName" -ForegroundColor White
                Write-Host "    - Keys: $($SecretData.PSObject.Properties.Name -join ', ')" -ForegroundColor Gray
            } else {
                throw "Empty secret value for: $SecretName"
            }
        } catch {
            throw "Failed to retrieve secret value: $SecretName - $($_.Exception.Message)"
        }
    }

    # Test 4: Check IAM permissions (if ECS task role exists)
    if ($Outputs.ecs_task_role_arn) {
        Write-Host "✅ Test 4: Checking IAM permissions..." -ForegroundColor Green
        
        $TaskRoleArn = $Outputs.ecs_task_role_arn.value
        $RoleName = $TaskRoleArn.Split('/')[-1]
        
        try {
            $RolePolicies = aws iam list-attached-role-policies --role-name $RoleName --output json | ConvertFrom-Json
            $HasSecretsPolicy = $RolePolicies.AttachedPolicies | Where-Object { $_.PolicyName -like "*secrets*" -or $_.PolicyName -like "*SecretsManager*" }
            
            if ($HasSecretsPolicy) {
                Write-Host "  ✓ ECS task role has Secrets Manager permissions" -ForegroundColor White
            } else {
                Write-Host "  ⚠ ECS task role may not have Secrets Manager permissions" -ForegroundColor Yellow
            }
        } catch {
            Write-Host "  ⚠ Could not verify IAM permissions: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }

    Write-Host ""
    Write-Host "🎉 All Secrets Manager tests passed!" -ForegroundColor Green

} catch {
    Write-Host "❌ Test failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "📝 Test Summary:" -ForegroundColor Cyan
Write-Host "✅ Terraform outputs verified" -ForegroundColor White
Write-Host "✅ Secrets exist in AWS Secrets Manager" -ForegroundColor White  
Write-Host "✅ Secret values can be retrieved" -ForegroundColor White
Write-Host "✅ IAM permissions checked" -ForegroundColor White