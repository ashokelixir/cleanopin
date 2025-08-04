# Main Terraform configuration
terraform {
  required_version = ">= 1.5"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }

  backend "s3" {
    # Backend configuration will be provided via backend config file
    # or command line arguments during terraform init
  }
}

# Configure the AWS Provider
provider "aws" {
  region = var.aws_region

  default_tags {
    tags = local.common_tags
  }
}

# Data source for current AWS account
data "aws_caller_identity" "current" {}

# Data source for available AZs
data "aws_availability_zones" "available" {
  state = "available"
}

# VPC Module
module "vpc" {
  source = "./modules/vpc"

  vpc_name              = local.vpc_name
  vpc_cidr              = local.vpc_cidr
  availability_zones    = var.availability_zones
  public_subnet_cidrs   = local.public_subnet_cidrs
  private_subnet_cidrs  = local.private_subnet_cidrs
  database_subnet_cidrs = local.database_subnet_cidrs
  public_subnet_name    = local.public_subnet_name
  private_subnet_name   = local.private_subnet_name
  database_subnet_name  = local.database_subnet_name
  aws_region            = var.aws_region

  tags = local.common_tags
}

# Security Groups Module
module "security_groups" {
  source = "./modules/security-groups"

  name_prefix             = local.name_prefix
  vpc_id                  = module.vpc.vpc_id
  vpc_cidr_block          = module.vpc.vpc_cidr_block
  app_port                = var.app_port
  allowed_ssh_cidr_blocks = var.allowed_ssh_cidr_blocks

  tags = local.common_tags
}

# RDS PostgreSQL Module
module "rds" {
  source = "./modules/rds"

  name_prefix         = local.name_prefix
  database_subnet_ids = module.vpc.database_subnet_ids
  security_group_ids  = [module.security_groups.rds_security_group_id]

  # Database configuration
  database_name     = local.db_name
  database_username = local.db_username
  database_port     = local.db_port
  postgres_version  = var.postgres_version

  # Instance configuration
  instance_class        = local.db_config.instance_class
  allocated_storage     = local.db_config.allocated_storage
  max_allocated_storage = var.db_max_allocated_storage

  # Backup configuration
  backup_retention_period = local.db_config.backup_retention_period
  backup_window           = var.db_backup_window

  # Maintenance configuration
  maintenance_window         = var.db_maintenance_window
  auto_minor_version_upgrade = true

  # High availability
  multi_az = local.db_config.multi_az

  # Monitoring configuration
  monitoring_interval                   = local.db_config.monitoring_interval
  performance_insights_enabled          = local.db_config.performance_insights_enabled
  performance_insights_retention_period = local.db_config.performance_insights_retention_period

  # Security configuration
  deletion_protection = local.db_config.deletion_protection

  # Secrets Manager configuration
  secret_name            = local.db_secret_name
  secret_recovery_window = var.environment == "prod" ? 30 : 7

  # CloudWatch alarms (can be extended with SNS topics later)
  alarm_actions = []

  tags = local.common_tags

  depends_on = [
    module.vpc,
    module.security_groups
  ]
}

# Application Secrets Module
module "secrets" {
  source = "./modules/secrets"

  name_prefix = local.name_prefix
  environment = var.environment

  # JWT Configuration
  jwt_secret_key                       = var.jwt_secret_key
  jwt_issuer                          = var.jwt_issuer
  jwt_audience                        = var.jwt_audience
  jwt_access_token_expiration_minutes = var.jwt_access_token_expiration_minutes
  jwt_refresh_token_expiration_days   = var.jwt_refresh_token_expiration_days

  # External API Keys
  datadog_api_key = var.datadog_api_key
  seq_api_key     = var.seq_api_key
  aws_access_key  = var.aws_access_key
  aws_secret_key  = var.aws_secret_key
  redis_password  = var.redis_password

  # Application Configuration
  encryption_key         = var.encryption_key
  cors_allowed_origins   = var.cors_allowed_origins
  swagger_enabled        = var.swagger_enabled

  # Recovery window
  recovery_window_in_days = var.environment == "prod" ? 30 : 7

  tags = local.common_tags

  depends_on = [
    module.vpc
  ]
}

# Application Load Balancer Module
module "alb" {
  source = "./modules/alb"

