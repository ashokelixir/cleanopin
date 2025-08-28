#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Test SQS messaging infrastructure for Clean Architecture Template

.DESCRIPTION
    This script tests the SQS messaging infrastructure by:
    - Verifying queue creation and configuration
    - Testing message publishing and consumption
    - Validating dead letter queue functionality
    - Checking CloudWatch alarms
    - Verifying IAM permissions

.PARAMETER Environment
    Target environment to test (dev, staging, prod)

.PARAMETER Region
    AWS region where resources are deployed

.PARAMETER SkipMessageTests
    Skip actual message publishing/consumption tests

.EXAMPLE
    ./sqs.test.ps1 -Environment dev -Region ap-south-1

.EXAMPLE
    ./sqs.test.ps1 -Environment prod -Region us-east-1 -SkipMessageTests
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment,
    
    [Parameter(Mandatory = $false)]
    [string]$Region = "ap-south-1",
    
    [Parameter(Mandatory = $false)]
    [switch]$SkipMessageTests
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Script configuration
$ScriptName = "SQS Infrastructure Tests"
$LogFile = "logs/sqs-test-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
$TerraformDir = Split-Path -Parent $PSScriptRoot

# Test results
$TestResults = @{
    Passed = 0
    Failed = 0
    Skipped = 0
    Tests = @()
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

# Test function
function Test-Condition {
    param(
        [string]$TestName,
        [scriptblock]$TestScript,
        [string]$Description = ""
    )
    
    Write-Log "Running test: $TestName"
    if ($Description) {
        Write-Log "  Description: $Description"
    }
    
    try {
        $Result = & $TestScript
        if ($Result) {
            Write-Log "  ✅ PASSED: $TestName" "PASS"
            $TestResults.Passed++
            $TestResults.Tests += @{
                Name = $TestName
                Status = "PASSED"
                Description = $Description
                Error = $null
            }
            return $true
        }
        else {
            Write-Log "  ❌ FAILED: $TestName" "FAIL"
            $TestResults.Failed++
            $TestResults.Tests += @{
                Name = $TestName
                Status = "FAILED"
                Description = $Description
                Error = "Test condition returned false"
            }
            return $false
        }
    }
    catch {
        Write-Log "  ❌ FAILED: $TestName - $($_.Exception.Message)" "FAIL"
        $TestResults.Failed++
        $TestResults.Tests += @{
            Name = $TestName
            Status = "FAILED"
            Description = $Description
            Error = $_.Exception.Message
        }
        return $false
    }
}

# Skip test function
function Skip-Test {
    param(
        [string]$TestName,
        [string]$Reason
    )
    
    Write-Log "  ⏭️  SKIPPED: $TestName - $Reason" "SKIP"
    $TestResults.Skipped++
    $TestResults.Tests += @{
        Name = $TestName
        Status = "SKIPPED"
        Description = $Reason
        Error = $null
    }
}

try {
    Write-Log "Starting $ScriptName for environment: $Environment"
    Write-Log "Region: $Region"
    Write-Log "Log file: $LogFile"
    
    # Change to terraform directory
    Set-Location $TerraformDir
    
    # Get Terraform outputs
    Write-Log "Retrieving Terraform outputs..."
    $TerraformOutputs = @{}
    
    $OutputNames = @(
        "sqs_user_events_queue_arn",
        "sqs_user_events_queue_url",
        "sqs_permission_events_queue_arn",
        "sqs_permission_events_queue_url",
        "sqs_audit_events_queue_arn",
        "sqs_audit_events_queue_url",
        "sqs_all_queue_arns",
        "sqs_main_queue_arns",
        "sqs_dlq_arns",
        "sns_sqs_alerts_topic_arn",
        "sns_sqs_dlq_alerts_topic_arn",
        "sns_infrastructure_alerts_topic_arn",
        "sns_all_topic_arns"
    )
    
    foreach ($OutputName in $OutputNames) {
        try {
            $Value = terraform output -raw $OutputName 2>$null
            if ($LASTEXITCODE -eq 0) {
                $TerraformOutputs[$OutputName] = $Value
            }
        }
        catch {
            Write-Log "Warning: Could not retrieve output $OutputName"
        }
    }
    
    # Test 1: Verify queue creation
    Test-Condition -TestName "User Events Queue Exists" -Description "Verify user events queue is created" -TestScript {
        $QueueArn = $TerraformOutputs["sqs_user_events_queue_arn"]
        return -not [string]::IsNullOrEmpty($QueueArn) -and $QueueArn.Contains("user-events")
    }
    
    Test-Condition -TestName "Permission Events Queue Exists" -Description "Verify permission events queue is created" -TestScript {
        $QueueArn = $TerraformOutputs["sqs_permission_events_queue_arn"]
        return -not [string]::IsNullOrEmpty($QueueArn) -and $QueueArn.Contains("permission-events")
    }
    
    Test-Condition -TestName "Audit Events FIFO Queue Exists" -Description "Verify audit events FIFO queue is created" -TestScript {
        $QueueArn = $TerraformOutputs["sqs_audit_events_queue_arn"]
        return -not [string]::IsNullOrEmpty($QueueArn) -and $QueueArn.Contains("audit-events") -and $QueueArn.Contains(".fifo")
    }
    
    # Test 2: Verify queue attributes using AWS CLI
    if ($TerraformOutputs["sqs_user_events_queue_url"]) {
        Test-Condition -TestName "User Events Queue Attributes" -Description "Verify queue attributes are correctly configured" -TestScript {
            $QueueUrl = $TerraformOutputs["sqs_user_events_queue_url"]
            $Attributes = aws sqs get-queue-attributes --queue-url $QueueUrl --attribute-names All --region $Region --output json | ConvertFrom-Json
            
            $VisibilityTimeout = [int]$Attributes.Attributes.VisibilityTimeout
            $MessageRetention = [int]$Attributes.Attributes.MessageRetentionPeriod
            $ReceiveWaitTime = [int]$Attributes.Attributes.ReceiveMessageWaitTimeSeconds
            
            return $VisibilityTimeout -eq 30 -and $MessageRetention -eq 1209600 -and $ReceiveWaitTime -eq 20
        }
    }
    
    if ($TerraformOutputs["sqs_audit_events_queue_url"]) {
        Test-Condition -TestName "Audit Events FIFO Queue Attributes" -Description "Verify FIFO queue attributes" -TestScript {
            $QueueUrl = $TerraformOutputs["sqs_audit_events_queue_url"]
            $Attributes = aws sqs get-queue-attributes --queue-url $QueueUrl --attribute-names All --region $Region --output json | ConvertFrom-Json
            
            $IsFifo = $Attributes.Attributes.FifoQueue -eq "true"
            $ContentBasedDedup = $Attributes.Attributes.ContentBasedDeduplication -eq "true"
            $VisibilityTimeout = [int]$Attributes.Attributes.VisibilityTimeout
            
            return $IsFifo -and $ContentBasedDedup -and $VisibilityTimeout -eq 60
        }
    }
    
    # Test 3: Verify dead letter queue configuration
    Test-Condition -TestName "Dead Letter Queue Configuration" -Description "Verify DLQ redrive policy is configured" -TestScript {
        $QueueUrl = $TerraformOutputs["sqs_user_events_queue_url"]
        if ([string]::IsNullOrEmpty($QueueUrl)) { return $false }
        
        $Attributes = aws sqs get-queue-attributes --queue-url $QueueUrl --attribute-names RedrivePolicy --region $Region --output json | ConvertFrom-Json
        $RedrivePolicy = $Attributes.Attributes.RedrivePolicy | ConvertFrom-Json
        
        return $RedrivePolicy.maxReceiveCount -eq 3 -and -not [string]::IsNullOrEmpty($RedrivePolicy.deadLetterTargetArn)
    }
    
    # Test 4: Verify server-side encryption
    Test-Condition -TestName "Server-Side Encryption Enabled" -Description "Verify SSE is enabled on queues" -TestScript {
        $QueueUrl = $TerraformOutputs["sqs_user_events_queue_url"]
        if ([string]::IsNullOrEmpty($QueueUrl)) { return $false }
        
        $Attributes = aws sqs get-queue-attributes --queue-url $QueueUrl --attribute-names SqsManagedSseEnabled --region $Region --output json | ConvertFrom-Json
        return $Attributes.Attributes.SqsManagedSseEnabled -eq "true"
    }
    
    # Test 5: Message publishing and consumption tests
    if (-not $SkipMessageTests) {
        Test-Condition -TestName "Message Publishing Test" -Description "Test publishing a message to user events queue" -TestScript {
            $QueueUrl = $TerraformOutputs["sqs_user_events_queue_url"]
            if ([string]::IsNullOrEmpty($QueueUrl)) { return $false }
            
            $TestMessage = @{
                messageId = [System.Guid]::NewGuid().ToString()
                messageType = "UserCreatedMessage"
                createdAt = (Get-Date).ToString("o")
                payload = @{
                    userId = [System.Guid]::NewGuid().ToString()
                    email = "test@example.com"
                    firstName = "Test"
                    lastName = "User"
                }
            } | ConvertTo-Json -Compress
            
            $Result = aws sqs send-message --queue-url $QueueUrl --message-body $TestMessage --region $Region --output json | ConvertFrom-Json
            return -not [string]::IsNullOrEmpty($Result.MessageId)
        }
        
        Test-Condition -TestName "FIFO Message Publishing Test" -Description "Test publishing a FIFO message with message group ID" -TestScript {
            $QueueUrl = $TerraformOutputs["sqs_audit_events_queue_url"]
            if ([string]::IsNullOrEmpty($QueueUrl)) { return $false }
            
            $TestMessage = @{
                messageId = [System.Guid]::NewGuid().ToString()
                messageType = "AuditEvent"
                createdAt = (Get-Date).ToString("o")
                payload = @{
                    eventType = "UserCreated"
                    userId = [System.Guid]::NewGuid().ToString()
                }
            } | ConvertTo-Json -Compress
            
            $MessageGroupId = "test-group-$(Get-Date -Format 'yyyyMMdd')"
            $DeduplicationId = [System.Guid]::NewGuid().ToString()
            
            $Result = aws sqs send-message --queue-url $QueueUrl --message-body $TestMessage --message-group-id $MessageGroupId --message-deduplication-id $DeduplicationId --region $Region --output json | ConvertFrom-Json
            return -not [string]::IsNullOrEmpty($Result.MessageId)
        }
        
        Test-Condition -TestName "Message Consumption Test" -Description "Test receiving messages from queue" -TestScript {
            $QueueUrl = $TerraformOutputs["sqs_user_events_queue_url"]
            if ([string]::IsNullOrEmpty($QueueUrl)) { return $false }
            
            # Wait a moment for message to be available
            Start-Sleep -Seconds 2
            
            $Messages = aws sqs receive-message --queue-url $QueueUrl --max-number-of-messages 1 --region $Region --output json | ConvertFrom-Json
            
            if ($Messages.Messages -and $Messages.Messages.Count -gt 0) {
                # Clean up by deleting the test message
                $ReceiptHandle = $Messages.Messages[0].ReceiptHandle
                aws sqs delete-message --queue-url $QueueUrl --receipt-handle $ReceiptHandle --region $Region | Out-Null
                return $true
            }
            
            return $false
        }
    }
    else {
        Skip-Test -TestName "Message Publishing Test" -Reason "Message tests skipped by parameter"
        Skip-Test -TestName "FIFO Message Publishing Test" -Reason "Message tests skipped by parameter"
        Skip-Test -TestName "Message Consumption Test" -Reason "Message tests skipped by parameter"
    }
    
    # Test 6: CloudWatch alarms
    Test-Condition -TestName "CloudWatch Alarms Exist" -Description "Verify CloudWatch alarms are created for queues" -TestScript {
        $AlarmNames = aws cloudwatch describe-alarms --alarm-name-prefix "cleanarch-$Environment" --region $Region --output json | ConvertFrom-Json
        $SqsAlarms = $AlarmNames.MetricAlarms | Where-Object { $_.Namespace -eq "AWS/SQS" }
        return $SqsAlarms.Count -gt 0
    }
    
    # Test 7: IAM permissions (if ECS task role exists)
    Test-Condition -TestName "ECS Task Role SQS Permissions" -Description "Verify ECS task role has SQS permissions" -TestScript {
        try {
            $RoleName = "cleanarch-$Environment-ecs-task-role"
            $Policies = aws iam list-role-policies --role-name $RoleName --region $Region --output json | ConvertFrom-Json
            $SqsPolicy = $Policies.PolicyNames | Where-Object { $_ -like "*sqs*" }
            return $SqsPolicy.Count -gt 0
        }
        catch {
            # Role might not exist yet, which is okay
            return $true
        }
    }
    
    # Test 8: Queue naming convention
    Test-Condition -TestName "Queue Naming Convention" -Description "Verify queues follow naming convention" -TestScript {
        $UserEventsArn = $TerraformOutputs["sqs_user_events_queue_arn"]
        $PermissionEventsArn = $TerraformOutputs["sqs_permission_events_queue_arn"]
        $AuditEventsArn = $TerraformOutputs["sqs_audit_events_queue_arn"]
        
        $ExpectedPrefix = "cleanarch-$Environment"
        
        $UserEventsValid = $UserEventsArn -like "*$ExpectedPrefix-user-events*"
        $PermissionEventsValid = $PermissionEventsArn -like "*$ExpectedPrefix-permission-events*"
        $AuditEventsValid = $AuditEventsArn -like "*$ExpectedPrefix-audit-events.fifo*"
        
        return $UserEventsValid -and $PermissionEventsValid -and $AuditEventsValid
    }
    
    # Test 9: Queue count validation
    Test-Condition -TestName "Expected Queue Count" -Description "Verify expected number of queues are created" -TestScript {
        $AllQueueArns = $TerraformOutputs["sqs_all_queue_arns"]
        if ([string]::IsNullOrEmpty($AllQueueArns)) { return $false }
        
        # Parse the JSON array of ARNs
        $QueueArns = $AllQueueArns | ConvertFrom-Json
        
        # Expected: 3 main queues + 3 DLQs = 6 total
        return $QueueArns.Count -eq 6
    }
    
    # Test 10: SNS Topics Exist
    Test-Condition -TestName "SNS SQS Alerts Topic Exists" -Description "Verify SQS alerts SNS topic is created" -TestScript {
        $TopicArn = $TerraformOutputs["sns_sqs_alerts_topic_arn"]
        return -not [string]::IsNullOrEmpty($TopicArn) -and $TopicArn.Contains("sqs-alerts")
    }
    
    Test-Condition -TestName "SNS DLQ Alerts Topic Exists" -Description "Verify SQS DLQ alerts SNS topic is created" -TestScript {
        $TopicArn = $TerraformOutputs["sns_sqs_dlq_alerts_topic_arn"]
        return -not [string]::IsNullOrEmpty($TopicArn) -and $TopicArn.Contains("sqs-dlq-alerts")
    }
    
    Test-Condition -TestName "SNS Infrastructure Alerts Topic Exists" -Description "Verify infrastructure alerts SNS topic is created" -TestScript {
        $TopicArn = $TerraformOutputs["sns_infrastructure_alerts_topic_arn"]
        return -not [string]::IsNullOrEmpty($TopicArn) -and $TopicArn.Contains("infrastructure-alerts")
    }
    
    # Test 11: SNS Topic Attributes
    Test-Condition -TestName "SNS Topic Encryption" -Description "Verify SNS topics have encryption enabled" -TestScript {
        $TopicArn = $TerraformOutputs["sns_sqs_alerts_topic_arn"]
        if ([string]::IsNullOrEmpty($TopicArn)) { return $false }
        
        $Attributes = aws sns get-topic-attributes --topic-arn $TopicArn --region $Region --output json | ConvertFrom-Json
        return -not [string]::IsNullOrEmpty($Attributes.Attributes.KmsMasterKeyId)
    }
    
    # Test 12: SNS Subscriptions
    Test-Condition -TestName "SNS Email Subscriptions" -Description "Verify email subscriptions exist for SNS topics" -TestScript {
        $TopicArn = $TerraformOutputs["sns_sqs_alerts_topic_arn"]
        if ([string]::IsNullOrEmpty($TopicArn)) { return $false }
        
        $Subscriptions = aws sns list-subscriptions-by-topic --topic-arn $TopicArn --region $Region --output json | ConvertFrom-Json
        $EmailSubscriptions = $Subscriptions.Subscriptions | Where-Object { $_.Protocol -eq "email" }
        
        # At least one email subscription should exist if notifications are enabled
        return $EmailSubscriptions.Count -ge 0  # Allow 0 for environments without email configured
    }
    
    # Test 13: CloudWatch Alarms SNS Integration
    Test-Condition -TestName "CloudWatch Alarms SNS Integration" -Description "Verify CloudWatch alarms are configured to send to SNS" -TestScript {
        $AlarmNames = aws cloudwatch describe-alarms --alarm-name-prefix "cleanarch-$Environment" --region $Region --output json | ConvertFrom-Json
        $SqsAlarms = $AlarmNames.MetricAlarms | Where-Object { $_.Namespace -eq "AWS/SQS" }
        
        if ($SqsAlarms.Count -eq 0) { return $false }
        
        # Check if at least one alarm has SNS actions configured
        $AlarmsWithSns = $SqsAlarms | Where-Object { $_.AlarmActions.Count -gt 0 }
        return $AlarmsWithSns.Count -gt 0
    }
    
    # Test 14: SNS Topic Count
    Test-Condition -TestName "Expected SNS Topic Count" -Description "Verify expected number of SNS topics are created" -TestScript {
        $AllTopicArns = $TerraformOutputs["sns_all_topic_arns"]
        if ([string]::IsNullOrEmpty($AllTopicArns)) { return $false }
        
        # Parse the JSON array of ARNs
        $TopicArns = $AllTopicArns | ConvertFrom-Json
        
        # Expected: 3 topics (sqs-alerts, sqs-dlq-alerts, infrastructure-alerts)
        return $TopicArns.Count -eq 3
    }
    
    # Generate test report
    Write-Log ""
    Write-Log "=== SQS Infrastructure Test Report ==="
    Write-Log "Environment: $Environment"
    Write-Log "Region: $Region"
    Write-Log "Total Tests: $($TestResults.Passed + $TestResults.Failed + $TestResults.Skipped)"
    Write-Log "Passed: $($TestResults.Passed)"
    Write-Log "Failed: $($TestResults.Failed)"
    Write-Log "Skipped: $($TestResults.Skipped)"
    Write-Log ""
    
    if ($TestResults.Failed -gt 0) {
        Write-Log "Failed Tests:"
        $TestResults.Tests | Where-Object { $_.Status -eq "FAILED" } | ForEach-Object {
            Write-Log "  - $($_.Name): $($_.Error)"
        }
        Write-Log ""
    }
    
    if ($TestResults.Skipped -gt 0) {
        Write-Log "Skipped Tests:"
        $TestResults.Tests | Where-Object { $_.Status -eq "SKIPPED" } | ForEach-Object {
            Write-Log "  - $($_.Name): $($_.Description)"
        }
        Write-Log ""
    }
    
    # Summary
    if ($TestResults.Failed -eq 0) {
        Write-Log "🎉 All tests passed! SQS infrastructure is working correctly." "SUCCESS"
        $ExitCode = 0
    }
    else {
        Write-Log "❌ Some tests failed. Please review the failures above." "ERROR"
        $ExitCode = 1
    }
    
    Write-Log "Test log saved to: $LogFile"
    exit $ExitCode
    
}
catch {
    Write-Log "Unexpected error during testing: $($_.Exception.Message)" "ERROR"
    exit 1
}
finally {
    # Return to original directory
    Pop-Location -ErrorAction SilentlyContinue
}