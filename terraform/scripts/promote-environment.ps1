# Environment Promotion Workflow Script
# Usage: .\promote-environment.ps1 -FromEnvironment <env> -ToEnvironment <env> [-AutoApprove] [-SkipTests]
# Example: .\promote-environment.ps1 -FromEnvironment dev -ToEnvironment staging
# Example: .\promote-environment.ps1 -FromEnvironment staging -ToEnvironment prod -AutoApprove

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$FromEnvironment,
    
    [Parameter(Mandatory=$true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$ToEnvironment,
    
    [switch]$AutoApprove,
    [switch]$SkipTests,
    [switch]$SkipDriftCheck,
    [switch]$Force,
    [string]$NotificationWebhook = ""
)

# Configuration
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$TerraformDir = Split-Path -Parent $ScriptDir
$LogDir = Join-Path $TerraformDir "logs"
$TestDir = Join-Path $TerraformDir "tests"

# Ensure directories exist
@($LogDir, $TestDir) | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -ItemType Directory -Path $_ -Force | Out-Null
    }
}

# Logging configuration
$Timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$LogFile = Join-Path $LogDir "promotion-$FromEnvironment-to-$ToEnvironment-$Timestamp.log"

# Valid promotion paths
$ValidPromotions = @{
    "dev" = @("staging")
    "staging" = @("prod")
}

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $LogMessage = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] [$Level] $Message"
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

function Send-Notification {
    param([string]$Title, [string]$Message, [string]$Color = "warning")
    
    if ($NotificationWebhook) {
        try {
            $Payload = @{
                attachments = @(
                    @{
                        color = $Color
                        title = $Title
                        text = $Message
                        fields = @(
                            @{
                                title = "From Environment"
                                value = $FromEnvironment
                                short = $true
                            },
                            @{
                                title = "To Environment"
                                value = $ToEnvironment
                                short = $true
                            },
                            @{
                                title = "Timestamp"
                                value = (Get-Date -Format 'yyyy-MM-dd HH:mm:ss UTC')
                                short = $true
                            }
                        )
                    }
                )
            } | ConvertTo-Json -Depth 4
            
            Invoke-RestMethod -Uri $NotificationWebhook -Method Post -Body $Payload -ContentType "application/json"
            Write-Log "Notification sent successfully" "SUCCESS"
        }
        catch {
            Write-Log "Failed to send notification: $($_.Exception.Message)" "ERROR"
        }
    }
}

function Test-PromotionPath {
    Write-Log "Validating promotion path: $FromEnvironment -> $ToEnvironment" "INFO"
    
    # Check if source and target are the same
    if ($FromEnvironment -eq $ToEnvironment) {
        throw "Source and target environments cannot be the same"
    }
    
    # Check if promotion path is valid
    if (-not $ValidPromotions[$FromEnvironment] -or $ToEnvironment -notin $ValidPromotions[$FromEnvironment]) {
        throw "Invalid promotion path: $FromEnvironment -> $ToEnvironment. Valid paths: dev->staging, staging->prod"
    }
    
    Write-Log "Promotion path is valid" "SUCCESS"
}

function Test-Prerequisites {
    Write-Log "Validating prerequisites for environment promotion" "INFO"
    
    # Check Terraform
    try {
        $TerraformVersion = terraform version -json | ConvertFrom-Json
        Write-Log "Terraform version: $($TerraformVersion.terraform_version)" "SUCCESS"
    }
    catch {
        throw "Terraform is not installed or not in PATH"
    }
    
    # Check AWS CLI
    try {
        $CallerIdentity = aws sts get-caller-identity | ConvertFrom-Json
        Write-Log "AWS Account: $($CallerIdentity.Account)" "SUCCESS"
    }
    catch {
        throw "AWS CLI is not configured"
    }
    
    # Check required files for both environments
    foreach ($Env in @($FromEnvironment, $ToEnvironment)) {
        $BackendConfig = Join-Path $TerraformDir "backend-configs\$Env.hcl"
        $VarFile = Join-Path $TerraformDir "environments\$Env.tfvars"
        
        if (-not (Test-Path $BackendConfig)) {
            throw "Backend config file not found: $BackendConfig"
        }
        
        if (-not (Test-Path $VarFile)) {
            throw "Variables file not found: $VarFile"
        }
    }
    
    Write-Log "Prerequisites validation completed" "SUCCESS"
}

