#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Sets up AWS Secrets Manager secrets for the Clean Architecture Template
.DESCRIPTION
    This script creates the necessary secrets in AWS Secrets Manager for different environments.
    It creates secrets for database credentials, JWT settings, and external API keys.
.PARAMETER Environment
    The environment to create secrets for (development, staging, production)
.PARAMETER Region
    The AWS region to create secrets in (default: us-east-1)
.PARAMETER Force
    Force overwrite existing secrets
.EXAMPLE
    ./setup-secrets.ps1 -Environment production -Region us-east-1
.EXAMPLE
    ./setup-secrets.ps1 -Environment staging -Force
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("development", "staging", "production")]
    [string]$Environment,
    
    [Parameter(Mandatory = $false)]
    [string]$Region = "us-east-1",
    
    [Parameter(Mandatory = $false)]
    [switch]$Force
)

# Set error action preference
$ErrorActionPreference = "Stop"

Write-Host "Setting up AWS Secrets Manager secrets for environment: $Environment" -ForegroundColor Green
Write-Host "Region: $Region" -ForegroundColor Green

# Check if AWS CLI is installed
try {
    aws --version | Out-Null
    Write-Host "✓ AWS CLI is installed" -ForegroundColor Green
}
catch {
    Write-Error "AWS CLI is not installed or not in PATH. Please install AWS CLI first."
    exit 1
}

# Check AWS credentials
try {
    $identity = aws sts get-caller-identity --output json | ConvertFrom-Json
    Write-Host "✓ AWS credentials are configured for account: $($identity.Account)" -ForegroundColor Green
}
catch {
    Write-Error "AWS credentials are not configured. Please run 'aws configure' first."
    exit 1
}

# Function to create or update a secret
function Set-Secret {
    param(
        [string]$SecretName,
        [string]$SecretValue,
        [string]$Description
    )
    
    $fullSecretName = "$Environment/$SecretName"
    
    try {
        # Check if secret exists
        $existingSecret = aws secretsmanager describe-secret --secret-id $fullSecretName --region $Region 2>$null
        
        if ($existingSecret -and -not $Force) {
            Write-Warning "Secret '$fullSecretName' already exists. Use -Force to overwrite."
            return
        }
        
        if ($existingSecret) {
            # Update existing secret
            aws secretsmanager update-secret `
                --secret-id $fullSecretName `
                --secret-string $SecretValue `
                --region $Region | Out-Null
            Write-Host "✓ Updated secret: $fullSecretName" -ForegroundColor Green
        }
        else {
            # Create new secret
            aws secretsmanager create-secret `
                --name $fullSecretName `
                --description $Description `
                --secret-string $SecretValue `
                --region $Region | Out-Null
            Write-Host "✓ Created secret: $fullSecretName" -ForegroundColor Green
        }
    }
    catch {
        Write-Error "Failed to create/update secret '$fullSecretName': $_"
    }
}

# Database credentials secret
$databaseCredentials = @{
    host = if ($Environment -eq "production") { "prod-db.cluster-xyz.us-east-1.rds.amazonaws.com" } 
           elseif ($Environment -eq "staging") { "staging-db.cluster-xyz.us-east-1.rds.amazonaws.com" }
           else { "localhost" }
    port = 5432
    database = "cleanarch"
    username = if ($Environment -eq "development") { "postgres" } else { "app_user" }
    password = if ($Environment -eq "development") { "WBn9uqfzyroot" } else { "CHANGE_ME_$(Get-Random -Minimum 100000 -Maximum 999999)" }
    engine = "postgres"
} | ConvertTo-Json -Compress

Set-Secret -SecretName "database-credentials" -SecretValue $databaseCredentials -Description "Database connection credentials for $Environment environment"

# JWT settings secret
$jwtSettings = @{
    "Jwt:SecretKey" = "CHANGE_ME_$(([System.Web.Security.Membership]::GeneratePassword(64, 10)))"
    "Jwt:Issuer" = "CleanArchTemplate"
    "Jwt:Audience" = "CleanArchTemplate"
    "Jwt:AccessTokenExpirationMinutes" = if ($Environment -eq "production") { "60" } else { "120" }
} | ConvertTo-Json -Compress

Set-Secret -SecretName "jwt-settings" -SecretValue $jwtSettings -Description "JWT authentication settings for $Environment environment"

# External API keys secret
$externalApiKeys = @{
    "Datadog:ApiKey" = "CHANGE_ME_datadog_api_key"
    "SendGrid:ApiKey" = "CHANGE_ME_sendgrid_api_key"
    "Stripe:SecretKey" = "CHANGE_ME_stripe_secret_key"
    "AWS:AccessKey" = "CHANGE_ME_aws_access_key"
    "AWS:SecretKey" = "CHANGE_ME_aws_secret_key"
} | ConvertTo-Json -Compress

Set-Secret -SecretName "external-api-keys" -SecretValue $externalApiKeys -Description "External service API keys for $Environment environment"

Write-Host ""
Write-Host "✓ All secrets have been set up successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Update the secret values with your actual credentials" -ForegroundColor White
Write-Host "2. Configure IAM permissions for your application to access these secrets" -ForegroundColor White
Write-Host "3. Update your application configuration to use the correct environment and region" -ForegroundColor White
Write-Host ""
Write-Host "To view the created secrets:" -ForegroundColor Yellow
Write-Host "aws secretsmanager list-secrets --region $Region --query 'SecretList[?starts_with(Name, ``$Environment/``)]'" -ForegroundColor White
Write-Host ""
Write-Host "To get a secret value:" -ForegroundColor Yellow
Write-Host "aws secretsmanager get-secret-value --secret-id '$Environment/database-credentials' --region $Region" -ForegroundColor White

# Create IAM policy template
$iamPolicy = @{
    Version = "2012-10-17"
    Statement = @(
        @{
            Effect = "Allow"
            Action = @(
                "secretsmanager:GetSecretValue",
                "secretsmanager:DescribeSecret"
            )
            Resource = @(
                "arn:aws:secretsmanager:${Region}:*:secret:${Environment}/*"
            )
        }
    )
} | ConvertTo-Json -Depth 10

$policyFileName = "iam-policy-secrets-$Environment.json"
$iamPolicy | Out-File -FilePath $policyFileName -Encoding UTF8

Write-Host ""
Write-Host "✓ Created IAM policy template: $policyFileName" -ForegroundColor Green
Write-Host "Use this policy to grant your application access to the secrets." -ForegroundColor White