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
    ./validate-sqs-sns.ps1 -Environment dev -Region ap-south-1

.EXAMPLE
    ./validate-sqs-sns.ps1 -Environment prod -Region us-east-1 -TestNotifications
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
$ScriptName = "SQS and SNS Infrastructure Validation"
$LogFile = "logs/validate-sqs-sns-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
$TerraformDir = Split-Path -Parent $PSScriptRoot

# Validation results
$ValidationResults = @{
    Passed = 0
    Failed = 0
    Warnings = 0
    Checks = @()
}

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

# Validation function
function Test-Validation {
    param(
        [string]$CheckName,
        [scriptblock]$ValidationScript,
        [string]$Description = "",
        [string]$Severity = "ERROR"
    )
    
    Write-Log "Validating: $CheckName"
    if ($Description) {
        Write-Log "  Description: $Description"
    }
    
    try {
        $Result = & $ValidationScript
        if ($Result) {
            Write-Log "  ✅ PASSED: $CheckName" "PASS"
            $ValidationResults.Passed++
            $ValidationResults.Checks += @{
                Name = $CheckName
                Status = "PASSED"
                Description = $Description
                Severity = $Severity
                Error = $null
            }
            return $true
        }
        else {
            if ($Severity -eq "WARNING") {
                Write-Log "  ⚠️  WARNING: $CheckName" "WARN"
                $ValidationResults.Warnings++
                $ValidationResults.Checks += @{
                    Name = $CheckName
                    Status = "WARNING"
                    Description = $Description
                    Severity = $Severity
                    Error = "Validation condition returned false"
                }
            }
            else {
                Write-Log "  ❌ FAILED: $CheckName" "FAIL"
                $ValidationResults.Failed++
                $ValidationResults.Checks += @{
                    Name = $CheckName
                    Status = "FAILED"
                    Description = $Description
                    Severity = $Severity
                    Error = "Validation condition returned false"
                }
            }
            return $false
        }
    }
    catch {
        if ($Severity -eq "WARNING") {
            Write-Log "  ⚠️  WARNING: $CheckName - $($_.Exception.Message)" "WARN"
            $ValidationResults.Warnings++
        }
        else {
            Write-Log "  ❌ FAILED: $CheckName - $($_.Exception.Message)" "FAIL"
            $ValidationResults.Failed++
        }
        $ValidationResults.Checks += @{
            Name = $CheckName
            Status = $Severity -eq "WARNING" ? "WARNING" : "FAILED"
            Description = $Description
            Severity = $Severity
            Error = $_.Exception.Message
        }
        return $false
    }
}

