# CI/CD Pipeline Script for Terraform Deployment
# This script orchestrates the complete CI/CD workflow for infrastructure deployment
# Usage: .\ci-cd-pipeline.ps1 -Environment <env> -Action <action> [-Branch <branch>] [-CommitSha <sha>]

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment,
    
    [Parameter(Mandatory=$true)]
    [ValidateSet("validate", "plan", "apply", "destroy", "promote")]
    [string]$Action,
    
    [string]$Branch = "main",
    [string]$CommitSha = "",
    [string]$PullRequestId = "",
    [string]$BuildId = "",
    [switch]$AutoApprove,
    [string]$NotificationWebhook = "",
    [string]$SlackChannel = "",
    [switch]$SkipTests,
    [switch]$EnableDriftDetection,
    [string]$PromoteFrom = ""
)

# Configuration
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$TerraformDir = Split-Path -Parent $ScriptDir
$LogDir = Join-Path $TerraformDir "logs"
$PlanDir = Join-Path $TerraformDir "plans"
$TestDir = Join-Path $TerraformDir "tests"
$ReportDir = Join-Path $TerraformDir "reports"

# Ensure directories exist
@($LogDir, $PlanDir, $TestDir, $ReportDir) | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -ItemType Directory -Path $_ -Force | Out-Null
    }
}

# Logging configuration
$Timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$LogFile = Join-Path $LogDir "ci-cd-$Environment-$Action-$Timestamp.log"
$ErrorActionPreference = "Stop"

# Pipeline metadata
$PipelineMetadata = @{
    Environment = $Environment
    Action = $Action
    Branch = $Branch
    CommitSha = $CommitSha
    PullRequestId = $PullRequestId
    BuildId = $BuildId
    StartTime = Get-Date
    LogFile = $LogFile
}

function Write-PipelineLog {
    param([string]$Message, [string]$Level = "INFO")
    $LogMessage = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] [$Level] [CI/CD] $Message"
    Write-Host $LogMessage -ForegroundColor $(
        switch ($Level) {
            "ERROR" { "Red" }
            "WARNING" { "Yellow" }
            "SUCCESS" { "Green" }
            "HEADER" { "Blue" }
            default { "White" }
        }
    )
    Add-Content -Path $LogFile -Value $LogMessage
}

function Send-PipelineNotification {
    param([string]$Title, [string]$Message, [string]$Color = "warning", [hashtable]$Fields = @{})
    
    if ($NotificationWebhook) {
        try {
            $NotificationFields = @(
                @{
                    title = "Environment"
                    value = $Environment
                    short = $true
                },
                @{
                    title = "Action"
                    value = $Action
                    short = $true
                },
                @{
                    title = "Branch"
                    value = $Branch
                    short = $true
                },
                @{
                    title = "Timestamp"
                    value = (Get-Date -Format 'yyyy-MM-dd HH:mm:ss UTC')
                    short = $true
                }
            )
            
            # Add custom fields
            foreach ($Field in $Fields.GetEnumerator()) {
                $NotificationFields += @{
                    title = $Field.Key
                    value = $Field.Value
                    short = $true
                }
            }
            
            $Payload = @{
                channel = $SlackChannel
                attachments = @(
                    @{
                        color = $Color
                        title = $Title
                        text = $Message
                        fields = $NotificationFields
                    }
                )
            } | ConvertTo-Json -Depth 4
            
            Invoke-RestMethod -Uri $NotificationWebhook -Method Post -Body $Payload -ContentType "application/json"
            Write-PipelineLog "Pipeline notification sent successfully" "SUCCESS"
        }
        catch {
            Write-PipelineLog "Failed to send pipeline notification: $($_.Exception.Message)" "ERROR"
        }
    }
}

