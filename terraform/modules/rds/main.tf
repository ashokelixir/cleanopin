# RDS PostgreSQL Module
# Random password for database
resource "random_password" "db_password" {
  length  = 32
  special = true
  # Exclude characters that are not allowed in RDS passwords: '/', '@', '"', ' '
  override_special = "!#$%&*()-_=+[]{}<>:?"
}

# DB Subnet Group
resource "aws_db_subnet_group" "main" {
  name       = "${var.name_prefix}-db-subnet-group"
  subnet_ids = var.database_subnet_ids

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-db-subnet-group"
  })
}

# DB Parameter Group
resource "aws_db_parameter_group" "main" {
  family = "postgres15"
  name   = "${var.name_prefix}-db-params"

  # Basic logging parameters (dynamic parameters only)
  parameter {
    name  = "log_min_duration_statement"
    value = "1000" # Log queries taking longer than 1 second
  }

  parameter {
    name  = "log_connections"
    value = "1"
  }

  parameter {
    name  = "log_disconnections"
    value = "1"
  }

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-db-params"
  })

  lifecycle {
    create_before_destroy = true
  }
}

# RDS Instance
resource "aws_db_instance" "main" {
  identifier = "${var.name_prefix}-postgres"

  # Engine configuration
  engine         = "postgres"
  engine_version = var.postgres_version
  instance_class = var.instance_class

  # Database configuration
  db_name  = var.database_name
  username = var.database_username
  password = random_password.db_password.result
  port     = var.database_port

  # Storage configuration
  allocated_storage     = var.allocated_storage
  max_allocated_storage = var.max_allocated_storage
  storage_type          = var.storage_type
  storage_encrypted     = true
  kms_key_id            = var.kms_key_id

  # Network configuration
  db_subnet_group_name   = aws_db_subnet_group.main.name
  vpc_security_group_ids = var.security_group_ids
  publicly_accessible    = false

  # Parameter and option groups
  parameter_group_name = aws_db_parameter_group.main.name

  # Backup configuration
  backup_retention_period  = var.backup_retention_period
  backup_window            = var.backup_window
  copy_tags_to_snapshot    = true
  delete_automated_backups = false

  # Maintenance configuration
  maintenance_window          = var.maintenance_window
  auto_minor_version_upgrade  = var.auto_minor_version_upgrade
  allow_major_version_upgrade = false

  # High availability
  multi_az = var.multi_az

  # Monitoring
  monitoring_interval = var.monitoring_interval
  monitoring_role_arn = var.monitoring_interval > 0 ? aws_iam_role.rds_enhanced_monitoring[0].arn : null
  enabled_cloudwatch_logs_exports = [
    "postgresql"
  ]

  # Performance Insights
  performance_insights_enabled          = var.performance_insights_enabled
  performance_insights_retention_period = var.performance_insights_enabled ? var.performance_insights_retention_period : null
  performance_insights_kms_key_id       = var.performance_insights_enabled ? var.kms_key_id : null

  # Deletion protection
  deletion_protection       = var.deletion_protection
  skip_final_snapshot       = !var.deletion_protection
  final_snapshot_identifier = var.deletion_protection ? "${var.name_prefix}-postgres-final-snapshot-${formatdate("YYYY-MM-DD-hhmm", timestamp())}" : null

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-postgres"
  })

  depends_on = [
    aws_db_subnet_group.main,
    aws_db_parameter_group.main
  ]
}

# Enhanced Monitoring IAM Role (only created if monitoring is enabled)
resource "aws_iam_role" "rds_enhanced_monitoring" {
  count = var.monitoring_interval > 0 ? 1 : 0
  name  = "${var.name_prefix}-rds-enhanced-monitoring"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "monitoring.rds.amazonaws.com"
        }
      }
    ]
  })

  tags = var.tags
}

resource "aws_iam_role_policy_attachment" "rds_enhanced_monitoring" {
  count      = var.monitoring_interval > 0 ? 1 : 0
  role       = aws_iam_role.rds_enhanced_monitoring[0].name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonRDSEnhancedMonitoringRole"
}

