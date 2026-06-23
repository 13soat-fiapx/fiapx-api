#!/usr/bin/env bash
set -euo pipefail

AWS_REGION=${AWS_REGION:-us-east-1}
BUCKET=${FIAPX_BUCKET:-fiapx-media}
TABLE=${FIAPX_PROCESSING_JOBS_TABLE:-fiapx-processing-jobs}
REQUESTED_QUEUE=${FIAPX_REQUESTED_QUEUE:-video-processing-requested}
REQUESTED_DLQ=${FIAPX_REQUESTED_DLQ:-video-processing-requested-dlq}
COMPLETED_QUEUE=${FIAPX_COMPLETED_QUEUE:-video-processing-completed}
ENDPOINT_URL=${AWS_ENDPOINT_URL:-http://localhost:4566}

aws_local() {
  aws --endpoint-url="$ENDPOINT_URL" --region "$AWS_REGION" "$@"
}

if ! aws_local s3api head-bucket --bucket "$BUCKET" >/dev/null 2>&1; then
  aws_local s3api create-bucket --bucket "$BUCKET"
fi

aws_local s3api put-object --bucket "$BUCKET" --key videos/ >/dev/null
aws_local s3api put-object --bucket "$BUCKET" --key frames/ >/dev/null

if ! aws_local dynamodb describe-table --table-name "$TABLE" >/dev/null 2>&1; then
  aws_local dynamodb create-table \
    --table-name "$TABLE" \
    --billing-mode PAY_PER_REQUEST \
    --attribute-definitions \
      AttributeName=id,AttributeType=S \
      AttributeName=userId,AttributeType=S \
      AttributeName=resultFileId,AttributeType=S \
    --key-schema AttributeName=id,KeyType=HASH \
    --global-secondary-indexes '[{"IndexName":"userId-index","KeySchema":[{"AttributeName":"userId","KeyType":"HASH"}],"Projection":{"ProjectionType":"ALL"}},{"IndexName":"resultFileId-index","KeySchema":[{"AttributeName":"resultFileId","KeyType":"HASH"}],"Projection":{"ProjectionType":"ALL"}}]' >/dev/null
fi

for queue in "$REQUESTED_QUEUE" "$REQUESTED_DLQ" "$COMPLETED_QUEUE"; do
  aws_local sqs create-queue --queue-name "$queue" >/dev/null
done

echo "FIAP X local AWS resources are ready."