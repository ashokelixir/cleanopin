#!/bin/bash

# Initialize LocalStack services for development
echo "Initializing LocalStack services..."

# Create SQS queues
echo "Creating SQS queues..."

# Standard queues for event-driven architecture
awslocal sqs create-queue --queue-name user-events
awslocal sqs create-queue --queue-name user-events-dlq
awslocal sqs create-queue --queue-name permission-events
awslocal sqs create-queue --queue-name permission-events-dlq
awslocal sqs create-queue --queue-name user-permission-events
awslocal sqs create-queue --queue-name user-permission-events-dlq
awslocal sqs create-queue --queue-name role-events
awslocal sqs create-queue --queue-name role-events-dlq
awslocal sqs create-queue --queue-name role-permission-events
awslocal sqs create-queue --queue-name role-permission-events-dlq
awslocal sqs create-queue --queue-name user-role-events
awslocal sqs create-queue --queue-name user-role-events-dlq

# FIFO queues
awslocal sqs create-queue --queue-name audit-events.fifo --attributes FifoQueue=true,ContentBasedDeduplication=true
awslocal sqs create-queue --queue-name audit-events-dlq.fifo --attributes FifoQueue=true

# Configure dead letter queue redrive policies
echo "Configuring dead letter queue policies..."

# Get queue ARNs
USER_EVENTS_DLQ_ARN=$(awslocal sqs get-queue-attributes --queue-url http://localhost:4566/000000000000/user-events-dlq --attribute-names QueueArn --query 'Attributes.QueueArn' --output text)
PERMISSION_EVENTS_DLQ_ARN=$(awslocal sqs get-queue-attributes --queue-url http://localhost:4566/000000000000/permission-events-dlq --attribute-names QueueArn --query 'Attributes.QueueArn' --output text)
USER_PERMISSION_EVENTS_DLQ_ARN=$(awslocal sqs get-queue-attributes --queue-url http://localhost:4566/000000000000/user-permission-events-dlq --attribute-names QueueArn --query 'Attributes.QueueArn' --output text)
ROLE_EVENTS_DLQ_ARN=$(awslocal sqs get-queue-attributes --queue-url http://localhost:4566/000000000000/role-events-dlq --attribute-names QueueArn --query 'Attributes.QueueArn' --output text)
ROLE_PERMISSION_EVENTS_DLQ_ARN=$(awslocal sqs get-queue-attributes --queue-url http://localhost:4566/000000000000/role-permission-events-dlq --attribute-names QueueArn --query 'Attributes.QueueArn' --output text)
USER_ROLE_EVENTS_DLQ_ARN=$(awslocal sqs get-queue-attributes --queue-url http://localhost:4566/000000000000/user-role-events-dlq --attribute-names QueueArn --query 'Attributes.QueueArn' --output text)
AUDIT_EVENTS_DLQ_ARN=$(awslocal sqs get-queue-attributes --queue-url http://localhost:4566/000000000000/audit-events-dlq.fifo --attribute-names QueueArn --query 'Attributes.QueueArn' --output text)

# Set redrive policies
awslocal sqs set-queue-attributes --queue-url http://localhost:4566/000000000000/user-events --attributes RedrivePolicy="{\"deadLetterTargetArn\":\"$USER_EVENTS_DLQ_ARN\",\"maxReceiveCount\":3}"
awslocal sqs set-queue-attributes --queue-url http://localhost:4566/000000000000/permission-events --attributes RedrivePolicy="{\"deadLetterTargetArn\":\"$PERMISSION_EVENTS_DLQ_ARN\",\"maxReceiveCount\":3}"
awslocal sqs set-queue-attributes --queue-url http://localhost:4566/000000000000/user-permission-events --attributes RedrivePolicy="{\"deadLetterTargetArn\":\"$USER_PERMISSION_EVENTS_DLQ_ARN\",\"maxReceiveCount\":3}"
awslocal sqs set-queue-attributes --queue-url http://localhost:4566/000000000000/role-events --attributes RedrivePolicy="{\"deadLetterTargetArn\":\"$ROLE_EVENTS_DLQ_ARN\",\"maxReceiveCount\":3}"
awslocal sqs set-queue-attributes --queue-url http://localhost:4566/000000000000/role-permission-events --attributes RedrivePolicy="{\"deadLetterTargetArn\":\"$ROLE_PERMISSION_EVENTS_DLQ_ARN\",\"maxReceiveCount\":3}"
awslocal sqs set-queue-attributes --queue-url http://localhost:4566/000000000000/user-role-events --attributes RedrivePolicy="{\"deadLetterTargetArn\":\"$USER_ROLE_EVENTS_DLQ_ARN\",\"maxReceiveCount\":3}"
awslocal sqs set-queue-attributes --queue-url http://localhost:4566/000000000000/audit-events.fifo --attributes RedrivePolicy="{\"deadLetterTargetArn\":\"$AUDIT_EVENTS_DLQ_ARN\",\"maxReceiveCount\":5}"

echo "SQS queues created and configured successfully!"

# Create S3 buckets
awslocal s3 mb s3://cleanarch-dev-bucket

# Create Secrets Manager secrets
awslocal secretsmanager create-secret \
    --name "cleanarch/dev/database" \
    --description "Database connection string for development" \
    --secret-string '{"ConnectionString":"Host=postgres;Port=5432;Database=cleanarch_dev;Username=postgres;Password=WBn9uqfzyroot;"}'

awslocal secretsmanager create-secret \
    --name "cleanarch/dev/jwt" \
    --description "JWT settings for development" \
    --secret-string '{"SecretKey":"your-super-secret-key-that-is-at-least-256-bits-long-for-security","Issuer":"CleanArchTemplate","Audience":"CleanArchTemplate"}'

echo "LocalStack initialization completed!"