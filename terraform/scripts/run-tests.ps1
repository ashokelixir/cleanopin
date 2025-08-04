# Infrastructure Test Runner
# Orchestrates execution of all infrastructure tests with reporting
# Usage: .\run-tests.ps1 -Environment <env> [-TestSuite <suite>] [-Parallel] [-GenerateReport]

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("dev", "staging", "prod")]
    [string]$Environment,
    
    [ValidateSet("all", "unit", "integration", "security", "performance")]
    [string]$TestSuite = "all",
    
    [switch]$Parallel,
    [switch]$GenerateReport,
    [switch]$ContinueOnFailure,
    [string]$OutputFormat = "console", # console, json, junit
    [string]$ReportPath = "",
    [int]$TimeoutMinutes = 30
)

# Configuration
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$TerraformDir = Split-Path -Parent $ScriptDir
$TestDir = Join-Path $TerraformDir "tests"
$ReportDir = Join-Path $TerraformDir "reports"
$LogDir = Join-Path $TerraformDir "logs"

# Ensure directories exist
@($ReportDir, $LogDir) | ForEach-Object {
    if (-not (Test-Path $_)) {
        New-Item -ItemType Directory -Path $_ -Force | Out-Null
    }
}

# Test configuration
$Timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$LogFile = Join-Path $LogDir "test-runner-$Environment-$Timestamp.log"

# Test suites definition
$TestSuites = @{
    "unit" = @("vpc.test.ps1", "rds.test.ps1", "ecs.test.ps1")
    "integration" = @("integration.test.ps1")
    "security" = @("security.test.ps1")
    "performance" = @("performance.test.ps1")
}

function Write-TestLog {
    param([string]$Message, [string]$Level = "INFO")
    $LogMessage = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] [$Level] [TEST-RUNNER] $Message"
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

function Get-TestFiles {
    param([string]$Suite)
    
    if ($Suite -eq "all") {
        return Get-ChildItem -Path $TestDir -Filter "*.test.ps1" | Select-Object -ExpandProperty Name
    }
    elseif ($TestSuites.ContainsKey($Suite)) {
        return $TestSuites[$Suite]
    }
    else {
        throw "Unknown test suite: $Suite"
    }
}

function Invoke-SingleTest {
    param([string]$TestFile, [string]$Environment)
    
    $TestPath = Join-Path $TestDir $TestFile
    $TestName = [System.IO.Path]::GetFileNameWithoutExtension($TestFile)
    
    if (-not (Test-Path $TestPath)) {
        return @{
            TestName = $TestName
            Status = "SKIP"
            Duration = 0
            Message = "Test file not found: $TestPath"
            Details = $null
        }
    }
    
    Write-TestLog "Running test: $TestName" "INFO"
    
    $StartTime = Get-Date
    
    try {
        # Set timeout for individual test
        $Job = Start-Job -ScriptBlock {
            param($TestPath, $Environment)
            & $TestPath -Environment $Environment
        } -ArgumentList $TestPath, $Environment
        
        $TestResult = Wait-Job -Job $Job -Timeout ($TimeoutMinutes * 60)
        
        if ($TestResult) {
            $Output = Receive-Job -Job $Job
            Remove-Job -Job $Job
            
            $EndTime = Get-Date
            $Duration = ($EndTime - $StartTime).TotalSeconds
            
            Write-TestLog "Test completed: $TestName (${Duration}s)" "SUCCESS"
            
            return @{
                TestName = $TestName
                Status = "PASS"
                Duration = $Duration
                Message = "Test passed successfully"
                Details = $Output
            }
        }
        else {
            Remove-Job -Job $Job -Force
            
            $EndTime = Get-Date
            $Duration = ($EndTime - $StartTime).TotalSeconds
            
            Write-TestLog "Test timed out: $TestName (${Duration}s)" "ERROR"
            
            return @{
                TestName = $TestName
                Status = "TIMEOUT"
                Duration = $Duration
                Message = "Test timed out after $TimeoutMinutes minutes"
                Details = $null
            }
        }
    }
    catch {
        $EndTime = Get-Date
        $Duration = ($EndTime - $StartTime).TotalSeconds
        
        Write-TestLog "Test failed: $TestName - $($_.Exception.Message)" "ERROR"
        
        return @{
            TestName = $TestName
            Status = "FAIL"
            Duration = $Duration
            Message = $_.Exception.Message
            Details = $null
        }
    }
}

