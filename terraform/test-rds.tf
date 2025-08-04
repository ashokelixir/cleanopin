# Test configuration for RDS module
# This file can be used to test the RDS module independently

# Uncomment the following to test the RDS module in isolation
/*
terraform {
  required_version = ">= 1.5"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.0"
    }
  }
}

provider "aws" {
  region = "us-east-1"
}

# Test data sources
data "aws_availability_zones" "available" {
  state = "available"
}

# Test VPC for RDS module
resource "aws_vpc" "test" {
  cidr_block           = "10.0.0.0/16"
  enable_dns_hostnames = true
  enable_dns_support   = true

  tags = {
    Name = "test-rds-vpc"
  }
}

# Test subnets
resource "aws_subnet" "test_db" {
  count = 2

  vpc_id            = aws_vpc.test.id
  cidr_block        = "10.0.${count.index + 20}.0/24"
  availability_zone = data.aws_availability_zones.available.names[count.index]

  tags = {
    Name = "test-db-subnet-${count.index + 1}"
  }
}

# Test security group
resource "aws_security_group" "test_rds" {
  name_prefix = "test-rds"
  vpc_id      = aws_vpc.test.id

  ingress {
    from_port   = 5432
    to_port     = 5432
    protocol    = "tcp"
    cidr_blocks = [aws_vpc.test.cidr_block]
  }

  tags = {
    Name = "test-rds-sg"
  }
}

# Test RDS module
module "test_rds" {
  source = "./modules/rds"

  name_prefix         = "test-rds"
  database_subnet_ids = aws_subnet.test_db[*].id
  security_group_ids  = [aws_security_group.test_rds.id]

  database_name     = "testdb"
  database_username = "testuser"
  secret_name       = "test-rds/database"

  # Use minimal configuration for testing
  instance_class        = "db.t3.micro"
  allocated_storage     = 20
  backup_retention_period = 1
  multi_az             = false
  deletion_protection  = false
  monitoring_interval  = 0
  performance_insights_enabled = false

  tags = {
    Environment = "test"
    Purpose     = "rds-module-testing"
  }
}

# Test outputs
output "test_rds_endpoint" {
  value = module.test_rds.db_instance_endpoint
}

output "test_secret_arn" {
  value = module.test_rds.secret_arn
}
*/