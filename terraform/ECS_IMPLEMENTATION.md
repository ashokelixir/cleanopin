# ECS Fargate Infrastructure Implementation

This document describes the implementation of ECS Fargate infrastructure with Terraform for the Clean Architecture Template project.

## Overview

The ECS Fargate infrastructure provides a fully managed, serverless container platform that automatically scales based on demand. This implementation includes:

- **ECS Cluster** with Fargate capacity providers
- **Application Load Balancer** with target groups and health checks
- **ECS Task Definition** with proper resource allocation
- **ECS Service** with auto-scaling and deployment configuration
- **CloudWatch Log Groups** for container logging
- **IAM Roles** with least privilege access
- **Auto Scaling** policies for CPU and memory utilization

## Architecture Components

### ECS Cluster
- **Name**: `{project}-{environment}-cluster`
- **Capacity Providers**: FARGATE and FARGATE_SPOT
- **Container Insights**: Enabled for monitoring
- **Default Strategy**: 100% FARGATE with base capacity of 1

### Application Load Balancer (ALB)
- **Type**: Application Load Balancer
- **Scheme**: Internet-facing
- **Subnets**: Public subnets across multiple AZs
- **Security Groups**: ALB security group with HTTP/HTTPS access
- **Access Logs**: Stored in S3 bucket with lifecycle policies
- **Health Checks**: Configurable path and thresholds

### ECS Task Definition
- **Launch Type**: Fargate
- **Network Mode**: awsvpc
- **CPU/Memory**: Environment-specific allocation
- **Execution Role**: Access to ECR, CloudWatch, and Secrets Manager
- **Task Role**: Application-specific permissions for AWS services

### ECS Service
- **Launch Type**: Fargate
- **Network**: Private subnets with security groups
- **Load Balancer**: Integrated with ALB target group
- **Deployment**: Rolling updates with circuit breaker
- **Auto Scaling**: CPU and memory-based scaling policies

## Environment Configuration

### Development
- **Task Resources**: 256 CPU, 512 MB Memory
- **Desired Count**: 1 task
- **Auto Scaling**: 1-3 tasks
- **Log Retention**: 7 days
- **SSL**: HTTP only (no certificate)

### Staging
- **Task Resources**: 512 CPU, 1024 MB Memory
- **Desired Count**: 2 tasks
- **Auto Scaling**: 2-6 tasks
- **Log Retention**: 14 days
- **SSL**: HTTPS with certificate (optional)

### Production
- **Task Resources**: 1024 CPU, 2048 MB Memory
- **Desired Count**: 3 tasks
- **Auto Scaling**: 3-10 tasks
- **Log Retention**: 30 days
- **SSL**: HTTPS with certificate (required)

## Security Features

### Network Security
- Tasks run in private subnets
- ALB in public subnets with security groups
- VPC endpoints for AWS services
- Security groups with least privilege access

### IAM Security
- **Task Execution Role**: ECR, CloudWatch, Secrets Manager access
- **Task Role**: Application-specific AWS service permissions
- Principle of least privilege applied
- Separate roles for execution and runtime

### Data Security
- Secrets stored in AWS Secrets Manager
- Environment variables for non-sensitive configuration
- S3 bucket encryption for ALB logs
- CloudWatch log encryption (optional)

## Monitoring and Observability

### CloudWatch Metrics
- ECS service metrics (CPU, memory, task count)
- ALB metrics (request count, response time, errors)
- Custom application metrics via CloudWatch agent

### CloudWatch Alarms
- ALB response time threshold
- Unhealthy target count
- 5XX error rate
- ECS service scaling triggers

### Logging
- Container logs to CloudWatch Logs
- ALB access logs to S3
- Structured logging with correlation IDs
- Configurable log retention periods

## Auto Scaling Configuration

### Target Tracking Policies
- **CPU Utilization**: 70% target (configurable)
- **Memory Utilization**: 80% target (configurable)
- **Scale Out Cooldown**: 300 seconds
- **Scale In Cooldown**: 300 seconds

### Scaling Limits
- **Minimum Capacity**: Environment-specific
- **Maximum Capacity**: Environment-specific
- **Scaling Increment**: 1 task per scaling event

## Deployment Process

### Prerequisites
1. VPC and networking infrastructure deployed
2. Security groups configured
3. RDS database available
4. Container image pushed to ECR
5. AWS credentials configured

### Deployment Steps

#### Using PowerShell (Windows)
```powershell
# Deploy to development
./scripts/deploy-ecs.ps1 -Environment dev -ContainerImage myapp:latest

# Deploy to production with auto-approve
./scripts/deploy-ecs.ps1 -Environment prod -ContainerImage myapp:v1.0.0 -AutoApprove

# Plan only (no changes applied)
./scripts/deploy-ecs.ps1 -Environment staging -ContainerImage myapp:v1.1.0 -Plan
```

