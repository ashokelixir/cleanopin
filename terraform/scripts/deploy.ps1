# PowerShell Terraform deployment script
# Usage: .\deploy.ps1 -Environment <env> -Action <action>
# Example: .\deploy.ps1 -Environment dev -Action plan
# Example: .\deploy.ps1 -Environment prod -Action apply

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment,
    
    [Parameter(Mandatory=$true)]
    [ValidateSet("init", "plan", "apply", "destroy", "validate", "fmt", "show")]
    [string]$Action
)

# Configuration
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$TerraformDir = Split-Path -Parent $ScriptDir

# Function to write colored output
function Write-Status {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

function Write-Header {
    param([string]$Message)
    Write-Host "[DEPLOY] $Message" -ForegroundColor Blue
}

# Function to show usage
function Show-Usage {
    Write-Host "Usage: .\deploy.ps1 -Environment <env> -Action <action>"
    Write-Host ""
    Write-Host "Environments: dev, staging, prod"
    Write-Host "Actions: init, plan, apply, destroy, validate, fmt, show"
    Write-Host ""
    Write-Host "Examples:"
    Write-Host "  .\deploy.ps1 -Environment dev -Action init     # Initialize Terraform for dev environment"
    Write-Host "  .\deploy.ps1 -Environment dev -Action plan     # Plan changes for dev environment"
    Write-Host "  .\deploy.ps1 -Environment dev -Action apply    # Apply changes for dev environment"
    Write-Host "  .\deploy.ps1 -Environment prod -Action destroy # Destroy prod environment (use with caution!)"
    Write-Host ""
}

# Function to validate inputs
function Test-Prerequisites {
    param([string]$Environment)
    
    # Check if required files exist
    $BackendConfig = Join-Path $TerraformDir "backend-configs\$Environment.hcl"
    $VarFile = Join-Path $TerraformDir "environments\$Environment.tfvars"
    
    if (-not (Test-Path $BackendConfig)) {
        Write-Error "Backend config file not found: $BackendConfig"
        exit 1
    }
    
    if (-not (Test-Path $VarFile)) {
        Write-Error "Variables file not found: $VarFile"
        exit 1
    }
    
    # Check if AWS CLI is configured
    try {
        aws sts get-caller-identity | Out-Null
    }
    catch {
        Write-Error "AWS CLI is not configured. Please run 'aws configure' first."
        exit 1
    }
}

# Function to initialize Terraform
function Initialize-Terraform {
    param([string]$Environment)
    
    $BackendConfig = Join-Path $TerraformDir "backend-configs\$Environment.hcl"
    
    Write-Header "Initializing Terraform for environment: $Environment"
    
    Set-Location $TerraformDir
    
    # Initialize with backend config
    terraform init -backend-config="$BackendConfig" -reconfigure
    
    # Create or select workspace
    $WorkspaceList = terraform workspace list
    if ($WorkspaceList -match $Environment) {
        Write-Status "Selecting existing workspace: $Environment"
        terraform workspace select $Environment
    }
    else {
        Write-Status "Creating new workspace: $Environment"
        terraform workspace new $Environment
    }
    
    Write-Status "Terraform initialization completed for environment: $Environment"
}

# Function to run terraform plan
function Invoke-TerraformPlan {
    param([string]$Environment)
    
    $VarFile = Join-Path $TerraformDir "environments\$Environment.tfvars"
    
    Write-Header "Planning Terraform changes for environment: $Environment"
    
    Set-Location $TerraformDir
    
    # Ensure we're in the correct workspace
    terraform workspace select $Environment
    
    # Run plan
    terraform plan -var-file="$VarFile" -out="$Environment.tfplan"
    
    Write-Status "Terraform plan completed for environment: $Environment"
    Write-Status "Plan file saved as: $Environment.tfplan"
}

# Function to run terraform apply
function Invoke-TerraformApply {
    param([string]$Environment)
    
    $VarFile = Join-Path $TerraformDir "environments\$Environment.tfvars"
    $PlanFile = Join-Path $TerraformDir "$Environment.tfplan"
    
    Write-Header "Applying Terraform changes for environment: $Environment"
    
    Set-Location $TerraformDir
    
    # Ensure we're in the correct workspace
    terraform workspace select $Environment
    
    # Check if plan file exists
    if (Test-Path $PlanFile) {
        Write-Status "Applying from plan file: $Environment.tfplan"
        terraform apply "$Environment.tfplan"
        Remove-Item $PlanFile -Force
    }
    else {
        Write-Warning "No plan file found. Running apply with auto-approve disabled."
        terraform apply -var-file="$VarFile"
    }
    
    Write-Status "Terraform apply completed for environment: $Environment"
}

# Function to run terraform destroy
function Invoke-TerraformDestroy {
    param([string]$Environment)
    
    $VarFile = Join-Path $TerraformDir "environments\$Environment.tfvars"
    
    Write-Header "Destroying Terraform resources for environment: $Environment"
    Write-Warning "This will destroy all resources in the $Environment environment!"
    
    # Extra confirmation for production
    if ($Environment -eq "prod") {
        Write-Warning "You are about to destroy PRODUCTION resources!"
        $Confirm = Read-Host "Type 'yes' to confirm destruction of PRODUCTION environment"
        if ($Confirm -ne "yes") {
            Write-Status "Destruction cancelled."
            exit 0
        }
    }
    
    Set-Location $TerraformDir
    
    # Ensure we're in the correct workspace
    terraform workspace select $Environment
    
    # Run destroy
    terraform destroy -var-file="$VarFile"
    
    Write-Status "Terraform destroy completed for environment: $Environment"
}

# Function to validate Terraform configuration
function Test-TerraformConfiguration {
    Write-Header "Validating Terraform configuration"
    
    Set-Location $TerraformDir
    
    terraform validate
    
    Write-Status "Terraform validation completed"
}

# Function to format Terraform files
function Format-TerraformFiles {
    Write-Header "Formatting Terraform files"
    
    Set-Location $TerraformDir
    
    terraform fmt -recursive
    
    Write-Status "Terraform formatting completed"
}

# Function to show Terraform state
function Show-TerraformState {
    param([string]$Environment)
    
    Write-Header "Showing Terraform state for environment: $Environment"
    
    Set-Location $TerraformDir
    
    # Ensure we're in the correct workspace
    terraform workspace select $Environment
    
    terraform show
}

# Main execution
function Main {
    # Validate prerequisites
    Test-Prerequisites -Environment $Environment
    
    # Execute the requested action
    switch ($Action) {
        "init" {
            Initialize-Terraform -Environment $Environment
        }
        "plan" {
            Invoke-TerraformPlan -Environment $Environment
        }
        "apply" {
            Invoke-TerraformApply -Environment $Environment
        }
        "destroy" {
            Invoke-TerraformDestroy -Environment $Environment
        }
        "validate" {
            Test-TerraformConfiguration
        }
        "fmt" {
            Format-TerraformFiles
        }
        "show" {
            Show-TerraformState -Environment $Environment
        }
        default {
            Write-Error "Unknown action: $Action"
            Show-Usage
            exit 1
        }
    }
}

# Run main function
try {
    Main
}
catch {
    Write-Error "Script execution failed: $($_.Exception.Message)"
    exit 1
}