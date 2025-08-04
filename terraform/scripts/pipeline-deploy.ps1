# Enhanced Terraform deployment pipeline script with state locking and drift detection
# Usage: .\pipeline-deploy.ps1 -Environment <env> -Action <action> [-AutoApprove] [-DriftCheck] [-SkipTests]
# Example: .\pipeline-deploy.ps1 -Environment dev -Action plan
# Example: .\pipeline-deploy.ps1 -Environment prod -Action apply -DriftCheck

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment,
    
    [Parameter(Mandatory=$true)]
    [ValidateSet("init", "plan", "apply", "destroy", "validate", "fmt", "show", "drift-check", "promote")]
    [string]$Action,
    
    [switch]$AutoApprove,
    [switch]$DriftCheck,
    [switch]$SkipTests,
    [switch]$Force,
    [string]$PromoteFrom = ""
)

# Configuration
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$TerraformDir = Split-Path -Parent $ScriptDir
$LogDir = Join-Path $TerraformDir "logs"
$PlanDir = Join-Path $TerraformDir "plans"
$TestDir = Join-Path $TerraformDir "tests"

# Ensure directories exist
@($LogDir, $PlanDir, $TestDir) | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -ItemType Directory -Path $_ -Force | Out-Null
    }
}

# Logging configuration
$LogFile = Join-Path $LogDir "deploy-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
$ErrorActionPreference = "Stop"

# Function to write colored output and log
function Write-Status {
    param([string]$Message)
    $LogMessage = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] [INFO] $Message"
    Write-Host $LogMessage -ForegroundColor Green
    Add-Content -Path $LogFile -Value $LogMessage
}

function Write-Warning {
    param([string]$Message)
    $LogMessage = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] [WARNING] $Message"
    Write-Host $LogMessage -ForegroundColor Yellow
    Add-Content -Path $LogFile -Value $LogMessage
}

function Write-Error {
    param([string]$Message)
    $LogMessage = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] [ERROR] $Message"
    Write-Host $LogMessage -ForegroundColor Red
    Add-Content -Path $LogFile -Value $LogMessage
}

function Write-Header {
    param([string]$Message)
    $LogMessage = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] [DEPLOY] $Message"
    Write-Host $LogMessage -ForegroundColor Blue
    Add-Content -Path $LogFile -Value $LogMessage
}

# Function to validate prerequisites
function Test-Prerequisites {
    param([string]$Environment)
    
    Write-Status "Validating prerequisites for environment: $Environment"
    
    # Check if required files exist
    $BackendConfig = Join-Path $TerraformDir "backend-configs\$Environment.hcl"
    $VarFile = Join-Path $TerraformDir "environments\$Environment.tfvars"
    
    if (-not (Test-Path $BackendConfig)) {
        Write-Error "Backend config file not found: $BackendConfig"
        throw "Missing backend configuration"
    }
    
    if (-not (Test-Path $VarFile)) {
        Write-Error "Variables file not found: $VarFile"
        throw "Missing variables file"
    }
    
    # Check if AWS CLI is configured
    try {
        $CallerIdentity = aws sts get-caller-identity | ConvertFrom-Json
        Write-Status "AWS Account: $($CallerIdentity.Account), User: $($CallerIdentity.Arn)"
    }
    catch {
        Write-Error "AWS CLI is not configured. Please run 'aws configure' first."
        throw "AWS CLI not configured"
    }
    
    # Check Terraform version
    try {
        $TerraformVersion = terraform version -json | ConvertFrom-Json
        Write-Status "Terraform version: $($TerraformVersion.terraform_version)"
    }
    catch {
        Write-Error "Terraform is not installed or not in PATH"
        throw "Terraform not available"
    }
    
    # Validate backend state bucket exists
    $BackendConfigContent = Get-Content $BackendConfig | Where-Object { $_ -match 'bucket\s*=' }
    if ($BackendConfigContent) {
        $BucketName = ($BackendConfigContent -split '=')[1].Trim().Trim('"')
        try {
            aws s3api head-bucket --bucket $BucketName | Out-Null
            Write-Status "Backend S3 bucket verified: $BucketName"
        }
        catch {
            Write-Error "Backend S3 bucket not accessible: $BucketName"
            throw "Backend bucket not accessible"
        }
    }
    
    Write-Status "Prerequisites validation completed successfully"
}

