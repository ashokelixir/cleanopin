#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Deploy ECS Fargate infrastructure with Terraform

.DESCRIPTION
    This script deploys the ECS Fargate infrastructure including:
    - ECS Cluster with Fargate capacity providers
    - Application Load Balancer with target groups and health checks
    - ECS Task Definition with proper resource allocation
    - ECS Service with auto-scaling and deployment configuration
    - CloudWatch log groups for container logging

.PARAMETER Environment
    The environment to deploy to (dev, staging, prod)

.PARAMETER Region
    AWS region to deploy to (default: ap-south-1)

.PARAMETER ContainerImage
    Docker container image to deploy

.PARAMETER Plan
    Run terraform plan only without applying changes

.PARAMETER AutoApprove
    Auto approve terraform apply without confirmation

.EXAMPLE
    ./deploy-ecs.ps1 -Environment dev -ContainerImage myapp:latest

.EXAMPLE
    ./deploy-ecs.ps1 -Environment prod -Region us-west-2 -ContainerImage myapp:v1.0.0 -AutoApprove
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment,
    
    [Parameter(Mandatory = $false)]
    [string]$Region = "ap-south-1",
    
    [Parameter(Mandatory = $true)]
    [string]$ContainerImage,
    
    [Parameter(Mandatory = $false)]
    [switch]$Plan,
    
    [Parameter(Mandatory = $false)]
    [switch]$AutoApprove
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Script directory
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$TerraformDir = Split-Path -Parent $ScriptDir

Write-Host "🚀 Deploying ECS Fargate Infrastructure" -ForegroundColor Green
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host "Region: $Region" -ForegroundColor Yellow
Write-Host "Container Image: $ContainerImage" -ForegroundColor Yellow
Write-Host ""

# Change to terraform directory
Set-Location $TerraformDir

try {
    # Check if terraform is installed
    if (-not (Get-Command terraform -ErrorAction SilentlyContinue)) {
        throw "Terraform is not installed or not in PATH"
    }

    # Check if AWS CLI is installed and configured
    if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
        throw "AWS CLI is not installed or not in PATH"
    }

    # Verify AWS credentials
    Write-Host "🔐 Verifying AWS credentials..." -ForegroundColor Blue
    $awsIdentity = aws sts get-caller-identity --output json | ConvertFrom-Json
    if ($LASTEXITCODE -ne 0) {
        throw "AWS credentials not configured or invalid"
    }
    Write-Host "✅ AWS Account: $($awsIdentity.Account)" -ForegroundColor Green
    Write-Host ""

    # Initialize Terraform
    Write-Host "🔧 Initializing Terraform..." -ForegroundColor Blue
    $backendConfig = "backend-configs/$Environment.hcl"
    
    if (Test-Path $backendConfig) {
        terraform init -backend-config=$backendConfig -upgrade
    } else {
        Write-Warning "Backend config file not found: $backendConfig"
        terraform init -upgrade
    }
    
    if ($LASTEXITCODE -ne 0) {
        throw "Terraform initialization failed"
    }
    Write-Host "✅ Terraform initialized successfully" -ForegroundColor Green
    Write-Host ""

    # Select or create workspace
    Write-Host "🏗️  Setting up Terraform workspace..." -ForegroundColor Blue
    terraform workspace select $Environment 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Creating new workspace: $Environment" -ForegroundColor Yellow
        terraform workspace new $Environment
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to create workspace: $Environment"
        }
    }
    Write-Host "✅ Using workspace: $Environment" -ForegroundColor Green
    Write-Host ""

    # Prepare terraform variables
    $tfVarsFile = "environments/$Environment.tfvars"
    $tfVars = @(
        "-var", "environment=$Environment",
        "-var", "aws_region=$Region",
        "-var", "container_image=$ContainerImage"
    )

    if (Test-Path $tfVarsFile) {
        $tfVars += "-var-file=$tfVarsFile"
        Write-Host "📋 Using variables file: $tfVarsFile" -ForegroundColor Blue
    } else {
        Write-Warning "Variables file not found: $tfVarsFile"
    }

    # Run terraform plan
    Write-Host "📋 Running Terraform plan..." -ForegroundColor Blue
    terraform plan @tfVars -out="tfplan-$Environment"
    
    if ($LASTEXITCODE -ne 0) {
        throw "Terraform plan failed"
    }
    Write-Host "✅ Terraform plan completed successfully" -ForegroundColor Green
    Write-Host ""

    # Apply changes if not plan-only
    if (-not $Plan) {
        Write-Host "🚀 Applying Terraform changes..." -ForegroundColor Blue
        
        if ($AutoApprove) {
            terraform apply -auto-approve "tfplan-$Environment"
        } else {
            terraform apply "tfplan-$Environment"
        }
        
        if ($LASTEXITCODE -ne 0) {
            throw "Terraform apply failed"
        }
        
        Write-Host "✅ ECS Fargate infrastructure deployed successfully!" -ForegroundColor Green
        Write-Host ""

        # Display important outputs
        Write-Host "📊 Infrastructure Outputs:" -ForegroundColor Blue
        Write-Host "=========================" -ForegroundColor Blue
        
        $albDnsName = terraform output -raw alb_dns_name 2>$null
        if ($albDnsName) {
            Write-Host "🌐 Load Balancer DNS: $albDnsName" -ForegroundColor Cyan
        }
        
        $ecsClusterName = terraform output -raw ecs_cluster_name 2>$null
        if ($ecsClusterName) {
            Write-Host "🐳 ECS Cluster: $ecsClusterName" -ForegroundColor Cyan
        }
        
        $ecsServiceName = terraform output -raw ecs_service_name 2>$null
        if ($ecsServiceName) {
            Write-Host "⚙️  ECS Service: $ecsServiceName" -ForegroundColor Cyan
        }
        
        $logGroupName = terraform output -raw ecs_log_group_name 2>$null
        if ($logGroupName) {
            Write-Host "📝 Log Group: $logGroupName" -ForegroundColor Cyan
        }
        
        Write-Host ""
        Write-Host "🎉 Deployment completed successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Yellow
        Write-Host "1. Update your CI/CD pipeline to push images to ECR" -ForegroundColor White
        Write-Host "2. Configure your domain to point to the ALB DNS name" -ForegroundColor White
        Write-Host "3. Monitor the ECS service and application logs" -ForegroundColor White
        Write-Host "4. Set up CloudWatch alarms and notifications" -ForegroundColor White
    } else {
        Write-Host "✅ Plan completed. Use -AutoApprove to apply changes." -ForegroundColor Green
    }

} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    # Clean up plan file
    if (Test-Path "tfplan-$Environment") {
        Remove-Item "tfplan-$Environment" -Force
    }
}