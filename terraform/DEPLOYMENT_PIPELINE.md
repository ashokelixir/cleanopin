# Terraform Deployment Pipeline Documentation

This document provides comprehensive documentation for the Terraform deployment pipeline configuration, including all scripts, workflows, and best practices for managing infrastructure deployments.

## Overview

The Terraform deployment pipeline provides a complete CI/CD solution for infrastructure management with the following key features:

- **Multi-environment support** (dev, staging, prod)
- **State management and locking** with S3 and DynamoDB
- **Automated testing** with comprehensive test suites
- **Drift detection and remediation**
- **Environment promotion workflows**
- **Security and compliance validation**
- **Comprehensive logging and reporting**

## Architecture

```
terraform/
├── scripts/                    # Deployment and management scripts
│   ├── ci-cd-pipeline.ps1     # Main CI/CD orchestration script
│   ├── pipeline-deploy.ps1    # Enhanced deployment script
│   ├── init-backend.ps1       # Backend infrastructure setup
│   ├── promote-environment.ps1 # Environment promotion workflow
│   ├── drift-detection.ps1    # Configuration drift detection
│   ├── run-tests.ps1          # Infrastructure test runner
│   └── state-management.ps1   # State backup and management
├── tests/                     # Infrastructure test suites
│   ├── vpc.test.ps1          # VPC infrastructure tests
│   ├── rds.test.ps1          # RDS database tests
│   ├── ecs.test.ps1          # ECS container tests
│   ├── security.test.ps1     # Security configuration tests
│   └── integration.test.ps1  # End-to-end integration tests
├── backend-configs/           # Environment-specific backend configs
├── environments/              # Environment variable files
├── logs/                     # Execution logs
├── reports/                  # Test and deployment reports
└── state-backups/           # State backup files
```

## Scripts Overview

### 1. CI/CD Pipeline Script (`ci-cd-pipeline.ps1`)

The main orchestration script that manages the complete CI/CD workflow.

**Usage:**
```powershell
.\ci-cd-pipeline.ps1 -Environment <env> -Action <action> [options]
```

**Actions:**
- `validate` - Validate Terraform configuration
- `plan` - Generate execution plan
- `apply` - Apply infrastructure changes
- `promote` - Promote between environments

**Key Features:**
- Automated prerequisite validation
- Parallel test execution
- Slack/webhook notifications
- Comprehensive error handling
- Detailed logging and reporting

**Example:**
```powershell
# Plan changes for staging environment
.\ci-cd-pipeline.ps1 -Environment staging -Action plan -EnableDriftDetection

# Apply changes to production with auto-approval
.\ci-cd-pipeline.ps1 -Environment prod -Action apply -AutoApprove -NotificationWebhook "https://hooks.slack.com/..."
```

### 2. Pipeline Deploy Script (`pipeline-deploy.ps1`)

Enhanced deployment script with advanced features for production use.

**Usage:**
```powershell
.\pipeline-deploy.ps1 -Environment <env> -Action <action> [options]
```

**Key Features:**
- State locking and encryption
- Drift detection integration
- Infrastructure testing
- Environment promotion
- Production safety checks

**Example:**
```powershell
# Deploy to development with drift check
.\pipeline-deploy.ps1 -Environment dev -Action apply -DriftCheck

# Promote from staging to production
.\pipeline-deploy.ps1 -Environment prod -Action promote -PromoteFrom staging
```

### 3. Backend Initialization (`init-backend.ps1`)

Sets up the Terraform backend infrastructure with S3 and DynamoDB.

**Usage:**
```powershell
.\init-backend.ps1 -Region <region> -Environments @("dev","staging","prod") [options]
```

**Features:**
- KMS encryption for state files
- S3 versioning and lifecycle policies
- DynamoDB state locking
- CloudWatch monitoring
- Security best practices

**Example:**
```powershell
# Initialize backend for all environments
.\init-backend.ps1 -Region ap-south-1 -EnableEncryption -EnableMonitoring
```

### 4. Environment Promotion (`promote-environment.ps1`)