# Function to initialize Terraform with enhanced state locking
function Initialize-Terraform {
    param([string]$Environment)
    
    $BackendConfig = Join-Path $TerraformDir "backend-configs\$Environment.hcl"
    
    Write-Header "Initializing Terraform for environment: $Environment"
    
    Set-Location $TerraformDir
    
    # Clean up any existing .terraform directory for fresh init
    if (Test-Path ".terraform") {
        Write-Status "Cleaning up existing .terraform directory"
        Remove-Item ".terraform" -Recurse -Force
    }
    
    # Initialize with backend config and upgrade providers
    Write-Status "Initializing Terraform backend with state locking"
    terraform init -backend-config="$BackendConfig" -upgrade -reconfigure
    
    if ($LASTEXITCODE -ne 0) {
        throw "Terraform initialization failed"
    }
    
    # Create or select workspace
    $WorkspaceList = terraform workspace list
    if ($WorkspaceList -match "\s$Environment\s") {
        Write-Status "Selecting existing workspace: $Environment"
        terraform workspace select $Environment
    }
    else {
        Write-Status "Creating new workspace: $Environment"
        terraform workspace new $Environment
    }
    
    if ($LASTEXITCODE -ne 0) {
        throw "Workspace operation failed"
    }
    
    Write-Status "Terraform initialization completed for environment: $Environment"
}

# Function to run terraform plan with enhanced output
function Invoke-TerraformPlan {
    param([string]$Environment)
    
    $VarFile = Join-Path $TerraformDir "environments\$Environment.tfvars"
    $PlanFile = Join-Path $PlanDir "$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss').tfplan"
    $PlanJsonFile = "$PlanFile.json"
    
    Write-Header "Planning Terraform changes for environment: $Environment"
    
    Set-Location $TerraformDir
    
    # Ensure we're in the correct workspace
    terraform workspace select $Environment
    
    # Run plan with detailed output
    Write-Status "Generating execution plan..."
    terraform plan -var-file="$VarFile" -out="$PlanFile" -detailed-exitcode
    $PlanExitCode = $LASTEXITCODE
    
    # Generate JSON output for analysis
    terraform show -json "$PlanFile" > "$PlanJsonFile"
    
    # Analyze plan results
    switch ($PlanExitCode) {
        0 {
            Write-Status "No changes detected in plan"
        }
        1 {
            Write-Error "Terraform plan failed"
            throw "Plan generation failed"
        }
        2 {
            Write-Status "Changes detected in plan"
            
            # Parse and display plan summary
            $PlanJson = Get-Content "$PlanJsonFile" | ConvertFrom-Json
            $Changes = $PlanJson.planned_changes
            
            if ($Changes) {
                Write-Status "Plan Summary:"
                Write-Status "  Resources to add: $($Changes.add)"
                Write-Status "  Resources to change: $($Changes.change)"
                Write-Status "  Resources to destroy: $($Changes.destroy)"
            }
        }
    }
    
    Write-Status "Terraform plan completed for environment: $Environment"
    Write-Status "Plan file saved as: $PlanFile"
    
    return @{
        PlanFile = $PlanFile
        JsonFile = $PlanJsonFile
        ExitCode = $PlanExitCode
    }
}

# Function to run terraform apply with safety checks
function Invoke-TerraformApply {
    param([string]$Environment, [string]$PlanFile = "")
    
    $VarFile = Join-Path $TerraformDir "environments\$Environment.tfvars"
    
    Write-Header "Applying Terraform changes for environment: $Environment"
    
    # Production safety check
    if ($Environment -eq "prod" -and -not $AutoApprove -and -not $Force) {
        Write-Warning "You are about to apply changes to PRODUCTION environment!"
        $Confirm = Read-Host "Type 'APPLY' to confirm (case-sensitive)"
        if ($Confirm -ne "APPLY") {
            Write-Status "Apply operation cancelled."
            return
        }
    }
    
    Set-Location $TerraformDir
    
    # Ensure we're in the correct workspace
    terraform workspace select $Environment
    
    # Apply changes
    if ($PlanFile -and (Test-Path $PlanFile)) {
        Write-Status "Applying from plan file: $PlanFile"
        if ($AutoApprove) {
            terraform apply -auto-approve "$PlanFile"
        }
        else {
            terraform apply "$PlanFile"
        }
    }
    else {
        Write-Status "No plan file provided, running direct apply"
        if ($AutoApprove) {
            terraform apply -var-file="$VarFile" -auto-approve
        }
        else {
            terraform apply -var-file="$VarFile"
        }
    }
    
    if ($LASTEXITCODE -ne 0) {
        throw "Terraform apply failed"
    }
    
    Write-Status "Terraform apply completed for environment: $Environment"
    
    # Clean up plan file after successful apply
    if ($PlanFile -and (Test-Path $PlanFile)) {
        Remove-Item $PlanFile -Force
        if (Test-Path "$PlanFile.json") {
            Remove-Item "$PlanFile.json" -Force
        }
    }
}

