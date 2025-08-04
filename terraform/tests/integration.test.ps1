# Integration Infrastructure Tests
# Tests end-to-end infrastructure integration and connectivity

param(
    [Parameter(Mandatory=$true)]
    [string]$Environment
)

# Test configuration
$TestName = "Integration Infrastructure Test"
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

function Test-VPCToRDSConnectivity {
    param([string]$VpcId, [string]$DBInstanceId)
    
    Write-TestStatus "Testing VPC to RDS connectivity"
    
    try {
        # Get RDS endpoint
        $DBInstance = aws rds describe-db-instances --db-instance-identifier $DBInstanceId --query 'DBInstances[0]' --output json | ConvertFrom-Json
        $DBEndpoint = $DBInstance.Endpoint.Address
        $DBPort = $DBInstance.Endpoint.Port
        
        # Get VPC CIDR
        $VPC = aws ec2 describe-vpcs --vpc-ids $VpcId --query 'Vpcs[0]' --output json | ConvertFrom-Json
        $VpcCidr = $VPC.CidrBlock
        
        # Check if RDS is in the same VPC
        $DBSubnetGroup = aws rds describe-db-subnet-groups --db-subnet-group-name $DBInstance.DBSubnetGroup --query 'DBSubnetGroups[0]' --output json | ConvertFrom-Json
        $DBVpcId = $DBSubnetGroup.VpcId
        
        if ($DBVpcId -eq $VpcId) {
            Write-TestPass "RDS instance is in the correct VPC"
        }
        else {
            Write-TestFail "RDS instance is not in the expected VPC"
            return $false
        }
        
        # Check security group connectivity
        $DBSecurityGroups = $DBInstance.VpcSecurityGroups
        $VPCSecurityGroups = aws ec2 describe-security-groups --filters "Name=vpc-id,Values=$VpcId" --query 'SecurityGroups' --output json | ConvertFrom-Json
        
        $ConnectivityAllowed = $false
        
        foreach ($DBSG in $DBSecurityGroups) {
            $DBSGRules = aws ec2 describe-security-groups --group-ids $DBSG.VpcSecurityGroupId --query 'SecurityGroups[0].IpPermissions' --output json | ConvertFrom-Json
            
            foreach ($Rule in $DBSGRules) {
                if ($Rule.FromPort -eq $DBPort -and $Rule.ToPort -eq $DBPort) {
                    # Check if rule allows VPC CIDR or references ECS security group
                    foreach ($IpRange in $Rule.IpRanges) {
                        if ($IpRange.CidrIp -eq $VpcCidr) {
                            $ConnectivityAllowed = $true
                            break
                        }
                    }
                    
                    # Check for security group references
                    foreach ($SGRef in $Rule.UserIdGroupPairs) {
                        $ReferencedSG = $VPCSecurityGroups | Where-Object { $_.GroupId -eq $SGRef.GroupId }
                        if ($ReferencedSG -and $ReferencedSG.GroupName -like "*ecs*") {
                            $ConnectivityAllowed = $true
                            break
                        }
                    }
                }
            }
        }
        
        if ($ConnectivityAllowed) {
            Write-TestPass "Security groups allow VPC to RDS connectivity"
        }
        else {
            Write-TestFail "Security groups do not allow VPC to RDS connectivity"
            return $false
        }
        
        return $true
    }
    catch {
        Write-TestFail "Error testing VPC to RDS connectivity: $($_.Exception.Message)"
        return $false
    }
}

function Test-ALBToECSConnectivity {
    param([string]$LoadBalancerArn, [string]$ClusterName, [string]$ServiceName)
    
    Write-TestStatus "Testing ALB to ECS connectivity"
    
    try {
        # Get ALB target groups
        $TargetGroups = aws elbv2 describe-target-groups --load-balancer-arn $LoadBalancerArn --query 'TargetGroups' --output json | ConvertFrom-Json
        
        if ($TargetGroups.Count -eq 0) {
            Write-TestFail "No target groups found for ALB"
            return $false
        }
        
        $TargetGroup = $TargetGroups[0]
        
        # Check target group health
        $TargetHealth = aws elbv2 describe-target-health --target-group-arn $TargetGroup.TargetGroupArn --query 'TargetHealthDescriptions' --output json | ConvertFrom-Json
        
        if ($TargetHealth.Count -gt 0) {
            $HealthyTargets = $TargetHealth | Where-Object { $_.TargetHealth.State -eq "healthy" }
            $UnhealthyTargets = $TargetHealth | Where-Object { $_.TargetHealth.State -ne "healthy" }
            
            Write-TestStatus "Target health: $($HealthyTargets.Count) healthy, $($UnhealthyTargets.Count) unhealthy"
            
            if ($HealthyTargets.Count -gt 0) {
                Write-TestPass "ALB has healthy targets"
            }
            else {
                Write-TestFail "ALB has no healthy targets"
                
                # Log unhealthy target details for debugging
                foreach ($Target in $UnhealthyTargets) {
                    Write-TestStatus "Unhealthy target: $($Target.Target.Id) - $($Target.TargetHealth.State) - $($Target.TargetHealth.Description)"
                }
                
                return $false
            }
        }
        else {
            Write-TestFail "No targets registered in target group"
            return $false
        }
        
        # Check ECS service integration
        $Service = aws ecs describe-services --cluster $ClusterName --services $ServiceName --query 'services[0]' --output json | ConvertFrom-Json
        
        if ($Service.loadBalancers -and $Service.loadBalancers.Count -gt 0) {
            $ServiceTargetGroup = $Service.loadBalancers[0].targetGroupArn
            
            if ($ServiceTargetGroup -eq $TargetGroup.TargetGroupArn) {
                Write-TestPass "ECS service is properly integrated with ALB target group"
            }
            else {
                Write-TestFail "ECS service target group does not match ALB target group"
                return $false
            }
        }
        else {
            Write-TestFail "ECS service is not configured with load balancer"
            return $false
        }
        
        return $true
    }
    catch {
        Write-TestFail "Error testing ALB to ECS connectivity: $($_.Exception.Message)"
        return $false
    }
}

