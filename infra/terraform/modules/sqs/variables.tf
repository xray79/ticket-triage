variable "name_prefix" {
  type = string
}

variable "queue_names" {
  type        = list(string)
  description = "Base names, e.g. [\"tickets-inbox\", \"triage-inbox\"]."
}

variable "visibility_timeout_seconds" {
  type    = number
  default = 60
}

variable "max_receive_count" {
  type    = number
  default = 5
}

variable "tags" {
  type    = map(string)
  default = {}
}
