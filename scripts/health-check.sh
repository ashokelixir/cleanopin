#!/bin/sh

# Health check script for Docker container
# This script is used by the HEALTHCHECK instruction in Dockerfile

set -e

# Check if the application is responding
if wget --no-verbose --tries=1 --spider --timeout=10 http://localhost:8080/health; then
    echo "Health check passed"
    exit 0
else
    echo "Health check failed"
    exit 1
fi