# ECS Task Execution Role Outputs
output "ecs_task_execution_role_arn" {
  description = "ARN of the ECS task execution role"
  value       = aws_iam_role.ecs_task_execution.arn
}

output "ecs_task_execution_role_name" {
  description = "Name of the ECS task execution role"
  value       = aws_iam_role.ecs_task_execution.name
}

# ECS Task Role Outputs
output "ecs_task_role_arn" {
  description = "ARN of the ECS task role"
  value       = aws_iam_role.ecs_task.arn
}

output "ecs_task_role_name" {
  description = "Name of the ECS task role"
  value       = aws_iam_role.ecs_task.name
}

# CI/CD Cross-Account Role Outputs
output "cicd_cross_account_role_arn" {
  description = "ARN of the CI/CD cross-account role"
  value       = length(var.cicd_account_ids) > 0 ? aws_iam_role.cicd_cross_account[0].arn : null
}

output "cicd_cross_account_role_name" {
  description = "Name of the CI/CD cross-account role"
  value       = length(var.cicd_account_ids) > 0 ? aws_iam_role.cicd_cross_account[0].name : null
}

# Lambda Execution Role Outputs
output "lambda_execution_role_arn" {
  description = "ARN of the Lambda execution role"
  value       = var.create_lambda_role ? aws_iam_role.lambda_execution[0].arn : null
}

output "lambda_execution_role_name" {
  description = "Name of the Lambda execution role"
  value       = var.create_lambda_role ? aws_iam_role.lambda_execution[0].name : null
}

# Role ARNs for easy reference
output "all_role_arns" {
  description = "Map of all created IAM role ARNs"
  value = {
    ecs_task_execution = aws_iam_role.ecs_task_execution.arn
    ecs_task           = aws_iam_role.ecs_task.arn
    cicd_cross_account = length(var.cicd_account_ids) > 0 ? aws_iam_role.cicd_cross_account[0].arn : null
    lambda_execution   = var.create_lambda_role ? aws_iam_role.lambda_execution[0].arn : null
  }
}

# Policy Names for reference
output "policy_names" {
  description = "Map of created IAM policy names"
  value = {
    ecs_task_execution_ecr     = aws_iam_role_policy.ecs_task_execution_ecr.name
    ecs_task_execution_logs    = aws_iam_role_policy.ecs_task_execution_logs.name
    ecs_task_execution_secrets = aws_iam_role_policy.ecs_task_execution_secrets.name
    ecs_task_secrets           = aws_iam_role_policy.ecs_task_secrets.name
    ecs_task_cloudwatch        = aws_iam_role_policy.ecs_task_cloudwatch.name
  }
}