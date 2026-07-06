# Postmortem 001 — A corrupted outbox message retries forever and could starve the batch

**Status:** Resolved. **Severity:** Low in this environment (no user-facing impact observed),
but the underlying gap would matter in a real deployment — see Impact.

This is a genuinely simulated incident (stretch stage S6), deliberately induced in this
sandbox's own local, disposable Postgres — not a narrative written after the fact. The log
excerpts below are real output from a real run.

## Summary

A single outbox row with a corrupted `Type` column was manually introduced (simulating what a
redelivered, truncated, or otherwise malformed message looks like once it reaches the
dispatcher) into `tickets.outbox_messages` while the main `Host` was running against local
Postgres. `OutboxDispatcherHostedService` retried publishing that same row every 5-second poll,
indefinitely, with no backoff and no way to stop — confirmed by direct log observation, not
just by reading the code. This is a blameless review: the outbox pattern's core design (Stage 0)
never had a documented poison-message story, and this is the first time that gap was actually
exercised end to end rather than left as a theoretical concern.

## What was done to induce the incident

```sql
-- Corrupt a real, already-existing pending outbox row's Type column, simulating a
-- malformed/corrupted message arriving via redelivery.
UPDATE tickets.outbox_messages
SET "Type" = 'Tickets.Contracts.Events.TotallyBogusEventType, Tickets.Contracts'
WHERE "Id" = 'bfaffc8a-3a82-4060-853a-f0a8941c7096';
```

The Host was already running (`dotnet run --project src/Host` against local Postgres,
LocalStack unreachable in this sandbox — the same pre-existing condition documented since
Stage 0), so every outbox row was already failing to publish for the ordinary reason (no SQS
endpoint to reach). That baseline noise is what makes this a realistic test: the poisoned
message had to be distinguishable from routine "SQS is down" errors, not tested in isolation
against an otherwise-healthy system.

## Timeline (real timestamps from this run)

- **23:39:xx** — three tickets created via `POST /api/tickets` to have fresh outbox rows to
  observe alongside the corrupted one; oldest pre-existing pending row's `Type` corrupted via
  the `UPDATE` above.
