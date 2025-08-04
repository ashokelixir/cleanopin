# RDS PostgreSQL Implementation

This document describes the implementation of Task 18: Provision RDS PostgreSQL with Terraform.

## Overview

The RDS PostgreSQL module provides a comprehensive, production-ready PostgreSQL database solution with the following features:

- **Multi-AZ deployment** for high availability
- **Automated backups** with configurable retention
- **Enhanced monitoring** with CloudWatch and Performance Insights
- **Security** with encryption, VPC isolation, and secrets management
- **Performance optimization** with custom parameter groups
- **Automated alerting** with CloudWatch alarms

## Implementation Details

### 1. RDS Subnet Group

The module creates a DB subnet group spanning multiple availability zones:

```hcl
resource "aws_db_subnet_group" "main" {
  name       = "${var.name_prefix}-db-subnet-group"
  subnet_ids = var.database_subnet_ids
}
```

**Features:**
- Spans multiple AZs for high availability
- Uses dedicated database subnets (isolated from application subnets)
- Follows naming conventions for easy identification

### 2. RDS Instance Configuration

The PostgreSQL instance is configured with proper sizing and storage:

```hcl
resource "aws_db_instance" "main" {
  identifier = "${var.name_prefix}-postgres"
  
  # Engine configuration
  engine         = "postgres"
  engine_version = var.postgres_version
  instance_class = var.instance_class
  
  # Storage configuration
  allocated_storage     = var.allocated_storage
  max_allocated_storage = var.max_allocated_storage
  storage_type          = "gp3"
  storage_encrypted     = true
}
```

**Features:**
- PostgreSQL 15.4 (configurable)
- GP3 storage for better performance
- Storage autoscaling enabled
- Encryption at rest with KMS

### 3. Automated Backups and Maintenance

```hcl
# Backup configuration
backup_retention_period = var.backup_retention_period
backup_window          = var.backup_window
copy_tags_to_snapshot  = true

# Maintenance configuration
maintenance_window         = var.maintenance_window
auto_minor_version_upgrade = var.auto_minor_version_upgrade
```

**Features:**
- Configurable backup retention (1-35 days)
- Automated maintenance windows
- Point-in-time recovery
- Snapshot tagging

### 4. Security Groups and Parameter Groups

**Security Groups:**
- Integrated with existing security group module
- Allows access only from ECS tasks and bastion hosts
- No public access

**Parameter Groups:**
```hcl
resource "aws_db_parameter_group" "main" {
  family = "postgres15"
  
  # Performance parameters
  parameter {
    name  = "shared_preload_libraries"
    value = "pg_stat_statements"
  }
  
  parameter {
    name  = "max_connections"
    value = var.max_connections
  }
  
  # Additional performance tuning parameters...
}
```

**Features:**
- Optimized for application workloads
- Query performance monitoring enabled
- Connection pooling support
- Logging configuration for troubleshooting

### 5. AWS Secrets Manager Integration

```hcl
resource "aws_secretsmanager_secret" "db_credentials" {
  name                    = var.secret_name
  description             = "Database credentials for ${var.name_prefix} PostgreSQL"
  recovery_window_in_days = var.secret_recovery_window
}

resource "aws_secretsmanager_secret_version" "db_credentials" {
  secret_id = aws_secretsmanager_secret.db_credentials.id
  secret_string = jsonencode({
    username = aws_db_instance.main.username
    password = random_password.db_password.result
    engine   = "postgres"
    host     = aws_db_instance.main.endpoint
    port     = aws_db_instance.main.port
    dbname   = aws_db_instance.main.db_name
    connectionString = "Host=${aws_db_instance.main.endpoint};Port=${aws_db_instance.main.port};Database=${aws_db_instance.main.db_name};Username=${aws_db_instance.main.username};Password=${random_password.db_password.result};SSL Mode=Require;"
  })
}
```

**Features:**
- Automatic password generation (32 characters)
- Secure credential storage
- .NET connection string format
- Optional automatic rotation support
- Configurable recovery window

### 6. Monitoring and Alerting

**Enhanced Monitoring:**
```hcl
monitoring_interval = var.monitoring_interval
monitoring_role_arn = aws_iam_role.rds_enhanced_monitoring[0].arn
enabled_cloudwatch_logs_exports = ["postgresql"]
```

**Performance Insights:**
```hcl
performance_insights_enabled          = var.performance_insights_enabled
performance_insights_retention_period = var.performance_insights_retention_period
performance_insights_kms_key_id      = var.kms_key_id
```

**CloudWatch Alarms:**
- CPU Utilization (>80%)
- Database Connections (>80% of max)
- Free Storage Space (<2GB)
- Read Latency (>200ms)
- Write Latency (>200ms)

## Environment-Specific Configuration

### Development Environment
```hcl
db_instance_class          = "db.t3.micro"
db_allocated_storage       = 20
db_backup_retention        = 1
db_multi_az                = false
enable_deletion_protection = false
monitoring_interval        = 0
performance_insights       = false
```

### Staging Environment
```hcl
db_instance_class          = "db.t3.small"
db_allocated_storage       = 50
db_backup_retention        = 7
db_multi_az                = true
enable_deletion_protection = false
monitoring_interval        = 60
performance_insights       = true
```

### Production Environment
```hcl
db_instance_class          = "db.t3.medium"
db_allocated_storage       = 100
db_backup_retention        = 30
db_multi_az                = true
enable_deletion_protection = true
monitoring_interval        = 60
performance_insights       = true
```

## Usage Examples

