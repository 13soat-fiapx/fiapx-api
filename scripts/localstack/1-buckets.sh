#!/usr/bin/env bash
set -euo pipefail

AWS_REGION=${AWS_REGION:-us-east-1}
FIAPX_PROJECT=${FIAPX_PROJECT:-fiapx}
FIAPX_ENVIRONMENT=${FIAPX_ENVIRONMENT:-dev}
LOCALSTACK_ACCOUNT_ID=${LOCALSTACK_ACCOUNT_ID:-000000000000}
RESOURCE_PREFIX="${FIAPX_PROJECT}-${FIAPX_ENVIRONMENT}"
BUCKET=${FIAPX_BUCKET:-${RESOURCE_PREFIX}-artifacts-${LOCALSTACK_ACCOUNT_ID}}

awslocal --region "$AWS_REGION" s3api create-bucket --bucket "$BUCKET" >/dev/null

awslocal --region "$AWS_REGION" s3api put-object --bucket "$BUCKET" --key videos/ >/dev/null
awslocal --region "$AWS_REGION" s3api put-object --bucket "$BUCKET" --key frames/ >/dev/null

awslocal --region "$AWS_REGION" s3api put-bucket-cors \
  --bucket "$BUCKET" \
  --cors-configuration '{
    "CORSRules": [
      {
        "AllowedOrigins": ["http://localhost:8080", "https://d2nyagk7gn75jo.cloudfront.net"],
        "AllowedMethods": ["GET", "HEAD", "PUT"],
        "AllowedHeaders": ["*"],
        "ExposeHeaders": ["ETag"],
        "MaxAgeSeconds": 3000
      }
    ]
  }'


echo "Bucket '$BUCKET' is ready."
