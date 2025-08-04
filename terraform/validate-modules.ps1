#!/usr/bin/env pwsh
# Terraform Module Validation Script

Write-Host "Validating Terraform modules..." -ForegroundColor Green

# Validate VPC module
Write-Host "Validating VPC module..." -ForegroundColor Yellow
Push-Location "modules/vpc"
try {
    terraform init -backend=false
    terraform validate
    Write-Host "✅ VPC module validation successful" -ForegroundColor Green
} catch {
    Write-Host "❌ VPC module validation failed: $_" -ForegroundColor Red
} finally {
    Pop-Location
}

# Validate Security Groups module
Write-Host "Validating Security Groups module..." -ForegroundColor Yellow
Push-Location "modules/security-groups"
try {
    terraform init -backend=false
    terraform validate
    Write-Host "✅ Security Groups module validation successful" -ForegroundColor Green
} catch {
    Write-Host "❌ Security Groups module validation failed: $_" -ForegroundColor Red
} finally {
    Pop-Location
}

Write-Host "Module validation complete!" -ForegroundColor Green