  name_prefix                = local.name_prefix
  account_id                 = local.account_id
  alb_name                   = "${local.name_prefix}-alb"
  vpc_id                     = module.vpc.vpc_id
  public_subnet_ids          = module.vpc.public_subnet_ids
  alb_security_group_id      = module.security_groups.alb_security_group_id
  target_port                = var.app_port
  health_check_path          = var.health_check_path
  certificate_arn            = var.certificate_arn
  enable_deletion_protection = local.current_env_config.enable_deletion_protection
  log_retention_days         = var.alb_log_retention_days
  alarm_actions              = []

  tags = local.common_tags

  depends_on = [
    module.vpc,
    module.security_groups
  ]
}

# IAM Module
module "iam" {
  source = "./modules/iam"

  name_prefix = local.name_prefix
  environment = var.environment

  # Secrets Manager ARNs
  secrets_manager_arns = concat([
    module.rds.secret_arn,
  ], module.secrets.all_secret_arns)

  # SQS Queue ARNs (will be populated when SQS module is implemented)
  sqs_queue_arns = [
    # Add SQS queue ARNs when SQS module is implemented
  ]

  # CI/CD Configuration
  cicd_account_ids    = var.cicd_account_ids
  cicd_external_id    = var.cicd_external_id
  ecr_repository_arns = var.ecr_repository_arns

  # Optional features
  enable_xray       = var.enable_xray
  enable_s3_access  = var.enable_s3_access
  s3_bucket_arns    = var.s3_bucket_arns
  enable_sns_access = var.enable_sns_access
  sns_topic_arns    = var.sns_topic_arns

  # RDS Configuration
  enable_rds_access  = var.enable_rds_access
  rds_db_instance_id = var.enable_rds_access ? module.rds.db_instance_id : ""
  rds_db_username    = var.enable_rds_access ? local.db_username : ""

  tags = local.common_tags

  depends_on = [
    module.rds
  ]
}

# ECS Fargate Module
module "ecs" {
  source = "./modules/ecs"

  name_prefix           = local.name_prefix
  aws_region            = var.aws_region
  cluster_name          = "${local.name_prefix}-cluster"
  task_family           = "${local.name_prefix}-task"
  task_cpu              = local.current_env_config.ecs_task_cpu
  task_memory           = local.current_env_config.ecs_task_memory
  container_name        = local.app_name
  container_image       = var.container_image
  container_port        = var.app_port
  health_check_path     = var.health_check_path
  service_name          = "${local.name_prefix}-service"
  desired_count         = local.current_env_config.ecs_desired_count
  private_subnet_ids    = module.vpc.private_subnet_ids
  ecs_security_group_id = module.security_groups.ecs_security_group_id
  target_group_arn      = module.alb.target_group_arn
  min_capacity          = var.ecs_min_capacity
  max_capacity          = var.ecs_max_capacity
  cpu_target_value      = var.ecs_cpu_target_value
  memory_target_value   = var.ecs_memory_target_value
  log_retention_days    = var.ecs_log_retention_days

  # IAM Roles from IAM module
  ecs_task_execution_role_arn = module.iam.ecs_task_execution_role_arn
  ecs_task_role_arn           = module.iam.ecs_task_role_arn

  # Environment variables
  environment_variables = [
    {
      name  = "ASPNETCORE_ENVIRONMENT"
      value = title(var.environment)
    },
    {
      name  = "ASPNETCORE_URLS"
      value = "http://+:${var.app_port}"
    },
    {
      name  = "AWS_REGION"
      value = var.aws_region
    }
  ]

  # Secrets from AWS Secrets Manager
  secrets = [
    {
      name      = "ConnectionStrings__DefaultConnection"
      valueFrom = "${module.rds.secret_arn}:connectionString::"
    }
    # JWT secret will be added when application secrets module is implemented
  ]

  secrets_arns = [
    module.rds.secret_arn,
    # Add application secrets ARN when secrets module is implemented
  ]

  sqs_queue_arns = [
    # Add SQS queue ARNs when SQS module is implemented
  ]

  tags = local.common_tags

  depends_on = [
    module.vpc,
    module.security_groups,
    module.alb,
    module.rds,
    module.iam
  ]
}