function Test-PipelinePrerequisites {
    Write-PipelineLog "Validating CI/CD pipeline prerequisites" "INFO"
    
    # Check Terraform
    try {
        $TerraformVersion = terraform version -json | ConvertFrom-Json
        Write-PipelineLog "Terraform version: $($TerraformVersion.terraform_version)" "SUCCESS"
    }
    catch {
        throw "Terraform is not installed or not in PATH"
    }
    
    # Check AWS CLI
    try {
        $CallerIdentity = aws sts get-caller-identity | ConvertFrom-Json
        Write-PipelineLog "AWS Account: $($CallerIdentity.Account)" "SUCCESS"
        Write-PipelineLog "AWS User: $($CallerIdentity.Arn)" "SUCCESS"
    }
    catch {
        throw "AWS CLI is not configured"
    }
    
    # Check required files
    $BackendConfig = Join-Path $TerraformDir "backend-configs\$Environment.hcl"
    $VarFile = Join-Path $TerraformDir "environments\$Environment.tfvars"
    
    if (-not (Test-Path $BackendConfig)) {
        throw "Backend config file not found: $BackendConfig"
    }
    
    if (-not (Test-Path $VarFile)) {
        throw "Variables file not found: $VarFile"
    }
    
    # Validate Git information if provided
    if ($CommitSha) {
        Write-PipelineLog "Commit SHA: $CommitSha" "INFO"
    }
    
    if ($PullRequestId) {
        Write-PipelineLog "Pull Request ID: $PullRequestId" "INFO"
    }
    
    if ($BuildId) {
        Write-PipelineLog "Build ID: $BuildId" "INFO"
    }
    
    Write-PipelineLog "Pipeline prerequisites validation completed" "SUCCESS"
}

function Initialize-PipelineTerraform {
    Write-PipelineLog "Initializing Terraform for CI/CD pipeline" "HEADER"
    
    $BackendConfig = Join-Path $TerraformDir "backend-configs\$Environment.hcl"
    
    Set-Location $TerraformDir
    
    # Clean up any existing .terraform directory for fresh init
    if (Test-Path ".terraform") {
        Write-PipelineLog "Cleaning up existing .terraform directory" "INFO"
        Remove-Item ".terraform" -Recurse -Force
    }
    
    # Initialize with backend config
    Write-PipelineLog "Initializing Terraform backend with state locking" "INFO"
    terraform init -backend-config="$BackendConfig" -upgrade -reconfigure
    
    if ($LASTEXITCODE -ne 0) {
        throw "Terraform initialization failed"
    }
    
    # Create or select workspace
    $WorkspaceList = terraform workspace list
    if ($WorkspaceList -match "\s$Environment\s") {
        Write-PipelineLog "Selecting existing workspace: $Environment" "INFO"
        terraform workspace select $Environment
    }
    else {
        Write-PipelineLog "Creating new workspace: $Environment" "INFO"
        terraform workspace new $Environment
    }
    
    if ($LASTEXITCODE -ne 0) {
        throw "Workspace operation failed"
    }
    
    Write-PipelineLog "Terraform initialization completed" "SUCCESS"
}

function Invoke-TerraformValidation {
    Write-PipelineLog "Running Terraform validation" "HEADER"
    
    Set-Location $TerraformDir
    
    # Format check
    Write-PipelineLog "Checking Terraform formatting" "INFO"
    terraform fmt -check -recursive
    
    if ($LASTEXITCODE -ne 0) {
        Write-PipelineLog "Terraform formatting issues detected" "WARNING"
        terraform fmt -recursive
        Write-PipelineLog "Terraform files have been formatted" "INFO"
    }
    else {
        Write-PipelineLog "Terraform formatting is correct" "SUCCESS"
    }
    
    # Validation
    Write-PipelineLog "Validating Terraform configuration" "INFO"
    terraform validate
    
    if ($LASTEXITCODE -ne 0) {
        throw "Terraform validation failed"
    }
    
    Write-PipelineLog "Terraform validation completed successfully" "SUCCESS"
}

