#!/bin/bash

# Terraform deployment script
# Usage: ./deploy.sh <environment> <action>
# Example: ./deploy.sh dev plan
# Example: ./deploy.sh prod apply

set -e

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TERRAFORM_DIR="$(dirname "${SCRIPT_DIR}")"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
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

print_header() {
    echo -e "${BLUE}[DEPLOY]${NC} $1"
}

# Function to show usage
show_usage() {
    echo "Usage: $0 <environment> <action>"
    echo
    echo "Environments: dev, staging, prod"
    echo "Actions: init, plan, apply, destroy, validate, fmt, show"
    echo
    echo "Examples:"
    echo "  $0 dev init     # Initialize Terraform for dev environment"
    echo "  $0 dev plan     # Plan changes for dev environment"
    echo "  $0 dev apply    # Apply changes for dev environment"
    echo "  $0 prod destroy # Destroy prod environment (use with caution!)"
    echo
}

# Function to validate inputs
validate_inputs() {
    local env=$1
    local action=$2
    
    # Validate environment
    if [[ ! "$env" =~ ^(dev|staging|prod)$ ]]; then
        print_error "Invalid environment: $env"
        show_usage
        exit 1
    fi
    
    # Validate action
    if [[ ! "$action" =~ ^(init|plan|apply|destroy|validate|fmt|show)$ ]]; then
        print_error "Invalid action: $action"
        show_usage
        exit 1
    fi
    
    # Check if required files exist
    local backend_config="${TERRAFORM_DIR}/backend-configs/${env}.hcl"
    local var_file="${TERRAFORM_DIR}/environments/${env}.tfvars"
    
    if [[ ! -f "$backend_config" ]]; then
        print_error "Backend config file not found: $backend_config"
        exit 1
    fi
    
    if [[ ! -f "$var_file" ]]; then
        print_error "Variables file not found: $var_file"
        exit 1
    fi
}

# Function to initialize Terraform
terraform_init() {
    local env=$1
    local backend_config="${TERRAFORM_DIR}/backend-configs/${env}.hcl"
    
    print_header "Initializing Terraform for environment: $env"
    
    cd "$TERRAFORM_DIR"
    
    # Initialize with backend config
    terraform init -backend-config="$backend_config" -reconfigure
    
    # Create or select workspace
    if terraform workspace list | grep -q "$env"; then
        print_status "Selecting existing workspace: $env"
        terraform workspace select "$env"
    else
        print_status "Creating new workspace: $env"
        terraform workspace new "$env"
    fi
    
    print_status "Terraform initialization completed for environment: $env"
}

# Function to run terraform plan
terraform_plan() {
    local env=$1
    local var_file="${TERRAFORM_DIR}/environments/${env}.tfvars"
    
    print_header "Planning Terraform changes for environment: $env"
    
    cd "$TERRAFORM_DIR"
    
    # Ensure we're in the correct workspace
    terraform workspace select "$env"
    
    # Run plan
    terraform plan -var-file="$var_file" -out="${env}.tfplan"
    
    print_status "Terraform plan completed for environment: $env"
    print_status "Plan file saved as: ${env}.tfplan"
}

# Function to run terraform apply
terraform_apply() {
    local env=$1
    local var_file="${TERRAFORM_DIR}/environments/${env}.tfvars"
    
    print_header "Applying Terraform changes for environment: $env"
    
    cd "$TERRAFORM_DIR"
    
    # Ensure we're in the correct workspace
    terraform workspace select "$env"
    
    # Check if plan file exists
    if [[ -f "${env}.tfplan" ]]; then
        print_status "Applying from plan file: ${env}.tfplan"
        terraform apply "${env}.tfplan"
        rm -f "${env}.tfplan"
    else
        print_warning "No plan file found. Running apply with auto-approve disabled."
        terraform apply -var-file="$var_file"
    fi
    
    print_status "Terraform apply completed for environment: $env"
}

# Function to run terraform destroy
terraform_destroy() {
    local env=$1
    local var_file="${TERRAFORM_DIR}/environments/${env}.tfvars"
    
    print_header "Destroying Terraform resources for environment: $env"
    print_warning "This will destroy all resources in the $env environment!"
    
    # Extra confirmation for production
    if [[ "$env" == "prod" ]]; then
        print_warning "You are about to destroy PRODUCTION resources!"
        read -p "Type 'yes' to confirm destruction of PRODUCTION environment: " confirm
        if [[ "$confirm" != "yes" ]]; then
            print_status "Destruction cancelled."
            exit 0
        fi
    fi
    
    cd "$TERRAFORM_DIR"
    
    # Ensure we're in the correct workspace
    terraform workspace select "$env"
    
    # Run destroy
    terraform destroy -var-file="$var_file"
    
    print_status "Terraform destroy completed for environment: $env"
}

# Function to validate Terraform configuration
terraform_validate() {
    print_header "Validating Terraform configuration"
    
    cd "$TERRAFORM_DIR"
    
    terraform validate
    
    print_status "Terraform validation completed"
}

# Function to format Terraform files
terraform_fmt() {
    print_header "Formatting Terraform files"
    
    cd "$TERRAFORM_DIR"
    
    terraform fmt -recursive
    
    print_status "Terraform formatting completed"
}

# Function to show Terraform state
terraform_show() {
    local env=$1
    
    print_header "Showing Terraform state for environment: $env"
    
    cd "$TERRAFORM_DIR"
    
    # Ensure we're in the correct workspace
    terraform workspace select "$env"
    
    terraform show
}

# Main execution
main() {
    local env=$1
    local action=$2
    
    # Check if correct number of arguments provided
    if [[ $# -ne 2 ]]; then
        print_error "Invalid number of arguments"
        show_usage
        exit 1
    fi
    
    # Validate inputs
    validate_inputs "$env" "$action"
    
    # Check if AWS CLI is configured
    if ! aws sts get-caller-identity >/dev/null 2>&1; then
        print_error "AWS CLI is not configured. Please run 'aws configure' first."
        exit 1
    fi
    
    # Execute the requested action
    case "$action" in
        init)
            terraform_init "$env"
            ;;
        plan)
            terraform_plan "$env"
            ;;
        apply)
            terraform_apply "$env"
            ;;
        destroy)
            terraform_destroy "$env"
            ;;
        validate)
            terraform_validate
            ;;
        fmt)
            terraform_fmt
            ;;
        show)
            terraform_show "$env"
            ;;
        *)
            print_error "Unknown action: $action"
            show_usage
            exit 1
            ;;
    esac
}

# Run main function with all arguments
main "$@"