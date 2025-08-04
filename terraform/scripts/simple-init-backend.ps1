#!/usr/bin/env pwsh

param(
    [Parameter(Mandatory = $true)]
    [string]$Environment = "dev",
    
    [Parameter(Mandatory = $false)]
    [string]$Region = "us-east-1",
    
    [Parameter(Mandatory = $false)]
    [string]$ProjectName = "cleanarch-template"
)

# Set error action preference
$ErrorActionPreference = "Stop"

Write-Host "Initializing Terraform backend for environment: $Environment" -ForegroundColor Green

# Get AWS account ID
$AccountId = aws sts get-caller-identity --query Account --output text
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to get AWS account ID. Please check your AWS credentials."
    exit 1
}

Write-Host "AWS Account ID: $AccountId" -ForegroundColor Yellow

# Define resource names
$BucketName = "$ProjectName-terraform-state-$Environment"
$TableName = "$ProjectName-terraform-locks-$Environment"

Write-Host "Creating S3 bucket: $BucketName" -ForegroundColor Yellow

# Create S3 bucket
try {
    if ($Region -eq "us-east-1") {
        aws s3api create-bucket --bucket $BucketName --region $Region
    } else {
        aws s3api create-bucket --bucket $BucketName --region $Region --create-bucket-configuration LocationConstraint=$Region
    }
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Bucket might already exist, continuing..." -ForegroundColor Yellow
    }
} catch {
    Write-Host "Bucket creation failed or bucket already exists, continuing..." -ForegroundColor Yellow
}

# Enable versioning
Write-Host "Enabling versioning on S3 bucket..." -ForegroundColor Yellow
aws s3api put-bucket-versioning --bucket $BucketName --versioning-configuration Status=Enabled

# Block public access
Write-Host "Blocking public access on S3 bucket..." -ForegroundColor Yellow
aws s3api put-public-access-block --bucket $BucketName --public-access-block-configuration "BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true"

# Create DynamoDB table
Write-Host "Creating DynamoDB table: $TableName" -ForegroundColor Yellow
try {
    aws dynamodb create-table `
        --table-name $TableName `
        --attribute-definitions AttributeName=LockID,AttributeType=S `
        --key-schema AttributeName=LockID,KeyType=HASH `
        --provisioned-throughput ReadCapacityUnits=5,WriteCapacityUnits=5 `
        --region $Region
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Waiting for DynamoDB table to be active..." -ForegroundColor Yellow
        aws dynamodb wait table-exists --table-name $TableName --region $Region
    } else {
        Write-Host "Table might already exist, continuing..." -ForegroundColor Yellow
    }
} catch {
    Write-Host "Table creation failed or table already exists, continuing..." -ForegroundColor Yellow
}

# Create backend config file
$BackendConfigDir = "../backend-configs"
if (!(Test-Path $BackendConfigDir)) {
    New-Item -ItemType Directory -Path $BackendConfigDir -Force
}

$BackendConfigFile = "$BackendConfigDir/$Environment.hcl"
$BackendConfig = @"
bucket         = "$BucketName"
key            = "terraform.tfstate"
region         = "$Region"
dynamodb_table = "$TableName"
encrypt        = true
"@

Write-Host "Creating backend config file: $BackendConfigFile" -ForegroundColor Yellow
$BackendConfig | Out-File -FilePath $BackendConfigFile -Encoding UTF8

Write-Host "Backend initialization completed successfully!" -ForegroundColor Green
Write-Host "S3 Bucket: $BucketName" -ForegroundColor Cyan
Write-Host "DynamoDB Table: $TableName" -ForegroundColor Cyan
Write-Host "Backend Config: $BackendConfigFile" -ForegroundColor Cyan