function Test-SourceEnvironmentState {
    Write-Log "Validating source environment state: $FromEnvironment" "INFO"
    
    Set-Location $TerraformDir
    
    # Initialize and select source workspace
    $BackendConfig = Join-Path $TerraformDir "backend-configs\$FromEnvironment.hcl"
    terraform init -backend-config="$BackendConfig" -upgrade > $null 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to initialize Terraform for source environment"
    }
    
    terraform workspace select $FromEnvironment > $null 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to select source workspace: $FromEnvironment"
    }
    
    # Check for pending changes in source environment
    $VarFile = Join-Path $TerraformDir "environments\$FromEnvironment.tfvars"
    terraform plan -var-file="$VarFile" -detailed-exitcode > $null 2>&1
    $PlanExitCode = $LASTEXITCODE
    
    switch ($PlanExitCode) {
        0 {
            Write-Log "Source environment is in sync with configuration" "SUCCESS"
        }
        1 {
            throw "Source environment has configuration errors"
        }
        2 {
            Write-Log "Source environment has pending changes" "WARNING"
            if (-not $Force) {
                throw "Cannot promote environment with pending changes. Use -Force to override or apply changes first."
            }
            Write-Log "Proceeding with promotion despite pending changes (Force flag enabled)" "WARNING"
        }
    }
    
    # Run drift detection if not skipped
    if (-not $SkipDriftCheck) {
        Write-Log "Running drift detection on source environment" "INFO"
        
        try {
            & "$ScriptDir\drift-detection.ps1" -Environment $FromEnvironment
            Write-Log "No drift detected in source environment" "SUCCESS"
        }
        catch {
            Write-Log "Drift detected in source environment: $($_.Exception.Message)" "WARNING"
            if (-not $Force) {
                throw "Cannot promote environment with configuration drift. Use -Force to override or remediate drift first."
            }
            Write-Log "Proceeding with promotion despite drift (Force flag enabled)" "WARNING"
        }
    }
    
    Write-Log "Source environment validation completed" "SUCCESS"
}

function Get-EnvironmentDifferences {
    Write-Log "Analyzing differences between environments" "INFO"
    
    Set-Location $TerraformDir
    
    # Get current state of both environments
    $SourceState = @{}
    $TargetState = @{}
    
    # Get source environment state
    terraform workspace select $FromEnvironment > $null 2>&1
    try {
        $SourceStateJson = terraform show -json | ConvertFrom-Json
        if ($SourceStateJson.values -and $SourceStateJson.values.root_module) {
            $SourceState = $SourceStateJson.values.root_module.resources
        }
    }
    catch {
        Write-Log "Could not retrieve source environment state" "WARNING"
    }
    
    # Get target environment state
    $BackendConfig = Join-Path $TerraformDir "backend-configs\$ToEnvironment.hcl"
    terraform init -backend-config="$BackendConfig" -upgrade > $null 2>&1
    terraform workspace select $ToEnvironment > $null 2>&1
    
    try {
        $TargetStateJson = terraform show -json | ConvertFrom-Json
        if ($TargetStateJson.values -and $TargetStateJson.values.root_module) {
            $TargetState = $TargetStateJson.values.root_module.resources
        }
    }
    catch {
        Write-Log "Could not retrieve target environment state (may be empty)" "INFO"
    }
    
    # Compare resource counts
    $SourceResourceCount = if ($SourceState) { $SourceState.Count } else { 0 }
    $TargetResourceCount = if ($TargetState) { $TargetState.Count } else { 0 }
    
    Write-Log "Source environment resources: $SourceResourceCount" "INFO"
    Write-Log "Target environment resources: $TargetResourceCount" "INFO"
    
    return @{
        SourceResourceCount = $SourceResourceCount
        TargetResourceCount = $TargetResourceCount
        SourceState = $SourceState
        TargetState = $TargetState
    }
}