# Database secrets in AWS Secrets Manager
resource "aws_secretsmanager_secret" "db_credentials" {
  name                    = var.secret_name
  description             = "Database credentials for ${var.name_prefix} PostgreSQL"
  recovery_window_in_days = var.secret_recovery_window

  tags = merge(var.tags, {
    Name = "${var.name_prefix}-db-credentials"
  })
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
    # Connection string for .NET applications
    connectionString = "Host=${aws_db_instance.main.endpoint};Port=${aws_db_instance.main.port};Database=${aws_db_instance.main.db_name};Username=${aws_db_instance.main.username};Password=${random_password.db_password.result};SSL Mode=Require;"
  })

  lifecycle {
    ignore_changes = [secret_string]
  }
}

# Automatic rotation configuration (optional)
resource "aws_secretsmanager_secret_rotation" "db_credentials" {
  count               = var.enable_automatic_rotation ? 1 : 0
  secret_id           = aws_secretsmanager_secret.db_credentials.id
  rotation_lambda_arn = var.rotation_lambda_arn

  rotation_rules {
    automatically_after_days = var.rotation_days
  }

  depends_on = [aws_secretsmanager_secret_version.db_credentials]
}

# CloudWatch Alarms for monitoring
resource "aws_cloudwatch_metric_alarm" "database_cpu" {
  alarm_name          = "${var.name_prefix}-rds-cpu-utilization"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "CPUUtilization"
  namespace           = "AWS/RDS"
  period              = "300"
  statistic           = "Average"
  threshold           = "80"
  alarm_description   = "This metric monitors RDS CPU utilization"
  alarm_actions       = var.alarm_actions

  dimensions = {
    DBInstanceIdentifier = aws_db_instance.main.id
  }

  tags = var.tags
}

resource "aws_cloudwatch_metric_alarm" "database_connections" {
  alarm_name          = "${var.name_prefix}-rds-connection-count"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "DatabaseConnections"
  namespace           = "AWS/RDS"
  period              = "300"
  statistic           = "Average"
  threshold           = var.max_connections * 0.8 # Alert at 80% of max connections
  alarm_description   = "This metric monitors RDS connection count"
  alarm_actions       = var.alarm_actions

  dimensions = {
    DBInstanceIdentifier = aws_db_instance.main.id
  }

  tags = var.tags
}

resource "aws_cloudwatch_metric_alarm" "database_free_storage" {
  alarm_name          = "${var.name_prefix}-rds-free-storage"
  comparison_operator = "LessThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "FreeStorageSpace"
  namespace           = "AWS/RDS"
  period              = "300"
  statistic           = "Average"
  threshold           = 2000000000 # 2GB in bytes
  alarm_description   = "This metric monitors RDS free storage space"
  alarm_actions       = var.alarm_actions

  dimensions = {
    DBInstanceIdentifier = aws_db_instance.main.id
  }

  tags = var.tags
}

resource "aws_cloudwatch_metric_alarm" "database_read_latency" {
  alarm_name          = "${var.name_prefix}-rds-read-latency"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "ReadLatency"
  namespace           = "AWS/RDS"
  period              = "300"
  statistic           = "Average"
  threshold           = "0.2"
  alarm_description   = "This metric monitors RDS read latency"
  alarm_actions       = var.alarm_actions

  dimensions = {
    DBInstanceIdentifier = aws_db_instance.main.id
  }

  tags = var.tags
}

resource "aws_cloudwatch_metric_alarm" "database_write_latency" {
  alarm_name          = "${var.name_prefix}-rds-write-latency"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = "2"
  metric_name         = "WriteLatency"
  namespace           = "AWS/RDS"
  period              = "300"
  statistic           = "Average"
  threshold           = "0.2"
  alarm_description   = "This metric monitors RDS write latency"
  alarm_actions       = var.alarm_actions

  dimensions = {
    DBInstanceIdentifier = aws_db_instance.main.id
  }

  tags = var.tags
}