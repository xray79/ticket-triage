output "queue_urls" {
  value = { for k, q in aws_sqs_queue.inbox : k => q.url }
}

output "queue_arns" {
  value = { for k, q in aws_sqs_queue.inbox : k => q.arn }
}
