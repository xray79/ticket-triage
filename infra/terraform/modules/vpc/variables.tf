variable "name" {
  type        = string
  description = "Prefix for resource names, e.g. \"ticket-triage-dev\"."
}

variable "cidr_block" {
  type    = string
  default = "10.0.0.0/16"
}

variable "availability_zones" {
  type        = list(string)
  description = "At least two AZs for a demo VPC (e.g. [\"us-east-1a\", \"us-east-1b\"])."
}

variable "tags" {
  type    = map(string)
  default = {}
}
