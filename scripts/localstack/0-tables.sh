#!/usr/bin/env bash
set -euo pipefail

AWS_REGION=${AWS_REGION:-us-east-1}
FIAPX_PROJECT=${FIAPX_PROJECT:-fiapx}
FIAPX_ENVIRONMENT=${FIAPX_ENVIRONMENT:-dev}
RESOURCE_PREFIX="${FIAPX_PROJECT}-${FIAPX_ENVIRONMENT}"
TABLE=${FIAPX_PROCESSING_JOBS_TABLE:-${RESOURCE_PREFIX}-videos-db}

awslocal --region "$AWS_REGION" dynamodb create-table \
  --table-name "$TABLE" \
  --billing-mode PAY_PER_REQUEST \
  --attribute-definitions \
    AttributeName=id,AttributeType=S \
    AttributeName=userId,AttributeType=S \
    AttributeName=resultFileId,AttributeType=S \
  --key-schema AttributeName=id,KeyType=HASH \
  --global-secondary-indexes '[{"IndexName":"userId-index","KeySchema":[{"AttributeName":"userId","KeyType":"HASH"}],"Projection":{"ProjectionType":"ALL"}},{"IndexName":"resultFileId-index","KeySchema":[{"AttributeName":"resultFileId","KeyType":"HASH"}],"Projection":{"ProjectionType":"ALL"}}]' >/dev/null

echo "Table '$TABLE' created."