function Invoke-PromotionPlan {
    Write-Log "Generating promotion plan for target environment: $ToEnvironment" "INFO"
    
    Set-Location $TerraformDir
    
    # Ensure we're in the target workspace
    terraform workspace select $ToEnvironment > $null 2>&1
    
    $VarFile = Join-Path $TerraformDir "environments\$ToEnvironment.tfvars"
    $PlanFile = Join-Path $TerraformDir "promotion-$FromEnvironment-to-$ToEnvironment-$Timestamp.tfplan"
    
    # Generate plan
    terraform plan -var-file="$VarFile" -out="$PlanFile" -detailed-exitcode
    $PlanExitCode = $LASTEXITCODE
    
    # Generate JSON output for analysis
    $PlanJsonFile = "$PlanFile.json"
    terraform show -json "$PlanFile" > "$PlanJsonFile" 2>$null
    
    $PlanResult = @{
        ExitCode = $PlanExitCode
        PlanFile = $PlanFile
        JsonFile = $PlanJsonFile
        HasChanges = $false
        Changes = @()
        Summary = @{
            Add = 0
            Change = 0
            Destroy = 0
        }
    }
    
    switch ($PlanExitCode) {
        0 {
            Write-Log "No changes required for promotion" "SUCCESS"
            $PlanResult.HasChanges = $false
        }
        1 {
            throw "Promotion plan generation failed"
        }
        2 {
            Write-Log "Changes detected for promotion" "INFO"
            $PlanResult.HasChanges = $true
            
            # Parse plan details
            if (Test-Path $PlanJsonFile) {
                try {
                    $PlanJson = Get-Content $PlanJsonFile | ConvertFrom-Json
                    
                    if ($PlanJson.resource_changes) {
                        foreach ($Change in $PlanJson.resource_changes) {
                            $ChangeDetail = @{
                                Address = $Change.address
                                Type = $Change.type
                                Actions = $Change.change.actions
                            }
                            
                            $PlanResult.Changes += $ChangeDetail
                            
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
                    
                    Write-Log "Promotion Summary - Add: $($PlanResult.Summary.Add), Change: $($PlanResult.Summary.Change), Destroy: $($PlanResult.Summary.Destroy)" "INFO"
                }
                catch {
                    Write-Log "Failed to parse promotion plan JSON: $($_.Exception.Message)" "ERROR"
                }
            }
        }
    }
    
    return $PlanResult
}

function Show-PromotionPlan {
    param([object]$PlanResult)
    
    if (-not $PlanResult.HasChanges) {
        Write-Log "No changes will be made during promotion" "INFO"
        return
    }
    
    Write-Log "Promotion Plan Details:" "HEADER"
    Write-Log "======================" "HEADER"
    
    # Show plan summary
    terraform show $PlanResult.PlanFile
    
    Write-Log "======================" "HEADER"
    Write-Log "Summary of changes:" "INFO"
    Write-Log "  Resources to add: $($PlanResult.Summary.Add)" "INFO"
    Write-Log "  Resources to change: $($PlanResult.Summary.Change)" "INFO"
    Write-Log "  Resources to destroy: $($PlanResult.Summary.Destroy)" "INFO"
}

function Confirm-Promotion {
    param([object]$PlanResult)
    
    if (-not $PlanResult.HasChanges) {
        return $true
    }
    
    if ($AutoApprove) {
        Write-Log "Auto-approve enabled, proceeding with promotion" "INFO"
        return $true
    }
    
    # Special confirmation for production
    if ($ToEnvironment -eq "prod") {
        Write-Log "PRODUCTION PROMOTION CONFIRMATION" "WARNING"
        Write-Log "You are about to promote changes to PRODUCTION environment!" "WARNING"
        Write-Log "This will affect the production infrastructure." "WARNING"
        Write-Host ""
        
        $Confirm = Read-Host "Type 'PROMOTE TO PRODUCTION' to confirm (case-sensitive)"
        if ($Confirm -ne "PROMOTE TO PRODUCTION") {
            Write-Log "Production promotion cancelled by user" "INFO"
            return $false
        }
    }
    else {
        Write-Host ""
        $Confirm = Read-Host "Proceed with promotion? (y/N)"
        if ($Confirm -ne "y" -and $Confirm -ne "Y") {
            Write-Log "Promotion cancelled by user" "INFO"
            return $false
        }
    }
    
    return $true
}

function Invoke-PromotionApply {
    param([object]$PlanResult)
    
    if (-not $PlanResult.HasChanges) {
        Write-Log "No changes to apply for promotion" "INFO"
        return $true
    }
    
    Write-Log "Applying promotion changes to environment: $ToEnvironment" "INFO"
    
    Set-Location $TerraformDir
    
    # Ensure we're in the target workspace
    terraform workspace select $ToEnvironment > $null 2>&1
    
    # Apply the plan
    terraform apply -auto-approve $PlanResult.PlanFile
    
    if ($LASTEXITCODE -eq 0) {
        Write-Log "Promotion apply completed successfully" "SUCCESS"
        
        # Clean up plan files
        Remove-Item $PlanResult.PlanFile -Force -ErrorAction SilentlyContinue
        Remove-Item $PlanResult.JsonFile -Force -ErrorAction SilentlyContinue
        
        return $true
    }
    else {
        Write-Log "Promotion apply failed" "ERROR"
        return $false
    }
}

function Invoke-PostPromotionTests {
    Write-Log "Running post-promotion tests for environment: $ToEnvironment" "INFO"
    
    if ($SkipTests) {
        Write-Log "Skipping post-promotion tests (--SkipTests specified)" "INFO"
        return $true
    }
    
    # Check if test files exist
    $TestFiles = Get-ChildItem -Path $TestDir -Filter "*.test.ps1" -ErrorAction SilentlyContinue
    
    if (-not $TestFiles) {
        Write-Log "No test files found, skipping post-promotion tests" "WARNING"
        return $true
    }
    
    $TestResults = @()
    $AllTestsPassed = $true
    
    foreach ($TestFile in $TestFiles) {
        Write-Log "Running post-promotion test: $($TestFile.Name)" "INFO"
        
        try {
            $TestResult = & $TestFile.FullName -Environment $ToEnvironment
            $TestResults += @{
                Test = $TestFile.Name
                Result = "PASS"
                Details = $TestResult
            }
            Write-Log "Test passed: $($TestFile.Name)" "SUCCESS"
        }
        catch {
            $TestResults += @{
                Test = $TestFile.Name
                Result = "FAIL"
                Details = $_.Exception.Message
            }
            Write-Log "Test failed: $($TestFile.Name) - $($_.Exception.Message)" "ERROR"
            $AllTestsPassed = $false
        }
    }
    
    $PassedTests = ($TestResults | Where-Object { $_.Result -eq "PASS" }).Count
    $FailedTests = ($TestResults | Where-Object { $_.Result -eq "FAIL" }).Count
    
    Write-Log "Post-promotion test summary: $PassedTests passed, $FailedTests failed" "INFO"
    
    if (-not $AllTestsPassed) {
        Write-Log "Some post-promotion tests failed. Please review the target environment." "WARNING"
    }
    
    return $AllTestsPassed
}

function Generate-PromotionReport {
    param([object]$PlanResult, [bool]$PromotionSuccess, [bool]$TestsSuccess)
    
    $Report = @{
        Timestamp = (Get-Date -Format 'yyyy-MM-dd HH:mm:ss UTC')
        FromEnvironment = $FromEnvironment
        ToEnvironment = $ToEnvironment
        PromotionSuccess = $PromotionSuccess
        TestsSuccess = $TestsSuccess
        HasChanges = $PlanResult.HasChanges
        Summary = $PlanResult.Summary
        Changes = $PlanResult.Changes
        LogFile = $LogFile
    }
    
    $ReportFile = Join-Path $LogDir "promotion-report-$FromEnvironment-to-$ToEnvironment-$Timestamp.json"
    $Report | ConvertTo-Json -Depth 5 | Out-File $ReportFile -Encoding UTF8
    
    Write-Log "Promotion report generated: $ReportFile" "SUCCESS"
}

# Main execution function
function Main {
    Write-Log "Starting environment promotion workflow" "HEADER"
    Write-Log "From: $FromEnvironment -> To: $ToEnvironment" "INFO"
    Write-Log "Auto-approve: $AutoApprove" "INFO"
    Write-Log "Skip tests: $SkipTests" "INFO"
    Write-Log "Log file: $LogFile" "INFO"
    
    $PromotionSuccess = $false
    $TestsSuccess = $false
    $PlanResult = $null
    
    try {
        # Send start notification
        Send-Notification -Title "Environment Promotion Started" -Message "Starting promotion from $FromEnvironment to $ToEnvironment environment." -Color "warning"
        
        # Validate promotion path
        Test-PromotionPath
        
        # Validate prerequisites
        Test-Prerequisites
        
        # Validate source environment
        Test-SourceEnvironmentState
        
        # Analyze environment differences
        $Differences = Get-EnvironmentDifferences
        Write-Log "Environment analysis completed" "SUCCESS"
        
        # Generate promotion plan
        $PlanResult = Invoke-PromotionPlan
        
        # Show promotion plan
        Show-PromotionPlan -PlanResult $PlanResult
        
        # Confirm promotion
        $ConfirmPromotion = Confirm-Promotion -PlanResult $PlanResult
        
        if (-not $ConfirmPromotion) {
            Write-Log "Promotion cancelled" "INFO"
            Send-Notification -Title "Environment Promotion Cancelled" -Message "Promotion from $FromEnvironment to $ToEnvironment was cancelled." -Color "warning"
            return
        }
        
        # Apply promotion
        $PromotionSuccess = Invoke-PromotionApply -PlanResult $PlanResult
        
        if ($PromotionSuccess) {
            Write-Log "Environment promotion completed successfully" "SUCCESS"
            
            # Run post-promotion tests
            $TestsSuccess = Invoke-PostPromotionTests
            
            # Send success notification
            $NotificationMessage = "Environment promotion completed successfully.`n" +
                                 "From: $FromEnvironment -> To: $ToEnvironment`n" +
                                 "Changes: Add: $($PlanResult.Summary.Add), Change: $($PlanResult.Summary.Change), Destroy: $($PlanResult.Summary.Destroy)`n" +
                                 "Tests: $(if ($TestsSuccess) { 'PASSED' } else { 'FAILED' })"
            
            Send-Notification -Title "Environment Promotion Completed" -Message $NotificationMessage -Color "good"
        }
        else {
            throw "Promotion apply failed"
        }
        
    }
    catch {
        Write-Log "Environment promotion failed: $($_.Exception.Message)" "ERROR"
        
        # Send failure notification
        Send-Notification -Title "Environment Promotion Failed" -Message "Promotion from $FromEnvironment to $ToEnvironment failed: $($_.Exception.Message)" -Color "danger"
        
        exit 1
    }
    finally {
        # Generate promotion report
        if ($PlanResult) {
            Generate-PromotionReport -PlanResult $PlanResult -PromotionSuccess $PromotionSuccess -TestsSuccess $TestsSuccess
        }
    }
}

# Execute main function
Main