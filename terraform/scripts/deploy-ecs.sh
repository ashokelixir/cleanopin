#!/bin/bash

# Deploy ECS Fargate infrastructure with Terraform
#
# This script deploys the ECS Fargate infrastructure including:
# - ECS Cluster with Fargate capacity providers
# - Application Load Balancer with target groups and health checks
# - ECS Task Definition with proper resource allocation
# - ECS Service with auto-scaling and deployment configuration
# - CloudWatch log groups for container logging

set -e

# Default values
REGION="us-east-1"
PLAN_ONLY=false
AUTO_APPROVE=false

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}✅ $1${NC}"
}

print_info() {
    echo -e "${BLUE}🔧 $1${NC}"
}

print_warning() {
    echo -e "${YELLOW}⚠️  $1${NC}"
}

print_error() {
    echo -e "${RED}❌ $1${NC}"
}

print_header() {
    echo -e "${CYAN}$1${NC}"
}

# Function to show usage
usage() {
    cat << EOF
Usage: $0 -e ENVIRONMENT -i CONTAINER_IMAGE [OPTIONS]

Deploy ECS Fargate infrastructure with Terraform

Required Arguments:
    -e, --environment    Environment to deploy to (dev, staging, prod)
    -i, --image         Docker container image to deploy

Optional Arguments:
    -r, --region        AWS region to deploy to (default: us-east-1)
    -p, --plan          Run terraform plan only without applying changes
    -a, --auto-approve  Auto approve terraform apply without confirmation
    -h, --help          Show this help message

Examples:
    $0 -e dev -i myapp:latest
    $0 -e prod -r us-west-2 -i myapp:v1.0.0 -a
EOF
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -i|--image)
            CONTAINER_IMAGE="$2"
            shift 2
            ;;
        -r|--region)
            REGION="$2"
            shift 2
            ;;
        -p|--plan)
            PLAN_ONLY=true
            shift
            ;;
        -a|--auto-approve)
            AUTO_APPROVE=true
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            echo "Unknown option $1"
            usage
            exit 1
            ;;
    esac
done

# Validate required arguments
if [[ -z "$ENVIRONMENT" ]]; then
    print_error "Environment is required"
    usage
    exit 1
fi

if [[ -z "$CONTAINER_IMAGE" ]]; then
    print_error "Container image is required"
    usage
    exit 1
fi

# Validate environment
if [[ ! "$ENVIRONMENT" =~ ^(dev|staging|prod)$ ]]; then
    print_error "Environment must be one of: dev, staging, prod"
    exit 1
fi

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TERRAFORM_DIR="$(dirname "$SCRIPT_DIR")"

echo -e "${GREEN}🚀 Deploying ECS Fargate Infrastructure${NC}"
echo -e "${YELLOW}Environment: $ENVIRONMENT${NC}"
echo -e "${YELLOW}Region: $REGION${NC}"
echo -e "${YELLOW}Container Image: $CONTAINER_IMAGE${NC}"
echo ""

# Change to terraform directory
cd "$TERRAFORM_DIR"

# Check if terraform is installed
if ! command -v terraform &> /dev/null; then
    print_error "Terraform is not installed or not in PATH"
    exit 1
fi

# Check if AWS CLI is installed
if ! command -v aws &> /dev/null; then
    print_error "AWS CLI is not installed or not in PATH"
    exit 1
fi

# Verify AWS credentials
print_info "Verifying AWS credentials..."
if ! aws sts get-caller-identity > /dev/null 2>&1; then
    print_error "AWS credentials not configured or invalid"
    exit 1
fi

AWS_ACCOUNT=$(aws sts get-caller-identity --query Account --output text)
print_status "AWS Account: $AWS_ACCOUNT"
echo ""

# Initialize Terraform
print_info "Initializing Terraform..."
BACKEND_CONFIG="backend-configs/$ENVIRONMENT.hcl"

if [[ -f "$BACKEND_CONFIG" ]]; then
    terraform init -backend-config="$BACKEND_CONFIG" -upgrade
else
    print_warning "Backend config file not found: $BACKEND_CONFIG"
    terraform init -upgrade
fi

print_status "Terraform initialized successfully"
echo ""

# Select or create workspace
print_info "Setting up Terraform workspace..."
if ! terraform workspace select "$ENVIRONMENT" 2>/dev/null; then
    print_warning "Creating new workspace: $ENVIRONMENT"
    terraform workspace new "$ENVIRONMENT"
fi

print_status "Using workspace: $ENVIRONMENT"
echo ""

# Prepare terraform variables
TF_VARS_FILE="environments/$ENVIRONMENT.tfvars"
TF_VARS=(
    "-var" "environment=$ENVIRONMENT"
    "-var" "aws_region=$REGION"
    "-var" "container_image=$CONTAINER_IMAGE"
)

if [[ -f "$TF_VARS_FILE" ]]; then
    TF_VARS+=("-var-file=$TF_VARS_FILE")
    print_info "Using variables file: $TF_VARS_FILE"
else
    print_warning "Variables file not found: $TF_VARS_FILE"
fi

# Run terraform plan
print_info "Running Terraform plan..."
terraform plan "${TF_VARS[@]}" -out="tfplan-$ENVIRONMENT"

print_status "Terraform plan completed successfully"
echo ""

# Apply changes if not plan-only
if [[ "$PLAN_ONLY" == "false" ]]; then
    print_info "Applying Terraform changes..."
    
    if [[ "$AUTO_APPROVE" == "true" ]]; then
        terraform apply -auto-approve "tfplan-$ENVIRONMENT"
    else
        terraform apply "tfplan-$ENVIRONMENT"
    fi
    
    print_status "ECS Fargate infrastructure deployed successfully!"
    echo ""

    # Display important outputs
    print_header "📊 Infrastructure Outputs:"
    print_header "========================="
    
    if ALB_DNS_NAME=$(terraform output -raw alb_dns_name 2>/dev/null); then
        echo -e "${CYAN}🌐 Load Balancer DNS: $ALB_DNS_NAME${NC}"
    fi
    
    if ECS_CLUSTER_NAME=$(terraform output -raw ecs_cluster_name 2>/dev/null); then
        echo -e "${CYAN}🐳 ECS Cluster: $ECS_CLUSTER_NAME${NC}"
    fi
    
    if ECS_SERVICE_NAME=$(terraform output -raw ecs_service_name 2>/dev/null); then
        echo -e "${CYAN}⚙️  ECS Service: $ECS_SERVICE_NAME${NC}"
    fi
    
    if LOG_GROUP_NAME=$(terraform output -raw ecs_log_group_name 2>/dev/null); then
        echo -e "${CYAN}📝 Log Group: $LOG_GROUP_NAME${NC}"
    fi
    
    echo ""
    print_status "Deployment completed successfully!"
    echo ""
    echo -e "${YELLOW}Next steps:${NC}"
    echo -e "${WHITE}1. Update your CI/CD pipeline to push images to ECR${NC}"
    echo -e "${WHITE}2. Configure your domain to point to the ALB DNS name${NC}"
    echo -e "${WHITE}3. Monitor the ECS service and application logs${NC}"
    echo -e "${WHITE}4. Set up CloudWatch alarms and notifications${NC}"
else
    print_status "Plan completed. Remove -p/--plan flag to apply changes."
fi

# Clean up plan file
if [[ -f "tfplan-$ENVIRONMENT" ]]; then
    rm -f "tfplan-$ENVIRONMENT"
fi