Manages promotion of infrastructure changes between environments.

**Usage:**
```powershell
.\promote-environment.ps1 -FromEnvironment <env> -ToEnvironment <env> [options]
```

**Features:**
- Validation of promotion paths (dev→staging→prod)
- Source environment drift detection
- Automated testing post-promotion
- Rollback capabilities
- Notification integration

**Example:**
```powershell
# Promote from dev to staging
.\promote-environment.ps1 -FromEnvironment dev -ToEnvironment staging -NotificationWebhook "https://hooks.slack.com/..."
```

### 5. Drift Detection (`drift-detection.ps1`)

Detects and optionally remediates configuration drift.

**Usage:**
```powershell
.\drift-detection.ps1 -Environment <env> [options]
```

**Features:**
- Automated drift detection
- Severity assessment
- Auto-remediation capabilities
- Detailed reporting
- Alert notifications

**Example:**
```powershell
# Check for drift in production
.\drift-detection.ps1 -Environment prod -GenerateReport -NotificationWebhook "https://hooks.slack.com/..."

# Auto-remediate drift in development
.\drift-detection.ps1 -Environment dev -AutoRemediate
```

### 6. Test Runner (`run-tests.ps1`)

Orchestrates infrastructure testing with multiple test suites.

**Usage:**
```powershell
.\run-tests.ps1 -Environment <env> -TestSuite <suite> [options]
```

**Test Suites:**
- `unit` - Individual component tests (VPC, RDS, ECS)
- `integration` - End-to-end integration tests
- `security` - Security configuration validation
- `all` - Complete test suite

**Features:**
- Parallel test execution
- Multiple output formats (console, JSON, JUnit, HTML)
- Timeout handling
- Comprehensive reporting

**Example:**
```powershell
# Run all tests in parallel with HTML report
.\run-tests.ps1 -Environment staging -TestSuite all -Parallel -OutputFormat html

# Run security tests only
.\run-tests.ps1 -Environment prod -TestSuite security -GenerateReport
```

### 7. State Management (`state-management.ps1`)

Comprehensive Terraform state management and backup solution.

**Usage:**
```powershell
.\state-management.ps1 -Environment <env> -Action <action> [options]
```

**Actions:**
- `backup` - Create state backup
- `restore` - Restore from backup
- `list-backups` - List available backups
- `cleanup` - Clean old backups
- `validate` - Validate state integrity
- `import` - Import existing resources
- `remove` - Remove resources from state

**Example:**
```powershell
# Create backup before major changes
.\state-management.ps1 -Environment prod -Action backup

# Restore from specific backup
.\state-management.ps1 -Environment staging -Action restore -BackupName "state-backup-staging-20241201-120000"
```

## Infrastructure Tests

### Test Categories

#### 1. VPC Tests (`vpc.test.ps1`)
- VPC existence and configuration
- Subnet distribution across AZs
- Internet Gateway configuration
- NAT Gateway setup
- Security group validation

#### 2. RDS Tests (`rds.test.ps1`)
- Database instance availability
- Encryption configuration
- Backup and monitoring setup
- Security group connectivity
- Secrets Manager integration

#### 3. ECS Tests (`ecs.test.ps1`)
- Cluster and service status
- Task definition validation
- Load balancer integration
- Auto-scaling configuration
- Health check validation

#### 4. Security Tests (`security.test.ps1`)
- S3 bucket security (encryption, public access)
- Security group rule validation
- IAM role and policy review
- Secrets Manager encryption
- SSL/TLS configuration

#### 5. Integration Tests (`integration.test.ps1`)
- Cross-AZ redundancy
- VPC to RDS connectivity
- ALB to ECS integration
- NAT Gateway routing
- CloudWatch Logs integration

## Workflow Examples

### Standard Deployment Workflow

1. **Development Environment:**
```powershell
# Deploy to development
.\ci-cd-pipeline.ps1 -Environment dev -Action apply

# Run tests
.\run-tests.ps1 -Environment dev -TestSuite all

# Check for drift
.\drift-detection.ps1 -Environment dev
```

