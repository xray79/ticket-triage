# 004 — Trunk-based development with promote-forward deploys

## Context

Environments in this project are provisioned on demand via Terraform and torn
down after use (see the plan §14/§21) rather than kept running continuously.
Any branching model that ties a long-lived branch to a long-lived environment
(GitFlow's `develop`/`release` branches, or environment-named branches like
`staging`/`prod`) would fight against that: those branches drift out of sync
with each other over time, and there'd be nothing for them to track anyway once
the corresponding environment doesn't persist between sessions.

## Decision

Trunk-based development: `main` is always deployable, every merge to it
auto-deploys to `dev`. Feature work happens on short-lived branches
(`feature/…`, `fix/…`, `chore/…`, `spike/…`) branched off `main` and merged back
via a PR gated by CI (build, architecture tests, unit tests) plus one review.
Promotion to staging/prod happens by tagging a specific commit and deploying
that exact CI-built image forward — never by merging into an
environment-named branch. Anything unfinished or risky (e.g. a new provider
integration) merges behind a feature flag rather than staying on a long branch.

## Tradeoffs considered

- **GitFlow.** Rejected: its long-lived `develop`/`release` branches assume
  environments that persist and need ongoing reconciliation — the opposite of
  this project's ephemeral-environment model.
- **Environment-named branches (`staging`, `prod`) merged forward.** Rejected:
  these branches inevitably drift from each other and from `main`, and
  "promote by merging" conflates "what code is this" with "what's currently
  deployed where" — a tagged commit answers the second question unambiguously
  without needing a branch to stay in sync.
- **Long-lived feature branches.** Rejected in favor of feature flags — a
  branch open for weeks accumulates merge conflicts and drifts from `main`;
  flags let the same code land on `main` quickly while staying inert until
  ready.

## Why this one won

It matches the actual deployment model instead of fighting it: since promotion
means "redeploy this exact image," a tag is the correct unit of promotion, and
`main` staying always-deployable is what makes the tag trustworthy in the first
place.
