param(
    [Parameter(Mandatory=$true)]
    [string]$Environment = "dev",
    
    [Parameter(Mandatory=$false)]
    [string]$Region = "ap-south-1",
    
    [Parameter(Mandatory=$false)]
    [string]$AccountId = "456752697984",
    
    [Parameter(Mandatory=$false)]
    [string]$RepositoryName = "cleanarchtemplate",
    
    [Parameter(Mandatory=$false)]
    [string]$Tag = "latest"
)

function Write-Status {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "🔧 $Message" -ForegroundColor Blue
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}
}

# Construct ECR repository URI
$EcrUri = "$AccountId.dkr.ecr.$Region.amazonaws.com/$RepositoryName"
$ImageTag = "${EcrUri}:$Tag"

Write-Info "Building and pushing .NET application to ECR"
Write-Info "Environment: $Environment"
Write-Info "Region: $Region"
Write-Info "Repository: $EcrUri"
Write-Info "Tag: $Tag"
Write-Host ""

# Check if Docker is running
Write-Info "Checking Docker status..."
try {
    docker version | Out-Null
    Write-Status "Docker is running"
} catch {
    Write-Error "Docker is not running or not installed"
    exit 1
}

# Check if AWS CLI is installed and configured
Write-Info "Checking AWS CLI..."
try {
    $awsAccount = aws sts get-caller-identity --query Account --output text
    Write-Status "AWS CLI configured for account: $awsAccount"
} catch {
    Write-Error "AWS CLI is not configured or not installed"
    exit 1
}

# Login to ECR
Write-Info "Logging in to ECR..."
try {
    $ecrDomain = $EcrUri.Split('/')[0]
    aws ecr get-login-password --region $Region | docker login --username AWS --password-stdin $ecrDomain
    Write-Status "Successfully logged in to ECR"
} catch {
    Write-Error "Failed to login to ECR"
    exit 1
}

# Build the Docker image
Write-Info "Building Docker image..."
try {
    docker build -t "cleanarch-template:$Tag" .
    Write-Status "Docker image built successfully"
} catch {
    Write-Error "Failed to build Docker image"
    exit 1
}

# Tag the image for ECR
Write-Info "Tagging image for ECR..."
try {
    docker tag "cleanarch-template:$Tag" $ImageTag
    Write-Status "Image tagged successfully: $ImageTag"
} catch {
    Write-Error "Failed to tag image"
    exit 1
}

# Push the image to ECR
Write-Info "Pushing image to ECR..."
try {
    docker push $ImageTag
    Write-Status "Image pushed successfully to ECR"
} catch {
    Write-Error "Failed to push image to ECR"
    exit 1
}

# Verify the image was pushed
Write-Info "Verifying image in ECR..."
try {
    $images = aws ecr describe-images --repository-name $RepositoryName --region $Region --image-ids "imageTag=$Tag" --output table
    Write-Status "Image verification:"
    Write-Host $images
} catch {
    Write-Warning "Could not verify image in ECR, but push appeared successful"
}

Write-Host ""
Write-Status "Build and push completed successfully!"
Write-Info "Next steps:"
Write-Info "1. Update your ECS service to use the new image"
$clusterName = "cleanarch-template-$Environment-cluster"
$serviceName = "cleanarch-template-$Environment-service"
Write-Info "2. Run the following command:"
Write-Host "   aws ecs update-service --cluster $clusterName --service $serviceName --force-new-deployment --region $Region" -ForegroundColor Cyan