function Test-InternetConnectivity {
    param([string]$LoadBalancerArn)
    
    Write-TestStatus "Testing internet connectivity through ALB"
    
    try {
        # Get ALB DNS name
        $ALB = aws elbv2 describe-load-balancers --load-balancer-arns $LoadBalancerArn --query 'LoadBalancers[0]' --output json | ConvertFrom-Json
        $ALBDnsName = $ALB.DNSName
        
        Write-TestStatus "Testing connectivity to ALB: $ALBDnsName"
        
        # Test DNS resolution
        try {
            $DnsResult = Resolve-DnsName -Name $ALBDnsName -ErrorAction Stop
            Write-TestPass "ALB DNS name resolves successfully"
        }
        catch {
            Write-TestFail "ALB DNS name does not resolve: $($_.Exception.Message)"
            return $false
        }
        
        # Test HTTP connectivity (basic TCP test)
        try {
            $HttpTest = Test-NetConnection -ComputerName $ALBDnsName -Port 80 -WarningAction SilentlyContinue
            if ($HttpTest.TcpTestSucceeded) {
                Write-TestPass "HTTP connectivity to ALB successful"
            }
            else {
                Write-TestFail "HTTP connectivity to ALB failed"
                return $false
            }
        }
        catch {
            Write-TestFail "Error testing HTTP connectivity: $($_.Exception.Message)"
            return $false
        }
        
        # Test HTTPS connectivity if configured
        $Listeners = aws elbv2 describe-listeners --load-balancer-arn $LoadBalancerArn --query 'Listeners' --output json | ConvertFrom-Json
        $HasHTTPS = $Listeners | Where-Object { $_.Protocol -eq "HTTPS" }
        
        if ($HasHTTPS) {
            try {
                $HttpsTest = Test-NetConnection -ComputerName $ALBDnsName -Port 443 -WarningAction SilentlyContinue
                if ($HttpsTest.TcpTestSucceeded) {
                    Write-TestPass "HTTPS connectivity to ALB successful"
                }
                else {
                    Write-TestFail "HTTPS connectivity to ALB failed"
                    return $false
                }
            }
            catch {
                Write-TestFail "Error testing HTTPS connectivity: $($_.Exception.Message)"
                return $false
            }
        }
        
        return $true
    }
    catch {
        Write-TestFail "Error testing internet connectivity: $($_.Exception.Message)"
        return $false
    }
}

function Test-NATGatewayConnectivity {
    param([string]$VpcId)
    
    Write-TestStatus "Testing NAT Gateway connectivity"
    
    try {
        # Get NAT Gateways
        $NATGateways = aws ec2 describe-nat-gateways --filter "Name=vpc-id,Values=$VpcId" --query 'NatGateways[?State==`available`]' --output json | ConvertFrom-Json
        
        if ($NATGateways.Count -eq 0) {
            Write-TestFail "No available NAT Gateways found"
            return $false
        }
        
        # Get private subnets
        $PrivateSubnets = aws ec2 describe-subnets --filters "Name=vpc-id,Values=$VpcId" --query 'Subnets[?Tags[?Key==`Type` && Value==`Private`]]' --output json | ConvertFrom-Json
        
        # Check route tables for private subnets
        foreach ($Subnet in $PrivateSubnets) {
            $RouteTables = aws ec2 describe-route-tables --filters "Name=association.subnet-id,Values=$($Subnet.SubnetId)" --query 'RouteTables' --output json | ConvertFrom-Json
            
            if ($RouteTables.Count -eq 0) {
                # Check main route table
                $RouteTables = aws ec2 describe-route-tables --filters "Name=vpc-id,Values=$VpcId" "Name=association.main,Values=true" --query 'RouteTables' --output json | ConvertFrom-Json
            }
            
            $HasNATRoute = $false
            
            foreach ($RouteTable in $RouteTables) {
                foreach ($Route in $RouteTable.Routes) {
                    if ($Route.DestinationCidrBlock -eq "0.0.0.0/0" -and $Route.NatGatewayId) {
                        $HasNATRoute = $true
                        break
                    }
                }
            }
            
            if ($HasNATRoute) {
                Write-TestPass "Private subnet $($Subnet.SubnetId) has NAT Gateway route"
            }
            else {
                Write-TestFail "Private subnet $($Subnet.SubnetId) does not have NAT Gateway route"
                return $false
            }
        }
        
        return $true
    }
    catch {
        Write-TestFail "Error testing NAT Gateway connectivity: $($_.Exception.Message)"
        return $false
    }
}

