# Deploy RDS PostgreSQL Infrastructure
# This script deploys only the RDS module for testing purposes

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment,
    
    [Parameter(Mandatory=$false)]
    [string]$Region = "us-east-1",
    
    [Parameter(Mandatory=$false)]
    [switch]$Plan,
    
    [Parameter(Mandatory=$false)]
    [switch]$Destroy,
    
    [Parameter(Mandatory=$false)]
    [switch]$AutoApprove
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Colors for output
$Green = "Green"
$Red = "Red"
$Yellow = "Yellow"
$Blue = "Blue"

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White"
    )
    Write-Host $Message -ForegroundColor $Color
}

function Test-Prerequisites {
    Write-ColorOutput "Checking prerequisites..." $Blue
    
    # Check if Terraform is installed
    try {
        $terraformVersion = terraform version
        Write-ColorOutput "✓ Terraform is installed: $($terraformVersion[0])" $Green
    }
    catch {
        Write-ColorOutput "✗ Terraform is not installed or not in PATH" $Red
        exit 1
    }
    
    # Check if AWS CLI is configured
    try {
        $awsIdentity = aws sts get-caller-identity 2>$null | ConvertFrom-Json
        Write-ColorOutput "✓ AWS CLI is configured for account: $($awsIdentity.Account)" $Green
    }
    catch {
        Write-ColorOutput "✗ AWS CLI is not configured or credentials are invalid" $Red
        exit 1
    }
    
    # Check if required files exist
    $requiredFiles = @(
        "main.tf",
        "variables.tf",
        "locals.tf",
        "outputs.tf",
        "environments/$Environment.tfvars"
    )
    
    foreach ($file in $requiredFiles) {
        if (Test-Path $file) {
            Write-ColorOutput "✓ Found $file" $Green
        } else {
            Write-ColorOutput "✗ Missing required file: $file" $Red
            exit 1
        }
    }
}

function Initialize-Terraform {
    Write-ColorOutput "Initializing Terraform..." $Blue
    
    # Initialize Terraform with backend configuration
    $backendConfig = "backend-configs/$Environment.hcl"
    
    if (Test-Path $backendConfig) {
        terraform init -backend-config=$backendConfig
    } else {
        Write-ColorOutput "Warning: Backend config file not found: $backendConfig" $Yellow
        Write-ColorOutput "Initializing with local backend..." $Yellow
        terraform init
    }
    
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput "✗ Terraform initialization failed" $Red
        exit 1
    }
    
    Write-ColorOutput "✓ Terraform initialized successfully" $Green
}

function Plan-Infrastructure {
    Write-ColorOutput "Planning infrastructure changes..." $Blue
    
    $planArgs = @(
        "plan",
        "-var-file=environments/$Environment.tfvars",
        "-var=aws_region=$Region",
        "-target=module.vpc",
        "-target=module.security_groups", 
        "-target=module.rds",
        "-out=tfplan-rds-$Environment"
    )
    
    & terraform @planArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput "✗ Terraform plan failed" $Red
        exit 1
    }
    
    Write-ColorOutput "✓ Terraform plan completed successfully" $Green
    Write-ColorOutput "Plan saved to: tfplan-rds-$Environment" $Blue
}

function Apply-Infrastructure {
    Write-ColorOutput "Applying infrastructure changes..." $Blue
    
    if (Test-Path "tfplan-rds-$Environment") {
        if ($AutoApprove) {
            terraform apply -auto-approve "tfplan-rds-$Environment"
        } else {
            terraform apply "tfplan-rds-$Environment"
        }
    } else {
        Write-ColorOutput "No plan file found. Running plan and apply..." $Yellow
        
        $applyArgs = @(
            "apply",
            "-var-file=environments/$Environment.tfvars",
            "-var=aws_region=$Region",
            "-target=module.vpc",
            "-target=module.security_groups",
            "-target=module.rds"
        )
        
        if ($AutoApprove) {
            $applyArgs += "-auto-approve"
        }
        
        & terraform @applyArgs
    }
    
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput "✗ Terraform apply failed" $Red
        exit 1
    }
    
    Write-ColorOutput "✓ Infrastructure deployed successfully" $Green
}

function Destroy-Infrastructure {
    Write-ColorOutput "Destroying infrastructure..." $Yellow
    
    $destroyArgs = @(
        "destroy",
        "-var-file=environments/$Environment.tfvars",
        "-var=aws_region=$Region",
        "-target=module.rds",
        "-target=module.security_groups",
        "-target=module.vpc"
    )
    
    if ($AutoApprove) {
        $destroyArgs += "-auto-approve"
    }
    
    & terraform @destroyArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-ColorOutput "✗ Terraform destroy failed" $Red
        exit 1
    }
    
    Write-ColorOutput "✓ Infrastructure destroyed successfully" $Green
}

function Show-Outputs {
    Write-ColorOutput "Retrieving infrastructure outputs..." $Blue
    
    $outputs = terraform output -json | ConvertFrom-Json
    
    Write-ColorOutput "`n=== Infrastructure Outputs ===" $Blue
    Write-ColorOutput "Environment: $Environment" $Green
    Write-ColorOutput "Region: $Region" $Green
    
    if ($outputs.rds_endpoint) {
        Write-ColorOutput "`nDatabase Information:" $Blue
        Write-ColorOutput "  RDS Endpoint: $($outputs.rds_endpoint.value)" $Green
        Write-ColorOutput "  RDS Port: $($outputs.rds_port.value)" $Green
        Write-ColorOutput "  Database Name: $($outputs.rds_database_name.value)" $Green
        Write-ColorOutput "  Secret ARN: $($outputs.database_secret_arn.value)" $Green
    }
    
    if ($outputs.vpc_id) {
        Write-ColorOutput "`nNetwork Information:" $Blue
        Write-ColorOutput "  VPC ID: $($outputs.vpc_id.value)" $Green
        Write-ColorOutput "  VPC CIDR: $($outputs.vpc_cidr_block.value)" $Green
    }
}

# Main execution
try {
    Write-ColorOutput "=== RDS PostgreSQL Deployment Script ===" $Blue
    Write-ColorOutput "Environment: $Environment" $Green
    Write-ColorOutput "Region: $Region" $Green
    
    if ($Destroy) {
        Write-ColorOutput "Mode: DESTROY" $Red
    } elseif ($Plan) {
        Write-ColorOutput "Mode: PLAN ONLY" $Yellow
    } else {
        Write-ColorOutput "Mode: DEPLOY" $Green
    }
    
    Write-ColorOutput ""
    
    # Run prerequisite checks
    Test-Prerequisites
    
    # Initialize Terraform
    Initialize-Terraform
    
    if ($Destroy) {
        # Destroy infrastructure
        Destroy-Infrastructure
    } elseif ($Plan) {
        # Plan only
        Plan-Infrastructure
    } else {
        # Plan and apply
        Plan-Infrastructure
        Apply-Infrastructure
        Show-Outputs
    }
    
    Write-ColorOutput "`n=== Deployment completed successfully ===" $Green
    
} catch {
    Write-ColorOutput "`n✗ Deployment failed: $($_.Exception.Message)" $Red
    exit 1
}