# RDS Module Outputs

output "db_instance_id" {
  description = "RDS instance identifier"
  value       = aws_db_instance.main.id
}

output "db_instance_arn" {
  description = "RDS instance ARN"
  value       = aws_db_instance.main.arn
}

output "db_instance_endpoint" {
  description = "RDS instance endpoint"
  value       = aws_db_instance.main.endpoint
}

output "db_instance_hosted_zone_id" {
  description = "RDS instance hosted zone ID"
  value       = aws_db_instance.main.hosted_zone_id
}

output "db_instance_port" {
  description = "RDS instance port"
  value       = aws_db_instance.main.port
}

output "db_instance_name" {
  description = "RDS instance database name"
  value       = aws_db_instance.main.db_name
}

output "db_instance_username" {
  description = "RDS instance username"
  value       = aws_db_instance.main.username
  sensitive   = true
}

output "db_instance_engine" {
  description = "RDS instance engine"
  value       = aws_db_instance.main.engine
}

output "db_instance_engine_version" {
  description = "RDS instance engine version"
  value       = aws_db_instance.main.engine_version
}

output "db_instance_class" {
  description = "RDS instance class"
  value       = aws_db_instance.main.instance_class
}

output "db_instance_status" {
  description = "RDS instance status"
  value       = aws_db_instance.main.status
}

output "db_subnet_group_id" {
  description = "DB subnet group identifier"
  value       = aws_db_subnet_group.main.id
}

output "db_subnet_group_arn" {
  description = "DB subnet group ARN"
  value       = aws_db_subnet_group.main.arn
}

output "db_parameter_group_id" {
  description = "DB parameter group identifier"
  value       = aws_db_parameter_group.main.id
}

output "db_parameter_group_arn" {
  description = "DB parameter group ARN"
  value       = aws_db_parameter_group.main.arn
}

output "secret_arn" {
  description = "ARN of the database credentials secret"
  value       = aws_secretsmanager_secret.db_credentials.arn
}

output "secret_name" {
  description = "Name of the database credentials secret"
  value       = aws_secretsmanager_secret.db_credentials.name
}

output "secret_version_id" {
  description = "Version ID of the database credentials secret"
  value       = aws_secretsmanager_secret_version.db_credentials.version_id
}

output "monitoring_role_arn" {
  description = "ARN of the enhanced monitoring IAM role"
  value       = var.monitoring_interval > 0 ? aws_iam_role.rds_enhanced_monitoring[0].arn : null
}

output "cloudwatch_log_group_name" {
  description = "Name of the CloudWatch log group for PostgreSQL logs"
  value       = "/aws/rds/instance/${aws_db_instance.main.id}/postgresql"
}

# Connection information for applications
output "connection_info" {
  description = "Database connection information"
  value = {
    host     = aws_db_instance.main.endpoint
    port     = aws_db_instance.main.port
    database = aws_db_instance.main.db_name
    username = aws_db_instance.main.username
  }
  sensitive = true
}

# CloudWatch alarm names for monitoring
output "cloudwatch_alarms" {
  description = "CloudWatch alarm names created for monitoring"
  value = {
    cpu_utilization  = aws_cloudwatch_metric_alarm.database_cpu.alarm_name
    connection_count = aws_cloudwatch_metric_alarm.database_connections.alarm_name
    free_storage     = aws_cloudwatch_metric_alarm.database_free_storage.alarm_name
    read_latency     = aws_cloudwatch_metric_alarm.database_read_latency.alarm_name
    write_latency    = aws_cloudwatch_metric_alarm.database_write_latency.alarm_name
  }
}