2. **Staging Promotion:**
```powershell
# Promote to staging
.\promote-environment.ps1 -FromEnvironment dev -ToEnvironment staging

# Validate staging deployment
.\run-tests.ps1 -Environment staging -TestSuite integration
```

3. **Production Deployment:**
```powershell
# Create production backup
.\state-management.ps1 -Environment prod -Action backup

# Promote to production
.\promote-environment.ps1 -FromEnvironment staging -ToEnvironment prod -NotificationWebhook "https://hooks.slack.com/..."

# Validate production deployment
.\run-tests.ps1 -Environment prod -TestSuite security
```

### CI/CD Integration

#### GitHub Actions Example

```yaml
name: Infrastructure Deployment

on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  terraform:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Configure AWS credentials
        uses: aws-actions/configure-aws-credentials@v2
        with:
          aws-access-key-id: ${{ secrets.AWS_ACCESS_KEY_ID }}
          aws-secret-access-key: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
          aws-region: us-east-1
      
      - name: Terraform Plan
        run: |
          .\terraform\scripts\ci-cd-pipeline.ps1 -Environment dev -Action plan -EnableDriftDetection
        
      - name: Terraform Apply
        if: github.ref == 'refs/heads/main'
        run: |
          .\terraform\scripts\ci-cd-pipeline.ps1 -Environment dev -Action apply -AutoApprove -NotificationWebhook ${{ secrets.SLACK_WEBHOOK }}
      
      - name: Run Tests
        run: |
          .\terraform\scripts\run-tests.ps1 -Environment dev -TestSuite all -OutputFormat junit
      
      - name: Publish Test Results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: Infrastructure Tests
          path: terraform/reports/*.xml
          reporter: java-junit
```

#### Azure DevOps Pipeline Example

```yaml
trigger:
  branches:
    include:
      - main

pool:
  vmImage: 'windows-latest'

variables:
  - group: terraform-variables

stages:
  - stage: Plan
    jobs:
      - job: TerraformPlan
        steps:
          - task: PowerShell@2
            displayName: 'Terraform Plan'
            inputs:
              targetType: 'filePath'
              filePath: 'terraform/scripts/ci-cd-pipeline.ps1'
              arguments: '-Environment dev -Action plan'
              
  - stage: Apply
    condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
    jobs:
      - deployment: TerraformApply
        environment: 'development'
        strategy:
          runOnce:
            deploy:
              steps:
                - task: PowerShell@2
                  displayName: 'Terraform Apply'
                  inputs:
                    targetType: 'filePath'
                    filePath: 'terraform/scripts/ci-cd-pipeline.ps1'
                    arguments: '-Environment dev -Action apply -AutoApprove'
                    
                - task: PowerShell@2
                  displayName: 'Run Tests'
                  inputs:
                    targetType: 'filePath'
                    filePath: 'terraform/scripts/run-tests.ps1'
                    arguments: '-Environment dev -TestSuite all -OutputFormat junit'
                    
                - task: PublishTestResults@2
                  displayName: 'Publish Test Results'
                  inputs:
                    testResultsFormat: 'JUnit'
                    testResultsFiles: 'terraform/reports/*.xml'
```

## Configuration

### Environment Variables

Set the following environment variables for CI/CD integration:

```powershell
# AWS Configuration
$env:AWS_ACCESS_KEY_ID = "your-access-key"
$env:AWS_SECRET_ACCESS_KEY = "your-secret-key"
$env:AWS_DEFAULT_REGION = "us-east-1"

# Notification Configuration
$env:SLACK_WEBHOOK_URL = "https://hooks.slack.com/services/..."
$env:TEAMS_WEBHOOK_URL = "https://outlook.office.com/webhook/..."

# Pipeline Configuration
$env:CI = "true"  # Enables CI mode
$env:TERRAFORM_AUTO_APPROVE = "true"  # For automated deployments
```

### Backend Configuration Files

Create environment-specific backend configuration files:

