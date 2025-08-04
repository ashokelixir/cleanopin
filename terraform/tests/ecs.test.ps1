# ECS Infrastructure Tests
# Tests ECS Fargate cluster and service configuration

param(
    [Parameter(Mandatory=$true)]
    [string]$Environment
)

# Test configuration
$TestName = "ECS Infrastructure Test"
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

function Test-ECSCluster {
    param([string]$ClusterName)
    
    Write-TestStatus "Testing ECS cluster: $ClusterName"
    
    try {
        $Cluster = aws ecs describe-clusters --clusters $ClusterName --query 'clusters[0]' --output json | ConvertFrom-Json
        
        if ($Cluster.status -eq "ACTIVE") {
            Write-TestPass "ECS cluster is active"
            
            # Check capacity providers
            if ($Cluster.capacityProviders -contains "FARGATE") {
                Write-TestPass "Fargate capacity provider is configured"
            }
            else {
                Write-TestFail "Fargate capacity provider is not configured"
                return $false
            }
            
            # Check cluster settings
            $ContainerInsights = $Cluster.settings | Where-Object { $_.name -eq "containerInsights" }
            if ($ContainerInsights -and $ContainerInsights.value -eq "enabled") {
                Write-TestPass "Container Insights is enabled"
            }
            else {
                Write-TestFail "Container Insights is not enabled"
                return $false
            }
            
            return $true
        }
        else {
            Write-TestFail "ECS cluster is not active. Status: $($Cluster.status)"
            return $false
        }
    }
    catch {
        Write-TestFail "Error checking ECS cluster: $($_.Exception.Message)"
        return $false
    }
}

function Test-ECSService {
    param([string]$ClusterName, [string]$ServiceName)
    
    Write-TestStatus "Testing ECS service: $ServiceName"
    
    try {
        $Service = aws ecs describe-services --cluster $ClusterName --services $ServiceName --query 'services[0]' --output json | ConvertFrom-Json
        
        if ($Service.status -eq "ACTIVE") {
            Write-TestPass "ECS service is active"
            
            # Check desired vs running count
            if ($Service.runningCount -eq $Service.desiredCount) {
                Write-TestPass "Service has desired number of running tasks ($($Service.runningCount))"
            }
            else {
                Write-TestFail "Service running count ($($Service.runningCount)) does not match desired count ($($Service.desiredCount))"
                return $false
            }
            
            # Check launch type
            if ($Service.launchType -eq "FARGATE") {
                Write-TestPass "Service is using Fargate launch type"
            }
            else {
                Write-TestFail "Service is not using Fargate launch type: $($Service.launchType)"
                return $false
            }
            
            # Check load balancer configuration
            if ($Service.loadBalancers -and $Service.loadBalancers.Count -gt 0) {
                Write-TestPass "Service has load balancer configured"
                
                $LoadBalancer = $Service.loadBalancers[0]
                if ($LoadBalancer.targetGroupArn) {
                    Write-TestPass "Target group is configured: $($LoadBalancer.targetGroupArn)"
                }
                else {
                    Write-TestFail "Target group is not configured"
                    return $false
                }
            }
            else {
                Write-TestFail "Service does not have load balancer configured"
                return $false
            }
            
            return $true
        }
        else {
            Write-TestFail "ECS service is not active. Status: $($Service.status)"
            return $false
        }
    }
    catch {
        Write-TestFail "Error checking ECS service: $($_.Exception.Message)"
        return $false
    }
}