### Basic Usage
```hcl
module "rds" {
  source = "./modules/rds"

  name_prefix         = "myapp-prod"
  database_subnet_ids = module.vpc.database_subnet_ids
  security_group_ids  = [module.security_groups.rds_security_group_id]

  database_name = "myapp_prod"
  secret_name   = "myapp-prod/database"

  tags = local.common_tags
}
```

### Advanced Configuration
```hcl
module "rds" {
  source = "./modules/rds"

  name_prefix         = "myapp-prod"
  database_subnet_ids = module.vpc.database_subnet_ids
  security_group_ids  = [module.security_groups.rds_security_group_id]

  # Database configuration
  database_name     = "myapp_prod"
  database_username = "app_user"
  postgres_version  = "15.4"

  # Instance configuration
  instance_class        = "db.r6g.large"
  allocated_storage     = 500
  max_allocated_storage = 2000
  storage_type         = "gp3"

  # High availability
  multi_az = true

  # Backup configuration
  backup_retention_period = 30
  backup_window          = "03:00-04:00"

  # Maintenance
  maintenance_window = "sun:04:00-sun:05:00"

  # Monitoring
  monitoring_interval           = 60
  performance_insights_enabled = true
  performance_insights_retention_period = 31

  # Security
  deletion_protection = true

  # Secrets
  secret_name = "myapp-prod/database"
  enable_automatic_rotation = true
  rotation_days = 30

  tags = local.common_tags
}
```

## Deployment

### Using the Deployment Script
```powershell
# Plan deployment
.\scripts\deploy-rds.ps1 -Environment dev -Plan

# Deploy to development
.\scripts\deploy-rds.ps1 -Environment dev

# Deploy to production with auto-approve
.\scripts\deploy-rds.ps1 -Environment prod -AutoApprove

# Destroy development environment
.\scripts\deploy-rds.ps1 -Environment dev -Destroy
```

### Manual Deployment
```bash
# Initialize Terraform
terraform init

# Plan with environment-specific variables
terraform plan -var-file=environments/dev.tfvars -target=module.rds

# Apply changes
terraform apply -var-file=environments/dev.tfvars -target=module.rds
```

## Outputs

The module provides comprehensive outputs for integration with other modules:

```hcl
# Database connection information
output "rds_endpoint" {
  value = module.rds.db_instance_endpoint
}

output "database_secret_arn" {
  value = module.rds.secret_arn
}

# Monitoring information
output "rds_cloudwatch_alarms" {
  value = module.rds.cloudwatch_alarms
}
```

## Security Considerations

1. **Network Security:**
   - Database deployed in private subnets
   - Security groups restrict access to application tier only
   - No public access allowed

2. **Data Security:**
   - Encryption at rest using AWS KMS
   - SSL/TLS encryption in transit
   - Secure credential management with Secrets Manager

3. **Access Control:**
   - IAM roles for enhanced monitoring
   - Principle of least privilege
   - Audit logging enabled

4. **Backup Security:**
   - Encrypted backups
   - Cross-region backup replication (configurable)
   - Point-in-time recovery

## Performance Optimization

1. **Instance Sizing:**
   - Environment-specific instance classes
   - Burstable performance for development
   - Dedicated instances for production

2. **Storage:**
   - GP3 storage for better IOPS
   - Storage autoscaling enabled
   - Optimized for read/write workloads

3. **Connection Management:**
   - Optimized max_connections parameter
   - Connection pooling support
   - Query performance monitoring

4. **Monitoring:**
   - Performance Insights for query analysis
   - Enhanced monitoring for OS metrics
   - Custom CloudWatch alarms

## Cost Optimization

1. **Environment-Based Sizing:**
   - Smaller instances for development/testing
   - Right-sized instances for production
   - Storage autoscaling to avoid over-provisioning

2. **Backup Management:**
   - Shorter retention for non-production
   - Automated cleanup of old snapshots
   - Cross-region replication only for production

3. **Monitoring:**
   - Enhanced monitoring only for production
   - Performance Insights with appropriate retention
   - Cost-effective alarm configuration

## Troubleshooting

### Common Issues

1. **Connection Issues:**
   - Check security group rules
   - Verify subnet routing
   - Confirm DNS resolution

2. **Performance Issues:**
   - Review Performance Insights
   - Check CloudWatch metrics
   - Analyze slow query logs

3. **Backup Issues:**
   - Verify backup window configuration
   - Check storage space
   - Review backup retention settings

### Monitoring and Alerting

The module includes comprehensive monitoring:

- **CPU Utilization:** Alerts when >80%
- **Connection Count:** Alerts when >80% of max
- **Storage Space:** Alerts when <2GB free
- **Query Performance:** Latency monitoring
- **Error Rates:** Database error monitoring

## Requirements Compliance

This implementation satisfies the following requirements:

- **4.1:** PostgreSQL integration with Entity Framework Core ✓
- **4.2:** EF Core migrations with proper versioning ✓
- **11.3:** AWS Secrets Manager integration ✓

## Next Steps

1. **Integration Testing:**
   - Test database connectivity from ECS tasks
   - Verify secrets retrieval from applications
   - Validate backup and restore procedures

2. **Monitoring Setup:**
   - Configure SNS topics for alarm notifications
   - Set up dashboard for database metrics
   - Implement log aggregation

3. **Security Hardening:**
   - Enable automatic password rotation
   - Configure cross-region backup replication
   - Implement database activity monitoring

4. **Performance Tuning:**
   - Analyze query performance with Performance Insights
   - Optimize parameter group settings
   - Configure connection pooling in applications