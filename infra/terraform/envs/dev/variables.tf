variable "environment" {
  type    = string
  default = "dev"
}

variable "region" {
  type    = string
  default = "us-east-1"
}

variable "availability_zones" {
  type    = list(string)
  default = ["us-east-1a", "us-east-1b"]
}

variable "vpc_cidr_block" {
  type    = string
  default = "10.0.0.0/16"
}

variable "api_image" {
  type        = string
  description = "Full ECR image URI:tag built by CI, e.g. <account>.dkr.ecr.us-east-1.amazonaws.com/ticket-triage-api:abc123."
}

variable "db_master_password" {
  type      = string
  sensitive = true
}

variable "enable_redis_cache" {
  type        = bool
  default     = false
  description = "Skip by default per the plan's cost guidance; enable only when demoing the caching behavior specifically."
}
