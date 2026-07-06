# 005 — Graceful fallback for optional infrastructure dependencies

## Context

Several capabilities added after Stage 0 depend on infrastructure that isn't
always present: Redis for triage-result caching (Add-on B), an SMTP host for
Notifications (Add-on C), and SQS itself is already optional-by-configuration
in Stage 0's `SqsEventPublisher` (a route with no configured queue is skipped,
not an error). A local dev environment, this project's CI, or a demo session
may reasonably not have Redis or a mail server available, and the plan's own
cost guidance (§21) explicitly recommends skipping ElastiCache "for early
stages; add only once the Reporting/caching stage is being demoed
specifically."

## Decision

Every optional infrastructure dependency gets a no-op-safe fallback selected
by configuration presence, not a separate feature flag:

- **Redis** (`Shared.Infrastructure.Caching.AddDistributedCaching`): binds to
  `ConnectionStrings:Redis` when set, otherwise registers ASP.NET Core's
  in-memory `IDistributedCache`. `ITriageResultCache` and its consumer
  (`TicketCreatedIntegrationEventHandler`) are written against the
  `IDistributedCache` abstraction and never know which backend is active.
- **SMTP** (`Notifications.Infrastructure.DependencyInjection`): binds
  `Notifications:Smtp:Host`; when blank, registers `LoggingEmailSender`
  instead of `SmtpEmailSender`. The notification pipeline — including its
  idempotency check via `NotificationLog` — runs identically either way; only
  where the "email" ends up differs.
- **Health checks** register conditionally on the same presence check (e.g.
  `AddRedis` only when a Redis connection string exists), so `/health/ready`
  doesn't report a false negative for a dependency that was never supposed to
  be there.

## Tradeoffs considered

- **A separate `UseRedis`/`UseSmtp` boolean flag.** Rejected: it's redundant
  state that can drift from the actual configuration (flag says yes,
  connection string is blank) and is one more thing to keep in sync across
  environments.
- **Fail startup if Redis/SMTP isn't configured.** Rejected for the same
  reason the plan tears down infrastructure between demo sessions (§14) and
  recommends skipping ElastiCache early (§21): these are genuine
  optimizations/nice-to-haves, not correctness requirements, and a portfolio
  reviewer running `docker compose up` shouldn't need every optional piece
  wired up just to see the core flow work.
- **Mock the dependency entirely in non-prod.** Rejected in favor of a real,
  shippable fallback implementation (in-memory cache, logged email) — the
  fallback path is exercised by the same code path as production, not a
  test-only stub, so there's no "it only worked because it was mocked" gap.

## Why this one won

It matches this project's own operating model — infrastructure is stood up
only when it's earning its cost — at the code level: the application degrades
to the cheaper mode automatically instead of requiring a human to remember to
flip a switch, and the fallback is real production code exercised by both the
unit tests and any environment that happens not to have Redis/SMTP configured,
not a special test-only path.
