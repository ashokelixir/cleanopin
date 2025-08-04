# Test configuration for ECS Fargate infrastructure
# This file is used for testing the ECS and ALB modules

# Uncomment the following to test the ECS infrastructure
# Note: This requires the VPC and security groups to be deployed first

/*
# Test ALB Module
module "test_alb" {
  source = "./modules/alb"

  name_prefix               = "test-cleanarch"
  account_id                = "123456789012"
  alb_name                  = "test-cleanarch-alb"
  vpc_id                    = "vpc-12345678"
  public_subnet_ids         = ["subnet-12345678", "subnet-87654321"]
  alb_security_group_id     = "sg-12345678"
  target_port               = 8080
  health_check_path         = "/health"
  certificate_arn           = ""
  enable_deletion_protection = false
  log_retention_days        = 7
  alarm_actions             = []

  tags = {
    Environment = "test"
    Project     = "cleanarch-template"
  }
}

# Test ECS Module
module "test_ecs" {
  source = "./modules/ecs"

  name_prefix           = "test-cleanarch"
  aws_region            = "us-east-1"
  cluster_name          = "test-cleanarch-cluster"
  task_family           = "test-cleanarch-task"
  task_cpu              = 256
  task_memory           = 512
  container_name        = "test-cleanarch-app"
  container_image       = "nginx:latest"
  container_port        = 8080
  health_check_path     = "/health"
  service_name          = "test-cleanarch-service"
  desired_count         = 1
  private_subnet_ids    = ["subnet-12345678", "subnet-87654321"]
  ecs_security_group_id = "sg-12345678"
  target_group_arn      = module.test_alb.target_group_arn
  min_capacity          = 1
  max_capacity          = 3
  cpu_target_value      = 70
  memory_target_value   = 80
  log_retention_days    = 7

  environment_variables = [
    {
      name  = "ASPNETCORE_ENVIRONMENT"
      value = "Test"
    }
  ]

  secrets = []
  secrets_arns = []
  sqs_queue_arns = []

  tags = {
    Environment = "test"
    Project     = "cleanarch-template"
  }
}
*/