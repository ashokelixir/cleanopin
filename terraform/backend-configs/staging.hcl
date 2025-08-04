# Staging environment backend configuration
bucket         = "cleanarch-template-terraform-state-staging"
key            = "staging/terraform.tfstate"
region         = "us-east-1"
dynamodb_table = "cleanarch-template-terraform-locks-staging"
encrypt        = true

# Workspace configuration
workspace_key_prefix = "env"