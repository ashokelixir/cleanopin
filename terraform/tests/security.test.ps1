# Security Infrastructure Tests
# Tests security configurations across all AWS resources

param(
    [Parameter(Mandatory=$true)]
    [string]$Environment
)

# Test configuration
$TestName = "Security Infrastructure Test"
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

function Test-S3BucketSecurity {
    param([string]$BucketName)
    
    Write-TestStatus "Testing S3 bucket security: $BucketName"
    
    try {
        # Check public access block
        $PublicAccessBlock = aws s3api get-public-access-block --bucket $BucketName --query 'PublicAccessBlockConfiguration' --output json | ConvertFrom-Json
        
        if ($PublicAccessBlock.BlockPublicAcls -and $PublicAccessBlock.IgnorePublicAcls -and 
            $PublicAccessBlock.BlockPublicPolicy -and $PublicAccessBlock.RestrictPublicBuckets) {
            Write-TestPass "S3 bucket has proper public access block configuration"
        }
        else {
            Write-TestFail "S3 bucket public access block is not properly configured"
            return $false
        }
        
        # Check encryption
        try {
            $Encryption = aws s3api get-bucket-encryption --bucket $BucketName --query 'ServerSideEncryptionConfiguration.Rules[0].ApplyServerSideEncryptionByDefault' --output json | ConvertFrom-Json
            
            if ($Encryption.SSEAlgorithm -eq "aws:kms") {
                Write-TestPass "S3 bucket has KMS encryption enabled"
            }
            else {
                Write-TestFail "S3 bucket does not have KMS encryption enabled"
                return $false
            }
        }
        catch {
            Write-TestFail "S3 bucket encryption is not configured"
            return $false
        }
        
        # Check versioning
        $Versioning = aws s3api get-bucket-versioning --bucket $BucketName --query 'Status' --output text
        if ($Versioning -eq "Enabled") {
            Write-TestPass "S3 bucket versioning is enabled"
        }
        else {
            Write-TestFail "S3 bucket versioning is not enabled"
            return $false
        }
        
        return $true
    }
    catch {
        Write-TestFail "Error checking S3 bucket security: $($_.Exception.Message)"
        return $false
    }
}

function Test-SecurityGroupRules {
    param([string]$VpcId)
    
    Write-TestStatus "Testing security group rules for VPC: $VpcId"
    
    try {
        $SecurityGroups = aws ec2 describe-security-groups --filters "Name=vpc-id,Values=$VpcId" --query 'SecurityGroups' --output json | ConvertFrom-Json
        
        $SecurityIssues = @()
        
        foreach ($SG in $SecurityGroups) {
            # Check for overly permissive inbound rules
            foreach ($Rule in $SG.IpPermissions) {
                foreach ($IpRange in $Rule.IpRanges) {
                    if ($IpRange.CidrIp -eq "0.0.0.0/0") {
                        # Allow HTTP/HTTPS from anywhere for ALB
                        if ($SG.GroupName -like "*alb*" -and ($Rule.FromPort -eq 80 -or $Rule.FromPort -eq 443)) {
                            continue
                        }
                        
                        $SecurityIssues += "Security group $($SG.GroupName) allows inbound traffic from 0.0.0.0/0 on port $($Rule.FromPort)"
                    }
                }
            }
            
            # Check for overly permissive outbound rules (except for NAT gateways and ALBs)
            if ($SG.GroupName -notlike "*nat*" -and $SG.GroupName -notlike "*alb*") {
                foreach ($Rule in $SG.IpPermissionsEgress) {
                    foreach ($IpRange in $Rule.IpRanges) {
                        if ($IpRange.CidrIp -eq "0.0.0.0/0" -and $Rule.IpProtocol -eq "-1") {
                            $SecurityIssues += "Security group $($SG.GroupName) allows all outbound traffic to 0.0.0.0/0"
                        }
                    }
                }
            }
        }
        
        if ($SecurityIssues.Count -eq 0) {
            Write-TestPass "Security group rules are properly configured"
            return $true
        }
        else {
            foreach ($Issue in $SecurityIssues) {
                Write-TestFail $Issue
            }
            return $false
        }
    }
    catch {
        Write-TestFail "Error checking security group rules: $($_.Exception.Message)"
        return $false
    }
}

