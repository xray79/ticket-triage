# 006 — Extract Triage as a standalone service

## Context

ADR 001 called this the concrete test of the modular-monolith bet: pull one
module out into its own deployable and see whether the boundary actually
holds. Triage is the natural candidate — it's the most resource-hungry piece
(PII redaction calls plus the LLM fallback chain), the one most likely to need
independent scaling (a burst of ticket creation shouldn't compete with the
Identity/Tickets API's thread pool or the bulkhead limiting concurrent LLM
calls), and, per stage S1's own framing in the delivery plan, the one whose
extraction is most representative of "did the seams we already cut turn out
to be the real ones."

The plan's own description of this stage anticipates needing "an HTTP/gRPC
boundary where the in-process contract used to be" — a fair default
assumption for most modular monoliths, where cross-module calls typically
*are* in-process until extraction forces them onto the wire. That assumption
doesn't hold here, for a reason worth calling out explicitly below.

## Decision

Add a second deployable, `TriageService.Host`, referencing only
`Triage.Application`, `Triage.Infrastructure`, and `Shared.Infrastructure` —
no reference to `Tickets`, `Identity`, `Notifications`, or `Reporting`
whatsoever. It consumes `TicketCreated` off its own `triage-inbox` SQS queue
and publishes `TicketTriaged`/`TicketTriageFailed` back out through the same
outbox pattern every other module already uses, owns its own `Triage`
Postgres schema/migrations, and exposes nothing but `/health/live` and
`/health/ready` — it has no other HTTP surface, since nothing calls it
directly.

The main `Host` project drops its `Triage.Application`/`Triage.Infrastructure`
references, the `Triage` connection string, the `triage-db` health check, and
the `TriageInbox` SQS queue/route wiring. It keeps a reference to
`Triage.Contracts` only transitively, through `Notifications.Application` and
`Reporting.Application`, which still need the `TicketTriaged`/
`TicketTriageFailed` event *types* to deserialize and react to — they never
call into Triage's `Application`/`Infrastructure` layers.

**The load-bearing fact that makes this a same-day change, not a rewrite:**
Tickets and Triage never talked to each other in-process. Every other module
boundary in this codebase (ADR 001) was already enforced as "`Contracts`
project or async domain event, never direct" — for Tickets↔Triage
specifically, the *only* channel that ever existed was `TicketCreated` out
through the outbox to SQS, and `TicketTriaged`/`TicketTriageFailed` back the
same way. Splitting the deployable didn't require inventing a new protocol,
adding a client library, or changing a single handler's signature — it moved
where an existing SQS consumer/producer pair happens to run. The shared
`OpenTelemetry.Instrumentation.*`/tracing wiring moved to
`Shared.Infrastructure.Telemetry.OpenTelemetryExtensions` (previously
`Host`-local) so both deployables get identical tracing/metrics setup,
distinguished only by service name and which meters they own — `Triage
Service` is now the only process that registers `TriageMetrics`' meter, since
it's the only process that still runs `FallbackTriageClient`.

## Tradeoffs considered

- **Extract Tickets or Identity instead.** Rejected: Triage is the module with
  an actual scaling story (LLM call latency, redaction throughput) distinct
  from the rest of the system; extracting a module with no independent load
  profile would prove the mechanics work without demonstrating why you'd
  bother.
- **Give the two processes a synchronous HTTP/gRPC channel post-extraction, to
  "future-proof" request/response style calls.** Rejected: nothing in this
  system needs Triage to answer synchronously, and adding one would reintroduce
  exactly the coupling ADR 001's outbox-only rule was written to prevent.
  If a genuine synchronous need shows up later, add it deliberately then.
- **Split the database too (a second Postgres instance).** Rejected for now:
  Triage already owns its own schema and migration history table
  (`Triage.__ef_migrations_history`) inside the shared instance, which is
  enough isolation to prove the module boundary; a second instance is an
  infrastructure cost with no additional design benefit until Triage's own
  storage needs actually diverge (e.g. a different retention policy).

## Why this one won

It's the cheapest possible extraction that still counts: no protocol was
invented, no handler signature changed, and the only genuinely new work was
deployment topology (a second Dockerfile, a second Terraform ECS service, a
second appsettings surface) plus hoisting the previously Host-local
OpenTelemetry wiring somewhere both processes could share it. That the
diff is almost entirely *subtraction* from `Host` and *duplication* of
already-proven `Triage.Infrastructure` startup code into a new entry point —
rather than new business logic — is exactly the outcome ADR 001 predicted a
correctly-drawn module boundary should produce under extraction.
