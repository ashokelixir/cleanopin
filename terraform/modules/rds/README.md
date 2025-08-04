# RDS PostgreSQL Module

This module provisions an Amazon RDS PostgreSQL instance with comprehensive configuration for production workloads.

## Features

- **Multi-AZ Deployment**: Configurable high availability setup
- **Automated Backups**: Configurable retention period and backup windows
- **Enhanced Monitoring**: CloudWatch metrics and Performance Insights
- **Security**: Encryption at rest, VPC security groups, and private subnets
- **Secrets Management**: Automatic credential storage in AWS Secrets Manager
- **Parameter Groups**: Optimized PostgreSQL configuration
- **CloudWatch Alarms**: Monitoring for CPU, connections, storage, and latency
- **Automatic Rotation**: Optional password rotation support

## Usage

```hcl
module "rds" {
  source = "./modules/rds"

  name_prefix         = "myapp-prod"
  database_subnet_ids = ["subnet-12345", "subnet-67890"]
  security_group_ids  = ["sg-abcdef"]

  # Database configuration
  database_name     = "myapp_prod"
  database_username = "app_user"
  postgres_version  = "15.4"

  # Instance configuration
  instance_class        = "db.t3.small"
  allocated_storage     = 50
  max_allocated_storage = 200

  # High availability
  multi_az = true

  # Monitoring
  monitoring_interval           = 60
  performance_insights_enabled = true

  # Security
  deletion_protection = true

  # Secrets Manager
  secret_name = "myapp-prod/database"

  tags = {
    Environment = "prod"
    Project     = "myapp"
  }
}
```

## Requirements

| Name | Version |
|------|---------|
| terraform | >= 1.5 |
| aws | ~> 5.0 |
| random | ~> 3.0 |

## Providers

| Name | Version |
|------|---------|
| aws | ~> 5.0 |
| random | ~> 3.0 |

## Resources

| Name | Type |
|------|------|
| aws_db_instance.main | resource |
| aws_db_subnet_group.main | resource |
| aws_db_parameter_group.main | resource |
| aws_secretsmanager_secret.db_credentials | resource |
| aws_secretsmanager_secret_version.db_credentials | resource |
| aws_secretsmanager_secret_rotation.db_credentials | resource |
| aws_iam_role.rds_enhanced_monitoring | resource |
| aws_iam_role_policy_attachment.rds_enhanced_monitoring | resource |
| aws_cloudwatch_metric_alarm.database_cpu | resource |
| aws_cloudwatch_metric_alarm.database_connections | resource |
| aws_cloudwatch_metric_alarm.database_free_storage | resource |
| aws_cloudwatch_metric_alarm.database_read_latency | resource |
| aws_cloudwatch_metric_alarm.database_write_latency | resource |
| random_password.db_password | resource |

## Inputs

| Name | Description | Type | Default | Required |
|------|-------------|------|---------|:--------:|
| name_prefix | Prefix for resource names | `string` | n/a | yes |
| database_subnet_ids | List of subnet IDs for the DB subnet group | `list(string)` | n/a | yes |
| security_group_ids | List of security group IDs for the RDS instance | `list(string)` | n/a | yes |
| database_name | Name of the database to create | `string` | n/a | yes |
| secret_name | Name for the database credentials secret | `string` | n/a | yes |
| database_username | Username for the database | `string` | `"app_user"` | no |
| database_port | Port for the database | `number` | `5432` | no |
| postgres_version | PostgreSQL engine version | `string` | `"15.4"` | no |
| instance_class | RDS instance class | `string` | `"db.t3.micro"` | no |
| allocated_storage | Initial allocated storage in GB | `number` | `20` | no |
| max_allocated_storage | Maximum allocated storage in GB for autoscaling | `number` | `100` | no |
| storage_type | Storage type (gp2, gp3, io1, io2) | `string` | `"gp3"` | no |
| backup_retention_period | Backup retention period in days | `number` | `7` | no |
| backup_window | Backup window (UTC) | `string` | `"03:00-04:00"` | no |
| maintenance_window | Maintenance window (UTC) | `string` | `"sun:04:00-sun:05:00"` | no |
| multi_az | Enable Multi-AZ deployment | `bool` | `false` | no |
| monitoring_interval | Enhanced monitoring interval in seconds | `number` | `60` | no |
| performance_insights_enabled | Enable Performance Insights | `bool` | `true` | no |
| deletion_protection | Enable deletion protection | `bool` | `false` | no |
| tags | Tags to apply to all resources | `map(string)` | `{}` | no |

## Outputs

| Name | Description |
|------|-------------|
| db_instance_id | RDS instance identifier |
| db_instance_endpoint | RDS instance endpoint |
| db_instance_port | RDS instance port |
| secret_arn | ARN of the database credentials secret |
| secret_name | Name of the database credentials secret |
| connection_info | Database connection information |
| cloudwatch_alarms | CloudWatch alarm names created for monitoring |

## Security Considerations

1. **Encryption**: All data is encrypted at rest using AWS KMS
2. **Network Security**: Database is deployed in private subnets with security groups
3. **Access Control**: Database credentials are stored in AWS Secrets Manager
4. **Monitoring**: Comprehensive CloudWatch alarms for security and performance monitoring
5. **Backup**: Automated backups with configurable retention periods

## Performance Tuning

The module includes optimized PostgreSQL parameters:

- `shared_preload_libraries = pg_stat_statements`
- `work_mem = 4MB`
- `maintenance_work_mem = 64MB`
- `effective_cache_size = 128MB` (adjust based on instance size)
- `checkpoint_completion_target = 0.9`

## Monitoring and Alerting

The module creates CloudWatch alarms for:

- CPU utilization (threshold: 80%)
- Database connections (threshold: 80% of max_connections)
- Free storage space (threshold: 2GB)
- Read latency (threshold: 200ms)
- Write latency (threshold: 200ms)

## Environment-Specific Configuration

The module supports different configurations for different environments:

### Development
- Single AZ deployment
- Minimal monitoring
- Lower backup retention
- Smaller instance sizes

### Staging
- Multi-AZ deployment
- Enhanced monitoring enabled
- Moderate backup retention
- Performance Insights enabled

### Production
- Multi-AZ deployment
- Full monitoring and alerting
- Extended backup retention
- Performance Insights with extended retention
- Deletion protection enabled

## Secrets Management

Database credentials are automatically stored in AWS Secrets Manager with the following structure:

```json
{
  "username": "app_user",
  "password": "generated-password",
  "engine": "postgres",
  "host": "database-endpoint",
  "port": 5432,
  "dbname": "database-name",
  "connectionString": "Host=endpoint;Port=5432;Database=dbname;Username=user;Password=pass;SSL Mode=Require;"
}
```

## Automatic Password Rotation

The module supports automatic password rotation using AWS Lambda functions. To enable:

1. Deploy a rotation Lambda function
2. Set `enable_automatic_rotation = true`
3. Provide the `rotation_lambda_arn`
4. Configure `rotation_days` (default: 30 days)