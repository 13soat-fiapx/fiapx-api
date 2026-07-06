#!/usr/bin/env bash
set -euo pipefail

AWS_REGION=${AWS_REGION:-us-east-1}
FIAPX_PROJECT=${FIAPX_PROJECT:-fiapx}
FIAPX_ENVIRONMENT=${FIAPX_ENVIRONMENT:-dev}
RESOURCE_PREFIX="${FIAPX_PROJECT}-${FIAPX_ENVIRONMENT}"

QUEUES=(
  "${FIAPX_REQUESTED_QUEUE:-${RESOURCE_PREFIX}-video-processing-requested}"
  "${FIAPX_REQUESTED_DLQ:-${RESOURCE_PREFIX}-video-processing-requested-dlq}"
  "${FIAPX_COMPLETED_QUEUE:-${RESOURCE_PREFIX}-video-processing-completed}"
  "${FIAPX_COMPLETED_DLQ:-${RESOURCE_PREFIX}-video-processing-completed-dlq}"
)

for queue in "${QUEUES[@]}"; do
  awslocal --region "$AWS_REGION" sqs create-queue --queue-name "$queue" >/dev/null
  echo "Queue '$queue' is ready."
done
