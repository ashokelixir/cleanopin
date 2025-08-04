#!/bin/bash

# Script to initialize Terraform backend infrastructure
# This script creates the S3 bucket and DynamoDB table for Terraform state management

set -e

# Configuration
PROJECT_NAME="cleanarch-template"
AWS_REGION="us-east-1"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Function to create S3 bucket for Terraform state
create_s3_bucket() {
    local env=$1
    local bucket_name="${PROJECT_NAME}-terraform-state-${env}"
    
    print_status "Creating S3 bucket: ${bucket_name}"
    
    # Check if bucket already exists
    if aws s3api head-bucket --bucket "${bucket_name}" 2>/dev/null; then
        print_warning "S3 bucket ${bucket_name} already exists"
        return 0
    fi
    
    # Create bucket
    aws s3api create-bucket \
        --bucket "${bucket_name}" \
        --region "${AWS_REGION}" \
        --create-bucket-configuration LocationConstraint="${AWS_REGION}" 2>/dev/null || \
    aws s3api create-bucket \
        --bucket "${bucket_name}" \
        --region "${AWS_REGION}" 2>/dev/null
    
    # Enable versioning
    aws s3api put-bucket-versioning \
        --bucket "${bucket_name}" \
        --versioning-configuration Status=Enabled
    
    # Enable server-side encryption
    aws s3api put-bucket-encryption \
        --bucket "${bucket_name}" \
        --server-side-encryption-configuration '{
            "Rules": [
                {
                    "ApplyServerSideEncryptionByDefault": {
                        "SSEAlgorithm": "AES256"
                    }
                }
            ]
        }'
    
    # Block public access
    aws s3api put-public-access-block \
        --bucket "${bucket_name}" \
        --public-access-block-configuration \
        BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true
    
    print_status "S3 bucket ${bucket_name} created successfully"
}

# Function to create DynamoDB table for state locking
create_dynamodb_table() {
    local env=$1
    local table_name="${PROJECT_NAME}-terraform-locks-${env}"
    
    print_status "Creating DynamoDB table: ${table_name}"
    
    # Check if table already exists
    if aws dynamodb describe-table --table-name "${table_name}" 2>/dev/null; then
        print_warning "DynamoDB table ${table_name} already exists"
        return 0
    fi
    
    # Create table
    aws dynamodb create-table \
        --table-name "${table_name}" \
        --attribute-definitions AttributeName=LockID,AttributeType=S \
        --key-schema AttributeName=LockID,KeyType=HASH \
        --provisioned-throughput ReadCapacityUnits=5,WriteCapacityUnits=5 \
        --region "${AWS_REGION}"
    
    # Wait for table to be active
    print_status "Waiting for DynamoDB table to be active..."
    aws dynamodb wait table-exists --table-name "${table_name}" --region "${AWS_REGION}"
    
    print_status "DynamoDB table ${table_name} created successfully"
}

# Main execution
main() {
    print_status "Initializing Terraform backend infrastructure for ${PROJECT_NAME}"
    
    # Check if AWS CLI is configured
    if ! aws sts get-caller-identity >/dev/null 2>&1; then
        print_error "AWS CLI is not configured. Please run 'aws configure' first."
        exit 1
    fi
    
    # Create backend infrastructure for each environment
    for env in dev staging prod; do
        print_status "Setting up backend for environment: ${env}"
        create_s3_bucket "${env}"
        create_dynamodb_table "${env}"
        print_status "Backend setup completed for environment: ${env}"
        echo
    done
    
    print_status "All backend infrastructure created successfully!"
    print_status "You can now initialize Terraform with:"
    echo
    echo "  terraform init -backend-config=backend-configs/dev.hcl"
    echo "  terraform workspace new dev"
    echo "  terraform plan -var-file=environments/dev.tfvars"
    echo
}

# Run main function
main "$@"