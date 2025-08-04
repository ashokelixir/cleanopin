# RDS Infrastructure Tests
# Tests RDS PostgreSQL configuration and connectivity

param(
    [Parameter(Mandatory=$true)]
    [string]$Environment
)

# Test configuration
$TestName = "RDS Infrastructure Test"
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

function Test-RDSInstance {
    param([string]$DBInstanceId)
    
    Write-TestStatus "Testing RDS instance: $DBInstanceId"
    
    try {
        $DBInstance = aws rds describe-db-instances --db-instance-identifier $DBInstanceId --query 'DBInstances[0]' --output json | ConvertFrom-Json
        
        if ($DBInstance.DBInstanceStatus -eq "available") {
            Write-TestPass "RDS instance is available"
            
            # Check engine and version
            if ($DBInstance.Engine -eq "postgres") {
                Write-TestPass "Database engine is PostgreSQL"
            }
            else {
                Write-TestFail "Expected PostgreSQL engine, found: $($DBInstance.Engine)"
                return $false
            }
            
            # Check Multi-AZ configuration for production
            if ($Environment -eq "prod") {
                if ($DBInstance.MultiAZ) {
                    Write-TestPass "Multi-AZ is enabled for production"
                }
                else {
                    Write-TestFail "Multi-AZ should be enabled for production"
                    return $false
                }
            }
            
            # Check backup retention
            if ($DBInstance.BackupRetentionPeriod -gt 0) {
                Write-TestPass "Automated backups are enabled (retention: $($DBInstance.BackupRetentionPeriod) days)"
            }
            else {
                Write-TestFail "Automated backups are not enabled"
                return $false
            }
            
            # Check encryption
            if ($DBInstance.StorageEncrypted) {
                Write-TestPass "Storage encryption is enabled"
            }
            else {
                Write-TestFail "Storage encryption is not enabled"
                return $false
            }
            
            return $true
        }
        else {
            Write-TestFail "RDS instance is not available. Status: $($DBInstance.DBInstanceStatus)"
            return $false
        }
    }
    catch {
        Write-TestFail "Error checking RDS instance: $($_.Exception.Message)"
        return $false
    }
}

function Test-RDSSubnetGroup {
    param([string]$DBSubnetGroupName)
    
    Write-TestStatus "Testing RDS subnet group: $DBSubnetGroupName"
    
    try {
        $SubnetGroup = aws rds describe-db-subnet-groups --db-subnet-group-name $DBSubnetGroupName --query 'DBSubnetGroups[0]' --output json | ConvertFrom-Json
        
        if ($SubnetGroup.SubnetGroupStatus -eq "Complete") {
            Write-TestPass "DB subnet group is complete"
            
            # Check subnet count and AZ distribution
            $Subnets = $SubnetGroup.Subnets
            $AZs = $Subnets | Select-Object -ExpandProperty AvailabilityZone | Sort-Object -Unique
            
            if ($Subnets.Count -ge 2) {
                Write-TestPass "DB subnet group has sufficient subnets ($($Subnets.Count))"
            }
            else {
                Write-TestFail "DB subnet group has insufficient subnets ($($Subnets.Count))"
                return $false
            }
            
            if ($AZs.Count -ge 2) {
                Write-TestPass "DB subnets span multiple AZs ($($AZs.Count))"
            }
            else {
                Write-TestFail "DB subnets do not span multiple AZs ($($AZs.Count))"
                return $false
            }
            
            return $true
        }
        else {
            Write-TestFail "DB subnet group is not complete. Status: $($SubnetGroup.SubnetGroupStatus)"
            return $false
        }
    }
    catch {
        Write-TestFail "Error checking DB subnet group: $($_.Exception.Message)"
        return $false
    }
}