function Test-RDSEncryption {
    param([string]$DBInstanceId)
    
    Write-TestStatus "Testing RDS encryption: $DBInstanceId"
    
    try {
        $DBInstance = aws rds describe-db-instances --db-instance-identifier $DBInstanceId --query 'DBInstances[0]' --output json | ConvertFrom-Json
        
        if ($DBInstance.StorageEncrypted) {
            Write-TestPass "RDS instance has storage encryption enabled"
            
            # Check encryption key
            if ($DBInstance.KmsKeyId) {
                Write-TestPass "RDS instance uses KMS key for encryption"
            }
            else {
                Write-TestFail "RDS instance encryption key not specified"
                return $false
            }
            
            return $true
        }
        else {
            Write-TestFail "RDS instance does not have storage encryption enabled"
            return $false
        }
    }
    catch {
        Write-TestFail "Error checking RDS encryption: $($_.Exception.Message)"
        return $false
    }
}

function Test-ECSTaskSecurity {
    param([string]$TaskDefinitionArn)
    
    Write-TestStatus "Testing ECS task security: $TaskDefinitionArn"
    
    try {
        $TaskDef = aws ecs describe-task-definition --task-definition $TaskDefinitionArn --query 'taskDefinition' --output json | ConvertFrom-Json
        
        # Check if task uses execution role
        if ($TaskDef.executionRoleArn) {
            Write-TestPass "ECS task has execution role configured"
        }
        else {
            Write-TestFail "ECS task does not have execution role configured"
            return $false
        }
        
        # Check if task uses task role
        if ($TaskDef.taskRoleArn) {
            Write-TestPass "ECS task has task role configured"
        }
        else {
            Write-TestFail "ECS task does not have task role configured"
            return $false
        }
        
        # Check container security
        foreach ($Container in $TaskDef.containerDefinitions) {
            # Check if container runs as non-root (if user is specified)
            if ($Container.user -and $Container.user -ne "0" -and $Container.user -ne "root") {
                Write-TestPass "Container $($Container.name) runs as non-root user"
            }
            elseif (-not $Container.user) {
                Write-TestStatus "Container $($Container.name) user not specified (using image default)"
            }
            else {
                Write-TestFail "Container $($Container.name) runs as root user"
                return $false
            }
            
            # Check if container is read-only (if specified)
            if ($Container.readonlyRootFilesystem) {
                Write-TestPass "Container $($Container.name) has read-only root filesystem"
            }
            else {
                Write-TestStatus "Container $($Container.name) does not have read-only root filesystem"
            }
        }
        
        return $true
    }
    catch {
        Write-TestFail "Error checking ECS task security: $($_.Exception.Message)"
        return $false
    }
}

