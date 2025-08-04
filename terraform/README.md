# Terraform Infrastructure for Clean Architecture Template

This directory contains the Terraform infrastructure code for deploying the Clean Architecture Template application to AWS. The infrastructure follows best practices for security, scalability, and maintainability.

## Architecture Overview

The infrastructure includes:

- **VPC**: Multi-AZ VPC with public, private, and database subnets
- **Security Groups**: Layered security with least-privilege access
- **RDS PostgreSQL**: Multi-AZ database with automated backups
- **ElastiCache Redis**: Distributed caching layer
- **ECS Fargate**: Containerized application hosting
- **Application Load Balancer**: High-availability load balancing
- **SQS**: Message queuing for asynchronous processing
- **Secrets Manager**: Secure configuration management
- **CloudWatch**: Monitoring and logging
- **VPC Endpoints**: Secure AWS service access

## Core AWS Infrastructure (Task 17 - ✅ Completed)

The core AWS infrastructure has been implemented with the following components:

### VPC Module Features
- **Custom VPC** with DNS support and hostnames enabled
- **Multi-AZ Subnets**:
  - Public subnets (3 AZs) for load balancers and NAT gateways
  - Private subnets (3 AZs) for application workloads
  - Database subnets (3 AZs) for data tier isolation
- **Internet Gateway** for public internet access
- **NAT Gateways** (one per AZ) for secure outbound internet access from private subnets
- **Route Tables** with proper routing for each subnet tier

### Network Security (Defense in Depth)
- **Network ACLs** providing an additional security layer:
  - Public NACL: HTTP/HTTPS inbound, all outbound
  - Private NACL: VPC traffic and ephemeral ports for return traffic
  - Database NACL: Database ports (5432, 6379) from VPC only
- **Security Groups** for application, database, and cache tiers:
  - ALB Security Group: HTTP/HTTPS from internet
  - ECS Security Group: Application port from ALB, outbound to databases
  - RDS Security Group: PostgreSQL (5432) from ECS and bastion
  - Redis Security Group: Redis (6379) from ECS and bastion
  - Bastion Security Group: SSH from specified CIDR blocks
  - VPC Endpoints Security Group: HTTPS from VPC CIDR

### VPC Endpoints (Private AWS Service Access)
- **S3 Gateway Endpoint**: Cost-effective S3 access without internet routing
- **ECR Interface Endpoints**: Container image pulls (dkr and api)
- **Secrets Manager Endpoint**: Secure configuration retrieval
- **CloudWatch Logs Endpoint**: Log shipping without internet
- **CloudWatch Monitoring Endpoint**: Metrics collection

### Security Features
- **Least Privilege Access**: Minimal required permissions between tiers
- **Network Segmentation**: Separate subnets for each application tier
- **Private Connectivity**: VPC endpoints eliminate internet routing for AWS services
- **Bastion Host Support**: Secure SSH access to private resources
- **CIDR Restrictions**: SSH access limited to specified networks

## Directory Structure

```
terraform/
├── main.tf                    # Main Terraform configuration
├── variables.tf               # Global variables
├── locals.tf                  # Local values and computed configurations
├── outputs.tf                 # Output values
├── README.md                  # This file
├── backend-configs/           # Backend configuration files
│   ├── dev.hcl
│   ├── staging.hcl
│   └── prod.hcl
├── environments/              # Environment-specific variable files
│   ├── dev.tfvars
│   ├── staging.tfvars
│   └── prod.tfvars
├── modules/                   # Reusable Terraform modules
│   ├── vpc/                   # VPC and networking
│   └── security-groups/       # Security group configurations
└── scripts/                   # Deployment and utility scripts
    ├── init-backend.ps1       # Initialize S3 backend (PowerShell)
    ├── init-backend.sh        # Initialize S3 backend (Bash)
    ├── deploy.ps1             # Deployment script (PowerShell)
    └── deploy.sh              # Deployment script (Bash)
```

## Prerequisites

1. **AWS CLI**: Install and configure with appropriate credentials
   ```bash
   aws configure
   ```

2. **Terraform**: Install Terraform >= 1.5
   ```bash
   # Windows (using Chocolatey)
   choco install terraform
   
   # macOS (using Homebrew)
   brew install terraform
   
   # Linux (using package manager or direct download)
   ```

3. **AWS Credentials**: Ensure your AWS credentials have the following permissions:
   - EC2 (VPC, Subnets, Security Groups, etc.)
   - RDS (Database instances, subnet groups)
   - ElastiCache (Redis clusters)
   - ECS (Clusters, services, task definitions)
   - ELB (Application Load Balancers)
   - SQS (Queues)
   - Secrets Manager
   - CloudWatch
   - IAM (Roles and policies)
   - S3 (For Terraform state)
   - DynamoDB (For state locking)

## Environment Configuration

### Supported Environments

- **dev**: Development environment with minimal resources
- **staging**: Staging environment with production-like setup
- **prod**: Production environment with high availability

### Environment-Specific Settings

Each environment has different resource configurations:

| Resource | Dev | Staging | Prod |
|----------|-----|---------|------|
| DB Instance | db.t3.micro | db.t3.small | db.t3.medium |
| DB Multi-AZ | No | Yes | Yes |
| Redis Nodes | 1 | 2 | 3 |
| ECS Tasks | 1 | 2 | 3 |
| Task CPU | 256 | 512 | 1024 |
| Task Memory | 512 MB | 1024 MB | 2048 MB |

