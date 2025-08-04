# Terraform State Management and Backup Script
# Provides comprehensive state management, backup, and recovery capabilities
# Usage: .\state-management.ps1 -Environment <env> -Action <action> [options]

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment,
    
    [Parameter(Mandatory=$true)]
    [ValidateSet("backup", "restore", "list-backups", "cleanup", "validate", "lock", "unlock", "import", "remove")]
    [string]$Action,
    
    [string]$BackupName = "",
    [string]$ResourceAddress = "",
    [string]$ResourceId = "",
    [int]$RetentionDays = 30,
    [switch]$Force,
    [string]$LockId = ""
)

# Configuration
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$TerraformDir = Split-Path -Parent $ScriptDir
$BackupDir = Join-Path $TerraformDir "state-backups"
$LogDir = Join-Path $TerraformDir "logs"

# Ensure directories exist
@($BackupDir, $LogDir) | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -ItemType Directory -Path $_ -Force | Out-Null
    }
}

# Logging configuration
$Timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$LogFile = Join-Path $LogDir "state-management-$Environment-$Action-$Timestamp.log"

function Write-StateLog {
    param([string]$Message, [string]$Level = "INFO")
    $LogMessage = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] [$Level] [STATE] $Message"
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

function Test-StateManagementPrerequisites {
    Write-StateLog "Validating state management prerequisites" "INFO"
    
    # Check Terraform
    try {
        $TerraformVersion = terraform version -json | ConvertFrom-Json
        Write-StateLog "Terraform version: $($TerraformVersion.terraform_version)" "SUCCESS"
    }
    catch {
        throw "Terraform is not installed or not in PATH"
    }
    
    # Check AWS CLI
    try {
        $CallerIdentity = aws sts get-caller-identity | ConvertFrom-Json
        Write-StateLog "AWS Account: $($CallerIdentity.Account)" "SUCCESS"
    }
    catch {
        throw "AWS CLI is not configured"
    }
    
    # Check required files
    $BackendConfig = Join-Path $TerraformDir "backend-configs\$Environment.hcl"
    
    if (-not (Test-Path $BackendConfig)) {
        throw "Backend config file not found: $BackendConfig"
    }
    
    Write-StateLog "Prerequisites validation completed" "SUCCESS"
}

function Initialize-StateWorkspace {
    Write-StateLog "Initializing Terraform workspace for state management" "INFO"
    
    $BackendConfig = Join-Path $TerraformDir "backend-configs\$Environment.hcl"
    
    Set-Location $TerraformDir
    
    # Initialize Terraform
    terraform init -backend-config="$BackendConfig" -upgrade > $null 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        throw "Terraform initialization failed"
    }
    
    # Select workspace
    terraform workspace select $Environment > $null 2>&1
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to select workspace: $Environment"
    }
    
    Write-StateLog "Terraform workspace initialized successfully" "SUCCESS"
}

function Invoke-StateBackup {
    Write-StateLog "Creating Terraform state backup" "HEADER"
    
    Set-Location $TerraformDir
    
    # Generate backup name if not provided
    if (-not $BackupName) {
        $BackupName = "state-backup-$Environment-$Timestamp"
    }
    
    $BackupPath = Join-Path $BackupDir "$BackupName.tfstate"
    $MetadataPath = Join-Path $BackupDir "$BackupName.json"
    
    try {
        # Pull current state
        Write-StateLog "Pulling current state from remote backend" "INFO"
        terraform state pull > $BackupPath
        
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to pull state from remote backend"
        }
        
        # Validate state file
        if (-not (Test-Path $BackupPath) -or (Get-Item $BackupPath).Length -eq 0) {
            throw "State backup file is empty or not created"
        }
        
        # Get state metadata
        $StateContent = Get-Content $BackupPath | ConvertFrom-Json
        
        $Metadata = @{
            BackupName = $BackupName
            Environment = $Environment
            Timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss UTC'
            TerraformVersion = $StateContent.terraform_version
            Serial = $StateContent.serial
            ResourceCount = if ($StateContent.resources) { $StateContent.resources.Count } else { 0 }
            BackupSize = (Get-Item $BackupPath).Length
            BackupPath = $BackupPath
        }
        
        # Save metadata
        $Metadata | ConvertTo-Json -Depth 3 | Out-File $MetadataPath -Encoding UTF8
        
        Write-StateLog "State backup created successfully" "SUCCESS"
        Write-StateLog "Backup file: $BackupPath" "INFO"
        Write-StateLog "Metadata file: $MetadataPath" "INFO"
        Write-StateLog "Resource count: $($Metadata.ResourceCount)" "INFO"
        Write-StateLog "Backup size: $([math]::Round($Metadata.BackupSize / 1KB, 2)) KB" "INFO"
        
        return $Metadata
    }
    catch {
        Write-StateLog "State backup failed: $($_.Exception.Message)" "ERROR"
        
        # Clean up partial backup
        if (Test-Path $BackupPath) {
            Remove-Item $BackupPath -Force
        }
        if (Test-Path $MetadataPath) {
            Remove-Item $MetadataPath -Force
        }
        
        throw
    }
}