function Test-RDSSecurityGroup {
    param([string]$DBInstanceId)
    
    Write-TestStatus "Testing RDS security group configuration"
    
    try {
        $DBInstance = aws rds describe-db-instances --db-instance-identifier $DBInstanceId --query 'DBInstances[0]' --output json | ConvertFrom-Json
        $SecurityGroups = $DBInstance.VpcSecurityGroups
        
        if ($SecurityGroups.Count -gt 0) {
            Write-TestPass "RDS instance has security groups attached ($($SecurityGroups.Count))"
            
            # Check security group rules
            foreach ($SG in $SecurityGroups) {
                if ($SG.Status -eq "active") {
                    Write-TestPass "Security group is active: $($SG.VpcSecurityGroupId)"
                    
                    # Get security group rules
                    $SGRules = aws ec2 describe-security-groups --group-ids $SG.VpcSecurityGroupId --query 'SecurityGroups[0].IpPermissions' --output json | ConvertFrom-Json
                    
                    # Check for PostgreSQL port (5432)
                    $PostgreSQLRule = $SGRules | Where-Object { $_.FromPort -eq 5432 -and $_.ToPort -eq 5432 }
                    
                    if ($PostgreSQLRule) {
                        Write-TestPass "PostgreSQL port (5432) is configured in security group"
                    }
                    else {
                        Write-TestFail "PostgreSQL port (5432) is not configured in security group"
                        return $false
                    }
                }
                else {
                    Write-TestFail "Security group is not active: $($SG.VpcSecurityGroupId)"
                    return $false
                }
            }
            
            return $true
        }
        else {
            Write-TestFail "RDS instance has no security groups attached"
            return $false
        }
    }
    catch {
        Write-TestFail "Error checking RDS security groups: $($_.Exception.Message)"
        return $false
    }
}

function Test-RDSSecretsManager {
    param([string]$SecretArn)
    
    Write-TestStatus "Testing RDS Secrets Manager integration"
    
    try {
        $Secret = aws secretsmanager describe-secret --secret-id $SecretArn --query '{Name:Name,Description:Description,LastChangedDate:LastChangedDate}' --output json | ConvertFrom-Json
        
        if ($Secret.Name) {
            Write-TestPass "RDS secret exists in Secrets Manager: $($Secret.Name)"
            
            # Test secret retrieval (without exposing the actual secret)
            try {
                $SecretValue = aws secretsmanager get-secret-value --secret-id $SecretArn --query 'SecretString' --output text | ConvertFrom-Json
                
                if ($SecretValue.username -and $SecretValue.password -and $SecretValue.host -and $SecretValue.port) {
                    Write-TestPass "RDS secret contains required connection parameters"
                    return $true
                }
                else {
                    Write-TestFail "RDS secret is missing required connection parameters"
                    return $false
                }
            }
            catch {
                Write-TestFail "Error retrieving RDS secret value: $($_.Exception.Message)"
                return $false
            }
        }
        else {
            Write-TestFail "RDS secret not found in Secrets Manager"
            return $false
        }
    }
    catch {
        Write-TestFail "Error checking RDS Secrets Manager integration: $($_.Exception.Message)"
        return $false
    }
}

function Test-RDSMonitoring {
    param([string]$DBInstanceId)
    
    Write-TestStatus "Testing RDS monitoring configuration"
    
    try {
        $DBInstance = aws rds describe-db-instances --db-instance-identifier $DBInstanceId --query 'DBInstances[0]' --output json | ConvertFrom-Json
        
        # Check Performance Insights
        if ($DBInstance.PerformanceInsightsEnabled) {
            Write-TestPass "Performance Insights is enabled"
        }
        else {
            Write-TestFail "Performance Insights is not enabled"
            return $false
        }
        
        # Check Enhanced Monitoring
        if ($DBInstance.MonitoringInterval -gt 0) {
            Write-TestPass "Enhanced Monitoring is enabled (interval: $($DBInstance.MonitoringInterval) seconds)"
        }
        else {
            Write-TestFail "Enhanced Monitoring is not enabled"
            return $false
        }
        
        # Check CloudWatch Logs
        $LogTypes = $DBInstance.EnabledCloudwatchLogsExports
        if ($LogTypes -and $LogTypes.Count -gt 0) {
            Write-TestPass "CloudWatch Logs exports are enabled: $($LogTypes -join ', ')"
        }
        else {
            Write-TestFail "CloudWatch Logs exports are not enabled"
            return $false
        }
        
        return $true
    }
    catch {
        Write-TestFail "Error checking RDS monitoring: $($_.Exception.Message)"
        return $false
    }
}