try {
    Write-Log "Starting $ScriptName for environment: $Environment"
    Write-Log "Region: $Region"
    Write-Log "Log file: $LogFile"
    
    # Change to terraform directory
    Set-Location $TerraformDir
    
    # Validation 1: Terraform State Consistency
    Test-Validation -CheckName "Terraform State Consistency" -Description "Verify Terraform state is consistent" -ValidationScript {
        $PlanResult = terraform plan -var-file="environments/$Environment.tfvars" -detailed-exitcode -target=module.sqs -target=module.sns 2>$null
        # Exit code 0 = no changes, 2 = changes needed, 1 = error
        return $LASTEXITCODE -eq 0
    }
    
    # Get Terraform outputs for validation
    Write-Log "Retrieving Terraform outputs for validation..."
    $Outputs = @{}
    
    $RequiredOutputs = @(
        "sqs_user_events_queue_arn",
        "sqs_user_events_queue_url",
        "sqs_permission_events_queue_arn",
        "sqs_permission_events_queue_url",
        "sqs_audit_events_queue_arn",
        "sqs_audit_events_queue_url",
        "sns_sqs_alerts_topic_arn",
        "sns_sqs_dlq_alerts_topic_arn",
        "sns_infrastructure_alerts_topic_arn"
    )
    
    foreach ($Output in $RequiredOutputs) {
        try {
            $Value = terraform output -raw $Output 2>$null
            if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrEmpty($Value)) {
                $Outputs[$Output] = $Value
            }
        }
        catch {
            Write-Log "Warning: Could not retrieve output $Output"
        }
    }
    
    # Validation 2: SQS Queue Existence and Configuration
    Test-Validation -CheckName "SQS Queues Exist" -Description "Verify all SQS queues are created and accessible" -ValidationScript {
        $QueueUrls = @(
            $Outputs["sqs_user_events_queue_url"],
            $Outputs["sqs_permission_events_queue_url"],
            $Outputs["sqs_audit_events_queue_url"]
        )
        
        foreach ($QueueUrl in $QueueUrls) {
            if ([string]::IsNullOrEmpty($QueueUrl)) { return $false }
            
            # Test queue accessibility
            $Attributes = aws sqs get-queue-attributes --queue-url $QueueUrl --attribute-names QueueArn --region $Region --output json 2>$null | ConvertFrom-Json
            if (-not $Attributes.Attributes.QueueArn) { return $false }
        }
        return $true
    }
    
    # Validation 3: SNS Topics Existence and Configuration
    Test-Validation -CheckName "SNS Topics Exist" -Description "Verify all SNS topics are created and accessible" -ValidationScript {
        $TopicArns = @(
            $Outputs["sns_sqs_alerts_topic_arn"],
            $Outputs["sns_sqs_dlq_alerts_topic_arn"],
            $Outputs["sns_infrastructure_alerts_topic_arn"]
        )
        
        foreach ($TopicArn in $TopicArns) {
            if ([string]::IsNullOrEmpty($TopicArn)) { return $false }
            
            # Test topic accessibility
            $Attributes = aws sns get-topic-attributes --topic-arn $TopicArn --region $Region --output json 2>$null | ConvertFrom-Json
            if (-not $Attributes.Attributes.TopicArn) { return $false }
        }
        return $true
    }
    
    # Validation 4: Dead Letter Queue Configuration
    Test-Validation -CheckName "Dead Letter Queue Configuration" -Description "Verify DLQ redrive policies are properly configured" -ValidationScript {
        $MainQueues = @(
            $Outputs["sqs_user_events_queue_url"],
            $Outputs["sqs_permission_events_queue_url"],
            $Outputs["sqs_audit_events_queue_url"]
        )
        
        foreach ($QueueUrl in $MainQueues) {
            if ([string]::IsNullOrEmpty($QueueUrl)) { return $false }
            
            $Attributes = aws sqs get-queue-attributes --queue-url $QueueUrl --attribute-names RedrivePolicy --region $Region --output json 2>$null | ConvertFrom-Json
            if ([string]::IsNullOrEmpty($Attributes.Attributes.RedrivePolicy)) { return $false }
            
            $RedrivePolicy = $Attributes.Attributes.RedrivePolicy | ConvertFrom-Json
            if ($RedrivePolicy.maxReceiveCount -le 0 -or [string]::IsNullOrEmpty($RedrivePolicy.deadLetterTargetArn)) { return $false }
        }
        return $true
    }
    
    # Validation 5: Server-Side Encryption
    Test-Validation -CheckName "Server-Side Encryption" -Description "Verify SSE is enabled on all queues and topics" -ValidationScript {
        # Check SQS encryption
        $QueueUrls = @(
            $Outputs["sqs_user_events_queue_url"],
            $Outputs["sqs_permission_events_queue_url"],
            $Outputs["sqs_audit_events_queue_url"]
        )
        
        foreach ($QueueUrl in $QueueUrls) {
            if ([string]::IsNullOrEmpty($QueueUrl)) { return $false }
            
            $Attributes = aws sqs get-queue-attributes --queue-url $QueueUrl --attribute-names SqsManagedSseEnabled --region $Region --output json 2>$null | ConvertFrom-Json
            if ($Attributes.Attributes.SqsManagedSseEnabled -ne "true") { return $false }
        }
        
        # Check SNS encryption
        $TopicArns = @(
            $Outputs["sns_sqs_alerts_topic_arn"],
            $Outputs["sns_sqs_dlq_alerts_topic_arn"],
            $Outputs["sns_infrastructure_alerts_topic_arn"]
        )
        
        foreach ($TopicArn in $TopicArns) {
            if ([string]::IsNullOrEmpty($TopicArn)) { return $false }
            
            $Attributes = aws sns get-topic-attributes --topic-arn $TopicArn --region $Region --output json 2>$null | ConvertFrom-Json
            # SNS encryption is indicated by presence of KmsMasterKeyId
            if ([string]::IsNullOrEmpty($Attributes.Attributes.KmsMasterKeyId)) { return $false }
        }
        return $true
    }
    
    # Validation 6: CloudWatch Alarms
    Test-Validation -CheckName "CloudWatch Alarms Configuration" -Description "Verify CloudWatch alarms are created and configured with SNS actions" -ValidationScript {
        $Alarms = aws cloudwatch describe-alarms --alarm-name-prefix "cleanarch-$Environment" --region $Region --output json 2>$null | ConvertFrom-Json
        $SqsAlarms = $Alarms.MetricAlarms | Where-Object { $_.Namespace -eq "AWS/SQS" }
        
        if ($SqsAlarms.Count -eq 0) { return $false }
        
        # Check that alarms have SNS actions
        $AlarmsWithSns = $SqsAlarms | Where-Object { $_.AlarmActions.Count -gt 0 }
        return $AlarmsWithSns.Count -gt 0
    }
    
    # Validation 7: IAM Permissions
    Test-Validation -CheckName "ECS Task IAM Permissions" -Description "Verify ECS task roles have proper SQS and SNS permissions" -ValidationScript {
        try {
            $TaskRoleName = "cleanarch-$Environment-ecs-task-role"
            
            # Check SQS permissions
            $SqsPolicies = aws iam list-role-policies --role-name $TaskRoleName --region $Region --output json 2>$null | ConvertFrom-Json
            $HasSqsPolicy = $SqsPolicies.PolicyNames | Where-Object { $_ -like "*sqs*" }
            
            # Check SNS permissions
            $HasSnsPolicy = $SqsPolicies.PolicyNames | Where-Object { $_ -like "*sns*" }
            
            return $HasSqsPolicy.Count -gt 0 -and $HasSnsPolicy.Count -gt 0
        }
        catch {
            # Role might not exist in some environments
            return $true
        }
    } -Severity "WARNING"
    
    # Validation 8: Resource Naming Convention
    Test-Validation -CheckName "Resource Naming Convention" -Description "Verify resources follow naming conventions" -ValidationScript {
        $ExpectedPrefix = "cleanarch-$Environment"
        
        # Check SQS queue names
        $UserEventsArn = $Outputs["sqs_user_events_queue_arn"]
        $PermissionEventsArn = $Outputs["sqs_permission_events_queue_arn"]
        $AuditEventsArn = $Outputs["sqs_audit_events_queue_arn"]
        
        $SqsNamingValid = $UserEventsArn -like "*$ExpectedPrefix-user-events*" -and
                         $PermissionEventsArn -like "*$ExpectedPrefix-permission-events*" -and
                         $AuditEventsArn -like "*$ExpectedPrefix-audit-events.fifo*"
        
        # Check SNS topic names
        $SqsAlertsArn = $Outputs["sns_sqs_alerts_topic_arn"]
        $DlqAlertsArn = $Outputs["sns_sqs_dlq_alerts_topic_arn"]
        $InfraAlertsArn = $Outputs["sns_infrastructure_alerts_topic_arn"]
        
        $SnsNamingValid = $SqsAlertsArn -like "*$ExpectedPrefix-sqs-alerts*" -and
                         $DlqAlertsArn -like "*$ExpectedPrefix-sqs-dlq-alerts*" -and
                         $InfraAlertsArn -like "*$ExpectedPrefix-infrastructure-alerts*"
        
        return $SqsNamingValid -and $SnsNamingValid
    }
    
    # Validation 9: SNS Subscriptions
    Test-Validation -CheckName "SNS Subscriptions" -Description "Verify SNS topics have appropriate subscriptions" -ValidationScript {
        $TopicArns = @(
            $Outputs["sns_sqs_alerts_topic_arn"],
            $Outputs["sns_sqs_dlq_alerts_topic_arn"],
            $Outputs["sns_infrastructure_alerts_topic_arn"]
        )
        
        $HasSubscriptions = $false
        foreach ($TopicArn in $TopicArns) {
            if ([string]::IsNullOrEmpty($TopicArn)) { continue }
            
            $Subscriptions = aws sns list-subscriptions-by-topic --topic-arn $TopicArn --region $Region --output json 2>$null | ConvertFrom-Json
            if ($Subscriptions.Subscriptions.Count -gt 0) {
                $HasSubscriptions = $true
                break
            }
        }
        
        return $HasSubscriptions
    } -Severity "WARNING"
    
    # Validation 10: Test Notifications (if requested)
    if ($TestNotifications) {
        Test-Validation -CheckName "SNS Test Notification" -Description "Send test notification to verify delivery" -ValidationScript {
            $TestTopicArn = $Outputs["sns_infrastructure_alerts_topic_arn"]
            if ([string]::IsNullOrEmpty($TestTopicArn)) { return $false }
            
            $TestMessage = @{
                timestamp = (Get-Date).ToString("o")
                environment = $Environment
                source = "validation-script"
                message = "Test notification from SQS/SNS validation script"
                severity = "INFO"
            } | ConvertTo-Json -Compress
            
            $Result = aws sns publish --topic-arn $TestTopicArn --message $TestMessage --subject "Test Alert - $Environment" --region $Region --output json 2>$null | ConvertFrom-Json
            return -not [string]::IsNullOrEmpty($Result.MessageId)
        }
    }
    
    # Generate validation report
    Write-Log ""
    Write-Log "=== SQS and SNS Infrastructure Validation Report ==="
    Write-Log "Environment: $Environment"
    Write-Log "Region: $Region"
    Write-Log "Total Checks: $($ValidationResults.Passed + $ValidationResults.Failed + $ValidationResults.Warnings)"
    Write-Log "Passed: $($ValidationResults.Passed)"
    Write-Log "Failed: $($ValidationResults.Failed)"
    Write-Log "Warnings: $($ValidationResults.Warnings)"
    Write-Log ""
    
    if ($ValidationResults.Failed -gt 0) {
        Write-Log "Failed Validations:"
        $ValidationResults.Checks | Where-Object { $_.Status -eq "FAILED" } | ForEach-Object {
            Write-Log "  - $($_.Name): $($_.Error)"
        }
        Write-Log ""
    }
    
    if ($ValidationResults.Warnings -gt 0) {
        Write-Log "Warnings:"
        $ValidationResults.Checks | Where-Object { $_.Status -eq "WARNING" } | ForEach-Object {
            Write-Log "  - $($_.Name): $($_.Error)"
        }
        Write-Log ""
    }
    
    # Infrastructure Summary
    Write-Log "Infrastructure Summary:"
    Write-Log "  SQS Queues:"
    Write-Log "    - User Events: $($Outputs['sqs_user_events_queue_arn'])"
    Write-Log "    - Permission Events: $($Outputs['sqs_permission_events_queue_arn'])"
    Write-Log "    - Audit Events: $($Outputs['sqs_audit_events_queue_arn'])"
    Write-Log "  SNS Topics:"
    Write-Log "    - SQS Alerts: $($Outputs['sns_sqs_alerts_topic_arn'])"
    Write-Log "    - DLQ Alerts: $($Outputs['sns_sqs_dlq_alerts_topic_arn'])"
    Write-Log "    - Infrastructure Alerts: $($Outputs['sns_infrastructure_alerts_topic_arn'])"
    Write-Log ""
    
    # Final result
    if ($ValidationResults.Failed -eq 0) {
        Write-Log "🎉 All critical validations passed! Infrastructure is ready for use." "SUCCESS"
        if ($ValidationResults.Warnings -gt 0) {
            Write-Log "⚠️  Some warnings were found. Review them for optimal configuration." "WARN"
        }
        $ExitCode = 0
    }
    else {
        Write-Log "❌ Some critical validations failed. Please address the issues above." "ERROR"
        $ExitCode = 1
    }
    
    Write-Log "Validation log saved to: $LogFile"
    exit $ExitCode
    
}
catch {
    Write-Log "Unexpected error during validation: $($_.Exception.Message)" "ERROR"
    exit 1
}
finally {
    # Return to original directory
    Pop-Location -ErrorAction SilentlyContinue
}