#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Validate SQS and SNS infrastructure deployment

.DESCRIPTION
    This script validates the complete SQS and SNS infrastructure by:
    - Checking Terraform state consistency
    - Validating resource creation
    - Testing SNS notification delivery
    - Verifying CloudWatch alarm integration
    - Checking IAM permissions

.PARAMETER Environment
    Target environment (dev, staging, prod)

.PARAMETER Region
    AWS region where resources are deployed

.PARAMETER TestNotifications
    Send test notifications to verify SNS delivery

.EXAMPLE
    ./scripts/validate-sqs-sns.ps1 -Environment dev -Region ap-south-1

.EXAMPLE
    ./scripts/validate-sqs-sns.ps1 -Environment prod -Region us-east-1 -TestNotifications
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment,
    
    [Parameter(Mandatory = $false)]
    [string]$Region = "ap-south-1",
    
    [Parameter(Mandatory = $false)]
    [switch]$TestNotifications
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Script configuration
$ScriptName = "SQS and SNS In