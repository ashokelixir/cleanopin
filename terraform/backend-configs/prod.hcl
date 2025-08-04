# Production environment backend configuration
bucket         = "cleanarch-template-terraform-state-prod"
key            = "prod/terraform.tfstate"
region         = "us-east-1"
dynamodb_table = "cleanarch-template-terraform-locks-prod"
encrypt        = true

# Workspace configuration
workspace_key_prefix = "env"