# Function to check for configuration drift
function Test-TerraformDrift {
    param([string]$Environment)
    
    Write-Header "Checking for configuration drift in environment: $Environment"
    
    Set-Location $TerraformDir
    
    # Ensure we're in the correct workspace
    terraform workspace select $Environment
    
    # Run plan to detect drift
    $VarFile = Join-Path $TerraformDir "environments\$Environment.tfvars"
    $DriftFile = Join-Path $LogDir "drift-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
    
    terraform plan -var-file="$VarFile" -detailed-exitcode -no-color > $DriftFile 2>&1
    $DriftExitCode = $LASTEXITCODE
    
    switch ($DriftExitCode) {
        0 {
            Write-Status "No configuration drift detected"
            return $false
        }
        1 {
            Write-Error "Error checking for drift"
            throw "Drift check failed"
        }
        2 {
            Write-Warning "Configuration drift detected!"
            Write-Warning "Drift details saved to: $DriftFile"
            
            # Extract key changes from drift log
            $DriftContent = Get-Content $DriftFile
            $ChangedResources = $DriftContent | Where-Object { $_ -match "will be (created|updated|destroyed)" }
            
            if ($ChangedResources) {
                Write-Warning "Resources with drift:"
                $ChangedResources | ForEach-Object { Write-Warning "  $_" }
            }
            
            return $true
        }
    }
}

# Function to promote configuration between environments
function Invoke-EnvironmentPromotion {
    param([string]$FromEnvironment, [string]$ToEnvironment)
    
    Write-Header "Promoting configuration from $FromEnvironment to $ToEnvironment"
    
    # Validation
    if ($FromEnvironment -eq $ToEnvironment) {
        throw "Source and target environments cannot be the same"
    }
    
    # Validate promotion path
    $ValidPromotions = @{
        "dev" = @("staging")
        "staging" = @("prod")
    }
    
    if (-not $ValidPromotions[$FromEnvironment] -or $ToEnvironment -notin $ValidPromotions[$FromEnvironment]) {
        throw "Invalid promotion path: $FromEnvironment -> $ToEnvironment"
    }
    
    # Check source environment state
    Write-Status "Validating source environment: $FromEnvironment"
    Set-Location $TerraformDir
    terraform workspace select $FromEnvironment
    
    $SourceVarFile = Join-Path $TerraformDir "environments\$FromEnvironment.tfvars"
    terraform plan -var-file="$SourceVarFile" -detailed-exitcode > $null
    
    if ($LASTEXITCODE -eq 1) {
        throw "Source environment $FromEnvironment has errors"
    }
    elseif ($LASTEXITCODE -eq 2) {
        Write-Warning "Source environment $FromEnvironment has pending changes"
        if (-not $Force) {
            throw "Cannot promote environment with pending changes. Use -Force to override."
        }
    }
    
    # Generate promotion plan for target environment
    Write-Status "Generating promotion plan for target environment: $ToEnvironment"
    $PromotionPlan = Invoke-TerraformPlan -Environment $ToEnvironment
    
    if ($PromotionPlan.ExitCode -eq 2) {
        Write-Status "Promotion will make the following changes:"
        terraform show $PromotionPlan.PlanFile
        
        if (-not $AutoApprove) {
            $Confirm = Read-Host "Proceed with promotion? (y/N)"
            if ($Confirm -ne "y" -and $Confirm -ne "Y") {
                Write-Status "Promotion cancelled"
                return
            }
        }
        
        # Apply promotion
        Invoke-TerraformApply -Environment $ToEnvironment -PlanFile $PromotionPlan.PlanFile
        Write-Status "Environment promotion completed: $FromEnvironment -> $ToEnvironment"
    }
    else {
        Write-Status "No changes required for promotion"
    }
}

