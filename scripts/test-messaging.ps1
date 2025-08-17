#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Test script for the event-driven messaging system
.DESCRIPTION
    This script helps test the complete event-driven architecture by creating test data
    and verifying that domain events are properly published to SQS queues.
.PARAMETER BaseUrl
    The base URL of the API (default: http://localhost:8080)
.PARAMETER LocalStackUrl
    The LocalStack URL for checking SQS queues (default: http://localhost:4566)
#>

param(
    [string]$BaseUrl = "http://localhost:8080",
    [string]$LocalStackUrl = "http://localhost:4566"
)

Write-Host "🚀 Testing Event-Driven Architecture" -ForegroundColor Green
Write-Host "API URL: $BaseUrl" -ForegroundColor Cyan
Write-Host "LocalStack URL: $LocalStackUrl" -ForegroundColor Cyan
Write-Host ""

# Function to check if service is running
function Test-ServiceHealth {
    param([string]$Url, [string]$ServiceName)
    
    try {
        $response = Invoke-RestMethod -Uri "$Url/health" -Method Get -TimeoutSec 5
        Write-Host "✅ $ServiceName is healthy" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "❌ $ServiceName is not responding" -ForegroundColor Red
        return $false
    }
}

# Function to check SQS queue message count
function Get-QueueMessageCount {
    param([string]$QueueName)
    
    try {
        $queueUrl = "$LocalStackUrl/000000000000/$QueueName"
        $response = Invoke-RestMethod -Uri "$LocalStackUrl/" -Method Post -Headers @{
            "Content-Type" = "application/x-amz-json-1.0"
            "X-Amz-Target" = "AWSSimpleQueueService.GetQueueAttributes"
        } -Body (@{
            QueueUrl = $queueUrl
            AttributeNames = @("ApproximateNumberOfMessages")
        } | ConvertTo-Json)
        
        return $response.Attributes.ApproximateNumberOfMessages
    }
    catch {
        Write-Host "⚠️  Could not check queue $QueueName" -ForegroundColor Yellow
        return "Unknown"
    }
}

# Check service health
Write-Host "🔍 Checking service health..." -ForegroundColor Yellow
$apiHealthy = Test-ServiceHealth -Url $BaseUrl -ServiceName "API"

if (-not $apiHealthy) {
    Write-Host "❌ API is not healthy. Please start the application first." -ForegroundColor Red
    exit 1
}

# Check LocalStack health
try {
    $localStackHealth = Invoke-RestMethod -Uri "$LocalStackUrl/_localstack/health" -Method Get -TimeoutSec 5
    Write-Host "✅ LocalStack is healthy" -ForegroundColor Green
}
catch {
    Write-Host "❌ LocalStack is not responding. Please start LocalStack first." -ForegroundColor Red
    exit 1
}

Write-Host ""

# List all queues we expect
$expectedQueues = @(
    "user-events",
    "user-events-dlq",
    "permission-events", 
    "permission-events-dlq",
    "user-permission-events",
    "user-permission-events-dlq",
    "role-events",
    "role-events-dlq", 
    "role-permission-events",
    "role-permission-events-dlq",
    "user-role-events",
    "user-role-events-dlq",
    "audit-events.fifo",
    "audit-events-dlq.fifo"
)

Write-Host "📊 Checking SQS Queue Status:" -ForegroundColor Yellow
Write-Host "Queue Name                    | Message Count" -ForegroundColor Cyan
Write-Host "-------------------------------------------" -ForegroundColor Cyan

foreach ($queue in $expectedQueues) {
    $count = Get-QueueMessageCount -QueueName $queue
    $paddedName = $queue.PadRight(30)
    Write-Host "$paddedName| $count" -ForegroundColor White
}

Write-Host ""
Write-Host "🧪 Testing Event Flow..." -ForegroundColor Yellow

# Test 1: Create a user (should trigger UserCreatedEvent -> UserCreatedMessage)
Write-Host "1. Creating a test user..." -ForegroundColor Cyan
try {
    $createUserPayload = @{
        email = "test-$(Get-Random)@example.com"
        firstName = "Test"
        lastName = "User"
        password = "TestPassword123!"
    } | ConvertTo-Json

    $userResponse = Invoke-RestMethod -Uri "$BaseUrl/api/users" -Method Post -Body $createUserPayload -ContentType "application/json"
    Write-Host "   ✅ User created with ID: $($userResponse.id)" -ForegroundColor Green
    
    # Wait a moment for message processing
    Start-Sleep -Seconds 2
    
    $userEventsCount = Get-QueueMessageCount -QueueName "user-events"
    Write-Host "   📨 user-events queue now has: $userEventsCount messages" -ForegroundColor White
}
catch {
    Write-Host "   ❌ Failed to create user: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 2: Create a permission (should trigger PermissionCreatedEvent -> PermissionCreatedMessage)
Write-Host "2. Creating a test permission..." -ForegroundColor Cyan
try {
    $createPermissionPayload = @{
        resource = "TestResource"
        action = "Read"
        name = "TestResource.Read"
        description = "Test permission for messaging"
        category = "Testing"
    } | ConvertTo-Json

    $permissionResponse = Invoke-RestMethod -Uri "$BaseUrl/api/permissions" -Method Post -Body $createPermissionPayload -ContentType "application/json"
    Write-Host "   ✅ Permission created with ID: $($permissionResponse.id)" -ForegroundColor Green
    
    # Wait a moment for message processing
    Start-Sleep -Seconds 2
    
    $permissionEventsCount = Get-QueueMessageCount -QueueName "permission-events"
    Write-Host "   📨 permission-events queue now has: $permissionEventsCount messages" -ForegroundColor White
}
catch {
    Write-Host "   ❌ Failed to create permission: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 3: Create a role (should trigger RoleCreatedEvent -> RoleCreatedMessage)
Write-Host "3. Creating a test role..." -ForegroundColor Cyan
try {
    $createRolePayload = @{
        name = "TestRole-$(Get-Random)"
        description = "Test role for messaging"
    } | ConvertTo-Json

    $roleResponse = Invoke-RestMethod -Uri "$BaseUrl/api/roles" -Method Post -Body $createRolePayload -ContentType "application/json"
    Write-Host "   ✅ Role created with ID: $($roleResponse.id)" -ForegroundColor Green
    
    # Wait a moment for message processing
    Start-Sleep -Seconds 2
    
    $roleEventsCount = Get-QueueMessageCount -QueueName "role-events"
    Write-Host "   📨 role-events queue now has: $roleEventsCount messages" -ForegroundColor White
}
catch {
    Write-Host "   ❌ Failed to create role: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "🎉 Event-driven architecture test completed!" -ForegroundColor Green
Write-Host ""
Write-Host "💡 Tips:" -ForegroundColor Yellow
Write-Host "   • Check the application logs to see event handler execution"
Write-Host "   • Use Seq (http://localhost:5341) for structured log analysis"
Write-Host "   • Monitor queue depths to ensure messages are being processed"
Write-Host "   • Check dead letter queues for any failed messages"
Write-Host ""
Write-Host "📚 For more information, see docs/EVENT_DRIVEN_ARCHITECTURE.md" -ForegroundColor Cyan