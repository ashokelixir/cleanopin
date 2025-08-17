param(
    [string]$Environment = "dev"
)

Write-Host "Validating Terraform for environment: $Environment" -ForegroundColor Blue

if (Get-Command terraform -ErrorAction SilentlyContinue) {
    Write-Host "Terraform is installed" -ForegroundColor Green
} else {
    Write-Host "Terraform not found" -ForegroundColor Red
    exit 1
}

Write-Host "Checking formatting..." -ForegroundColor Blue
terraform fmt -check -recursive
if ($LASTEXITCODE -eq 0) {
    Write-Host "Formatting is correct" -ForegroundColor Green
} else {
    Write-Host "Formatting issues found" -ForegroundColor Yellow
}

Write-Host "Checking for security issues..." -ForegroundColor Blue
$files = Get-ChildItem "environments/*.tfvars"
$issues = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    if ($content -match 'AKIA[0-9A-Z]{16}') {
        Write-Host "Hardcoded AWS key found in $($file.Name)" -ForegroundColor Red
        $issues++
    }
}

if ($issues -eq 0) {
    Write-Host "No hardcoded credentials found" -ForegroundColor Green
}

Write-Host "Validation complete" -ForegroundColor Blue