function Invoke-StateRestore {
    param([string]$BackupName)
    
    Write-StateLog "Restoring Terraform state from backup" "HEADER"
    
    if (-not $BackupName) {
        throw "BackupName parameter is required for restore operation"
    }
    
    $BackupPath = Join-Path $BackupDir "$BackupName.tfstate"
    $MetadataPath = Join-Path $BackupDir "$BackupName.json"
    
    if (-not (Test-Path $BackupPath)) {
        throw "Backup file not found: $BackupPath"
    }
    
    if (-not (Test-Path $MetadataPath)) {
        throw "Backup metadata file not found: $MetadataPath"
    }
    
    try {
        # Load metadata
        $Metadata = Get-Content $MetadataPath | ConvertFrom-Json
        
        Write-StateLog "Backup metadata:" "INFO"
        Write-StateLog "  Environment: $($Metadata.Environment)" "INFO"
        Write-StateLog "  Timestamp: $($Metadata.Timestamp)" "INFO"
        Write-StateLog "  Resource count: $($Metadata.ResourceCount)" "INFO"
        Write-StateLog "  Terraform version: $($Metadata.TerraformVersion)" "INFO"
        
        # Validate environment match
        if ($Metadata.Environment -ne $Environment) {
            if (-not $Force) {
                throw "Backup environment ($($Metadata.Environment)) does not match target environment ($Environment). Use -Force to override."
            }
            Write-StateLog "Environment mismatch - proceeding with force flag" "WARNING"
        }
        
        # Production safety check
        if ($Environment -eq "prod" -and -not $Force) {
            Write-StateLog "PRODUCTION STATE RESTORE WARNING" "WARNING"
            Write-StateLog "You are about to restore state in PRODUCTION environment!" "WARNING"
            Write-StateLog "This will overwrite the current production state." "WARNING"
            
            $Confirm = Read-Host "Type 'RESTORE PRODUCTION STATE' to confirm (case-sensitive)"
            if ($Confirm -ne "RESTORE PRODUCTION STATE") {
                Write-StateLog "Production state restore cancelled by user" "INFO"
                return
            }
        }
        
        Set-Location $TerraformDir
        
        # Create current state backup before restore
        Write-StateLog "Creating backup of current state before restore" "INFO"
        $PreRestoreBackup = Invoke-StateBackup
        Write-StateLog "Pre-restore backup created: $($PreRestoreBackup.BackupName)" "SUCCESS"
        
        # Push restored state
        Write-StateLog "Pushing restored state to remote backend" "INFO"
        Get-Content $BackupPath | terraform state push -
        
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to push restored state to remote backend"
        }
        
        Write-StateLog "State restored successfully from backup: $BackupName" "SUCCESS"
        Write-StateLog "Pre-restore backup available: $($PreRestoreBackup.BackupName)" "INFO"
    }
    catch {
        Write-StateLog "State restore failed: $($_.Exception.Message)" "ERROR"
        throw
    }
}

function Get-StateBackups {
    Write-StateLog "Listing available state backups" "HEADER"
    
    $BackupFiles = Get-ChildItem -Path $BackupDir -Filter "*.json" | Sort-Object LastWriteTime -Descending
    
    if ($BackupFiles.Count -eq 0) {
        Write-StateLog "No state backups found" "INFO"
        return @()
    }
    
    $Backups = @()
    
    foreach ($MetadataFile in $BackupFiles) {
        try {
            $Metadata = Get-Content $MetadataFile.FullName | ConvertFrom-Json
            $Backups += $Metadata
        }
        catch {
            Write-StateLog "Failed to read metadata file: $($MetadataFile.Name)" "WARNING"
        }
    }
    
    # Filter by environment if specified
    $FilteredBackups = $Backups | Where-Object { $_.Environment -eq $Environment }
    
    Write-StateLog "Found $($FilteredBackups.Count) backups for environment: $Environment" "INFO"
    
    foreach ($Backup in $FilteredBackups) {
        Write-StateLog "Backup: $($Backup.BackupName)" "INFO"
        Write-StateLog "  Timestamp: $($Backup.Timestamp)" "INFO"
        Write-StateLog "  Resources: $($Backup.ResourceCount)" "INFO"
        Write-StateLog "  Size: $([math]::Round($Backup.BackupSize / 1KB, 2)) KB" "INFO"
        Write-StateLog "" "INFO"
    }
    
    return $FilteredBackups
}

