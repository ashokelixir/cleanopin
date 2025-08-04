# Enhanced PowerShell script to initialize Terraform backend infrastructure
# This script creates the S3 bucket and DynamoDB table for Terraform state management
# with enhanced security, encryption, and monitoring

param(
    [string]$Region = "us-east-1",
    [string[]]$Environments = @("dev", "staging", "prod"),
    [switch]$EnableVersioning = $true,
    [switch]$EnableEncryption = $true,
    [switch]$EnableLogging = $true,
    [switch]$EnableMonitoring = $true,
    [string]$KMSKeyAlias = "alias/terraform-state"
)

# Configuration
$ProjectName = "cleanarch-template"
$AwsRegion = $Region

# Function to write colored output
function Write-Status {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# Function to create or get KMS key for encryption
function New-TerraformKMSKey {
    Write-Status "Setting up KMS key for Terraform state encryption"
    
    try {
        # Check if KMS key alias already exists
        $ExistingKey = aws kms describe-key --key-id $KMSKeyAlias --query 'KeyMetadata.KeyId' --output text 2>$null
        
        if ($ExistingKey -and $ExistingKey -ne "None") {
            Write-Status "KMS key already exists: $KMSKeyAlias"
            return $ExistingKey
        }
    }
    catch {
        # Key doesn't exist, continue with creation
    }
    
    # Create KMS key
    $KeyPolicy = @{
        Version = "2012-10-17"
        Statement = @(
            @{
                Sid = "Enable IAM User Permissions"
                Effect = "Allow"
                Principal = @{
                    AWS = "arn:aws:iam::$(aws sts get-caller-identity --query Account --output text):root"
                }
                Action = "kms:*"
                Resource = "*"
            },
            @{
                Sid = "Allow Terraform to use the key"
                Effect = "Allow"
                Principal = @{
                    AWS = "arn:aws:iam::$(aws sts get-caller-identity --query Account --output text):root"
                }
                Action = @(
                    "kms:Encrypt",
                    "kms:Decrypt",
                    "kms:ReEncrypt*",
                    "kms:GenerateDataKey*",
                    "kms:DescribeKey"
                )
                Resource = "*"
            }
        )
    } | ConvertTo-Json -Depth 4 -Compress
    
    $KeyId = aws kms create-key --policy $KeyPolicy --description "Terraform state encryption key" --query 'KeyMetadata.KeyId' --output text
    
    # Create alias
    aws kms create-alias --alias-name $KMSKeyAlias --target-key-id $KeyId
    
    Write-Status "KMS key created successfully: $KMSKeyAlias"
    return $KeyId
}

# Function to create S3 bucket for Terraform state with enhanced security
function New-TerraformStateBucket {
    param([string]$Environment, [string]$KMSKeyId)
    
    $BucketName = "$ProjectName-terraform-state-$Environment"
    
    Write-Status "Creating S3 bucket: $BucketName"
    
    # Check if bucket already exists
    try {
        aws s3api head-bucket --bucket $BucketName 2>$null
        Write-Warning "S3 bucket $BucketName already exists, updating configuration..."
    }
    catch {
        # Create bucket
        try {
            if ($AwsRegion -eq "us-east-1") {
                aws s3api create-bucket --bucket $BucketName --region $AwsRegion
            }
            else {
                aws s3api create-bucket --bucket $BucketName --region $AwsRegion --create-bucket-configuration "LocationConstraint=$AwsRegion"
            }
            Write-Status "S3 bucket created: $BucketName"
        }
        catch {
            Write-Error "Failed to create S3 bucket: $($_.Exception.Message)"
            throw
        }
    }
    
    # Configure bucket settings
    try {
        # Enable versioning
        if ($EnableVersioning) {
            aws s3api put-bucket-versioning --bucket $BucketName --versioning-configuration "Status=Enabled"
            Write-Status "Versioning enabled for bucket: $BucketName"
        }
        
        # Enable server-side encryption with KMS
        if ($EnableEncryption) {
            $EncryptionConfig = @{
                Rules = @(
                    @{
                        ApplyServerSideEncryptionByDefault = @{
                            SSEAlgorithm = "aws:kms"
                            KMSMasterKeyID = $KMSKeyId
                        }
                        BucketKeyEnabled = $true
                    }
                )
            } | ConvertTo-Json -Depth 3 -Compress
            
            aws s3api put-bucket-encryption --bucket $BucketName --server-side-encryption-configuration $EncryptionConfig
            Write-Status "KMS encryption enabled for bucket: $BucketName"
        }
        
        # Block public access
        aws s3api put-public-access-block --bucket $BucketName --public-access-block-configuration "BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true"
        Write-Status "Public access blocked for bucket: $BucketName"
        
        # Enable access logging
        if ($EnableLogging) {
            $LoggingBucket = "$ProjectName-terraform-logs-$Environment"
            New-TerraformLoggingBucket -BucketName $LoggingBucket
            
            $LoggingConfig = @{
                LoggingEnabled = @{
                    TargetBucket = $LoggingBucket
                    TargetPrefix = "access-logs/"
                }
            } | ConvertTo-Json -Depth 2 -Compress
            
            aws s3api put-bucket-logging --bucket $BucketName --bucket-logging-status $LoggingConfig
            Write-Status "Access logging enabled for bucket: $BucketName"
        }
        
        # Configure lifecycle policy
        $LifecycleConfig = @{
            Rules = @(
                @{
                    ID = "terraform-state-lifecycle"
                    Status = "Enabled"
                    NoncurrentVersionTransitions = @(
                        @{
                            NoncurrentDays = 30
                            StorageClass = "STANDARD_IA"
                        },
                        @{
                            NoncurrentDays = 90
                            StorageClass = "GLACIER"
                        }
                    )
                    NoncurrentVersionExpiration = @{
                        NoncurrentDays = 365
                    }
                }
            )
        } | ConvertTo-Json -Depth 4 -Compress
        
        aws s3api put-bucket-lifecycle-configuration --bucket $BucketName --lifecycle-configuration $LifecycleConfig
        Write-Status "Lifecycle policy configured for bucket: $BucketName"
        
        # Set bucket policy for additional security
        $BucketPolicy = @{
            Version = "2012-10-17"
            Statement = @(
                @{
                    Sid = "DenyInsecureConnections"
                    Effect = "Deny"
                    Principal = "*"
                    Action = "s3:*"
                    Resource = @(
                        "arn:aws:s3:::$BucketName",
                        "arn:aws:s3:::$BucketName/*"
                    )
                    Condition = @{
                        Bool = @{
                            "aws:SecureTransport" = "false"
                        }
                    }
                }
            )
        } | ConvertTo-Json -Depth 4 -Compress
        
        aws s3api put-bucket-policy --bucket $BucketName --policy $BucketPolicy
        Write-Status "Security policy applied to bucket: $BucketName"
        
        Write-Status "S3 bucket $BucketName configured successfully"
    }
    catch {
        Write-Error "Failed to configure S3 bucket: $($_.Exception.Message)"
        throw
    }
}

# Function to create logging bucket
function New-TerraformLoggingBucket {
    param([string]$BucketName)
    
    try {
        aws s3api head-bucket --bucket $BucketName 2>$null
        return
    }
    catch {
        # Create logging bucket
        if ($AwsRegion -eq "us-east-1") {
            aws s3api create-bucket --bucket $BucketName --region $AwsRegion
        }
        else {
            aws s3api create-bucket --bucket $BucketName --region $AwsRegion --create-bucket-configuration "LocationConstraint=$AwsRegion"
        }
        
        # Block public access
        aws s3api put-public-access-block --bucket $BucketName --public-access-block-configuration "BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true"
        
        Write-Status "Logging bucket created: $BucketName"
    }
}

# Function to create DynamoDB table for state locking with enhanced features
function New-TerraformLockTable {
    param([string]$Environment, [string]$KMSKeyId)
    
    $TableName = "$ProjectName-terraform-locks-$Environment"
    
    Write-Status "Creating DynamoDB table: $TableName"
    
    # Check if table already exists
    try {
        $ExistingTable = aws dynamodb describe-table --table-name $TableName --region $AwsRegion 2>$null
        if ($ExistingTable) {
            Write-Warning "DynamoDB table $TableName already exists, updating configuration..."
            Update-TerraformLockTable -TableName $TableName -KMSKeyId $KMSKeyId
            return
        }
    }
    catch {
        # Table doesn't exist, continue with creation
    }
    
    # Create table with enhanced configuration
    try {
        $CreateTableParams = @(
            "--table-name", $TableName,
            "--attribute-definitions", "AttributeName=LockID,AttributeType=S",
            "--key-schema", "AttributeName=LockID,KeyType=HASH",
            "--billing-mode", "PAY_PER_REQUEST",
            "--region", $AwsRegion
        )
        
        # Add encryption if enabled
        if ($EnableEncryption) {
            $CreateTableParams += @(
                "--sse-specification", "Enabled=true,SSEType=KMS,KMSMasterKeyId=$KMSKeyId"
            )
        }
        
        aws dynamodb create-table @CreateTableParams
        
        # Wait for table to be active
        Write-Status "Waiting for DynamoDB table to be active..."
        aws dynamodb wait table-exists --table-name $TableName --region $AwsRegion
        
        # Enable point-in-time recovery
        aws dynamodb update-continuous-backups --table-name $TableName --point-in-time-recovery-specification "PointInTimeRecoveryEnabled=true" --region $AwsRegion
        Write-Status "Point-in-time recovery enabled for table: $TableName"
        
        # Add tags
        $Tags = @(
            @{ Key = "Project"; Value = $ProjectName },
            @{ Key = "Environment"; Value = $Environment },
            @{ Key = "Purpose"; Value = "TerraformStateLocking" },
            @{ Key = "ManagedBy"; Value = "Terraform" }
        )
        
        $TagsJson = $Tags | ConvertTo-Json -Compress
        $AccountId = aws sts get-caller-identity --query Account --output text
        aws dynamodb tag-resource --resource-arn "arn:aws:dynamodb:${AwsRegion}:${AccountId}:table/$TableName" --tags $TagsJson --region $AwsRegion
        Write-Status "Tags applied to table: $TableName"
        
        # Enable CloudWatch monitoring if requested
        if ($EnableMonitoring) {
            Enable-DynamoDBMonitoring -TableName $TableName
        }
        
        Write-Status "DynamoDB table $TableName created successfully"
    }
    catch {
        Write-Error "Failed to create DynamoDB table: $($_.Exception.Message)"
        throw
    }
}

# Function to update existing DynamoDB table
function Update-TerraformLockTable {
    param([string]$TableName, [string]$KMSKeyId)
    
    try {
        # Enable point-in-time recovery if not already enabled
        $BackupStatus = aws dynamodb describe-continuous-backups --table-name $TableName --region $AwsRegion --query 'ContinuousBackupsDescription.PointInTimeRecoveryDescription.PointInTimeRecoveryStatus' --output text
        
        if ($BackupStatus -ne "ENABLED") {
            aws dynamodb update-continuous-backups --table-name $TableName --point-in-time-recovery-specification "PointInTimeRecoveryEnabled=true" --region $AwsRegion
            Write-Status "Point-in-time recovery enabled for existing table: $TableName"
        }
        
        Write-Status "DynamoDB table $TableName updated successfully"
    }
    catch {
        Write-Warning "Failed to update DynamoDB table: $($_.Exception.Message)"
    }
}

# Function to enable DynamoDB monitoring
function Enable-DynamoDBMonitoring {
    param([string]$TableName)
    
    try {
        # Create CloudWatch alarms for the table
        $AlarmConfigs = @(
            @{
                Name = "$TableName-HighReadThrottles"
                MetricName = "ReadThrottles"
                Threshold = 5
                ComparisonOperator = "GreaterThanThreshold"
            },
            @{
                Name = "$TableName-HighWriteThrottles"
                MetricName = "WriteThrottles"
                Threshold = 5
                ComparisonOperator = "GreaterThanThreshold"
            }
        )
        
        foreach ($Config in $AlarmConfigs) {
            aws cloudwatch put-metric-alarm `
                --alarm-name $Config.Name `
                --alarm-description "Monitor $($Config.MetricName) for $TableName" `
                --metric-name $Config.MetricName `
                --namespace "AWS/DynamoDB" `
                --statistic "Sum" `
                --period 300 `
                --threshold $Config.Threshold `
                --comparison-operator $Config.ComparisonOperator `
                --evaluation-periods 2 `
                --dimensions "Name=TableName,Value=$TableName" `
                --region $AwsRegion
        }
        
        Write-Status "CloudWatch monitoring enabled for table: $TableName"
    }
    catch {
        Write-Warning "Failed to enable monitoring for table: $($_.Exception.Message)"
    }
}

# Function to validate backend infrastructure
function Test-BackendInfrastructure {
    param([string]$Environment, [string]$KMSKeyId)
    
    Write-Status "Validating backend infrastructure for environment: $Environment"
    
    $BucketName = "$ProjectName-terraform-state-$Environment"
    $TableName = "$ProjectName-terraform-locks-$Environment"
    
    # Test S3 bucket
    try {
        $BucketInfo = aws s3api head-bucket --bucket $BucketName 2>$null
        Write-Status "S3 bucket validation passed: $BucketName"
        
        # Test encryption
        $EncryptionInfo = aws s3api get-bucket-encryption --bucket $BucketName --query 'ServerSideEncryptionConfiguration.Rules[0].ApplyServerSideEncryptionByDefault' --output json | ConvertFrom-Json
        if ($EncryptionInfo.SSEAlgorithm -eq "aws:kms") {
            Write-Status "S3 bucket encryption validation passed: KMS"
        }
        
        # Test versioning
        $VersioningInfo = aws s3api get-bucket-versioning --bucket $BucketName --query 'Status' --output text
        if ($VersioningInfo -eq "Enabled") {
            Write-Status "S3 bucket versioning validation passed"
        }
    }
    catch {
        Write-Error "S3 bucket validation failed: $($_.Exception.Message)"
        return $false
    }
    
    # Test DynamoDB table
    try {
        $TableInfo = aws dynamodb describe-table --table-name $TableName --region $AwsRegion --query 'Table.TableStatus' --output text
        if ($TableInfo -eq "ACTIVE") {
            Write-Status "DynamoDB table validation passed: $TableName"
        }
        
        # Test encryption
        $EncryptionInfo = aws dynamodb describe-table --table-name $TableName --region $AwsRegion --query 'Table.SSEDescription.Status' --output text
        if ($EncryptionInfo -eq "ENABLED") {
            Write-Status "DynamoDB table encryption validation passed"
        }
    }
    catch {
        Write-Error "DynamoDB table validation failed: $($_.Exception.Message)"
        return $false
    }
    
    Write-Status "Backend infrastructure validation completed for environment: $Environment"
    return $true
}

# Function to generate backend configuration summary
function Show-BackendSummary {
    Write-Status "Backend Infrastructure Summary"
    Write-Status "=============================="
    
    foreach ($Environment in $Environments) {
        $BucketName = "$ProjectName-terraform-state-$Environment"
        $TableName = "$ProjectName-terraform-locks-$Environment"
        
        Write-Status "Environment: $Environment"
        Write-Status "  S3 Bucket: $BucketName"
        Write-Status "  DynamoDB Table: $TableName"
        Write-Status "  Region: $AwsRegion"
        Write-Status "  Encryption: $(if ($EnableEncryption) { 'Enabled (KMS)' } else { 'Disabled' })"
        Write-Status "  Versioning: $(if ($EnableVersioning) { 'Enabled' } else { 'Disabled' })"
        Write-Status "  Logging: $(if ($EnableLogging) { 'Enabled' } else { 'Disabled' })"
        Write-Status "  Monitoring: $(if ($EnableMonitoring) { 'Enabled' } else { 'Disabled' })"
        Write-Host ""
    }
    
    Write-Status "Usage Instructions:"
    Write-Status "==================="
    foreach ($Environment in $Environments) {
        Write-Host "# Initialize $Environment environment:"
        Write-Host "terraform init -backend-config=backend-configs/$Environment.hcl"
        Write-Host "terraform workspace select $Environment || terraform workspace new $Environment"
        Write-Host "terraform plan -var-file=environments/$Environment.tfvars"
        Write-Host ""
    }
}

# Main execution
function Main {
    Write-Status "Initializing enhanced Terraform backend infrastructure for $ProjectName"
    Write-Status "Region: $AwsRegion"
    Write-Status "Environments: $($Environments -join ', ')"
    Write-Status "Features: Encryption=$EnableEncryption, Versioning=$EnableVersioning, Logging=$EnableLogging, Monitoring=$EnableMonitoring"
    
    # Check if AWS CLI is configured
    try {
        $CallerIdentity = aws sts get-caller-identity | ConvertFrom-Json
        Write-Status "AWS Account: $($CallerIdentity.Account)"
        Write-Status "AWS User: $($CallerIdentity.Arn)"
    }
    catch {
        Write-Error "AWS CLI is not configured. Please run 'aws configure' first."
        exit 1
    }
    
    # Create KMS key if encryption is enabled
    $KMSKeyId = $null
    if ($EnableEncryption) {
        $KMSKeyId = New-TerraformKMSKey
    }
    
    # Create backend infrastructure for each environment
    foreach ($Environment in $Environments) {
        Write-Status "Setting up backend for environment: $Environment"
        
        try {
            New-TerraformStateBucket -Environment $Environment -KMSKeyId $KMSKeyId
            New-TerraformLockTable -Environment $Environment -KMSKeyId $KMSKeyId
            
            # Validate the infrastructure
            $ValidationResult = Test-BackendInfrastructure -Environment $Environment -KMSKeyId $KMSKeyId
            
            if ($ValidationResult) {
                Write-Status "Backend setup completed successfully for environment: $Environment"
            }
            else {
                Write-Error "Backend validation failed for environment: $Environment"
            }
        }
        catch {
            Write-Error "Failed to set up backend for environment ${Environment}: $($_.Exception.Message)"
            exit 1
        }
        
        Write-Host ""
    }
    
    Write-Status "All backend infrastructure created successfully!"
    Show-BackendSummary
}

# Run main function
try {
    Main
}
catch {
    Write-Error "Script execution failed: $($_.Exception.Message)"
    exit 1
}