function Invoke-ParallelTests {
    param([string[]]$TestFiles, [string]$Environment)
    
    Write-TestLog "Running tests in parallel mode" "INFO"
    
    $Jobs = @()
    
    foreach ($TestFile in $TestFiles) {
        $Job = Start-Job -ScriptBlock {
            param($ScriptDir, $TestFile, $Environment)
            
            # Import the test runner functions
            . "$ScriptDir\run-tests.ps1"
            
            Invoke-SingleTest -TestFile $TestFile -Environment $Environment
        } -ArgumentList $ScriptDir, $TestFile, $Environment
        
        $Jobs += @{
            Job = $Job
            TestFile = $TestFile
        }
    }
    
    $Results = @()
    
    foreach ($JobInfo in $Jobs) {
        try {
            $JobResult = Wait-Job -Job $JobInfo.Job -Timeout ($TimeoutMinutes * 60)
            
            if ($JobResult) {
                $Output = Receive-Job -Job $JobInfo.Job
                $Results += $Output
            }
            else {
                $Results += @{
                    TestName = [System.IO.Path]::GetFileNameWithoutExtension($JobInfo.TestFile)
                    Status = "TIMEOUT"
                    Duration = $TimeoutMinutes * 60
                    Message = "Parallel test job timed out"
                    Details = $null
                }
            }
            
            Remove-Job -Job $JobInfo.Job
        }
        catch {
            $Results += @{
                TestName = [System.IO.Path]::GetFileNameWithoutExtension($JobInfo.TestFile)
                Status = "FAIL"
                Duration = 0
                Message = $_.Exception.Message
                Details = $null
            }
            
            Remove-Job -Job $JobInfo.Job -Force
        }
    }
    
    return $Results
}

function Invoke-SequentialTests {
    param([string[]]$TestFiles, [string]$Environment)
    
    Write-TestLog "Running tests in sequential mode" "INFO"
    
    $Results = @()
    
    foreach ($TestFile in $TestFiles) {
        $Result = Invoke-SingleTest -TestFile $TestFile -Environment $Environment
        $Results += $Result
        
        # Stop on first failure if not continuing on failure
        if ($Result.Status -eq "FAIL" -and -not $ContinueOnFailure) {
            Write-TestLog "Stopping test execution due to failure (use -ContinueOnFailure to override)" "WARNING"
            break
        }
    }
    
    return $Results
}

function Generate-TestReport {
    param([array]$TestResults, [string]$Format, [string]$OutputPath)
    
    Write-TestLog "Generating test report in $Format format" "INFO"
    
    $ReportData = @{
        Timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss UTC'
        Environment = $Environment
        TestSuite = $TestSuite
        Summary = @{
            Total = $TestResults.Count
            Passed = ($TestResults | Where-Object { $_.Status -eq "PASS" }).Count
            Failed = ($TestResults | Where-Object { $_.Status -eq "FAIL" }).Count
            Skipped = ($TestResults | Where-Object { $_.Status -eq "SKIP" }).Count
            Timeout = ($TestResults | Where-Object { $_.Status -eq "TIMEOUT" }).Count
        }
        Duration = ($TestResults | Measure-Object -Property Duration -Sum).Sum
        Tests = $TestResults
    }
    
    if (-not $OutputPath) {
        $OutputPath = Join-Path $ReportDir "test-report-$Environment-$TestSuite-$Timestamp"
    }
    
    switch ($Format.ToLower()) {
        "json" {
            $JsonReport = $ReportData | ConvertTo-Json -Depth 5
            $ReportFile = "$OutputPath.json"
            $JsonReport | Out-File $ReportFile -Encoding UTF8
            Write-TestLog "JSON report generated: $ReportFile" "SUCCESS"
        }
        "junit" {
            $JUnitReport = Generate-JUnitReport -ReportData $ReportData
            $ReportFile = "$OutputPath.xml"
            $JUnitReport | Out-File $ReportFile -Encoding UTF8
            Write-TestLog "JUnit report generated: $ReportFile" "SUCCESS"
        }
        "html" {
            $HtmlReport = Generate-HtmlReport -ReportData $ReportData
            $ReportFile = "$OutputPath.html"
            $HtmlReport | Out-File $ReportFile -Encoding UTF8
            Write-TestLog "HTML report generated: $ReportFile" "SUCCESS"
        }
        default {
            Write-TestLog "Console output format selected - no file generated" "INFO"
        }
    }
    
    return $ReportData
}