function Invoke-PipelinePlan {
    Write-PipelineLog "Generating Terraform execution plan" "HEADER"
    
    $VarFile = Join-Path $TerraformDir "environments\$Environment.tfvars"
    $PlanFile = Join-Path $PlanDir "pipeline-$Environment-$Timestamp.tfplan"
    $PlanJsonFile = "$PlanFile.json"
    
    Set-Location $TerraformDir
    
    # Ensure we're in the correct workspace
    terraform workspace select $Environment
    
    # Run plan with detailed output
    Write-PipelineLog "Generating execution plan..." "INFO"
    terraform plan -var-file="$VarFile" -out="$PlanFile" -detailed-exitcode
    $PlanExitCode = $LASTEXITCODE
    
    # Generate JSON output for analysis
    terraform show -json "$PlanFile" > "$PlanJsonFile"
    
    $PlanResult = @{
        ExitCode = $PlanExitCode
        PlanFile = $PlanFile
        JsonFile = $PlanJsonFile
        HasChanges = $false
        Summary = @{
            Add = 0
            Change = 0
            Destroy = 0
        }
        Changes = @()
    }
    
    # Analyze plan results
    switch ($PlanExitCode) {
        0 {
            Write-PipelineLog "No changes detected in plan" "SUCCESS"
            $PlanResult.HasChanges = $false
        }
        1 {
            throw "Terraform plan generation failed"
        }
        2 {
            Write-PipelineLog "Changes detected in plan" "INFO"
            $PlanResult.HasChanges = $true
            
            # Parse and display plan summary
            try {
                $PlanJson = Get-Content "$PlanJsonFile" | ConvertFrom-Json
                
                if ($PlanJson.resource_changes) {
                    foreach ($Change in $PlanJson.resource_changes) {
                        $PlanResult.Changes += @{
                            Address = $Change.address
                            Type = $Change.type
                            Actions = $Change.change.actions
                        }
                        
                        # Count changes by type
                        if ($Change.change.actions -contains "create") {
                            $PlanResult.Summary.Add++
                        }
                        if ($Change.change.actions -contains "update") {
                            $PlanResult.Summary.Change++
                        }
                        if ($Change.change.actions -contains "delete") {
                            $PlanResult.Summary.Destroy++
                        }
                    }
                }
                
                Write-PipelineLog "Plan Summary:" "INFO"
                Write-PipelineLog "  Resources to add: $($PlanResult.Summary.Add)" "INFO"
                Write-PipelineLog "  Resources to change: $($PlanResult.Summary.Change)" "INFO"
                Write-PipelineLog "  Resources to destroy: $($PlanResult.Summary.Destroy)" "INFO"
            }
            catch {
                Write-PipelineLog "Failed to parse plan JSON: $($_.Exception.Message)" "WARNING"
            }
        }
    }
    
    Write-PipelineLog "Terraform plan completed" "SUCCESS"
    return $PlanResult
}

function Invoke-PipelineApply {
    param([object]$PlanResult)
    
    Write-PipelineLog "Applying Terraform changes" "HEADER"
    
    if (-not $PlanResult.HasChanges) {
        Write-PipelineLog "No changes to apply" "INFO"
        return $true
    }
    
    # Production safety check
    if ($Environment -eq "prod" -and -not $AutoApprove) {
        Write-PipelineLog "Production deployment requires explicit approval" "WARNING"
        
        if ($env:CI -eq "true") {
            Write-PipelineLog "Running in CI environment - checking for auto-approval flag" "INFO"
            if (-not $AutoApprove) {
                throw "Production deployment in CI requires -AutoApprove flag"
            }
        }
        else {
            $Confirm = Read-Host "Type 'DEPLOY TO PRODUCTION' to confirm (case-sensitive)"
            if ($Confirm -ne "DEPLOY TO PRODUCTION") {
                Write-PipelineLog "Production deployment cancelled by user" "INFO"
                return $false
            }
        }
    }
    
    Set-Location $TerraformDir
    
    # Ensure we're in the correct workspace
    terraform workspace select $Environment
    
    # Apply changes
    Write-PipelineLog "Applying changes from plan file: $($PlanResult.PlanFile)" "INFO"
    terraform apply -auto-approve $PlanResult.PlanFile
    
    if ($LASTEXITCODE -eq 0) {
        Write-PipelineLog "Terraform apply completed successfully" "SUCCESS"
        
        # Clean up plan file after successful apply
        Remove-Item $PlanResult.PlanFile -Force -ErrorAction SilentlyContinue
        Remove-Item $PlanResult.JsonFile -Force -ErrorAction SilentlyContinue
        
        return $true
    }
    else {
        throw "Terraform apply failed"
    }
}