function Test-ECSTaskDefinition {
    param([string]$TaskDefinitionArn)
    
    Write-TestStatus "Testing ECS task definition: $TaskDefinitionArn"
    
    try {
        $TaskDef = aws ecs describe-task-definition --task-definition $TaskDefinitionArn --query 'taskDefinition' --output json | ConvertFrom-Json
        
        if ($TaskDef.status -eq "ACTIVE") {
            Write-TestPass "Task definition is active"
            
            # Check network mode
            if ($TaskDef.networkMode -eq "awsvpc") {
                Write-TestPass "Task definition uses awsvpc network mode"
            }
            else {
                Write-TestFail "Task definition does not use awsvpc network mode: $($TaskDef.networkMode)"
                return $false
            }
            
            # Check requires compatibility
            if ($TaskDef.requiresCompatibilities -contains "FARGATE") {
                Write-TestPass "Task definition is compatible with Fargate"
            }
            else {
                Write-TestFail "Task definition is not compatible with Fargate"
                return $false
            }
            
            # Check CPU and memory configuration
            if ($TaskDef.cpu -and $TaskDef.memory) {
                Write-TestPass "Task definition has CPU ($($TaskDef.cpu)) and memory ($($TaskDef.memory)) configured"
            }
            else {
                Write-TestFail "Task definition is missing CPU or memory configuration"
                return $false
            }
            
            # Check container definitions
            if ($TaskDef.containerDefinitions -and $TaskDef.containerDefinitions.Count -gt 0) {
                Write-TestPass "Task definition has container definitions ($($TaskDef.containerDefinitions.Count))"
                
                $Container = $TaskDef.containerDefinitions[0]
                
                # Check essential container
                if ($Container.essential) {
                    Write-TestPass "Primary container is marked as essential"
                }
                else {
                    Write-TestFail "Primary container is not marked as essential"
                    return $false
                }
                
                # Check port mappings
                if ($Container.portMappings -and $Container.portMappings.Count -gt 0) {
                    Write-TestPass "Container has port mappings configured"
                }
                else {
                    Write-TestFail "Container does not have port mappings configured"
                    return $false
                }
                
                # Check logging configuration
                if ($Container.logConfiguration -and $Container.logConfiguration.logDriver -eq "awslogs") {
                    Write-TestPass "Container has CloudWatch logging configured"
                }
                else {
                    Write-TestFail "Container does not have CloudWatch logging configured"
                    return $false
                }
            }
            else {
                Write-TestFail "Task definition has no container definitions"
                return $false
            }
            
            return $true
        }
        else {
            Write-TestFail "Task definition is not active. Status: $($TaskDef.status)"
            return $false
        }
    }
    catch {
        Write-TestFail "Error checking ECS task definition: $($_.Exception.Message)"
        return $false
    }
}

function Test-ApplicationLoadBalancer {
    param([string]$LoadBalancerArn)
    
    Write-TestStatus "Testing Application Load Balancer: $LoadBalancerArn"
    
    try {
        $ALB = aws elbv2 describe-load-balancers --load-balancer-arns $LoadBalancerArn --query 'LoadBalancers[0]' --output json | ConvertFrom-Json
        
        if ($ALB.State.Code -eq "active") {
            Write-TestPass "Application Load Balancer is active"
            
            # Check scheme
            if ($ALB.Scheme -eq "internet-facing") {
                Write-TestPass "Load balancer is internet-facing"
            }
            else {
                Write-TestFail "Load balancer is not internet-facing: $($ALB.Scheme)"
                return $false
            }
            
            # Check availability zones
            if ($ALB.AvailabilityZones.Count -ge 2) {
                Write-TestPass "Load balancer spans multiple AZs ($($ALB.AvailabilityZones.Count))"
            }
            else {
                Write-TestFail "Load balancer does not span multiple AZs ($($ALB.AvailabilityZones.Count))"
                return $false
            }
            
            # Check listeners
            $Listeners = aws elbv2 describe-listeners --load-balancer-arn $LoadBalancerArn --query 'Listeners' --output json | ConvertFrom-Json
            
            if ($Listeners.Count -gt 0) {
                Write-TestPass "Load balancer has listeners configured ($($Listeners.Count))"
                
                # Check for HTTPS listener
                $HTTPSListener = $Listeners | Where-Object { $_.Protocol -eq "HTTPS" }
                if ($HTTPSListener) {
                    Write-TestPass "HTTPS listener is configured"
                }
                else {
                    Write-TestFail "HTTPS listener is not configured"
                    return $false
                }
            }
            else {
                Write-TestFail "Load balancer has no listeners configured"
                return $false
            }
            
            return $true
        }
        else {
            Write-TestFail "Application Load Balancer is not active. State: $($ALB.State.Code)"
            return $false
        }
    }
    catch {
        Write-TestFail "Error checking Application Load Balancer: $($_.Exception.Message)"
        return $false
    }
}

function Test-TargetGroup {
    param([string]$TargetGroupArn)
    
    Write-TestStatus "Testing Target Group: $TargetGroupArn"
    
    try {
        $TargetGroup = aws elbv2 describe-target-groups --target-group-arns $TargetGroupArn --query 'TargetGroups[0]' --output json | ConvertFrom-Json
        
        # Check target type
        if ($TargetGroup.TargetType -eq "ip") {
            Write-TestPass "Target group uses IP target type (required for Fargate)"
        }
        else {
            Write-TestFail "Target group does not use IP target type: $($TargetGroup.TargetType)"
            return $false
        }
        
        # Check health check configuration
        if ($TargetGroup.HealthCheckPath) {
            Write-TestPass "Health check path is configured: $($TargetGroup.HealthCheckPath)"
        }
        else {
            Write-TestFail "Health check path is not configured"
            return $false
        }
        
        # Check target health
        $TargetHealth = aws elbv2 describe-target-health --target-group-arn $TargetGroupArn --query 'TargetHealthDescriptions' --output json | ConvertFrom-Json
        
        if ($TargetHealth.Count -gt 0) {
            $HealthyTargets = $TargetHealth | Where-Object { $_.TargetHealth.State -eq "healthy" }
            $UnhealthyTargets = $TargetHealth | Where-Object { $_.TargetHealth.State -ne "healthy" }
            
            Write-TestStatus "Target health: $($HealthyTargets.Count) healthy, $($UnhealthyTargets.Count) unhealthy"
            
            if ($HealthyTargets.Count -gt 0) {
                Write-TestPass "At least one target is healthy"
            }
            else {
                Write-TestFail "No healthy targets found"
                return $false
            }
        }
        else {
            Write-TestFail "No targets registered in target group"
            return $false
        }
        
        return $true
    }
    catch {
        Write-TestFail "Error checking Target Group: $($_.Exception.Message)"
        return $false
    }
}