function Invoke-BackupCleanup {
    param([int]$RetentionDays)
    
    Write-StateLog "Cleaning up old state backups" "HEADER"
    Write-StateLog "Retention period: $RetentionDays days" "INFO"
    
    $CutoffDate = (Get-Date).AddDays(-$RetentionDays)
    $BackupFiles = Get-ChildItem -Path $BackupDir -Filter "*.json"
    
    $DeletedCount = 0
    $TotalSize = 0
    
    foreach ($MetadataFile in $BackupFiles) {
        try {
            $Metadata = Get-Content $MetadataFile.FullName | ConvertFrom-Json
            $BackupDate = [DateTime]::Parse($Metadata.Timestamp)
            
            if ($BackupDate -lt $CutoffDate -and $Metadata.Environment -eq $Environment) {
                $BackupStateFile = Join-Path $BackupDir "$($Metadata.BackupName).tfstate"
                
                # Delete state file
                if (Test-Path $BackupStateFile) {
                    $FileSize = (Get-Item $BackupStateFile).Length
                    Remove-Item $BackupStateFile -Force
                    $TotalSize += $FileSize
                }
                
                # Delete metadata file
                Remove-Item $MetadataFile.FullName -Force
                $DeletedCount++
                
                Write-StateLog "Deleted backup: $($Metadata.BackupName)" "INFO"
            }
        }
        catch {
            Write-StateLog "Failed to process backup file: $($MetadataFile.Name)" "WARNING"
        }
    }
    
    Write-StateLog "Cleanup completed" "SUCCESS"
    Write-StateLog "Deleted backups: $DeletedCount" "INFO"
    Write-StateLog "Freed space: $([math]::Round($TotalSize / 1KB, 2)) KB" "INFO"
}

function Test-StateValidation {
    Write-StateLog "Validating Terraform state" "HEADER"
    
    Set-Location $TerraformDir
    
    try {
        # Check state consistency
        Write-StateLog "Checking state consistency" "INFO"
        terraform state list > $null
        
        if ($LASTEXITCODE -ne 0) {
            throw "State list command failed - state may be corrupted"
        }
        
        # Get state statistics
        $StateResources = terraform state list
        $ResourceCount = if ($StateResources) { $StateResources.Count } else { 0 }
        
        Write-StateLog "State validation completed" "SUCCESS"
        Write-StateLog "Resources in state: $ResourceCount" "INFO"
        
        # Show resource summary
        if ($ResourceCount -gt 0) {
            $ResourceTypes = @{}
            foreach ($Resource in $StateResources) {
                $Type = $Resource.Split('.')[0]
                if ($ResourceTypes.ContainsKey($Type)) {
                    $ResourceTypes[$Type]++
                }
                else {
                    $ResourceTypes[$Type] = 1
                }
            }
            
            Write-StateLog "Resource types:" "INFO"
            foreach ($Type in $ResourceTypes.Keys | Sort-Object) {
                Write-StateLog "  $Type: $($ResourceTypes[$Type])" "INFO"
            }
        }
        
        return $true
    }
    catch {
        Write-StateLog "State validation failed: $($_.Exception.Message)" "ERROR"
        return $false
    }
}

function Invoke-StateLock {
    Write-StateLog "Acquiring Terraform state lock" "HEADER"
    
    Set-Location $TerraformDir
    
    try {
        # Force lock (this is dangerous and should be used carefully)
        if ($Force) {
            Write-StateLog "Force locking state (use with extreme caution)" "WARNING"
            terraform force-unlock -force $LockId
        }
        else {
            Write-StateLog "State locking is handled automatically by Terraform operations" "INFO"
            Write-StateLog "Manual locking is not typically required" "INFO"
        }
    }
    catch {
        Write-StateLog "State lock operation failed: $($_.Exception.Message)" "ERROR"
        throw
    }
}

function Invoke-StateUnlock {
    param([string]$LockId)
    
    Write-StateLog "Releasing Terraform state lock" "HEADER"
    
    if (-not $LockId) {
        throw "LockId parameter is required for unlock operation"
    }
    
    Set-Location $TerraformDir
    
    try {
        Write-StateLog "Unlocking state with lock ID: $LockId" "INFO"
        
        if ($Force) {
            terraform force-unlock -force $LockId
        }
        else {
            terraform force-unlock $LockId
        }
        
        if ($LASTEXITCODE -eq 0) {
            Write-StateLog "State unlocked successfully" "SUCCESS"
        }
        else {
            throw "State unlock failed"
        }
    }
    catch {
        Write-StateLog "State unlock operation failed: $($_.Exception.Message)" "ERROR"
        throw
    }
}

