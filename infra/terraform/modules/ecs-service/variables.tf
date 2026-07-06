variable "name" {
  type = string
}

variable "region" {
  type = string
}

variable "vpc_id" {
  type = string
}

variable "subnet_ids" {
  type = list(string)
}

variable "image" {
  type        = string
  description = "Full image URI including tag, e.g. <account>.dkr.ecr.<region>.amazonaws.com/ticket-triage-api:<sha>."
}

variable "container_port" {
  type    = number
  default = 8080
}

variable "cpu" {
  type    = string
  default = "512"
}

variable "memory" {
  type    = string
  default = "1024"
}

variable "desired_count" {
  type    = number
  default = 1
}

variable "use_fargate_spot" {
  type        = bool
  default     = true
  description = "Up to 70% cheaper; fine for non-prod. Set false for prod."
}

variable "environment" {
  type    = map(string)
  default = {}
}

variable "secret_arns" {
  type    = list(string)
  default = []
}

variable "sqs_queue_arns" {
  type    = list(string)
  default = []
}

variable "tags" {
  type    = map(string)
  default = {}
}