function Test-ALBSecurity {
    param([string]$LoadBalancerArn)
    
    Write-TestStatus "Testing ALB security: $LoadBalancerArn"
    
    try {
        # Check ALB attributes
        $Attributes = aws elbv2 describe-load-balancer-attributes --load-balancer-arn $LoadBalancerArn --query 'Attributes' --output json | ConvertFrom-Json
        
        $SecurityAttributes = @{
            "deletion_protection.enabled" = $false
            "access_logs.s3.enabled" = $false
        }
        
        foreach ($Attr in $Attributes) {
            if ($SecurityAttributes.ContainsKey($Attr.Key)) {
                $SecurityAttributes[$Attr.Key] = $Attr.Value -eq "true"
            }
        }
        
        # Check deletion protection for production
        if ($Environment -eq "prod") {
            if ($SecurityAttributes["deletion_protection.enabled"]) {
                Write-TestPass "ALB deletion protection is enabled for production"
            }
            else {
                Write-TestFail "ALB deletion protection should be enabled for production"
                return $false
            }
        }
        
        # Check access logs
        if ($SecurityAttributes["access_logs.s3.enabled"]) {
            Write-TestPass "ALB access logs are enabled"
        }
        else {
            Write-TestFail "ALB access logs are not enabled"
            return $false
        }
        
        # Check listeners for HTTPS
        $Listeners = aws elbv2 describe-listeners --load-balancer-arn $LoadBalancerArn --query 'Listeners' --output json | ConvertFrom-Json
        
        $HasHTTPS = $false
        $HasHTTPRedirect = $false
        
        foreach ($Listener in $Listeners) {
            if ($Listener.Protocol -eq "HTTPS") {
                $HasHTTPS = $true
                
                # Check SSL policy
                if ($Listener.SslPolicy -and $Listener.SslPolicy -like "ELBSecurityPolicy-TLS-1-2*") {
                    Write-TestPass "ALB HTTPS listener uses secure SSL policy"
                }
                else {
                    Write-TestFail "ALB HTTPS listener does not use secure SSL policy"
                    return $false
                }
            }
            elseif ($Listener.Protocol -eq "HTTP") {
                # Check if HTTP redirects to HTTPS
                foreach ($Action in $Listener.DefaultActions) {
                    if ($Action.Type -eq "redirect" -and $Action.RedirectConfig.Protocol -eq "HTTPS") {
                        $HasHTTPRedirect = $true
                    }
                }
            }
        }
        
        if ($HasHTTPS) {
            Write-TestPass "ALB has HTTPS listener configured"
        }
        else {
            Write-TestFail "ALB does not have HTTPS listener configured"
            return $false
        }
        
        if ($HasHTTPRedirect) {
            Write-TestPass "ALB redirects HTTP to HTTPS"
        }
        else {
            Write-TestStatus "ALB HTTP to HTTPS redirect not configured"
        }
        
        return $true
    }
    catch {
        Write-TestFail "Error checking ALB security: $($_.Exception.Message)"
        return $false
    }
}

function Test-IAMRoleSecurity {
    Write-TestStatus "Testing IAM role security"
    
    try {
        # Get all roles created by Terraform (assuming they have a specific tag or naming pattern)
        $Roles = aws iam list-roles --query 'Roles[?contains(RoleName, `cleanarch`) || contains(RoleName, `terraform`)]' --output json | ConvertFrom-Json
        
        $SecurityIssues = @()
        
        foreach ($Role in $Roles) {
            # Get role policy document
            $AssumeRolePolicy = $Role.AssumeRolePolicyDocument | ConvertFrom-Json
            
            # Check for overly permissive assume role policies
            foreach ($Statement in $AssumeRolePolicy.Statement) {
                if ($Statement.Principal -eq "*") {
                    $SecurityIssues += "Role $($Role.RoleName) allows any principal to assume the role"
                }
            }
            
            # Get attached policies
            $AttachedPolicies = aws iam list-attached-role-policies --role-name $Role.RoleName --query 'AttachedPolicies' --output json | ConvertFrom-Json
            
            foreach ($Policy in $AttachedPolicies) {
                # Check for AWS managed policies that might be overly permissive
                if ($Policy.PolicyArn -like "arn:aws:iam::aws:policy/*") {
                    $PolicyName = $Policy.PolicyName
                    
                    # Flag potentially dangerous policies
                    $DangerousPolicies = @("PowerUserAccess", "IAMFullAccess", "AdministratorAccess")
                    if ($PolicyName -in $DangerousPolicies) {
                        $SecurityIssues += "Role $($Role.RoleName) has overly permissive policy: $PolicyName"
                    }
                }
            }
        }
        
        if ($SecurityIssues.Count -eq 0) {
            Write-TestPass "IAM roles are properly configured"
            return $true
        }
        else {
            foreach ($Issue in $SecurityIssues) {
                Write-TestFail $Issue
            }
            return $false
        }
    }
    catch {
        Write-TestFail "Error checking IAM role security: $($_.Exception.Message)"
        return $false
    }
}

