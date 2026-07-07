# One inbox queue per subscribing module, each with a dead-letter queue so a message
# that fails repeatedly (e.g. malformed ticket data) surfaces as an alert instead of
# looping or being silently dropped.

resource "aws_sqs_queue" "dlq" {
  for_each = toset(var.queue_names)
  name     = "${var.name_prefix}-${each.value}-dlq"
  tags     = var.tags
}

resource "aws_sqs_queue" "inbox" {
  for_each                   = toset(var.queue_names)
  name                       = "${var.name_prefix}-${each.value}"
  visibility_timeout_seconds = var.visibility_timeout_seconds

  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.dlq[each.value].arn
    maxReceiveCount     = var.max_receive_count
  })

  tags = var.tags
}