- **23:40:14** — first observed retry of the corrupted message:
  ```
  [23:40:45 ERR] Failed to publish outbox message bfaffc8a-3a82-4060-853a-f0a8941c7096.
  System.InvalidOperationException: Cannot resolve outbox event type 'Tickets.Contracts.Events.TotallyBogusEventType, Tickets.Contracts'.
     at Shared.Infrastructure.Outbox.OutboxDispatcherHostedService`1.DispatchPendingAsync(CancellationToken ct)
  ```
  Notice this exception is thrown and logged **before** any network call — `Type.GetType(...)`
  fails immediately, unlike the surrounding messages, which each pay a real (if fast)
  `Connection refused (localhost:4566)` from actually attempting to reach SQS. The poison
  message is cheap to retry, which is almost worse: nothing about its cost profile makes it
  stand out in logs or metrics as different from ordinary transient noise.
- **23:40:14 → 23:41:07** — the same message ID recurs in the log **7 times** across roughly
  53 seconds (`grep -c "Failed to publish outbox message bfaffc8a"` → 9 by the time
  observation stopped), tracking the ~5-second poll interval, confirming: no backoff, no
  circuit breaker, no maximum retry count, no dead-letter path — it would have continued
  exactly like this indefinitely had the process kept running.
- **End state:** `tickets.outbox_messages` held 14 total rows, 13 unprocessed (the corrupted
  one plus 12 genuinely blocked on the unreachable SQS endpoint) — confirmed via
  `select count(*), count(*) filter (where "ProcessedOnUtc" is null) from tickets.outbox_messages;`.

## Root cause

`OutboxDispatcherHostedService.DispatchPendingAsync` wrapped **type resolution +
deserialization** and **publishing** in the same `try/catch`, and treated every failure
identically: log an error, set `Error`, leave `ProcessedOnUtc` null so the row is picked up
again on the very next poll. That's the right behavior for a publish failure (the broker being
briefly unreachable is exactly the kind of thing that should keep being retried) — but it's the
wrong behavior for a message whose `Type` can never resolve or whose `Content` can never
deserialize into that type, because **no amount of retrying changes that outcome.** The row's
own bytes are the problem, not the environment around it.

A secondary contributing factor: the batch query (`Take(BatchSize)`, oldest-first) has no
mechanism to skip past a row that keeps failing. A stuck row at the head of the queue occupies
one of the fixed `BatchSize` (20) slots on every single poll, forever — with enough poisoned
messages accumulated (or a large enough backlog behind a single one), newer legitimate messages
could be pushed out of every batch indefinitely, never getting a chance to be attempted at all.
This wasn't directly observed at the small scale of this test (13 unprocessed rows, batch size
20 — everything still fit in one batch), but it's a direct, mechanical consequence of the same
root cause and worth stating plainly rather than only reporting what was small enough to fit
inside this test's own blast radius.

## Impact (in this environment vs. in a real deployment)

**Here:** none beyond log noise — the sandbox has no real SQS traffic to starve, and the
backlog (13 rows) never approached the 20-row batch size where starvation would start to bite.

**In a real deployment**, this would mean: a single malformed message — from a bad
deserialization-breaking schema change, a bug in an event's own serialization, or genuine
message corruption in transit — silently retries forever, contributes a permanent low-level
error-log entry every poll cycle for as long as the process runs, and, if enough such messages
accumulate (or if the backlog grows large enough behind even one), can delay or starve
legitimate messages from ever being attempted. None of that requires an operator to notice
anything acute — the system doesn't crash, doesn't alert, and doesn't fail loudly. It just
quietly retries something that was never going to succeed, forever, which is the specific
failure mode blameless postmortems exist to catch: not "someone did something wrong," but "the
system's own design never had an answer for this input, and nothing forced that gap to surface
until it was deliberately gone looking for."

## What was fixed

`OutboxDispatcherHostedService.DispatchPendingAsync` now separates type resolution/
deserialization from publishing into two distinct try/catch blocks:

- **Unrecoverable** (type doesn't resolve, or content doesn't deserialize into it): logged as a
  `LogWarning` (not `LogError` — this is an expected, handled outcome now, not a genuine error
  every time it's observed), the row is marked `ProcessedOnUtc = now` with
  `Error = "Abandoned (unrecoverable): {message}"` so it stops occupying a batch slot and stops
  retrying, and the batch moves on to the next message in the same pass.
- **Transient** (the actual `publisher.PublishAsync` call throws — broker unreachable, timeout,
  etc.): unchanged behavior — logged as `LogError`, left unprocessed, retried on the next poll,
  exactly as before. This is still the right behavior for the actual "SQS is down" case this
  sandbox exercises constantly.

Covered by three new tests in `tests/UnitTests/Shared.Infrastructure.Tests/
OutboxDispatcherHostedServiceTests.cs` against a real (in-memory) EF Core `DbContext`: a valid
message publishes and is marked processed; an unresolvable-type message is marked processed
*without* publishing and carries an "Abandoned" error; and a transient publish failure is left
unprocessed while a different, valid message in the same batch still gets published — directly
reproducing the mechanism this incident surfaced, not just its symptom.

## Follow-ups not done here (documented, not silently dropped)

- **No dead-letter table/queue for abandoned messages.** They're marked processed and keep
  their `Error` text in the same row, but there's no separate place an operator would think to
  look for "messages that were given up on" versus "messages that succeeded." A real system
  would want these surfaced distinctly (a dashboard, an alert, or at minimum a query someone
  runs periodically) rather than indistinguishable from success unless you inspect `Error`.
- **No maximum-retry-count path for transient failures.** A publish failure that persists for
  hours or days (a genuinely extended broker outage, not this sandbox's permanent unavailability)
  still retries every 5 seconds forever with no escalation — the same shape of gap as the
  unrecoverable case, just for a different failure category, and intentionally out of scope for
  this fix since "how long is too long to keep retrying a real outage" is a product/ops decision,
  not a code-shape bug the way "retrying something that can mathematically never succeed" is.
- **No metric/counter for abandoned messages specifically.** `TriageMetrics` (Add-on A) has a
  precedent for exactly this kind of per-outcome counter; extending that pattern to the outbox
  dispatcher (`outbox.abandoned`, `outbox.published`, `outbox.retry`) would make this incident's
  class of problem visible in the OpenTelemetry metrics already wired up in Add-on B, instead of
  requiring someone to grep logs the way this postmortem's own investigation did.
