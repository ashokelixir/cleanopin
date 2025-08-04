#!/bin/bash

# Initialize LocalStack services for development
echo "Initializing LocalStack services..."

# Create SQS queues
awslocal sqs create-queue --queue-name user-events-queue
awslocal sqs create-queue --queue-name user-events-dlq
awslocal sqs create-queue --queue-name notification-queue
awslocal sqs create-queue --queue-name notification-dlq

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