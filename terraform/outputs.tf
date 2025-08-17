# VPC Outputs
output "vpc_id" {
  description = "ID of the VPC"
  value       = module.vpc.vpc_id
}

output "vpc_cidr_block" {
  description = "CIDR block of the VPC"
  value       = module.vpc.vpc_cidr_block
}

output "internet_gateway_id" {
  description = "ID of the Internet Gateway"
  value       = module.vpc.internet_gateway_id
}

output "public_subnet_ids" {
  description = "IDs of the public subnets"
  value       = module.vpc.public_subnet_ids
}

output "private_subnet_ids" {
  description = "IDs of the private subnets"
  value       = module.vpc.private_subnet_ids
}

output "database_subnet_ids" {
  description = "IDs of the database subnets"
  value       = module.vpc.database_subnet_ids
}

output "nat_gateway_ids" {
  description = "IDs of the NAT Gateways"
  value       = module.vpc.nat_gateway_ids
}

output "public_route_table_id" {
  description = "ID of the public route table"
  value       = module.vpc.public_route_table_id
}

output "private_route_table_ids" {
  description = "IDs of the private route tables"
  value       = module.vpc.private_route_table_ids
}

output "database_route_table_id" {
  description = "ID of the database route table"
  value       = module.vpc.database_route_table_id
}

# Network ACL Outputs
output "public_network_acl_id" {
  description = "ID of the public network ACL"
  value       = module.vpc.public_network_acl_id
}

output "private_network_acl_id" {
  description = "ID of the private network ACL"
  value       = module.vpc.private_network_acl_id
}

output "database_network_acl_id" {
  description = "ID of the database network ACL"
  value       = module.vpc.database_network_acl_id
}

# VPC Endpoint Outputs
output "s3_vpc_endpoint_id" {
  description = "ID of the S3 VPC endpoint"
  value       = module.vpc.s3_vpc_endpoint_id
}

output "ecr_dkr_vpc_endpoint_id" {
  description = "ID of the ECR DKR VPC endpoint"
  value       = module.vpc.ecr_dkr_vpc_endpoint_id
}

output "ecr_api_vpc_endpoint_id" {
  description = "ID of the ECR API VPC endpoint"
  value       = module.vpc.ecr_api_vpc_endpoint_id
}

output "secrets_manager_vpc_endpoint_id" {
  description = "ID of the Secrets Manager VPC endpoint"
  value       = module.vpc.secrets_manager_vpc_endpoint_id
}

output "logs_vpc_endpoint_id" {
  description = "ID of the CloudWatch Logs VPC endpoint"
  value       = module.vpc.logs_vpc_endpoint_id
}

output "monitoring_vpc_endpoint_id" {
  description = "ID of the CloudWatch Monitoring VPC endpoint"
  value       = module.vpc.monitoring_vpc_endpoint_id
}

# Security Group Outputs
output "alb_security_group_id" {
  description = "ID of the ALB security group"
  value       = module.security_groups.alb_security_group_id
}

output "ecs_security_group_id" {
  description = "ID of the ECS security group"
  value       = module.security_groups.ecs_security_group_id
}

output "rds_security_group_id" {
  description = "ID of the RDS security group"
  value       = module.security_groups.rds_security_group_id
}

output "redis_security_group_id" {
  description = "ID of the Redis security group"
  value       = module.security_groups.redis_security_group_id
}

output "bastion_security_group_id" {
  description = "ID of the Bastion security group"
  value       = module.security_groups.bastion_security_group_id
}

output "vpc_endpoints_security_group_id" {
  description = "ID of the VPC endpoints security group"
  value       = module.security_groups.vpc_endpoints_security_group_id
}

# Database Outputs
output "rds_instance_id" {
  description = "RDS instance identifier"
  value       = module.rds.db_instance_id
}

output "rds_endpoint" {
  description = "RDS instance endpoint"
  value       = module.rds.db_instance_endpoint
  sensitive   = true
}

output "rds_port" {
  description = "RDS instance port"
  value       = module.rds.db_instance_port
}

output "rds_database_name" {
  description = "RDS database name"
  value       = module.rds.db_instance_name
}

output "database_secret_arn" {
  description = "ARN of the database credentials secret"
  value       = module.rds.secret_arn
  sensitive   = true
}

output "database_secret_name" {
  description = "Name of the database credentials secret"
  value       = module.rds.secret_name
}

output "rds_subnet_group_id" {
  description = "RDS subnet group identifier"
  value       = module.rds.db_subnet_group_id
}

output "rds_parameter_group_id" {
  description = "RDS parameter group identifier"
  value       = module.rds.db_parameter_group_id
}

output "rds_cloudwatch_alarms" {
  description = "RDS CloudWatch alarm names"
  value       = module.rds.cloudwatch_alarms
}

# Cache Outputs (will be uncommented when Redis module is implemented)
# output "redis_endpoint" {
#   description = "Redis cluster endpoint"
#   value       = module.redis.redis_endpoint
#   sensitive   = true
# }

# output "redis_port" {
#   description = "Redis cluster port"
#   value       = module.redis.redis_port
# }

# ECS Outputs
output "ecs_cluster_id" {
  description = "ID of the ECS cluster"
  value       = module.ecs.cluster_id
}

