output "bucket_name" {
  value = aws_s3_bucket.this.bucket
}

output "distribution_domain_name" {
  value = aws_cloudfront_distribution.this.domain_name
}
