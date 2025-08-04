# VPC Infrastructure Tests
# Tests VPC configuration and connectivity

param(
    [Parameter(Mandatory=$true)]
    [string]$Environment
)

# Test configuration
$TestName = "VPC Infrastructure Test"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$TerraformDir = Split-Path -Parent $ScriptDir

function Write-TestStatus {
    param([string]$Message)
    Write-Host "[TEST] $Message" -ForegroundColor Cyan
}

function Write-TestPass {
    param([string]$Message)
    Write-Host "[PASS] $Message" -ForegroundColor Green
}

function Write-TestFail {
    param([string]$Message)
    Write-Host "[FAIL] $Message" -ForegroundColor Red
}

function Test-VPCExists {
    param([string]$VpcId)
    
    Write-TestStatus "Testing VPC existence: $VpcId"
    
    try {
        $VpcInfo = aws ec2 describe-vpcs --vpc-ids $VpcId --query 'Vpcs[0]' --output json | ConvertFrom-Json
        
        if ($VpcInfo.VpcId -eq $VpcId) {
            Write-TestPass "VPC exists and is accessible"
            return $true
        }
        else {
            Write-TestFail "VPC not found or not accessible"
            return $false
        }
    }
    catch {
        Write-TestFail "Error checking VPC: $($_.Exception.Message)"
        return $false
    }
}

function Test-SubnetConfiguration {
    param([string]$VpcId)
    
    Write-TestStatus "Testing subnet configuration for VPC: $VpcId"
    
    try {
        $Subnets = aws ec2 describe-subnets --filters "Name=vpc-id,Values=$VpcId" --query 'Subnets' --output json | ConvertFrom-Json
        
        # Check for required subnet types
        $PublicSubnets = $Subnets | Where-Object { $_.Tags | Where-Object { $_.Key -eq "Type" -and $_.Value -eq "Public" } }
        $PrivateSubnets = $Subnets | Where-Object { $_.Tags | Where-Object { $_.Key -eq "Type" -and $_.Value -eq "Private" } }
        $DatabaseSubnets = $Subnets | Where-Object { $_.Tags | Where-Object { $_.Key -eq "Type" -and $_.Value -eq "Database" } }
        
        $TestResults = @()
        
        if ($PublicSubnets.Count -ge 2) {
            Write-TestPass "Public subnets configured correctly ($($PublicSubnets.Count) subnets)"
            $TestResults += $true
        }
        else {
            Write-TestFail "Insufficient public subnets (found: $($PublicSubnets.Count), required: 2)"
            $TestResults += $false
        }
        
        if ($PrivateSubnets.Count -ge 2) {
            Write-TestPass "Private subnets configured correctly ($($PrivateSubnets.Count) subnets)"
            $TestResults += $true
        }
        else {
            Write-TestFail "Insufficient private subnets (found: $($PrivateSubnets.Count), required: 2)"
            $TestResults += $false
        }
        
        if ($DatabaseSubnets.Count -ge 2) {
            Write-TestPass "Database subnets configured correctly ($($DatabaseSubnets.Count) subnets)"
            $TestResults += $true
        }
        else {
            Write-TestFail "Insufficient database subnets (found: $($DatabaseSubnets.Count), required: 2)"
            $TestResults += $false
        }
        
        # Check availability zone distribution
        $PublicAZs = $PublicSubnets | Select-Object -ExpandProperty AvailabilityZone | Sort-Object -Unique
        $PrivateAZs = $PrivateSubnets | Select-Object -ExpandProperty AvailabilityZone | Sort-Object -Unique
        
        if ($PublicAZs.Count -ge 2 -and $PrivateAZs.Count -ge 2) {
            Write-TestPass "Subnets distributed across multiple AZs"
            $TestResults += $true
        }
        else {
            Write-TestFail "Subnets not properly distributed across AZs"
            $TestResults += $false
        }
        
        return ($TestResults | Where-Object { $_ -eq $false }).Count -eq 0
    }
    catch {
        Write-TestFail "Error checking subnet configuration: $($_.Exception.Message)"
        return $false
    }
}

function Test-InternetGateway {
    param([string]$VpcId)
    
    Write-TestStatus "Testing Internet Gateway configuration for VPC: $VpcId"
    
    try {
        $IGWs = aws ec2 describe-internet-gateways --filters "Name=attachment.vpc-id,Values=$VpcId" --query 'InternetGateways' --output json | ConvertFrom-Json
        
        if ($IGWs.Count -eq 1) {
            $IGW = $IGWs[0]
            $Attachment = $IGW.Attachments | Where-Object { $_.VpcId -eq $VpcId }
            
            if ($Attachment.State -eq "available") {
                Write-TestPass "Internet Gateway properly attached and available"
                return $true
            }
            else {
                Write-TestFail "Internet Gateway not in available state: $($Attachment.State)"
                return $false
            }
        }
        else {
            Write-TestFail "Expected 1 Internet Gateway, found: $($IGWs.Count)"
            return $false
        }
    }
    catch {
        Write-TestFail "Error checking Internet Gateway: $($_.Exception.Message)"
        return $false
    }
}

