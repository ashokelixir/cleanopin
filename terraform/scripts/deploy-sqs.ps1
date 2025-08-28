#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Deploy SQS messaging infrastructure for Clean Architecture Template

.DESCRIPTION
    This script deploys the SQS messaging infrastructure including:
    - Standard queues for user and permission events
    - FIFO queues for audit events
    - Dead letter queues for all main queues
    - CloudWatch alarms for monitoring
    - IAM permissions for ECS tasks

.PARAMETER Environment
    Target environment (dev, staging, prod)

.PARAMETER Region
    AWS region to deploy to

.PARAMETER AutoApprove
    Skip interactive approval of Terraform plan

.PARAMETER DestroyMode
    Run terraform destroy instead of apply

.PARAMETER PlanOnly
    Only run terraform plan, don't apply changes

.EXAMPLE
    ./deploy-sqs.ps1 -Environment dev -Region ap-south-1

.EXAMPLE
    ./deploy-sqs.ps1 -Environment prod -Region us-east-1 -AutoApprove

.EXAMPLE
    ./deploy-sqs.ps1 -Environment staging -PlanOnly
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment,
    
    [Parameter(Mandatory = $false)]
    [string]$Region = "ap-south-1",
    
    [Parameter(Mandatory = $false)]
    [switch]$AutoApprove,
    
    [Parameter(Mandatory = $false)]
    [switch]$DestroyMode,
    
    [Parameter(Mandatory = $false)]
    [switch]$PlanOnly
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Script configuration
$ScriptName = "Deploy SQS Infrastructure"
$LogFile = "logs/deploy-sqs-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
$TerraformDir = Split-Path -Parent $PSScriptRoot

# Ensure logs directory exists
$LogDir = Split-Path -Parent $LogFile
if (-not (Test-Path $LogDir)) {
    New-Item -ItemType Directory -Path $LogDir -Force | Out-Null
}

# Logging function
function Write-Log {
    param(
        [string]$Message,
        [string]$Level = "INFO"
    )
    
    $Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $LogMessage = "[$Timestamp] [$Level] $Message"
    
    Write-Host $LogMessage
    Add-Content -Path $LogFile -Value $LogMessage
}

# Error handling function
function Handle-Error {
    param(
        [string]$ErrorMessage,
        [int]$ExitCode = 1
    )
    
    Write-Log "ERROR: $ErrorMessage" "ERROR"
    Write-Log "Deployment failed. Check the log file: $LogFile" "ERROR"
    exit $ExitCode
}