function Generate-JUnitReport {
    param([hashtable]$ReportData)
    
    $JUnit = @"
<?xml version="1.0" encoding="UTF-8"?>
<testsuites name="Infrastructure Tests" tests="$($ReportData.Summary.Total)" failures="$($ReportData.Summary.Failed)" errors="0" time="$($ReportData.Duration)">
    <testsuite name="$($ReportData.TestSuite)" tests="$($ReportData.Summary.Total)" failures="$($ReportData.Summary.Failed)" errors="0" time="$($ReportData.Duration)">
"@
    
    foreach ($Test in $ReportData.Tests) {
        $JUnit += @"
        <testcase name="$($Test.TestName)" classname="Infrastructure.$($ReportData.Environment)" time="$($Test.Duration)">
"@
        
        if ($Test.Status -eq "FAIL") {
            $JUnit += @"
            <failure message="$($Test.Message)">$($Test.Message)</failure>
"@
        }
        elseif ($Test.Status -eq "SKIP") {
            $JUnit += @"
            <skipped message="$($Test.Message)">$($Test.Message)</skipped>
"@
        }
        elseif ($Test.Status -eq "TIMEOUT") {
            $JUnit += @"
            <error message="$($Test.Message)">$($Test.Message)</error>
"@
        }
        
        $JUnit += @"
        </testcase>
"@
    }
    
    $JUnit += @"
    </testsuite>
</testsuites>
"@
    
    return $JUnit
}

