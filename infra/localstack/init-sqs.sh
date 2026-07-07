#!/bin/sh
# Runs automatically on LocalStack startup (mounted into /etc/localstack/init/ready.d).
# Creates each module's inbox queue plus a matching dead-letter queue, wired via redrive
# policy, so a message that fails repeatedly surfaces instead of looping forever.
set -e

create_queue_with_dlq() {
  queue_name=$1
  dlq_name="${queue_name}-dlq"

  dlq_url=$(awslocal sqs create-queue --queue-name "$dlq_name" --query QueueUrl --output text)
  dlq_arn=$(awslocal sqs get-queue-attributes --queue-url "$dlq_url" --attribute-names QueueArn --query Attributes.QueueArn --output text)

  awslocal sqs create-queue --queue-name "$queue_name" --attributes "{
    \"RedrivePolicy\": \"{\\\"deadLetterTargetArn\\\":\\\"$dlq_arn\\\",\\\"maxReceiveCount\\\":\\\"5\\\"}\"
  }"
}

create_queue_with_dlq "tickets-inbox"
create_queue_with_dlq "triage-inbox"
create_queue_with_dlq "notifications-inbox"
create_queue_with_dlq "reporting-inbox"

echo "SQS queues ready: tickets-inbox, triage-inbox, notifications-inbox, reporting-inbox (+ DLQs)"
