# Terraform Drift Detection and Remediation Script
# Usage: .\drift-detection.ps1 -Environment <env> [-AutoRemediate] [-NotificationWebhook <url>]
# Example: .\drift-detection.ps1 -Environment prod -NotificationWebhook "https://hooks.slack.com/..."

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment,
    
    [switch]$AutoRemediate,
    [string]$NotificationWebhook = "",
    [string]$EmailRecipients = "",
    [switch]$GenerateReport,
    [int]$MaxRetries = 3
)

# Configuration
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$TerraformDir = Split-Path -Parent $ScriptDir
$LogDir = Join-Path $TerraformDir "logs"
$ReportDir = Join-Path $TerraformDir "reports"

# Ensure directories exist
@($LogDir, $ReportDir) | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -ItemType Directory -Path $_ -Force | Out-Null
    }
}

# Logging configuration
$Timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$LogFile = Join-Path $LogDir "drift-detection-$Environment-$Timestamp.log"
$DriftReportFile = Join-Path $ReportDir "drift-report-$Environment-$Timestamp.json"

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $LogMessage = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] [$Level] $Message"
    Write-Host $LogMessage -ForegroundColor $(
        switch ($Level) {
            "ERROR" { "Red" }
            "WARNING" { "Yellow" }
            "SUCCESS" { "Green" }
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
                                title = "Environment"
                                value = $Environment
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

function Send-EmailNotification {
    param([string]$Subject, [string]$Body)
    
    if ($EmailRecipients) {
        try {
            # This would require AWS SES or similar email service configuration
            # For now, just log the email content
            Write-Log "Email notification would be sent to: $EmailRecipients" "INFO"
            Write-Log "Subject: $Subject" "INFO"
            Write-Log "Body: $Body" "INFO"
        }
        catch {
            Write-Log "Failed to send email notification: $($_.Exception.Message)" "ERROR"
        }
    }
}

function Test-Prerequisites {
    Write-Log "Validating prerequisites for drift detection"
    
    # Check Terraform
    try {
        $TerraformVersion = terraform version -json | ConvertFrom-Json
        Write-Log "Terraform version: $($TerraformVersion.terraform_version)" "SUCCESS"
    }
    catch {
        Write-Log "Terraform is not installed or not in PATH" "ERROR"
        throw "Terraform not available"
    }
    
    # Check AWS CLI
    try {
        $CallerIdentity = aws sts get-caller-identity | ConvertFrom-Json
        Write-Log "AWS Account: $($CallerIdentity.Account)" "SUCCESS"
    }
    catch {
        Write-Log "AWS CLI is not configured" "ERROR"
        throw "AWS CLI not configured"
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
    
    Write-Log "Prerequisites validation completed" "SUCCESS"
}

function Initialize-TerraformWorkspace {
    Write-Log "Initializing Terraform workspace for drift detection"
    
    Set-Location $TerraformDir
    
    $BackendConfig = Join-Path $TerraformDir "backend-configs\$Environment.hcl"
    
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
    
    Write-Log "Terraform workspace initialized successfully" "SUCCESS"
}

function Get-TerraformState {
    Write-Log "Retrieving current Terraform state"
    
    try {
        $StateOutput = terraform show -json | ConvertFrom-Json
        return $StateOutput
    }
    catch {
        Write-Log "Failed to retrieve Terraform state: $($_.Exception.Message)" "ERROR"
        throw
    }
}

function Invoke-DriftDetection {
    Write-Log "Starting drift detection for environment: $Environment"
    
    Set-Location $TerraformDir
    
    $VarFile = Join-Path $TerraformDir "environments\$Environment.tfvars"
    $DriftPlanFile = Join-Path $TerraformDir "drift-$Environment-$Timestamp.tfplan"
    
    # Run terraform plan to detect drift
    terraform plan -var-file="$VarFile" -out="$DriftPlanFile" -detailed-exitcode -no-color > $LogFile 2>&1
    $PlanExitCode = $LASTEXITCODE
    
    # Generate JSON output for analysis
    $DriftPlanJsonFile = "$DriftPlanFile.json"
    terraform show -json "$DriftPlanFile" > "$DriftPlanJsonFile" 2>$null
    
    $DriftResult = @{
        HasDrift = $false
        ExitCode = $PlanExitCode
        PlanFile = $DriftPlanFile
        JsonFile = $DriftPlanJsonFile
        Changes = @()
        Summary = @{
            Add = 0
            Change = 0
            Destroy = 0
        }
    }
    
    switch ($PlanExitCode) {
        0 {
            Write-Log "No configuration drift detected" "SUCCESS"
            $DriftResult.HasDrift = $false
        }
        1 {
            Write-Log "Error during drift detection" "ERROR"
            throw "Drift detection failed"
        }
        2 {
            Write-Log "Configuration drift detected!" "WARNING"
            $DriftResult.HasDrift = $true
            
            # Parse drift details
            if (Test-Path $DriftPlanJsonFile) {
                try {
                    $PlanJson = Get-Content $DriftPlanJsonFile | ConvertFrom-Json
                    
                    if ($PlanJson.resource_changes) {
                        foreach ($Change in $PlanJson.resource_changes) {
                            $ChangeDetail = @{
                                Address = $Change.address
                                Type = $Change.type
                                Name = $Change.name
                                Actions = $Change.change.actions
                                Before = $Change.change.before
                                After = $Change.change.after
                            }
                            
                            $DriftResult.Changes += $ChangeDetail
                            
                            # Count changes by type
                            if ($Change.change.actions -contains "create") {
                                $DriftResult.Summary.Add++
                            }
                            if ($Change.change.actions -contains "update") {
                                $DriftResult.Summary.Change++
                            }
                            if ($Change.change.actions -contains "delete") {
                                $DriftResult.Summary.Destroy++
                            }
                        }
                    }
                    
                    Write-Log "Drift Summary - Add: $($DriftResult.Summary.Add), Change: $($DriftResult.Summary.Change), Destroy: $($DriftResult.Summary.Destroy)" "WARNING"
                }
                catch {
                    Write-Log "Failed to parse drift plan JSON: $($_.Exception.Message)" "ERROR"
                }
            }
        }
    }
    
    return $DriftResult
}

function Analyze-DriftSeverity {
    param([object]$DriftResult)
    
    Write-Log "Analyzing drift severity"
    
    $Severity = "LOW"
    $CriticalResources = @("aws_rds_instance", "aws_ecs_service", "aws_lb", "aws_security_group")
    $HighRiskActions = @("delete", "replace")
    
    foreach ($Change in $DriftResult.Changes) {
        # Check for critical resource types
        if ($Change.Type -in $CriticalResources) {
            $Severity = "HIGH"
            Write-Log "Critical resource drift detected: $($Change.Address)" "WARNING"
        }
        
        # Check for high-risk actions
        foreach ($Action in $Change.Actions) {
            if ($Action -in $HighRiskActions) {
                $Severity = "HIGH"
                Write-Log "High-risk action detected: $Action on $($Change.Address)" "WARNING"
            }
        }
        
        # Check for security group changes
        if ($Change.Type -eq "aws_security_group_rule") {
            $Severity = "MEDIUM"
            Write-Log "Security group rule drift detected: $($Change.Address)" "WARNING"
        }
    }
    
    # Check for production environment
    if ($Environment -eq "prod" -and $DriftResult.HasDrift) {
        if ($Severity -eq "LOW") {
            $Severity = "MEDIUM"
        }
        Write-Log "Production environment drift detected - severity elevated" "WARNING"
    }
    
    Write-Log "Drift severity assessed as: $Severity" "INFO"
    return $Severity
}

function Invoke-DriftRemediation {
    param([object]$DriftResult)
    
    if (-not $DriftResult.HasDrift) {
        Write-Log "No drift to remediate" "INFO"
        return
    }
    
    Write-Log "Starting drift remediation for environment: $Environment"
    
    # Analyze severity before remediation
    $Severity = Analyze-DriftSeverity -DriftResult $DriftResult
    
    # Safety check for production
    if ($Environment -eq "prod" -and $Severity -eq "HIGH") {
        Write-Log "High severity drift detected in production - manual intervention required" "ERROR"
        Send-Notification -Title "Critical Drift Detected" -Message "High severity configuration drift detected in production environment. Manual intervention required." -Color "danger"
        return
    }
    
    # Confirm remediation
    if (-not $AutoRemediate) {
        Write-Log "Auto-remediation not enabled. Use -AutoRemediate flag to enable automatic remediation" "WARNING"
        return
    }
    
    Write-Log "Applying drift remediation..." "INFO"
    
    $RetryCount = 0
    $RemediationSuccess = $false
    
    while ($RetryCount -lt $MaxRetries -and -not $RemediationSuccess) {
        try {
            $RetryCount++
            Write-Log "Remediation attempt $RetryCount of $MaxRetries" "INFO"
            
            # Apply the plan
            terraform apply -auto-approve $DriftResult.PlanFile
            
            if ($LASTEXITCODE -eq 0) {
                $RemediationSuccess = $true
                Write-Log "Drift remediation completed successfully" "SUCCESS"
                
                # Clean up plan files
                Remove-Item $DriftResult.PlanFile -Force -ErrorAction SilentlyContinue
                Remove-Item $DriftResult.JsonFile -Force -ErrorAction SilentlyContinue
                
                Send-Notification -Title "Drift Remediated" -Message "Configuration drift has been successfully remediated in $Environment environment." -Color "good"
            }
            else {
                Write-Log "Remediation attempt $RetryCount failed" "ERROR"
                
                if ($RetryCount -lt $MaxRetries) {
                    Write-Log "Waiting 30 seconds before retry..." "INFO"
                    Start-Sleep -Seconds 30
                }
            }
        }
        catch {
            Write-Log "Remediation attempt $RetryCount failed with error: $($_.Exception.Message)" "ERROR"
            
            if ($RetryCount -lt $MaxRetries) {
                Write-Log "Waiting 30 seconds before retry..." "INFO"
                Start-Sleep -Seconds 30
            }
        }
    }
    
    if (-not $RemediationSuccess) {
        Write-Log "Drift remediation failed after $MaxRetries attempts" "ERROR"
        Send-Notification -Title "Drift Remediation Failed" -Message "Failed to remediate configuration drift in $Environment environment after $MaxRetries attempts." -Color "danger"
        throw "Drift remediation failed"
    }
}

function Generate-DriftReport {
    param([object]$DriftResult, [string]$Severity)
    
    if (-not $GenerateReport) {
        return
    }
    
    Write-Log "Generating drift detection report"
    
    $Report = @{
        Timestamp = (Get-Date -Format 'yyyy-MM-dd HH:mm:ss UTC')
        Environment = $Environment
        HasDrift = $DriftResult.HasDrift
        Severity = $Severity
        Summary = $DriftResult.Summary
        Changes = $DriftResult.Changes
        LogFile = $LogFile
        Remediated = $AutoRemediate -and $DriftResult.HasDrift
    }
    
    $Report | ConvertTo-Json -Depth 5 | Out-File $DriftReportFile -Encoding UTF8
    
    Write-Log "Drift report generated: $DriftReportFile" "SUCCESS"
    
    # Generate HTML report for better readability
    $HtmlReportFile = $DriftReportFile -replace '\.json$', '.html'
    Generate-HtmlReport -Report $Report -OutputFile $HtmlReportFile
}

function Generate-HtmlReport {
    param([object]$Report, [string]$OutputFile)
    
    $Html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Terraform Drift Detection Report - $($Report.Environment)</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .header { background-color: #f0f0f0; padding: 20px; border-radius: 5px; }
        .severity-high { color: #d32f2f; font-weight: bold; }
        .severity-medium { color: #f57c00; font-weight: bold; }
        .severity-low { color: #388e3c; font-weight: bold; }
        .no-drift { color: #388e3c; font-weight: bold; }
        .change-item { margin: 10px 0; padding: 10px; border-left: 4px solid #ccc; }
        .change-create { border-left-color: #4caf50; }
        .change-update { border-left-color: #ff9800; }
        .change-delete { border-left-color: #f44336; }
        table { border-collapse: collapse; width: 100%; margin: 20px 0; }
        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
        th { background-color: #f2f2f2; }
    </style>
</head>
<body>
    <div class="header">
        <h1>Terraform Drift Detection Report</h1>
        <p><strong>Environment:</strong> $($Report.Environment)</p>
        <p><strong>Timestamp:</strong> $($Report.Timestamp)</p>
        <p><strong>Drift Status:</strong> $(if ($Report.HasDrift) { "DRIFT DETECTED" } else { "NO DRIFT" })</p>
        $(if ($Report.HasDrift) { "<p><strong>Severity:</strong> <span class='severity-$($Report.Severity.ToLower())'>$($Report.Severity)</span></p>" })
        $(if ($Report.Remediated) { "<p><strong>Remediation:</strong> <span class='no-drift'>COMPLETED</span></p>" })
    </div>
    
    $(if ($Report.HasDrift) {
        "<h2>Drift Summary</h2>
        <table>
            <tr><th>Action</th><th>Count</th></tr>
            <tr><td>Resources to Add</td><td>$($Report.Summary.Add)</td></tr>
            <tr><td>Resources to Change</td><td>$($Report.Summary.Change)</td></tr>
            <tr><td>Resources to Destroy</td><td>$($Report.Summary.Destroy)</td></tr>
        </table>
        
        <h2>Detailed Changes</h2>"
        
        foreach ($Change in $Report.Changes) {
            $ActionClass = if ($Change.Actions -contains "create") { "change-create" } 
                          elseif ($Change.Actions -contains "update") { "change-update" }
                          elseif ($Change.Actions -contains "delete") { "change-delete" }
                          else { "" }
            
            "<div class='change-item $ActionClass'>
                <h3>$($Change.Address)</h3>
                <p><strong>Type:</strong> $($Change.Type)</p>
                <p><strong>Actions:</strong> $($Change.Actions -join ', ')</p>
            </div>"
        }
    } else {
        "<h2>No Configuration Drift Detected</h2>
        <p>All infrastructure resources are in sync with the Terraform configuration.</p>"
    })
    
    <h2>Log File</h2>
    <p>Detailed logs available at: <code>$($Report.LogFile)</code></p>
</body>
</html>
"@
    
    $Html | Out-File $OutputFile -Encoding UTF8
    Write-Log "HTML report generated: $OutputFile" "SUCCESS"
}

# Main execution function
function Main {
    Write-Log "Starting Terraform drift detection and remediation"
    Write-Log "Environment: $Environment"
    Write-Log "Auto-remediate: $AutoRemediate"
    Write-Log "Log file: $LogFile"
    
    try {
        # Validate prerequisites
        Test-Prerequisites
        
        # Initialize Terraform workspace
        Initialize-TerraformWorkspace
        
        # Detect drift
        $DriftResult = Invoke-DriftDetection
        
        # Analyze severity
        $Severity = Analyze-DriftSeverity -DriftResult $DriftResult
        
        # Send notification if drift detected
        if ($DriftResult.HasDrift) {
            $NotificationMessage = "Configuration drift detected in $Environment environment.`n" +
                                 "Summary: Add: $($DriftResult.Summary.Add), Change: $($DriftResult.Summary.Change), Destroy: $($DriftResult.Summary.Destroy)`n" +
                                 "Severity: $Severity"
            
            Send-Notification -Title "Configuration Drift Detected" -Message $NotificationMessage -Color "warning"
            
            # Attempt remediation if enabled
            if ($AutoRemediate) {
                Invoke-DriftRemediation -DriftResult $DriftResult
            }
        }
        else {
            Send-Notification -Title "No Drift Detected" -Message "Infrastructure configuration is in sync for $Environment environment." -Color "good"
        }
        
        # Generate report
        Generate-DriftReport -DriftResult $DriftResult -Severity $Severity
        
        Write-Log "Drift detection completed successfully" "SUCCESS"
        
        # Exit with appropriate code
        if ($DriftResult.HasDrift -and -not $AutoRemediate) {
            Write-Log "Drift detected but not remediated. Manual intervention may be required." "WARNING"
            exit 1
        }
        
    }
    catch {
        Write-Log "Drift detection failed: $($_.Exception.Message)" "ERROR"
        Send-Notification -Title "Drift Detection Failed" -Message "Drift detection process failed for $Environment environment: $($_.Exception.Message)" -Color "danger"
        exit 1
    }
}

# Execute main function
Main