function Test-SecretsManagerSecurity {
    Write-TestStatus "Testing Secrets Manager security"
    
    try {
        # Get secrets created by Terraform
        $Secrets = aws secretsmanager list-secrets --query 'SecretList[?contains(Name, `cleanarch`) || contains(Name, `rds`)]' --output json | ConvertFrom-Json
        
        foreach ($Secret in $Secrets) {
            # Check encryption
            if ($Secret.KmsKeyId) {
                Write-TestPass "Secret $($Secret.Name) is encrypted with KMS"
            }
            else {
                Write-TestFail "Secret $($Secret.Name) is not encrypted with KMS"
                return $false
            }
            
            # Check automatic rotation (for RDS secrets)
            if ($Secret.Name -like "*rds*") {
                if ($Secret.RotationEnabled) {
                    Write-TestPass "RDS secret $($Secret.Name) has automatic rotation enabled"
                }
                else {
                    Write-TestFail "RDS secret $($Secret.Name) does not have automatic rotation enabled"
                    return $false
                }
            }
        }
        
        return $true
    }
    catch {
        Write-TestFail "Error checking Secrets Manager security: $($_.Exception.Message)"
        return $false
    }
}

# Main test execution
function Main {
    Write-TestStatus "Starting security infrastructure tests for environment: $Environment"
    
    # Get infrastructure details from Terraform output
    Set-Location $TerraformDir
    terraform workspace select $Environment
    
    try {
        $TerraformOutput = terraform output -json | ConvertFrom-Json
        
        # Extract resource identifiers
        $VpcId = $TerraformOutput.vpc_id.value
        $DBInstanceId = $TerraformOutput.rds_instance_id.value
        $TaskDefinitionArn = $TerraformOutput.ecs_task_definition_arn.value
        $LoadBalancerArn = $TerraformOutput.alb_arn.value
        
        # Get S3 buckets (state bucket)
        $StateBucketName = "cleanarch-template-terraform-state-$Environment"
        
        Write-TestStatus "Running security tests for environment: $Environment"
        
        # Run security tests
        $TestResults = @()
        $TestResults += Test-S3BucketSecurity -BucketName $StateBucketName
        $TestResults += Test-SecurityGroupRules -VpcId $VpcId
        $TestResults += Test-RDSEncryption -DBInstanceId $DBInstanceId
        $TestResults += Test-ECSTaskSecurity -TaskDefinitionArn $TaskDefinitionArn
        $TestResults += Test-ALBSecurity -LoadBalancerArn $LoadBalancerArn
        $TestResults += Test-IAMRoleSecurity
        $TestResults += Test-SecretsManagerSecurity
        
        # Calculate results
        $PassedTests = ($TestResults | Where-Object { $_ -eq $true }).Count
        $TotalTests = $TestResults.Count
        $FailedTests = $TotalTests - $PassedTests
        
        Write-TestStatus "Test Results: $PassedTests/$TotalTests passed"
        
        if ($FailedTests -eq 0) {
            Write-TestPass "All security infrastructure tests passed"
            return @{
                Status = "PASS"
                PassedTests = $PassedTests
                TotalTests = $TotalTests
                Details = "All security configurations are properly implemented"
            }
        }
        else {
            Write-TestFail "$FailedTests security infrastructure tests failed"
            throw "Security infrastructure tests failed"
        }
    }
    catch {
        Write-TestFail "Security infrastructure test execution failed: $($_.Exception.Message)"
        throw
    }
}

# Execute tests
Main