function Test-SecretsManagerIntegration {
    param([string]$SecretArn, [string]$TaskDefinitionArn)
    
    Write-TestStatus "Testing Secrets Manager integration with ECS"
    
    try {
        # Check if secret exists and is accessible
        $Secret = aws secretsmanager describe-secret --secret-id $SecretArn --query '{Name:Name,Description:Description}' --output json | ConvertFrom-Json
        
        if ($Secret.Name) {
            Write-TestPass "Secrets Manager secret is accessible"
        }
        else {
            Write-TestFail "Secrets Manager secret is not accessible"
            return $false
        }
        
        # Check ECS task definition for secrets integration
        $TaskDef = aws ecs describe-task-definition --task-definition $TaskDefinitionArn --query 'taskDefinition' --output json | ConvertFrom-Json
        
        $HasSecretsIntegration = $false
        
        foreach ($Container in $TaskDef.containerDefinitions) {
            if ($Container.secrets -and $Container.secrets.Count -gt 0) {
                foreach ($SecretRef in $Container.secrets) {
                    if ($SecretRef.valueFrom -eq $SecretArn) {
                        $HasSecretsIntegration = $true
                        Write-TestPass "ECS task definition references Secrets Manager secret"
                        break
                    }
                }
            }
            
            # Also check environment variables for secrets
            if ($Container.environment -and $Container.environment.Count -gt 0) {
                foreach ($EnvVar in $Container.environment) {
                    if ($EnvVar.value -and $EnvVar.value.Contains("secretsmanager")) {
                        $HasSecretsIntegration = $true
                        Write-TestPass "ECS task definition uses Secrets Manager in environment variables"
                        break
                    }
                }
            }
        }
        
        if (-not $HasSecretsIntegration) {
            Write-TestFail "ECS task definition does not integrate with Secrets Manager"
            return $false
        }
        
        return $true
    }
    catch {
        Write-TestFail "Error testing Secrets Manager integration: $($_.Exception.Message)"
        return $false
    }
}

function Test-CloudWatchLogsIntegration {
    param([string]$TaskDefinitionArn, [string]$ClusterName)
    
    Write-TestStatus "Testing CloudWatch Logs integration"
    
    try {
        # Check ECS task definition for CloudWatch logs configuration
        $TaskDef = aws ecs describe-task-definition --task-definition $TaskDefinitionArn --query 'taskDefinition' --output json | ConvertFrom-Json
        
        $LogGroups = @()
        
        foreach ($Container in $TaskDef.containerDefinitions) {
            if ($Container.logConfiguration -and $Container.logConfiguration.logDriver -eq "awslogs") {
                $LogGroupName = $Container.logConfiguration.options."awslogs-group"
                $LogGroups += $LogGroupName
                Write-TestPass "Container $($Container.name) configured for CloudWatch Logs: $LogGroupName"
            }
            else {
                Write-TestFail "Container $($Container.name) not configured for CloudWatch Logs"
                return $false
            }
        }
        
        # Check if log groups exist
        foreach ($LogGroupName in $LogGroups) {
            try {
                $LogGroup = aws logs describe-log-groups --log-group-name-prefix $LogGroupName --query 'logGroups[0]' --output json | ConvertFrom-Json
                
                if ($LogGroup.logGroupName -eq $LogGroupName) {
                    Write-TestPass "CloudWatch Log Group exists: $LogGroupName"
                    
                    # Check retention policy
                    if ($LogGroup.retentionInDays) {
                        Write-TestPass "Log Group has retention policy: $($LogGroup.retentionInDays) days"
                    }
                    else {
                        Write-TestStatus "Log Group has no retention policy (logs kept indefinitely)"
                    }
                }
                else {
                    Write-TestFail "CloudWatch Log Group does not exist: $LogGroupName"
                    return $false
                }
            }
            catch {
                Write-TestFail "Error checking CloudWatch Log Group: $LogGroupName"
                return $false
            }
        }
        
        return $true
    }
    catch {
        Write-TestFail "Error testing CloudWatch Logs integration: $($_.Exception.Message)"
        return $false
    }
}

