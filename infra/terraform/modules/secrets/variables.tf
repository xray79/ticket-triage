variable "name_prefix" {
  type = string
}

variable "secret_names" {
  type        = list(string)
  description = "e.g. [\"jwt-signing-key\", \"openai-api-key\", \"anthropic-api-key\", \"gemini-api-key\", \"db-credentials\"]."
}

variable "recovery_window_in_days" {
  type    = number
  default = 0 # immediate delete on destroy — this is a demo env torn down after each session
}

variable "tags" {
  type    = map(string)
  default = {}
}