function Invoke-PipelineTests {
    Write-PipelineLog "Running infrastructure tests" "HEADER"
    
    if ($SkipTests) {
        Write-PipelineLog "Skipping infrastructure tests (--SkipTests specified)" "INFO"
        return $true
    }
    
    # Check if test files exist
    $TestFiles = Get-ChildItem -Path $TestDir -Filter "*.test.ps1" -ErrorAction SilentlyContinue
    
    if (-not $TestFiles) {
        Write-PipelineLog "No infrastructure test files found in $TestDir" "WARNING"
        return $true
    }
    
    $TestResults = @()
    $AllTestsPassed = $true
    
    foreach ($TestFile in $TestFiles) {
        Write-PipelineLog "Running test: $($TestFile.Name)" "INFO"
        
        try {
            $TestResult = & $TestFile.FullName -Environment $Environment
            $TestResults += @{
                Test = $TestFile.Name
                Result = "PASS"
                Details = $TestResult
            }
            Write-PipelineLog "Test passed: $($TestFile.Name)" "SUCCESS"
        }
        catch {
            $TestResults += @{
                Test = $TestFile.Name
                Result = "FAIL"
                Details = $_.Exception.Message
            }
            Write-PipelineLog "Test failed: $($TestFile.Name) - $($_.Exception.Message)" "ERROR"
            $AllTestsPassed = $false
        }
    }
    
    # Generate test report
    $TestReport = Join-Path $ReportDir "pipeline-test-report-$Environment-$Timestamp.json"
    $TestResults | ConvertTo-Json -Depth 3 | Out-File $TestReport
    
    $PassedTests = ($TestResults | Where-Object { $_.Result -eq "PASS" }).Count
    $FailedTests = ($TestResults | Where-Object { $_.Result -eq "FAIL" }).Count
    
    Write-PipelineLog "Test Summary: $PassedTests passed, $FailedTests failed" "INFO"
    Write-PipelineLog "Test report saved to: $TestReport" "INFO"
    
    return $AllTestsPassed
}

function Invoke-DriftDetection {
    Write-PipelineLog "Running drift detection" "HEADER"
    
    if (-not $EnableDriftDetection) {
        Write-PipelineLog "Drift detection not enabled" "INFO"
        return $true
    }
    
    try {
        & "$ScriptDir\drift-detection.ps1" -Environment $Environment -GenerateReport
        Write-PipelineLog "Drift detection completed - no drift found" "SUCCESS"
        return $true
    }
    catch {
        Write-PipelineLog "Drift detection failed or drift found: $($_.Exception.Message)" "WARNING"
        return $false
    }
}

function Invoke-EnvironmentPromotion {
    Write-PipelineLog "Starting environment promotion" "HEADER"
    
    if (-not $PromoteFrom) {
        throw "PromoteFrom parameter is required for promotion"
    }
    
    try {
        & "$ScriptDir\promote-environment.ps1" -FromEnvironment $PromoteFrom -ToEnvironment $Environment -AutoApprove:$AutoApprove -SkipTests:$SkipTests -NotificationWebhook $NotificationWebhook
        Write-PipelineLog "Environment promotion completed successfully" "SUCCESS"
        return $true
    }
    catch {
        Write-PipelineLog "Environment promotion failed: $($_.Exception.Message)" "ERROR"
        throw
    }
}

function Generate-PipelineReport {
    param([object]$PlanResult, [bool]$ApplySuccess, [bool]$TestsSuccess, [bool]$DriftCheckSuccess)
    
    Write-PipelineLog "Generating pipeline execution report" "INFO"
    
    $PipelineMetadata.EndTime = Get-Date
    $PipelineMetadata.Duration = $PipelineMetadata.EndTime - $PipelineMetadata.StartTime
    
    $Report = @{
        Metadata = $PipelineMetadata
        Plan = @{
            HasChanges = $PlanResult.HasChanges
            Summary = $PlanResult.Summary
            Changes = $PlanResult.Changes
        }
        Results = @{
            ApplySuccess = $ApplySuccess
            TestsSuccess = $TestsSuccess
            DriftCheckSuccess = $DriftCheckSuccess
        }
        Duration = $PipelineMetadata.Duration.TotalMinutes
    }
    
    $ReportFile = Join-Path $ReportDir "pipeline-report-$Environment-$Action-$Timestamp.json"
    $Report | ConvertTo-Json -Depth 5 | Out-File $ReportFile -Encoding UTF8
    
    Write-PipelineLog "Pipeline report generated: $ReportFile" "SUCCESS"
    
    return $Report
}

