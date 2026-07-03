#!/usr/bin/env bash
set -euo pipefail

AWS_REGION=${AWS_REGION:-us-east-1}
FIAPX_PROJECT=${FIAPX_PROJECT:-fiapx}
FIAPX_ENVIRONMENT=${FIAPX_ENVIRONMENT:-dev}
LOCALSTACK_ACCOUNT_ID=${LOCALSTACK_ACCOUNT_ID:-000000000000}
RESOURCE_PREFIX="${FIAPX_PROJECT}-${FIAPX_ENVIRONMENT}"
BUCKET=${FIAPX_BUCKET:-${RESOURCE_PREFIX}-artifacts-${LOCALSTACK_ACCOUNT_ID}}

if ! awslocal --region "$AWS_REGION" s3api head-bucket --bucket "$BUCKET" >/dev/null 2>&1; then
  awslocal --region "$AWS_REGION" s3api create-bucket --bucket "$BUCKET" >/dev/null
fi

awslocal --region "$AWS_REGION" s3api put-object --bucket "$BUCKET" --key videos/ >/dev/null
awslocal --region "$AWS_REGION" s3api put-object --bucket "$BUCKET" --key frames/ >/dev/null

echo "Bucket '$BUCKET' is ready."
