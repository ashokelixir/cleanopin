#!/usr/bin/env pwsh

param(
    [string]$Configuration = "Debug",
    [switch]$Coverage,
    [switch]$UnitOnly,
    [switch]$IntegrationOnly,
    [switch]$ArchitectureOnly,
    [string]$Filter = ""
)

Write-Host "Running Clean Architecture Template Tests" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow

$testProjects = @()

if ($UnitOnly) {
    $testProjects += "tests/CleanArchTemplate.UnitTests"
    Write-Host "Running Unit Tests Only" -ForegroundColor Yellow
}
elseif ($IntegrationOnly) {
    $testProjects += "tests/CleanArchTemplate.IntegrationTests"
    Write-Host "Running Integration Tests Only" -ForegroundColor Yellow
}
elseif ($ArchitectureOnly) {
    $testProjects += "tests/CleanArchTemplate.ArchitectureTests"
    Write-Host "Running Architecture Tests Only" -ForegroundColor Yellow
}
else {
    $testProjects += "tests/CleanArchTemplate.UnitTests"
    $testProjects += "tests/CleanArchTemplate.IntegrationTests"
    $testProjects += "tests/CleanArchTemplate.ArchitectureTests"
    Write-Host "Running All Tests" -ForegroundColor Yellow
}

$testArgs = @(
    "--configuration", $Configuration,
    "--logger", "console;verbosity=normal",
    "--logger", "trx",
    "--results-directory", "TestResults"
)

if ($Coverage) {
    Write-Host "Code Coverage Enabled" -ForegroundColor Yellow
    $testArgs += "--collect", "XPlat Code Coverage"
    $testArgs += "--settings", "coverlet.runsettings"
}

if ($Filter) {
    Write-Host "Filter: $Filter" -ForegroundColor Yellow
    $testArgs += "--filter", $Filter
}

$success = $true

foreach ($project in $testProjects) {
    Write-Host "`nRunning tests for: $project" -ForegroundColor Cyan
    
    $result = dotnet test $project @testArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Tests failed for: $project" -ForegroundColor Red
        $success = $false
    }
    else {
        Write-Host "Tests passed for: $project" -ForegroundColor Green
    }
}

if ($Coverage) {
    Write-Host "`nGenerating Coverage Report..." -ForegroundColor Cyan
    
    # Install reportgenerator if not already installed
    dotnet tool install -g dotnet-reportgenerator-globaltool 2>$null
    
    # Generate HTML coverage report
    reportgenerator -reports:"TestResults/**/coverage.cobertura.xml" -targetdir:"TestResults/CoverageReport" -reporttypes:Html
    
    Write-Host "Coverage report generated at: TestResults/CoverageReport/index.html" -ForegroundColor Green
}

if ($success) {
    Write-Host "`nAll tests completed successfully!" -ForegroundColor Green
    exit 0
}
else {
    Write-Host "`nSome tests failed!" -ForegroundColor Red
    exit 1
}