function Test-ECSAutoScaling {
    param([string]$ClusterName, [string]$ServiceName)
    
    Write-TestStatus "Testing ECS Auto Scaling configuration"
    
    try {
        # Check if auto scaling is configured
        $ScalableTargets = aws application-autoscaling describe-scalable-targets --service-namespace ecs --resource-ids "service/$ClusterName/$ServiceName" --query 'ScalableTargets' --output json | ConvertFrom-Json
        
        if ($ScalableTargets.Count -gt 0) {
            Write-TestPass "Auto scaling is configured for ECS service"
            
            $ScalableTarget = $ScalableTargets[0]
            
            # Check min and max capacity
            if ($ScalableTarget.MinCapacity -ge 1 -and $ScalableTarget.MaxCapacity -gt $ScalableTarget.MinCapacity) {
                Write-TestPass "Auto scaling capacity is properly configured (min: $($ScalableTarget.MinCapacity), max: $($ScalableTarget.MaxCapacity))"
            }
            else {
                Write-TestFail "Auto scaling capacity is not properly configured"
                return $false
            }
            
            # Check scaling policies
            $ScalingPolicies = aws application-autoscaling describe-scaling-policies --service-namespace ecs --resource-id "service/$ClusterName/$ServiceName" --query 'ScalingPolicies' --output json | ConvertFrom-Json
            
            if ($ScalingPolicies.Count -gt 0) {
                Write-TestPass "Auto scaling policies are configured ($($ScalingPolicies.Count))")
            }
            else {
                Write-TestFail "No auto scaling policies configured"
                return $false
            }
            
            return $true
        }
        else {
            Write-TestFail "Auto scaling is not configured for ECS service"
            return $false
        }
    }
    catch {
        Write-TestFail "Error checking ECS Auto Scaling: $($_.Exception.Message)"
        return $false
    }
}

# Main test execution
function Main {
    Write-TestStatus "Starting ECS infrastructure tests for environment: $Environment"
    
    # Get ECS details from Terraform output
    Set-Location $TerraformDir
    terraform workspace select $Environment
    
    try {
        $TerraformOutput = terraform output -json | ConvertFrom-Json
        $ClusterName = $TerraformOutput.ecs_cluster_name.value
        $ServiceName = $TerraformOutput.ecs_service_name.value
        $TaskDefinitionArn = $TerraformOutput.ecs_task_definition_arn.value
        $LoadBalancerArn = $TerraformOutput.alb_arn.value
        $TargetGroupArn = $TerraformOutput.alb_target_group_arn.value
        
        if (-not $ClusterName) {
            throw "ECS cluster name not found in Terraform output"
        }
        
        Write-TestStatus "Testing ECS cluster: $ClusterName"
        
        # Run tests
        $TestResults = @()
        $TestResults += Test-ECSCluster -ClusterName $ClusterName
        $TestResults += Test-ECSService -ClusterName $ClusterName -ServiceName $ServiceName
        $TestResults += Test-ECSTaskDefinition -TaskDefinitionArn $TaskDefinitionArn
        $TestResults += Test-ApplicationLoadBalancer -LoadBalancerArn $LoadBalancerArn
        $TestResults += Test-TargetGroup -TargetGroupArn $TargetGroupArn
        $TestResults += Test-ECSAutoScaling -ClusterName $ClusterName -ServiceName $ServiceName
        
        # Calculate results
        $PassedTests = ($TestResults | Where-Object { $_ -eq $true }).Count
        $TotalTests = $TestResults.Count
        $FailedTests = $TotalTests - $PassedTests
        
        Write-TestStatus "Test Results: $PassedTests/$TotalTests passed"
        
        if ($FailedTests -eq 0) {
            Write-TestPass "All ECS infrastructure tests passed"
            return @{
                Status = "PASS"
                PassedTests = $PassedTests
                TotalTests = $TotalTests
                Details = "All ECS infrastructure components are properly configured"
            }
        }
        else {
            Write-TestFail "$FailedTests ECS infrastructure tests failed"
            throw "ECS infrastructure tests failed"
        }
    }
    catch {
        Write-TestFail "ECS infrastructure test execution failed: $($_.Exception.Message)"
        throw
    }
}

# Execute tests
Main