function Invoke-ResourceImport {
    param([string]$ResourceAddress, [string]$ResourceId)
    
    Write-StateLog "Importing resource into Terraform state" "HEADER"
    
    if (-not $ResourceAddress -or -not $ResourceId) {
        throw "ResourceAddress and ResourceId parameters are required for import operation"
    }
    
    Set-Location $TerraformDir
    
    try {
        Write-StateLog "Importing resource: $ResourceAddress" "INFO"
        Write-StateLog "Resource ID: $ResourceId" "INFO"
        
        terraform import $ResourceAddress $ResourceId
        
        if ($LASTEXITCODE -eq 0) {
            Write-StateLog "Resource imported successfully" "SUCCESS"
        }
        else {
            throw "Resource import failed"
        }
    }
    catch {
        Write-StateLog "Resource import operation failed: $($_.Exception.Message)" "ERROR"
        throw
    }
}

function Invoke-ResourceRemove {
    param([string]$ResourceAddress)
    
    Write-StateLog "Removing resource from Terraform state" "HEADER"
    
    if (-not $ResourceAddress) {
        throw "ResourceAddress parameter is required for remove operation"
    }
    
    Set-Location $TerraformDir
    
    try {
        Write-StateLog "Removing resource: $ResourceAddress" "INFO"
        
        # Safety check for production
        if ($Environment -eq "prod" -and -not $Force) {
            Write-StateLog "PRODUCTION STATE MODIFICATION WARNING" "WARNING"
            Write-StateLog "You are about to remove a resource from PRODUCTION state!" "WARNING"
            
            $Confirm = Read-Host "Type 'REMOVE FROM PRODUCTION' to confirm (case-sensitive)"
            if ($Confirm -ne "REMOVE FROM PRODUCTION") {
                Write-StateLog "Production state modification cancelled by user" "INFO"
                return
            }
        }
        
        terraform state rm $ResourceAddress
        
        if ($LASTEXITCODE -eq 0) {
            Write-StateLog "Resource removed successfully" "SUCCESS"
        }
        else {
            throw "Resource removal failed"
        }
    }
    catch {
        Write-StateLog "Resource removal operation failed: $($_.Exception.Message)" "ERROR"
        throw
    }
}

# Main execution function
function Main {
    Write-StateLog "Starting Terraform state management operation" "HEADER"
    Write-StateLog "Environment: $Environment" "INFO"
    Write-StateLog "Action: $Action" "INFO"
    Write-StateLog "Log file: $LogFile" "INFO"
    
    try {
        # Validate prerequisites
        Test-StateManagementPrerequisites
        
        # Initialize workspace for most operations
        if ($Action -notin @("list-backups", "cleanup")) {
            Initialize-StateWorkspace
        }
        
        # Execute the requested action
        switch ($Action) {
            "backup" {
                $BackupResult = Invoke-StateBackup
                Write-StateLog "Backup operation completed successfully" "SUCCESS"
            }
            "restore" {
                Invoke-StateRestore -BackupName $BackupName
                Write-StateLog "Restore operation completed successfully" "SUCCESS"
            }
            "list-backups" {
                $Backups = Get-StateBackups
                Write-StateLog "List backups operation completed successfully" "SUCCESS"
            }
            "cleanup" {
                Invoke-BackupCleanup -RetentionDays $RetentionDays
                Write-StateLog "Cleanup operation completed successfully" "SUCCESS"
            }
            "validate" {
                $ValidationResult = Test-StateValidation
                if ($ValidationResult) {
                    Write-StateLog "Validation operation completed successfully" "SUCCESS"
                }
                else {
                    throw "State validation failed"
                }
            }
            "lock" {
                Invoke-StateLock
                Write-StateLog "Lock operation completed successfully" "SUCCESS"
            }
            "unlock" {
                Invoke-StateUnlock -LockId $LockId
                Write-StateLog "Unlock operation completed successfully" "SUCCESS"
            }
            "import" {
                Invoke-ResourceImport -ResourceAddress $ResourceAddress -ResourceId $ResourceId
                Write-StateLog "Import operation completed successfully" "SUCCESS"
            }
            "remove" {
                Invoke-ResourceRemove -ResourceAddress $ResourceAddress
                Write-StateLog "Remove operation completed successfully" "SUCCESS"
            }
            default {
                throw "Unknown action: $Action"
            }
        }
        
        Write-StateLog "State management operation completed successfully" "SUCCESS"
    }
    catch {
        Write-StateLog "State management operation failed: $($_.Exception.Message)" "ERROR"
        Write-StateLog "Check log file for details: $LogFile" "ERROR"
        exit 1
    }
}

# Run main function
Main