variable "name" {
  type = string
}

variable "vpc_id" {
  type = string
}

variable "subnet_ids" {
  type = list(string)
}

variable "vpc_cidr_block" {
  type        = string
  description = "The VPC's CIDR block, so Postgres is reachable from any task in the VPC without an inter-module SG dependency."
}

variable "instance_class" {
  type    = string
  default = "db.t4g.micro"
}

variable "database_name" {
  type    = string
  default = "ticket_triage"
}

variable "master_username" {
  type    = string
  default = "ticket_triage"
}

variable "master_password" {
  type      = string
  sensitive = true
}

variable "tags" {
  type    = map(string)
  default = {}
}
