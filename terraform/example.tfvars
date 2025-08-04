# Example Terraform variables file
# Copy this file to environments/<environment>.tfvars and customize for your environment

# Environment configuration
environment = "dev"
aws_region  = "us-east-1"

# Project configuration
project_name = "cleanarch-template"
owner        = "your-team-name"

# Network configuration
vpc_cidr           = "10.0.0.0/16"
availability_zones = ["us-east-1a", "us-east-1b", "us-east-1c"]

# Database configuration
db_instance_class        = "db.t3.micro"
db_allocated_storage     = 20
db_max_allocated_storage = 100

# Cache configuration
redis_node_type       = "cache.t3.micro"
redis_num_cache_nodes = 1

# ECS configuration
ecs_task_cpu      = 512
ecs_task_memory   = 1024
ecs_desired_count = 2

# Application configuration
app_port          = 8080
health_check_path = "/health"