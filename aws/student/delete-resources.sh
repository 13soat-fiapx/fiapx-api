#!/usr/bin/env bash
set -euo pipefail

AWS_REGION=${AWS_REGION:-us-east-1}
BUCKET=${FIAPX_BUCKET:-fiapx-media}
TABLE=${FIAPX_PROCESSING_JOBS_TABLE:-fiapx-processing-jobs}
REQUESTED_QUEUE=${FIAPX_REQUESTED_QUEUE:-video-processing-requested}
REQUESTED_DLQ=${FIAPX_REQUESTED_DLQ:-video-processing-requested-dlq}
COMPLETED_QUEUE=${FIAPX_COMPLETED_QUEUE:-video-processing-completed}

for queue in "$REQUESTED_QUEUE" "$REQUESTED_DLQ" "$COMPLETED_QUEUE"; do
  queue_url=$(aws sqs get-queue-url --region "$AWS_REGION" --queue-name "$queue" --query QueueUrl --output text 2>/dev/null || true)
  if [ -n "$queue_url" ]; then
    aws sqs delete-queue --region "$AWS_REGION" --queue-url "$queue_url"
  fi
done

if aws dynamodb describe-table --table-name "$TABLE" --region "$AWS_REGION" >/dev/null 2>&1; then
  aws dynamodb delete-table --region "$AWS_REGION" --table-name "$TABLE"
fi

if aws s3api head-bucket --bucket "$BUCKET" >/dev/null 2>&1; then
  aws s3 rm "s3://$BUCKET" --recursive
  aws s3api delete-bucket --bucket "$BUCKET" --region "$AWS_REGION"
fi

echo "FIAP X AWS Student resources were removed."