#### Using Bash (Linux/macOS)
```bash
# Deploy to development
./scripts/deploy-ecs.sh -e dev -i myapp:latest

# Deploy to production with auto-approve
./scripts/deploy-ecs.sh -e prod -i myapp:v1.0.0 -a

# Plan only (no changes applied)
./scripts/deploy-ecs.sh -e staging -i myapp:v1.1.0 -p
```

### Manual Deployment
```bash
# Initialize Terraform
terraform init -backend-config=backend-configs/dev.hcl

# Select workspace
terraform workspace select dev

# Plan deployment
terraform plan -var-file=environments/dev.tfvars -var="container_image=myapp:latest"

# Apply changes
terraform apply -var-file=environments/dev.tfvars -var="container_image=myapp:latest"
```

## Configuration Files

### Environment Variables
The ECS task receives the following environment variables:
- `ASPNETCORE_ENVIRONMENT`: Environment name (Dev, Staging, Prod)
- `ASPNETCORE_URLS`: Application listening URLs
- `AWS_REGION`: AWS region for service discovery

### Secrets Management
Secrets are retrieved from AWS Secrets Manager:
- `ConnectionStrings__DefaultConnection`: Database connection string
- `JwtSettings__SecretKey`: JWT signing key
- Additional application secrets as needed

### Health Checks
- **Container Health Check**: HTTP GET to `/health` endpoint
- **ALB Health Check**: HTTP GET to `/health` endpoint
- **Thresholds**: 2 healthy, 2 unhealthy, 30-second interval

## Troubleshooting

### Common Issues

#### Task Startup Failures
1. Check CloudWatch logs for container errors
2. Verify secrets are accessible
3. Confirm security group rules
4. Check task definition resource limits

#### Load Balancer Issues
1. Verify target group health checks
2. Check security group rules for ALB
3. Confirm DNS resolution
4. Review ALB access logs

#### Auto Scaling Issues
1. Check CloudWatch metrics
2. Review scaling policies
3. Verify service limits
4. Monitor scaling activities

### Useful Commands

#### ECS Service Management
```bash
# List ECS clusters
aws ecs list-clusters

# Describe ECS service
aws ecs describe-services --cluster cluster-name --services service-name

# View service events
aws ecs describe-services --cluster cluster-name --services service-name --query 'services[0].events'

# Update service with new task definition
aws ecs update-service --cluster cluster-name --service service-name --task-definition task-definition-arn
```

#### CloudWatch Logs
```bash
# View recent logs
aws logs tail /ecs/cluster-name --follow

# Get log streams
aws logs describe-log-streams --log-group-name /ecs/cluster-name
```

#### Load Balancer
```bash
# Describe target group health
aws elbv2 describe-target-health --target-group-arn target-group-arn

# List load balancers
aws elbv2 describe-load-balancers
```

## Cost Optimization

### Fargate Pricing
- Pay only for vCPU and memory resources used
- No EC2 instance management overhead
- Automatic scaling reduces costs during low usage

### Optimization Strategies
1. **Right-size tasks**: Monitor CPU/memory usage and adjust
2. **Use Fargate Spot**: For non-critical workloads (up to 70% savings)
3. **Optimize scaling**: Tune scaling policies to avoid over-provisioning
4. **Log retention**: Set appropriate retention periods
5. **Reserved capacity**: Consider Savings Plans for predictable workloads

## Security Best Practices

### Network Security
- Use private subnets for ECS tasks
- Implement security groups with minimal required access
- Enable VPC Flow Logs for network monitoring
- Use VPC endpoints to avoid internet traffic

### Container Security
- Use minimal base images
- Scan images for vulnerabilities
- Run containers as non-root users
- Implement resource limits

### Access Control
- Use IAM roles instead of access keys
- Implement least privilege access
- Enable CloudTrail for API logging
- Use AWS Config for compliance monitoring

## Next Steps

After deploying the ECS infrastructure:

1. **Configure CI/CD Pipeline**: Set up automated deployments
2. **Implement Monitoring**: Add custom metrics and alarms
3. **Set up Notifications**: Configure SNS for alerts
4. **Performance Testing**: Load test the application
5. **Disaster Recovery**: Implement backup and recovery procedures
6. **Security Hardening**: Regular security assessments and updates

## Related Documentation

- [VPC Implementation](./modules/vpc/README.md)
- [Security Groups Configuration](./modules/security-groups/README.md)
- [RDS Implementation](./RDS_IMPLEMENTATION.md)
- [Terraform Best Practices](./README.md)