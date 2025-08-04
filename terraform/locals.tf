# Local values for computed and derived configurations
locals {
  # Common tags applied to all resources
  common_tags = {
    Project     = var.project_name
    Environment = var.environment
    Owner       = var.owner
    ManagedBy   = "terraform"
    CreatedAt   = timestamp()
  }

  # Naming conventions
  name_prefix = "${var.project_name}-${var.environment}"

  # Network configuration
  vpc_cidr = var.vpc_cidr

  # Calculate subnet CIDRs dynamically
  public_subnet_cidrs = [
    for i, az in slice(var.availability_zones, 0, min(3, length(var.availability_zones))) :
    cidrsubnet(local.vpc_cidr, 8, i + 1)
  ]

  private_subnet_cidrs = [
    for i, az in slice(var.availability_zones, 0, min(3, length(var.availability_zones))) :
    cidrsubnet(local.vpc_cidr, 8, i + 10)
  ]

  database_subnet_cidrs = [
    for i, az in slice(var.availability_zones, 0, min(3, length(var.availability_zones))) :
    cidrsubnet(local.vpc_cidr, 8, i + 20)
  ]

  # Environment-specific configurations
  environment_config = {
    dev = {
      db_instance_class          = "db.t3.micro"
      db_allocated_storage       = 20
      db_backup_retention        = 1
      db_multi_az                = false
      redis_node_type            = "cache.t3.micro"
      redis_num_cache_nodes      = 1
      ecs_desired_count          = 1
      ecs_task_cpu               = 256
      ecs_task_memory            = 512
      enable_deletion_protection = false
    }
    staging = {
      db_instance_class          = "db.t3.small"
      db_allocated_storage       = 50
      db_backup_retention        = 7
      db_multi_az                = true
      redis_node_type            = "cache.t3.small"
      redis_num_cache_nodes      = 2
      ecs_desired_count          = 2
      ecs_task_cpu               = 512
      ecs_task_memory            = 1024
      enable_deletion_protection = false
    }
    prod = {
      db_instance_class          = "db.t3.medium"
      db_allocated_storage       = 100
      db_backup_retention        = 30
      db_multi_az                = true
      redis_node_type            = "cache.t3.medium"
      redis_num_cache_nodes      = 3
      ecs_desired_count          = 3
      ecs_task_cpu               = 1024
      ecs_task_memory            = 2048
      enable_deletion_protection = true
    }
  }

  # Current environment configuration
  current_env_config = local.environment_config[var.environment]

  # AWS Account and Region info
  account_id = data.aws_caller_identity.current.account_id
  region     = var.aws_region

  # Resource naming
  vpc_name              = "${local.name_prefix}-vpc"
  internet_gateway_name = "${local.name_prefix}-igw"
  nat_gateway_name      = "${local.name_prefix}-nat"
  public_subnet_name    = "${local.name_prefix}-public"
  private_subnet_name   = "${local.name_prefix}-private"
  database_subnet_name  = "${local.name_prefix}-database"

  # Security group names
  alb_sg_name   = "${local.name_prefix}-alb-sg"
  ecs_sg_name   = "${local.name_prefix}-ecs-sg"
  rds_sg_name   = "${local.name_prefix}-rds-sg"
  redis_sg_name = "${local.name_prefix}-redis-sg"

  # Database configuration
  db_name     = replace("${var.project_name}_${var.environment}", "-", "_")
  db_username = "app_user"
  db_port     = 5432

  # Environment-specific database configuration
  db_config = {
    instance_class                        = local.current_env_config.db_instance_class
    allocated_storage                     = local.current_env_config.db_allocated_storage
    backup_retention_period               = local.current_env_config.db_backup_retention
    multi_az                              = local.current_env_config.db_multi_az
    deletion_protection                   = local.current_env_config.enable_deletion_protection
    monitoring_interval                   = var.environment == "prod" ? 60 : 0
    performance_insights_enabled          = var.enable_performance_insights
    performance_insights_retention_period = var.performance_insights_retention_period
  }

  # Cache configuration
  redis_port = 6379

  # Application configuration
  app_name = "${local.name_prefix}-app"

  # Secrets
  db_secret_name  = "${local.name_prefix}/database"
  app_secret_name = "${local.name_prefix}/application"
}