## Quick Start

### 1. Initialize Backend Infrastructure

First, create the S3 buckets and DynamoDB tables for Terraform state management:

**PowerShell (Windows):**
```powershell
.\scripts\init-backend.ps1
```

**Bash (Linux/macOS):**
```bash
./scripts/init-backend.sh
```

### 2. Initialize Terraform

**PowerShell:**
```powershell
.\scripts\deploy.ps1 -Environment dev -Action init
```

**Bash:**
```bash
./scripts/deploy.sh dev init
```

### 3. Plan Infrastructure Changes

**PowerShell:**
```powershell
.\scripts\deploy.ps1 -Environment dev -Action plan
```

**Bash:**
```bash
./scripts/deploy.sh dev plan
```

### 4. Apply Infrastructure

**PowerShell:**
```powershell
.\scripts\deploy.ps1 -Environment dev -Action apply
```

**Bash:**
```bash
./scripts/deploy.sh dev apply
```

## Manual Terraform Commands

If you prefer to run Terraform commands manually:

### Initialize with Backend Configuration
```bash
terraform init -backend-config=backend-configs/dev.hcl
```

### Create/Select Workspace
```bash
terraform workspace new dev
# or
terraform workspace select dev
```

### Plan Changes
```bash
terraform plan -var-file=environments/dev.tfvars
```

### Apply Changes
```bash
terraform apply -var-file=environments/dev.tfvars
```

### Destroy Infrastructure
```bash
terraform destroy -var-file=environments/dev.tfvars
```

## Terraform Workspaces

This configuration uses Terraform workspaces to manage multiple environments:

- **dev**: Development environment
- **staging**: Staging environment  
- **prod**: Production environment

Each workspace maintains separate state files and can have different variable values.

### Workspace Commands
```bash
# List workspaces
terraform workspace list

# Create new workspace
terraform workspace new <environment>

# Select workspace
terraform workspace select <environment>

# Show current workspace
terraform workspace show

# Delete workspace (be careful!)
terraform workspace delete <environment>
```

## State Management

### Backend Configuration

Terraform state is stored in S3 with DynamoDB for locking:

- **S3 Bucket**: `cleanarch-template-terraform-state-<env>`
- **DynamoDB Table**: `cleanarch-template-terraform-locks-<env>`
- **Encryption**: AES256 server-side encryption
- **Versioning**: Enabled for state recovery

### State Commands
```bash
# Show current state
terraform show

# List resources in state
terraform state list

# Show specific resource
terraform state show <resource_name>

# Import existing resource
terraform import <resource_type>.<resource_name> <resource_id>
```

## Security Considerations

### Network Security
- Private subnets for application and database tiers
- Security groups with least-privilege access
- VPC endpoints for AWS service access
- Network ACLs for additional security layer

### Data Security
- Encryption at rest for RDS and ElastiCache
- Secrets stored in AWS Secrets Manager
- SSL/TLS encryption in transit
- Private subnet isolation

### Access Control
- IAM roles with minimal required permissions
- Resource-based policies where applicable
- Cross-account access controls for CI/CD

## Monitoring and Observability

### CloudWatch Integration
- Application and infrastructure metrics
- Custom dashboards for each environment
- Automated alerting for critical events
- Log aggregation and analysis

### Health Checks
- Application Load Balancer health checks
- ECS service health monitoring
- Database connection monitoring
- Cache availability checks

## Cost Optimization

### Environment-Specific Sizing
- Development: Minimal resources for cost efficiency
- Staging: Production-like for testing
- Production: Optimized for performance and availability

### Resource Management
- Auto-scaling for ECS services
- Scheduled scaling for predictable workloads
- Reserved instances for production databases
- Spot instances where appropriate

## Troubleshooting

### Common Issues

1. **Backend Initialization Fails**
   - Ensure AWS credentials are configured
   - Verify S3 bucket and DynamoDB table exist
   - Check IAM permissions

2. **Plan/Apply Fails**
   - Verify workspace selection
   - Check variable file path
   - Ensure resource limits aren't exceeded

3. **State Lock Issues**
   - Check DynamoDB table accessibility
   - Force unlock if necessary: `terraform force-unlock <lock_id>`

4. **Resource Creation Fails**
   - Check AWS service limits
   - Verify IAM permissions
   - Review CloudTrail logs for detailed errors

### Debugging Commands
```bash
# Enable detailed logging
export TF_LOG=DEBUG

# Validate configuration
terraform validate

# Format code
terraform fmt -recursive

# Check for security issues
terraform plan -out=plan.out
terraform show -json plan.out | jq
```

## Contributing

### Code Standards
- Use consistent naming conventions
- Add comments for complex logic
- Follow Terraform best practices
- Test changes in development first

### Module Development
- Keep modules focused and reusable
- Document input variables and outputs
- Include examples and README files
- Version modules appropriately

## Support

For issues and questions:
1. Check the troubleshooting section
2. Review Terraform and AWS documentation
3. Check CloudTrail logs for AWS API errors
4. Contact the platform team for infrastructure support

## References

- [Terraform Documentation](https://www.terraform.io/docs)
- [AWS Provider Documentation](https://registry.terraform.io/providers/hashicorp/aws/latest/docs)
- [Terraform Best Practices](https://www.terraform.io/docs/cloud/guides/recommended-practices/index.html)
- [AWS Well-Architected Framework](https://aws.amazon.com/architecture/well-architected/)