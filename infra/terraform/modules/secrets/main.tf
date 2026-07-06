# Secrets Manager entries for the JWT signing key, LLM provider API keys, and DB credentials.
# Values are written outside Terraform (CI sets them via `aws secretsmanager put-secret-value`
# from GitHub Environment secrets) — Terraform only owns the secret's existence and rotation
# policy, never the plaintext value, so nothing sensitive lives in state or a PR diff.

resource "aws_secretsmanager_secret" "this" {
  for_each                = toset(var.secret_names)
  name                    = "${var.name_prefix}/${each.value}"
  recovery_window_in_days = var.recovery_window_in_days
  tags                    = var.tags
}
