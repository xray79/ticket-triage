# 002 — Local-first LLM with opt-in cloud escalation and automatic local fallback

## Context

Triage quality is generally better from frontier cloud models than from a local
open-weight model, but every ticket may contain customer PII, and support
tickets are exactly the kind of data a user would not expect to silently leave
the building. The system also needs to keep working when a cloud provider is
degraded or unreachable — triage failing entirely because of a third-party
outage is worse than a lower-quality local result.

## Decision

Every ticket is redacted before any triage call, local or cloud (see ADR 003).
The default provider preference is `local` (Ollama). An agent or org admin may
opt in, per ticket or as a standing preference, to a cloud provider
(OpenAI/Anthropic/Gemini). Whichever provider is selected is wrapped by a
`FallbackTriageClient` decorator (`Triage.Application.Providers`) that tries the
selected provider first and, on any failure, falls through to the local Ollama
client — which has no further fallback beneath it; it's the floor. Every
triaged ticket records and displays which provider actually produced the
result and whether fallback occurred (`TriageOutcome.WasFallback` on the
Tickets side, surfaced in the Angular UI's provider badge), so trust in a given
result is never ambiguous.

## Tradeoffs considered

- **Cloud-first, redact only for cloud calls.** Rejected: this makes "local"
  and "cloud" behave differently at the redaction layer, doubling the redaction
  code paths to reason about and test, for no benefit — redaction is cheap
  enough to always run.
- **No automatic fallback; surface a triage failure to the agent instead.**
  Considered, and still what happens if the local Ollama call itself fails —
  see the async processing section of the plan. But for a cloud-provider
  failure specifically, falling back to local costs nothing extra (Ollama is
  already running) and keeps the ticket triaged rather than stuck, at the cost
  of a lower-quality result that's clearly labeled as a fallback.
- **Silent fallback with no indication in the UI.** Rejected outright — an
  agent needs to know if they're looking at a local or cloud result while
  trust in local output quality is still being established.

## Why this one won

It makes privacy the default without making cloud quality unavailable, and it
turns a third-party outage into a visible quality dip instead of a hard
failure — directly serving the two things this system is graded on: not
leaking PII, and still triaging the ticket.