function Generate-HtmlReport {
    param([hashtable]$ReportData)
    
    $StatusColor = @{
        "PASS" = "green"
        "FAIL" = "red"
        "SKIP" = "orange"
        "TIMEOUT" = "purple"
    }
    
    $TestRows = ""
    foreach ($Test in $ReportData.Tests) {
        $Color = $StatusColor[$Test.Status]
        $TestRows += @"
        <tr>
            <td>$($Test.TestName)</td>
            <td style="color: $Color; font-weight: bold;">$($Test.Status)</td>
            <td>$([math]::Round($Test.Duration, 2))s</td>
            <td>$($Test.Message)</td>
        </tr>
"@
    }
    
    $Html = @"
<!DOCTYPE html>
<html>
<head>
    <title>Infrastructure Test Report - $($ReportData.Environment)</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 20px; }
        .header { background-color: #f0f0f0; padding: 20px; border-radius: 5px; margin-bottom: 20px; }
        .summary { display: flex; gap: 20px; margin-bottom: 20px; }
        .summary-item { background-color: #e8f4f8; padding: 15px; border-radius: 5px; text-align: center; }
        table { border-collapse: collapse; width: 100%; }
        th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
        th { background-color: #f2f2f2; }
        .pass { color: green; font-weight: bold; }
        .fail { color: red; font-weight: bold; }
        .skip { color: orange; font-weight: bold; }
        .timeout { color: purple; font-weight: bold; }
    </style>
</head>
<body>
    <div class="header">
        <h1>Infrastructure Test Report</h1>
        <p><strong>Environment:</strong> $($ReportData.Environment)</p>
        <p><strong>Test Suite:</strong> $($ReportData.TestSuite)</p>
        <p><strong>Timestamp:</strong> $($ReportData.Timestamp)</p>
        <p><strong>Total Duration:</strong> $([math]::Round($ReportData.Duration, 2)) seconds</p>
    </div>
    
    <div class="summary">
        <div class="summary-item">
            <h3>$($ReportData.Summary.Total)</h3>
            <p>Total Tests</p>
        </div>
        <div class="summary-item">
            <h3 class="pass">$($ReportData.Summary.Passed)</h3>
            <p>Passed</p>
        </div>
        <div class="summary-item">
            <h3 class="fail">$($ReportData.Summary.Failed)</h3>
            <p>Failed</p>
        </div>
        <div class="summary-item">
            <h3 class="skip">$($ReportData.Summary.Skipped)</h3>
            <p>Skipped</p>
        </div>
        <div class="summary-item">
            <h3 class="timeout">$($ReportData.Summary.Timeout)</h3>
            <p>Timeout</p>
        </div>
    </div>
    
    <h2>Test Results</h2>
    <table>
        <thead>
            <tr>
                <th>Test Name</th>
                <th>Status</th>
                <th>Duration</th>
                <th>Message</th>
            </tr>
        </thead>
        <tbody>
            $TestRows
        </tbody>
    </table>
</body>
</html>
"@
    
    return $Html
}

function Show-TestSummary {
    param([hashtable]$ReportData)
    
    Write-TestLog "Test Execution Summary" "HEADER"
    Write-TestLog "=====================" "HEADER"
    Write-TestLog "Environment: $($ReportData.Environment)" "INFO"
    Write-TestLog "Test Suite: $($ReportData.TestSuite)" "INFO"
    Write-TestLog "Total Duration: $([math]::Round($ReportData.Duration, 2)) seconds" "INFO"
    Write-TestLog "" "INFO"
    Write-TestLog "Results:" "INFO"
    Write-TestLog "  Total Tests: $($ReportData.Summary.Total)" "INFO"
    Write-TestLog "  Passed: $($ReportData.Summary.Passed)" "SUCCESS"
    Write-TestLog "  Failed: $($ReportData.Summary.Failed)" "ERROR"
    Write-TestLog "  Skipped: $($ReportData.Summary.Skipped)" "WARNING"
    Write-TestLog "  Timeout: $($ReportData.Summary.Timeout)" "ERROR"
    Write-TestLog "" "INFO"
    
    # Show individual test results
    foreach ($Test in $ReportData.Tests) {
        $Level = switch ($Test.Status) {
            "PASS" { "SUCCESS" }
            "FAIL" { "ERROR" }
            "SKIP" { "WARNING" }
            "TIMEOUT" { "ERROR" }
        }
        
        Write-TestLog "$($Test.TestName): $($Test.Status) ($([math]::Round($Test.Duration, 2))s)" $Level
        
        if ($Test.Status -ne "PASS" -and $Test.Message) {
            Write-TestLog "  Message: $($Test.Message)" "INFO"
        }
    }
}

# Main execution function
function Main {
    Write-TestLog "Starting infrastructure test runner" "HEADER"
    Write-TestLog "Environment: $Environment" "INFO"
    Write-TestLog "Test Suite: $TestSuite" "INFO"
    Write-TestLog "Parallel: $Parallel" "INFO"
    Write-TestLog "Continue on Failure: $ContinueOnFailure" "INFO"
    Write-TestLog "Timeout: $TimeoutMinutes minutes" "INFO"
    Write-TestLog "Log file: $LogFile" "INFO"
    
    try {
        # Get test files for the specified suite
        $TestFiles = Get-TestFiles -Suite $TestSuite
        
        if ($TestFiles.Count -eq 0) {
            Write-TestLog "No test files found for suite: $TestSuite" "WARNING"
            return
        }
        
        Write-TestLog "Found $($TestFiles.Count) test files: $($TestFiles -join ', ')" "INFO"
        
        # Execute tests
        $TestResults = if ($Parallel) {
            Invoke-ParallelTests -TestFiles $TestFiles -Environment $Environment
        }
        else {
            Invoke-SequentialTests -TestFiles $TestFiles -Environment $Environment
        }
        
        # Generate report
        $ReportData = Generate-TestReport -TestResults $TestResults -Format $OutputFormat -OutputPath $ReportPath
        
        # Show summary
        Show-TestSummary -ReportData $ReportData
        
        # Exit with appropriate code
        $FailedTests = $ReportData.Summary.Failed + $ReportData.Summary.Timeout
        
        if ($FailedTests -eq 0) {
            Write-TestLog "All tests completed successfully!" "SUCCESS"
            exit 0
        }
        else {
            Write-TestLog "$FailedTests test(s) failed" "ERROR"
            exit 1
        }
    }
    catch {
        Write-TestLog "Test runner execution failed: $($_.Exception.Message)" "ERROR"
        Write-TestLog "Check log file for details: $LogFile" "ERROR"
        exit 1
    }
}

# Only run main if script is executed directly (not dot-sourced)
if ($MyInvocation.InvocationName -ne '.') {
    Main
}