function Test-RDSConnectivity {
    param([string]$SecretArn)
    
    Write-TestStatus "Testing RDS connectivity (basic network test)"
    
    try {
        # Get connection details from secret
        $SecretValue = aws secretsmanager get-secret-value --secret-id $SecretArn --query 'SecretString' --output text | ConvertFrom-Json
        
        $Host = $SecretValue.host
        $Port = $SecretValue.port
        
        # Test network connectivity (TCP connection test)
        $TestConnection = Test-NetConnection -ComputerName $Host -Port $Port -WarningAction SilentlyContinue
        
        if ($TestConnection.TcpTestSucceeded) {
            Write-TestPass "Network connectivity to RDS instance successful"
            return $true
        }
        else {
            Write-TestFail "Network connectivity to RDS instance failed"
            return $false
        }
    }
    catch {
        Write-TestFail "Error testing RDS connectivity: $($_.Exception.Message)"
        return $false
    }
}

# Main test execution
function Main {
    Write-TestStatus "Starting RDS infrastructure tests for environment: $Environment"
    
    # Get RDS details from Terraform output
    Set-Location $TerraformDir
    terraform workspace select $Environment
    
    try {
        $TerraformOutput = terraform output -json | ConvertFrom-Json
        $DBInstanceId = $TerraformOutput.rds_instance_id.value
        $DBSubnetGroupName = $TerraformOutput.rds_subnet_group_name.value
        $SecretArn = $TerraformOutput.rds_secret_arn.value
        
        if (-not $DBInstanceId) {
            throw "RDS instance ID not found in Terraform output"
        }
        
        Write-TestStatus "Testing RDS instance: $DBInstanceId"
        
        # Run tests
        $TestResults = @()
        $TestResults += Test-RDSInstance -DBInstanceId $DBInstanceId
        $TestResults += Test-RDSSubnetGroup -DBSubnetGroupName $DBSubnetGroupName
        $TestResults += Test-RDSSecurityGroup -DBInstanceId $DBInstanceId
        $TestResults += Test-RDSSecretsManager -SecretArn $SecretArn
        $TestResults += Test-RDSMonitoring -DBInstanceId $DBInstanceId
        
        # Skip connectivity test in CI/CD environments or if not accessible
        if ($env:CI -ne "true") {
            $TestResults += Test-RDSConnectivity -SecretArn $SecretArn
        }
        else {
            Write-TestStatus "Skipping connectivity test in CI/CD environment"
        }
        
        # Calculate results
        $PassedTests = ($TestResults | Where-Object { $_ -eq $true }).Count
        $TotalTests = $TestResults.Count
        $FailedTests = $TotalTests - $PassedTests
        
        Write-TestStatus "Test Results: $PassedTests/$TotalTests passed"
        
        if ($FailedTests -eq 0) {
            Write-TestPass "All RDS infrastructure tests passed"
            return @{
                Status = "PASS"
                PassedTests = $PassedTests
                TotalTests = $TotalTests
                Details = "All RDS infrastructure components are properly configured"
            }
        }
        else {
            Write-TestFail "$FailedTests RDS infrastructure tests failed"
            throw "RDS infrastructure tests failed"
        }
    }
    catch {
        Write-TestFail "RDS infrastructure test execution failed: $($_.Exception.Message)"
        throw
    }
}

# Execute tests
Main