**backend-configs/dev.hcl:**
```hcl
bucket         = "cleanarch-template-terraform-state-dev"
key            = "terraform.tfstate"
region         = "us-east-1"
dynamodb_table = "cleanarch-template-terraform-locks-dev"
encrypt        = true
```

**backend-configs/staging.hcl:**
```hcl
bucket         = "cleanarch-template-terraform-state-staging"
key            = "terraform.tfstate"
region         = "us-east-1"
dynamodb_table = "cleanarch-template-terraform-locks-staging"
encrypt        = true
```

**backend-configs/prod.hcl:**
```hcl
bucket         = "cleanarch-template-terraform-state-prod"
key            = "terraform.tfstate"
region         = "us-east-1"
dynamodb_table = "cleanarch-template-terraform-locks-prod"
encrypt        = true
```

## Security Best Practices

### State File Security
- All state files are encrypted with KMS
- S3 buckets have public access blocked
- DynamoDB tables use encryption at rest
- Access is controlled via IAM policies

### Deployment Security
- Production deployments require explicit confirmation
- All operations are logged and auditable
- Secrets are managed via AWS Secrets Manager
- Security tests validate configurations

### Access Control
- Use IAM roles with least privilege principle
- Implement MFA for production access
- Regular access reviews and rotation
- Separate credentials per environment

## Monitoring and Alerting

### CloudWatch Integration
- All scripts log to CloudWatch
- Custom metrics for deployment success/failure
- Alarms for drift detection
- Dashboard for infrastructure health

### Notification Channels
- Slack integration for team notifications
- Email alerts for critical issues
- Teams integration for enterprise environments
- Custom webhook support

## Troubleshooting

### Common Issues

#### State Lock Issues
```powershell
# Check lock status
.\state-management.ps1 -Environment dev -Action validate

# Force unlock (use with caution)
.\state-management.ps1 -Environment dev -Action unlock -LockId "lock-id" -Force
```

#### Drift Detection
```powershell
# Check for drift
.\drift-detection.ps1 -Environment prod -GenerateReport

# Auto-remediate (non-production only)
.\drift-detection.ps1 -Environment dev -AutoRemediate
```

#### Test Failures
```powershell
# Run specific test suite
.\run-tests.ps1 -Environment staging -TestSuite security -ContinueOnFailure

# Generate detailed HTML report
.\run-tests.ps1 -Environment staging -TestSuite all -OutputFormat html
```

### Log Analysis

All scripts generate detailed logs in the `logs/` directory:

```powershell
# View recent deployment logs
Get-ChildItem terraform/logs/ | Sort-Object LastWriteTime -Descending | Select-Object -First 5

# Search for errors
Select-String -Path "terraform/logs/*.log" -Pattern "ERROR" | Select-Object -Last 10
```

## Best Practices

### Development Workflow
1. Always test changes in development first
2. Run full test suite before promotion
3. Create state backups before major changes
4. Use drift detection regularly
5. Monitor logs and notifications

### Production Deployments
1. Require manual approval for production
2. Create comprehensive backups
3. Run security tests post-deployment
4. Monitor for drift immediately after deployment
5. Have rollback plan ready

### State Management
1. Regular automated backups
2. Test restore procedures
3. Monitor state file size and complexity
4. Clean up old backups regularly
5. Validate state integrity periodically

## Support and Maintenance

### Regular Maintenance Tasks

1. **Weekly:**
   - Review drift detection reports
   - Clean up old logs and backups
   - Update test cases as needed

2. **Monthly:**
   - Review and update IAM policies
   - Test disaster recovery procedures
   - Update documentation

3. **Quarterly:**
   - Security audit of all configurations
   - Performance review of pipeline
   - Update Terraform and provider versions

### Getting Help

For issues with the deployment pipeline:

1. Check the logs in `terraform/logs/`
2. Review the test reports in `terraform/reports/`
3. Validate state integrity with state management script
4. Check AWS CloudWatch for infrastructure issues
5. Review notification channels for alerts

This comprehensive pipeline provides enterprise-grade infrastructure deployment capabilities with security, reliability, and maintainability as core principles.