function Test-CrossAZRedundancy {
    param([string]$VpcId)
    
    Write-TestStatus "Testing cross-AZ redundancy"
    
    try {
        # Check VPC subnets across AZs
        $Subnets = aws ec2 describe-subnets --filters "Name=vpc-id,Values=$VpcId" --query 'Subnets' --output json | ConvertFrom-Json
        
        $AZs = $Subnets | Select-Object -ExpandProperty AvailabilityZone | Sort-Object -Unique
        
        if ($AZs.Count -ge 2) {
            Write-TestPass "Infrastructure spans multiple AZs ($($AZs.Count)): $($AZs -join ', ')"
        }
        else {
            Write-TestFail "Infrastructure does not span multiple AZs"
            return $false
        }
        
        # Check subnet distribution
        $SubnetTypes = @("Public", "Private", "Database")
        
        foreach ($Type in $SubnetTypes) {
            $TypeSubnets = $Subnets | Where-Object { $_.Tags | Where-Object { $_.Key -eq "Type" -and $_.Value -eq $Type } }
            $TypeAZs = $TypeSubnets | Select-Object -ExpandProperty AvailabilityZone | Sort-Object -Unique
            
            if ($TypeAZs.Count -ge 2) {
                Write-TestPass "$Type subnets span multiple AZs ($($TypeAZs.Count))"
            }
            else {
                Write-TestFail "$Type subnets do not span multiple AZs"
                return $false
            }
        }
        
        return $true
    }
    catch {
        Write-TestFail "Error testing cross-AZ redundancy: $($_.Exception.Message)"
        return $false
    }
}

# Main test execution
function Main {
    Write-TestStatus "Starting integration infrastructure tests for environment: $Environment"
    
    # Get infrastructure details from Terraform output
    Set-Location $TerraformDir
    terraform workspace select $Environment
    
    try {
        $TerraformOutput = terraform output -json | ConvertFrom-Json
        
        # Extract resource identifiers
        $VpcId = $TerraformOutput.vpc_id.value
        $DBInstanceId = $TerraformOutput.rds_instance_id.value
        $ClusterName = $TerraformOutput.ecs_cluster_name.value
        $ServiceName = $TerraformOutput.ecs_service_name.value
        $TaskDefinitionArn = $TerraformOutput.ecs_task_definition_arn.value
        $LoadBalancerArn = $TerraformOutput.alb_arn.value
        $SecretArn = $TerraformOutput.rds_secret_arn.value
        
        Write-TestStatus "Running integration tests for environment: $Environment"
        
        # Run integration tests
        $TestResults = @()
        $TestResults += Test-CrossAZRedundancy -VpcId $VpcId
        $TestResults += Test-VPCToRDSConnectivity -VpcId $VpcId -DBInstanceId $DBInstanceId
        $TestResults += Test-ALBToECSConnectivity -LoadBalancerArn $LoadBalancerArn -ClusterName $ClusterName -ServiceName $ServiceName
        $TestResults += Test-NATGatewayConnectivity -VpcId $VpcId
        $TestResults += Test-SecretsManagerIntegration -SecretArn $SecretArn -TaskDefinitionArn $TaskDefinitionArn
        $TestResults += Test-CloudWatchLogsIntegration -TaskDefinitionArn $TaskDefinitionArn -ClusterName $ClusterName
        
        # Skip internet connectivity test in CI/CD environments
        if ($env:CI -ne "true") {
            $TestResults += Test-InternetConnectivity -LoadBalancerArn $LoadBalancerArn
        }
        else {
            Write-TestStatus "Skipping internet connectivity test in CI/CD environment"
        }
        
        # Calculate results
        $PassedTests = ($TestResults | Where-Object { $_ -eq $true }).Count
        $TotalTests = $TestResults.Count
        $FailedTests = $TotalTests - $PassedTests
        
        Write-TestStatus "Test Results: $PassedTests/$TotalTests passed"
        
        if ($FailedTests -eq 0) {
            Write-TestPass "All integration infrastructure tests passed"
            return @{
                Status = "PASS"
                PassedTests = $PassedTests
                TotalTests = $TotalTests
                Details = "All infrastructure components are properly integrated"
            }
        }
        else {
            Write-TestFail "$FailedTests integration infrastructure tests failed"
            throw "Integration infrastructure tests failed"
        }
    }
    catch {
        Write-TestFail "Integration infrastructure test execution failed: $($_.Exception.Message)"
        throw
    }
}

# Execute tests
Main