# Function to run infrastructure tests
function Invoke-InfrastructureTests {
    param([string]$Environment)
    
    if ($SkipTests) {
        Write-Status "Skipping infrastructure tests (--SkipTests specified)"
        return
    }
    
    Write-Header "Running infrastructure tests for environment: $Environment"
    
    # Check if test files exist
    $TestFiles = Get-ChildItem -Path $TestDir -Filter "*.test.ps1" -ErrorAction SilentlyContinue
    
    if (-not $TestFiles) {
        Write-Warning "No infrastructure test files found in $TestDir"
        return
    }
    
    $TestResults = @()
    
    foreach ($TestFile in $TestFiles) {
        Write-Status "Running test: $($TestFile.Name)"
        
        try {
            $TestResult = & $TestFile.FullName -Environment $Environment
            $TestResults += @{
                Test = $TestFile.Name
                Result = "PASS"
                Details = $TestResult
            }
            Write-Status "Test passed: $($TestFile.Name)"
        }
        catch {
            $TestResults += @{
                Test = $TestFile.Name
                Result = "FAIL"
                Details = $_.Exception.Message
            }
            Write-Error "Test failed: $($TestFile.Name) - $($_.Exception.Message)"
        }
    }
    
    # Generate test report
    $TestReport = Join-Path $LogDir "test-report-$Environment-$(Get-Date -Format 'yyyyMMdd-HHmmss').json"
    $TestResults | ConvertTo-Json -Depth 3 | Out-File $TestReport
    
    $PassedTests = ($TestResults | Where-Object { $_.Result -eq "PASS" }).Count
    $FailedTests = ($TestResults | Where-Object { $_.Result -eq "FAIL" }).Count
    
    Write-Status "Test Summary: $PassedTests passed, $FailedTests failed"
    Write-Status "Test report saved to: $TestReport"
    
    if ($FailedTests -gt 0) {
        throw "Infrastructure tests failed"
    }
}

# Main execution function
function Main {
    Write-Header "Starting Terraform deployment pipeline"
    Write-Status "Environment: $Environment"
    Write-Status "Action: $Action"
    Write-Status "Log file: $LogFile"
    
    try {
        # Validate prerequisites
        Test-Prerequisites -Environment $Environment
        
        # Execute the requested action
        switch ($Action) {
            "init" {
                Initialize-Terraform -Environment $Environment
            }
            "plan" {
                Initialize-Terraform -Environment $Environment
                $PlanResult = Invoke-TerraformPlan -Environment $Environment
                
                if ($DriftCheck -and $PlanResult.ExitCode -eq 2) {
                    Write-Warning "Configuration drift detected during plan"
                }
            }
            "apply" {
                Initialize-Terraform -Environment $Environment
                $PlanResult = Invoke-TerraformPlan -Environment $Environment
                
                if ($PlanResult.ExitCode -eq 2) {
                    Invoke-TerraformApply -Environment $Environment -PlanFile $PlanResult.PlanFile
                    Invoke-InfrastructureTests -Environment $Environment
                }
                else {
                    Write-Status "No changes to apply"
                }
            }
            "destroy" {
                # Implement destroy with safety checks (similar to existing script)
                Write-Warning "Destroy action not implemented in pipeline script"
                Write-Status "Use the standard deploy.ps1 script for destroy operations"
            }
            "validate" {
                Set-Location $TerraformDir
                terraform validate
                Write-Status "Terraform validation completed"
            }
            "fmt" {
                Set-Location $TerraformDir
                terraform fmt -recursive
                Write-Status "Terraform formatting completed"
            }
            "show" {
                Set-Location $TerraformDir
                terraform workspace select $Environment
                terraform show
            }
            "drift-check" {
                Initialize-Terraform -Environment $Environment
                $HasDrift = Test-TerraformDrift -Environment $Environment
                
                if ($HasDrift) {
                    Write-Warning "Configuration drift detected!"
                    exit 1
                }
                else {
                    Write-Status "No configuration drift detected"
                }
            }
            "promote" {
                if (-not $PromoteFrom) {
                    throw "PromoteFrom parameter is required for promotion"
                }
                Invoke-EnvironmentPromotion -FromEnvironment $PromoteFrom -ToEnvironment $Environment
            }
            default {
                throw "Unknown action: $Action"
            }
        }
        
        Write-Status "Pipeline execution completed successfully"
    }
    catch {
        Write-Error "Pipeline execution failed: $($_.Exception.Message)"
        Write-Error "Check log file for details: $LogFile"
        exit 1
    }
}

# Run main function
Main