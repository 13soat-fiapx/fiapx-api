#!/usr/bin/env bash
set -euo pipefail

AWS_REGION=${AWS_REGION:-us-east-1}
BUCKET=${FIAPX_BUCKET:-fiapx-media}
TABLE=${FIAPX_PROCESSING_JOBS_TABLE:-fiapx-processing-jobs}
REQUESTED_QUEUE=${FIAPX_REQUESTED_QUEUE:-video-processing-requested}
REQUESTED_DLQ=${FIAPX_REQUESTED_DLQ:-video-processing-requested-dlq}
COMPLETED_QUEUE=${FIAPX_COMPLETED_QUEUE:-video-processing-completed}

if ! aws s3api head-bucket --bucket "$BUCKET" >/dev/null 2>&1; then
  aws s3api create-bucket --bucket "$BUCKET" --region "$AWS_REGION"
fi

aws s3api put-object --bucket "$BUCKET" --key videos/ >/dev/null
aws s3api put-object --bucket "$BUCKET" --key frames/ >/dev/null

if ! aws dynamodb describe-table --table-name "$TABLE" --region "$AWS_REGION" >/dev/null 2>&1; then
  aws dynamodb create-table \
    --region "$AWS_REGION" \
    --table-name "$TABLE" \
    --billing-mode PAY_PER_REQUEST \
    --attribute-definitions \
      AttributeName=id,AttributeType=S \
      AttributeName=userId,AttributeType=S \
      AttributeName=resultFileId,AttributeType=S \
    --key-schema AttributeName=id,KeyType=HASH \
    --global-secondary-indexes '[{"IndexName":"userId-index","KeySchema":[{"AttributeName":"userId","KeyType":"HASH"}],"Projection":{"ProjectionType":"ALL"}},{"IndexName":"resultFileId-index","KeySchema":[{"AttributeName":"resultFileId","KeyType":"HASH"}],"Projection":{"ProjectionType":"ALL"}}]'
fi

for queue in "$REQUESTED_QUEUE" "$REQUESTED_DLQ" "$COMPLETED_QUEUE"; do
  aws sqs create-queue --region "$AWS_REGION" --queue-name "$queue" >/dev/null
done

echo "FIAP X AWS Student resources are ready."