try {
    Write-Log "Starting $ScriptName for environment: $Environment"
    Write-Log "Region: $Region"
    Write-Log "Log file: $LogFile"
    
    # Change to terraform directory
    Set-Location $TerraformDir
    Write-Log "Changed to Terraform directory: $TerraformDir"
    
    # Validate environment
    $ValidEnvironments = @("dev", "staging", "prod")
    if ($Environment -notin $ValidEnvironments) {
        Handle-Error "Invalid environment: $Environment. Must be one of: $($ValidEnvironments -join ', ')"
    }
    
    # Check if Terraform is installed
    try {
        $TerraformVersion = terraform version
        Write-Log "Terraform version: $($TerraformVersion[0])"
    }
    catch {
        Handle-Error "Terraform is not installed or not in PATH"
    }
    
    # Check if AWS CLI is installed and configured
    try {
        $AwsIdentity = aws sts get-caller-identity --output json | ConvertFrom-Json
        Write-Log "AWS Account ID: $($AwsIdentity.Account)"
        Write-Log "AWS User/Role: $($AwsIdentity.Arn)"
    }
    catch {
        Handle-Error "AWS CLI is not installed or not configured"
    }
    
    # Initialize Terraform
    Write-Log "Initializing Terraform..."
    $InitArgs = @(
        "init",
        "-backend-config=backend-configs/$Environment.hcl",
        "-upgrade"
    )
    
    $InitResult = & terraform @InitArgs
    if ($LASTEXITCODE -ne 0) {
        Handle-Error "Terraform init failed"
    }
    Write-Log "Terraform initialized successfully"
    
    # Validate Terraform configuration
    Write-Log "Validating Terraform configuration..."
    terraform validate
    if ($LASTEXITCODE -ne 0) {
        Handle-Error "Terraform validation failed"
    }
    Write-Log "Terraform configuration is valid"
    
    # Plan Terraform changes
    Write-Log "Planning Terraform changes..."
    $PlanFile = "plans/sqs-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss').tfplan"
    $PlanArgs = @(
        "plan",
        "-var-file=environments/$Environment.tfvars",
        "-target=module.sns",
        "-target=module.sqs",
        "-target=module.iam.aws_iam_role_policy.ecs_task_sqs",
        "-target=module.iam.aws_iam_role_policy.ecs_task_sns",
        "-out=$PlanFile"
    )
    
    if ($DestroyMode) {
        $PlanArgs += "-destroy"
        Write-Log "Planning destruction of SQS infrastructure..."
    }
    
    $PlanResult = & terraform @PlanArgs
    if ($LASTEXITCODE -ne 0) {
        Handle-Error "Terraform plan failed"
    }
    
    Write-Log "Terraform plan completed successfully"
    Write-Log "Plan file saved to: $PlanFile"
    
    # Exit if plan-only mode
    if ($PlanOnly) {
        Write-Log "Plan-only mode enabled. Exiting without applying changes."
        Write-Log "To apply the plan, run: terraform apply $PlanFile"
        exit 0
    }
    
    # Apply or destroy Terraform changes
    if ($DestroyMode) {
        Write-Log "Destroying SQS infrastructure..."
        $Action = "destroy"
    }
    else {
        Write-Log "Applying SQS infrastructure changes..."
        $Action = "apply"
    }
    
    $ApplyArgs = @($PlanFile)
    
    if ($AutoApprove) {
        Write-Log "Auto-approve enabled. Applying changes without confirmation."
    }
    else {
        Write-Host ""
        Write-Host "Review the plan above and confirm to proceed with $Action." -ForegroundColor Yellow
        $Confirmation = Read-Host "Do you want to continue? (yes/no)"
        
        if ($Confirmation -ne "yes") {
            Write-Log "Deployment cancelled by user"
            exit 0
        }
    }
    
    $ApplyResult = & terraform apply @ApplyArgs
    if ($LASTEXITCODE -ne 0) {
        Handle-Error "Terraform $Action failed"
    }
    
    Write-Log "Terraform $Action completed successfully"
    
    # Display SQS outputs
    if (-not $DestroyMode) {
        Write-Log "Retrieving SQS infrastructure outputs..."
        
        $SqsOutputs = @(
            "sqs_user_events_queue_arn",
            "sqs_user_events_queue_url",
            "sqs_permission_events_queue_arn",
            "sqs_permission_events_queue_url",
            "sqs_audit_events_queue_arn",
            "sqs_audit_events_queue_url",
            "sqs_all_queue_arns",
            "sns_sqs_alerts_topic_arn",
            "sns_sqs_dlq_alerts_topic_arn",
            "sns_infrastructure_alerts_topic_arn"
        )
        
        Write-Log "SQS Infrastructure Outputs:"
        foreach ($Output in $SqsOutputs) {
            try {
                $Value = terraform output -raw $Output 2>$null
                if ($LASTEXITCODE -eq 0) {
                    Write-Log "  $Output = $Value"
                }
            }
            catch {
                Write-Log "  $Output = <not available>"
            }
        }
        
        # Display queue configuration
        Write-Log ""
        Write-Log "SQS Queue Configuration Summary:"
        Write-Log "  Environment: $Environment"
        Write-Log "  Region: $Region"
        Write-Log "  Standard Queues: user-events, permission-events"
        Write-Log "  FIFO Queues: audit-events"
        Write-Log "  Dead Letter Queues: Configured for all main queues"
        Write-Log "  Server-Side Encryption: Enabled"
        Write-Log "  CloudWatch Alarms: Enabled"
        Write-Log "  SNS Topics: sqs-alerts, sqs-dlq-alerts, infrastructure-alerts"
        Write-Log "  SNS Notifications: Email, Slack (optional), SMS (optional)"
        
        Write-Log ""
        Write-Log "Next Steps:"
        Write-Log "1. Confirm SNS email subscriptions in your inbox"
        Write-Log "2. Update application configuration with queue URLs"
        Write-Log "3. Deploy ECS service to use the new SQS and SNS permissions"
        Write-Log "4. Test message publishing and consumption"
        Write-Log "5. Test SNS notifications by triggering CloudWatch alarms"
        Write-Log "6. Monitor CloudWatch alarms and metrics"
    }
    
    Write-Log "$ScriptName completed successfully"
    
}
catch {
    Handle-Error "Unexpected error: $($_.Exception.Message)"
}
finally {
    # Return to original directory
    Pop-Location -ErrorAction SilentlyContinue
}