# Main execution function
function Main {
    Write-PipelineLog "Starting CI/CD pipeline execution" "HEADER"
    Write-PipelineLog "Environment: $Environment" "INFO"
    Write-PipelineLog "Action: $Action" "INFO"
    Write-PipelineLog "Branch: $Branch" "INFO"
    Write-PipelineLog "Auto-approve: $AutoApprove" "INFO"
    Write-PipelineLog "Log file: $LogFile" "INFO"
    
    $PlanResult = $null
    $ApplySuccess = $false
    $TestsSuccess = $false
    $DriftCheckSuccess = $false
    
    try {
        # Send start notification
        Send-PipelineNotification -Title "CI/CD Pipeline Started" -Message "Starting $Action operation for $Environment environment." -Color "warning"
        
        # Validate prerequisites
        Test-PipelinePrerequisites
        
        # Initialize Terraform
        Initialize-PipelineTerraform
        
        # Execute the requested action
        switch ($Action) {
            "validate" {
                Invoke-TerraformValidation
                Write-PipelineLog "Validation completed successfully" "SUCCESS"
            }
            "plan" {
                Invoke-TerraformValidation
                $PlanResult = Invoke-PipelinePlan
                
                if ($EnableDriftDetection) {
                    $DriftCheckSuccess = Invoke-DriftDetection
                }
                
                Write-PipelineLog "Plan completed successfully" "SUCCESS"
            }
            "apply" {
                Invoke-TerraformValidation
                $PlanResult = Invoke-PipelinePlan
                
                if ($PlanResult.HasChanges) {
                    $ApplySuccess = Invoke-PipelineApply -PlanResult $PlanResult
                    
                    if ($ApplySuccess) {
                        $TestsSuccess = Invoke-PipelineTests
                        
                        if ($EnableDriftDetection) {
                            $DriftCheckSuccess = Invoke-DriftDetection
                        }
                    }
                }
                else {
                    Write-PipelineLog "No changes to apply" "INFO"
                    $ApplySuccess = $true
                    $TestsSuccess = $true
                    $DriftCheckSuccess = $true
                }
                
                Write-PipelineLog "Apply completed successfully" "SUCCESS"
            }
            "destroy" {
                Write-PipelineLog "Destroy action not implemented in CI/CD pipeline" "WARNING"
                Write-PipelineLog "Use the standard deploy.ps1 script for destroy operations" "INFO"
            }
            "promote" {
                $ApplySuccess = Invoke-EnvironmentPromotion
                $TestsSuccess = $true  # Tests are run within promotion script
                $DriftCheckSuccess = $true
                
                Write-PipelineLog "Promotion completed successfully" "SUCCESS"
            }
            default {
                throw "Unknown action: $Action"
            }
        }
        
        # Generate pipeline report
        if ($PlanResult) {
            $Report = Generate-PipelineReport -PlanResult $PlanResult -ApplySuccess $ApplySuccess -TestsSuccess $TestsSuccess -DriftCheckSuccess $DriftCheckSuccess
        }
        
        # Send success notification
        $NotificationFields = @{}
        if ($PlanResult -and $PlanResult.HasChanges) {
            $NotificationFields["Changes"] = "Add: $($PlanResult.Summary.Add), Change: $($PlanResult.Summary.Change), Destroy: $($PlanResult.Summary.Destroy)"
        }
        if ($Action -eq "apply") {
            $NotificationFields["Tests"] = if ($TestsSuccess) { "PASSED" } else { "FAILED" }
        }
        
        Send-PipelineNotification -Title "CI/CD Pipeline Completed" -Message "$Action operation completed successfully for $Environment environment." -Color "good" -Fields $NotificationFields
        
        Write-PipelineLog "CI/CD pipeline execution completed successfully" "SUCCESS"
    }
    catch {
        Write-PipelineLog "CI/CD pipeline execution failed: $($_.Exception.Message)" "ERROR"
        
        # Send failure notification
        Send-PipelineNotification -Title "CI/CD Pipeline Failed" -Message "$Action operation failed for $Environment environment: $($_.Exception.Message)" -Color "danger"
        
        Write-PipelineLog "Check log file for details: $LogFile" "ERROR"
        exit 1
    }
}

# Run main function
Main