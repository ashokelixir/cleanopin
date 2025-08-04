# IAM Module

This module creates IAM roles and policies for the Clean Architecture Template following the principle of least privilege.

## Features

- **ECS Task Execution Role**: For ECS to pull images from ECR and write logs to CloudWatch
- **ECS Task Role**: For the application running in containers to access AWS services
- **CI/CD Cross-Account Role**: For cross-account deployments from CI/CD pipelines
- **Lambda Execution Role**: Optional role for Lambda functions
- **Security Policies**: Enhanced security policies for production environments

## Usage

```hcl
module "iam" {
  source = "./modules/iam"

  name_prefix = "cleanarch-dev"
  environment = "dev"

  # Secrets Manager ARNs
  secrets_manager_arns = [
    "arn:aws:secretsmanager:us-east-1:123456789012:secret:cleanarch-dev-db-secret-abc123",
    "arn:aws:secretsmanager:us-east-1:123456789012:secret:cleanarch-dev-app-secret-def456"
  ]

  # SQS Queue ARNs
  sqs_queue_arns = [
    "arn:aws:sqs:us-east-1:123456789012:cleanarch-dev-queue",
    "arn:aws:sqs:us-east-1:123456789012:cleanarch-dev-dlq"
  ]

  # CI/CD Configuration
  cicd_account_ids = ["987654321098"]
  cicd_external_id = "unique-external-id"
  ecr_repository_arns = [
    "arn:aws:ecr:us-east-1:123456789012:repository/cleanarch"
  ]

  # Optional features
  enable_xray = true
  enable_s3_access = true
  s3_bucket_arns = [
    "arn:aws:s3:::cleanarch-dev-uploads"
  ]

  tags = {
    Environment = "dev"
    Project     = "CleanArchTemplate"
  }
}
```

## Roles Created

### ECS Task Execution Role

**Purpose**: Used by ECS to manage the container lifecycle
**Permissions**:
- Pull images from ECR
- Write logs to CloudWatch
- Retrieve secrets from Secrets Manager

### ECS Task Role

**Purpose**: Used by the application running in the container
**Permissions**:
- Access Secrets Manager secrets
- Send/receive SQS messages
- Write CloudWatch metrics and logs
- X-Ray tracing (optional)
- S3 access (optional)
- SNS publishing (optional)

### CI/CD Cross-Account Role

**Purpose**: Allows CI/CD pipelines from other accounts to deploy
**Permissions**:
- Update ECS services
- Register task definitions
- Push images to ECR
- Pass IAM roles to ECS

## Security Features

- **Principle of Least Privilege**: Each role has only the minimum required permissions
- **Resource-Based Conditions**: Policies include conditions to restrict access
- **Account Isolation**: Cross-account access requires external ID
- **Environment-Specific Policies**: Enhanced security for production environments
- **Service-Linked Roles**: Automatic creation of required service-linked roles

## Variables

| Name | Description | Type | Default | Required |
|------|-------------|------|---------|:--------:|
| name_prefix | Prefix for resource names | `string` | n/a | yes |
| environment | Environment name (dev, staging, prod) | `string` | `"dev"` | no |
| secrets_manager_arns | ARNs of Secrets Manager secrets | `list(string)` | `[]` | no |
| sqs_queue_arns | ARNs of SQS queues | `list(string)` | `[]` | no |
| cicd_account_ids | CI/CD account IDs for cross-account access | `list(string)` | `[]` | no |
| cicd_external_id | External ID for CI/CD role assumption | `string` | `""` | no |
| ecr_repository_arns | ARNs of ECR repositories | `list(string)` | `[]` | no |
| enable_xray | Enable X-Ray tracing permissions | `bool` | `true` | no |
| enable_s3_access | Enable S3 access | `bool` | `false` | no |
| s3_bucket_arns | ARNs of S3 buckets | `list(string)` | `[]` | no |
| enable_sns_access | Enable SNS access | `bool` | `false` | no |
| sns_topic_arns | ARNs of SNS topics | `list(string)` | `[]` | no |

## Outputs

| Name | Description |
|------|-------------|
| ecs_task_execution_role_arn | ARN of the ECS task execution role |
| ecs_task_role_arn | ARN of the ECS task role |
| cicd_cross_account_role_arn | ARN of the CI/CD cross-account role |
| lambda_execution_role_arn | ARN of the Lambda execution role |
| all_role_arns | Map of all created IAM role ARNs |

## Best Practices

1. **Regular Review**: Regularly review and audit IAM policies
2. **Least Privilege**: Only grant permissions that are actually needed
3. **Resource Conditions**: Use resource-based conditions to limit scope
4. **External IDs**: Always use external IDs for cross-account access
5. **Environment Separation**: Use different roles for different environments
6. **Monitoring**: Enable CloudTrail to monitor IAM usage

## Security Considerations

- All policies include account-based conditions to prevent cross-account access
- Production environments have additional security restrictions
- Cross-account roles require external ID for assumption
- Resource ARNs are explicitly specified rather than using wildcards
- Sensitive actions are explicitly denied in production environments