output "ecs_cluster_name" {
  description = "Name of the ECS cluster"
  value       = module.ecs.cluster_name
}

output "ecs_cluster_arn" {
  description = "ARN of the ECS cluster"
  value       = module.ecs.cluster_arn
}

output "ecs_service_name" {
  description = "Name of the ECS service"
  value       = module.ecs.service_name
}

output "ecs_service_arn" {
  description = "ARN of the ECS service"
  value       = module.ecs.service_arn
}

output "ecs_task_definition_arn" {
  description = "ARN of the ECS task definition"
  value       = module.ecs.task_definition_arn
}

output "ecs_log_group_name" {
  description = "Name of the ECS CloudWatch log group"
  value       = module.ecs.log_group_name
}

# Load Balancer Outputs
output "alb_dns_name" {
  description = "DNS name of the load balancer"
  value       = module.alb.alb_dns_name
}

output "alb_zone_id" {
  description = "Zone ID of the load balancer"
  value       = module.alb.alb_zone_id
}

output "alb_arn" {
  description = "ARN of the load balancer"
  value       = module.alb.alb_arn
}

output "target_group_arn" {
  description = "ARN of the target group"
  value       = module.alb.target_group_arn
}

output "alb_access_logs_bucket" {
  description = "S3 bucket for ALB access logs"
  value       = module.alb.access_logs_bucket_id
}

# Application Secrets Outputs
output "jwt_settings_secret_arn" {
  description = "ARN of the JWT settings secret"
  value       = module.secrets.jwt_settings_secret_arn
  sensitive   = true
}

output "jwt_settings_secret_name" {
  description = "Name of the JWT settings secret"
  value       = module.secrets.jwt_settings_secret_name
}

output "external_api_keys_secret_arn" {
  description = "ARN of the external API keys secret"
  value       = module.secrets.external_api_keys_secret_arn
  sensitive   = true
}

output "external_api_keys_secret_name" {
  description = "Name of the external API keys secret"
  value       = module.secrets.external_api_keys_secret_name
}

output "app_config_secret_arn" {
  description = "ARN of the application configuration secret"
  value       = module.secrets.app_config_secret_arn
  sensitive   = true
}

output "app_config_secret_name" {
  description = "Name of the application configuration secret"
  value       = module.secrets.app_config_secret_name
}

output "all_application_secret_arns" {
  description = "List of all application secret ARNs"
  value       = module.secrets.all_secret_arns
  sensitive   = true
}

# SQS Outputs
output "sqs_user_events_queue_arn" {
  description = "ARN of the user events queue"
  value       = module.sqs.user_events_queue_arn
}

output "sqs_user_events_queue_url" {
  description = "URL of the user events queue"
  value       = module.sqs.user_events_queue_url
}

output "sqs_permission_events_queue_arn" {
  description = "ARN of the permission events queue"
  value       = module.sqs.permission_events_queue_arn
}

output "sqs_permission_events_queue_url" {
  description = "URL of the permission events queue"
  value       = module.sqs.permission_events_queue_url
}

output "sqs_audit_events_queue_arn" {
  description = "ARN of the audit events FIFO queue"
  value       = module.sqs.audit_events_queue_arn
}

output "sqs_audit_events_queue_url" {
  description = "URL of the audit events FIFO queue"
  value       = module.sqs.audit_events_queue_url
}

output "sqs_all_queue_arns" {
  description = "List of all SQS queue ARNs (including DLQs)"
  value       = module.sqs.all_queue_arns
}

output "sqs_main_queue_arns" {
  description = "List of main SQS queue ARNs (excluding DLQs)"
  value       = module.sqs.main_queue_arns
}

output "sqs_dlq_arns" {
  description = "List of dead letter queue ARNs"
  value       = module.sqs.dlq_arns
}

output "sqs_queue_configuration" {
  description = "Complete SQS queue configuration for application use"
  value       = module.sqs.queue_configuration
  sensitive   = true
}

output "sqs_cloudwatch_alarm_arns" {
  description = "List of CloudWatch alarm ARNs for SQS queues"
  value       = module.sqs.cloudwatch_alarm_arns
}

# Environment Information
output "environment" {
  description = "Current environment"
  value       = var.environment
}

output "aws_region" {
  description = "AWS region"
  value       = var.aws_region
}

output "account_id" {
  description = "AWS account ID"
  value       = local.account_id
}

# IAM Outputs
output "ecs_task_execution_role_arn" {
  description = "ARN of the ECS task execution role"
  value       = module.iam.ecs_task_execution_role_arn
}

output "ecs_task_role_arn" {
  description = "ARN of the ECS task role"
  value       = module.iam.ecs_task_role_arn
}

output "cicd_cross_account_role_arn" {
  description = "ARN of the CI/CD cross-account role"
  value       = module.iam.cicd_cross_account_role_arn
  sensitive   = true
}

output "lambda_execution_role_arn" {
  description = "ARN of the Lambda execution role"
  value       = module.iam.lambda_execution_role_arn
}

output "iam_role_arns" {
  description = "Map of all IAM role ARNs"
  value       = module.iam.all_role_arns
  sensitive   = true
}