function Test-NATGateways {
    param([string]$VpcId)
    
    Write-TestStatus "Testing NAT Gateway configuration for VPC: $VpcId"
    
    try {
        $NATGateways = aws ec2 describe-nat-gateways --filter "Name=vpc-id,Values=$VpcId" --query 'NatGateways[?State==`available`]' --output json | ConvertFrom-Json
        
        if ($NATGateways.Count -ge 1) {
            Write-TestPass "NAT Gateways configured and available ($($NATGateways.Count) gateways)"
            
            # Check if NAT gateways are in public subnets
            $PublicSubnets = aws ec2 describe-subnets --filters "Name=vpc-id,Values=$VpcId" --query 'Subnets[?Tags[?Key==`Type` && Value==`Public`]].SubnetId' --output json | ConvertFrom-Json
            
            $NATsInPublicSubnets = $NATGateways | Where-Object { $_.SubnetId -in $PublicSubnets }
            
            if ($NATsInPublicSubnets.Count -eq $NATGateways.Count) {
                Write-TestPass "All NAT Gateways are in public subnets"
                return $true
            }
            else {
                Write-TestFail "Some NAT Gateways are not in public subnets"
                return $false
            }
        }
        else {
            Write-TestFail "No available NAT Gateways found"
            return $false
        }
    }
    catch {
        Write-TestFail "Error checking NAT Gateways: $($_.Exception.Message)"
        return $false
    }
}

function Test-SecurityGroups {
    param([string]$VpcId)
    
    Write-TestStatus "Testing Security Groups configuration for VPC: $VpcId"
    
    try {
        $SecurityGroups = aws ec2 describe-security-groups --filters "Name=vpc-id,Values=$VpcId" --query 'SecurityGroups' --output json | ConvertFrom-Json
        
        # Look for expected security groups
        $ExpectedSGs = @("alb", "ecs", "rds")
        $FoundSGs = @()
        
        foreach ($ExpectedSG in $ExpectedSGs) {
            $SG = $SecurityGroups | Where-Object { $_.GroupName -like "*$ExpectedSG*" }
            if ($SG) {
                $FoundSGs += $ExpectedSG
                Write-TestPass "Security group found: $ExpectedSG ($($SG.GroupId))"
            }
            else {
                Write-TestFail "Security group not found: $ExpectedSG"
            }
        }
        
        return $FoundSGs.Count -eq $ExpectedSGs.Count
    }
    catch {
        Write-TestFail "Error checking Security Groups: $($_.Exception.Message)"
        return $false
    }
}

# Main test execution
function Main {
    Write-TestStatus "Starting VPC infrastructure tests for environment: $Environment"
    
    # Get VPC ID from Terraform output
    Set-Location $TerraformDir
    terraform workspace select $Environment
    
    try {
        $TerraformOutput = terraform output -json | ConvertFrom-Json
        $VpcId = $TerraformOutput.vpc_id.value
        
        if (-not $VpcId) {
            throw "VPC ID not found in Terraform output"
        }
        
        Write-TestStatus "Testing VPC: $VpcId"
        
        # Run tests
        $TestResults = @()
        $TestResults += Test-VPCExists -VpcId $VpcId
        $TestResults += Test-SubnetConfiguration -VpcId $VpcId
        $TestResults += Test-InternetGateway -VpcId $VpcId
        $TestResults += Test-NATGateways -VpcId $VpcId
        $TestResults += Test-SecurityGroups -VpcId $VpcId
        
        # Calculate results
        $PassedTests = ($TestResults | Where-Object { $_ -eq $true }).Count
        $TotalTests = $TestResults.Count
        $FailedTests = $TotalTests - $PassedTests
        
        Write-TestStatus "Test Results: $PassedTests/$TotalTests passed"
        
        if ($FailedTests -eq 0) {
            Write-TestPass "All VPC infrastructure tests passed"
            return @{
                Status = "PASS"
                PassedTests = $PassedTests
                TotalTests = $TotalTests
                Details = "All VPC infrastructure components are properly configured"
            }
        }
        else {
            Write-TestFail "$FailedTests VPC infrastructure tests failed"
            throw "VPC infrastructure tests failed"
        }
    }
    catch {
        Write-TestFail "VPC infrastructure test execution failed: $($_.Exception.Message)"
        throw
    }
}

# Execute tests
Main