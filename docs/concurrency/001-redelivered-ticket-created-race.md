# Concurrency problem 001 — two workers racing on a redelivered `TicketCreated`

Stretch stage S7: one hard concurrency problem, explained — not just "there's an idempotency
check," but the actual race it's meant to close, why a naive version of that check doesn't
close it, and what does.

## The setup

SQS (and the outbox pattern generally) gives **at-least-once** delivery, never exactly-once.
A `TicketCreated` message can be delivered twice for entirely ordinary reasons that have
nothing to do with a bug: a consumer picks up the message, starts a slow LLM call, and the
message's SQS visibility timeout expires before the call finishes — SQS assumes the first
consumer died and redelivers the same message to another consumer (or the same consumer,
polling again). After stretch stage S1 extracted Triage into its own deployable, this isn't
hypothetical scaling foresight — running more than one replica of `TriageService.Host` behind
the same `triage-inbox` queue is the ordinary way to scale it, and it's exactly the setup where
two workers can end up processing the same `TicketCreated` message at the same time.

## The naive version: no idempotency check at all

Imagine `TicketCreatedIntegrationEventHandler.HandleAsync` with no guard — it redacts, calls the
LLM, and inserts a `TriageRecord` unconditionally. Two workers (call them A and B) both dequeue
the same redelivered message:

```
A: redact ticket -----------------------------\
                                                > both proceed, unaware of each other
B: redact ticket -----------------------------/
A: call LLM (2-4s)  ---------------------------\
                                                 > both call the LLM independently
B: call LLM (2-4s)  ---------------------------/
A: INSERT TriageRecord (succeeded) ; publish TicketTriaged
B: INSERT TriageRecord (succeeded) ; publish TicketTriaged
```

Result: two `TriageRecord` rows for one ticket, two `TicketTriaged` events published, two
notification emails sent to the customer ("your ticket has been triaged" — twice), and the
Reporting read-model double-counting one ticket's triage latency and provider-attribution stats.
Nothing here is a crash; it's silent duplication, which is arguably worse — nothing about it
looks like an error in any log, so nothing prompts anyone to go looking.

## The check that exists today — and why it doesn't fully close the race by itself

`TicketCreatedIntegrationEventHandler.HandleAsync` opens with:

```csharp
if (await _repository.ExistsForTicketAsync(integrationEvent.TicketId, ct))
    return;
```

This looks like it should prevent duplicate processing, and it does close the race in the
*sequential* case — the ordinary redelivery where the first attempt has already fully
completed and committed by the time a second delivery is processed (visibility timeout expired
long after success, a message somehow delivered twice well after the fact, etc.). That's the
overwhelmingly common case, and this check alone handles it correctly.

**It does not close the concurrent case.** This is a classic time-of-check-to-time-of-use
(TOCTOU) race: `ExistsForTicketAsync` is a `SELECT`, and nothing stops a second worker's
`SELECT` from running (and seeing "no record yet") before the first worker's `INSERT` has
committed. If both workers are far enough into the LLM call at nearly the same moment, both
checks return `false`, and both proceed all the way to insert — the naive version's race,
above, plays out identically. **A check-then-act pattern is only ever as strong as what backs
it at the point of the act** — and before this stage, nothing did: `TriageRecordConfiguration`
declared a *non-unique* index on `TicketId`
(`builder.HasIndex(r => r.TicketId);`, no `.IsUnique()`), so the database would have
happily accepted both concurrent `INSERT`s. The idempotency check existed; it just didn't
actually close the race it looked like it closed — confirmed by reading the configuration, not
assumed.

## What actually closes it

A **unique, filtered index** on `(TicketId)` where `Succeeded = true`:

```csharp
builder.HasIndex(r => r.TicketId)
    .IsUnique()
    .HasFilter("\"Succeeded\" = true");
```

Filtered rather than a plain unique index because a *failed* triage attempt must not block a
later retry from succeeding for the same ticket — multiple `Succeeded = false` rows for one
`TicketId` are fine (and expected, if a ticket's triage failed once and later succeeded on
retry); at most one `Succeeded = true` row per ticket is the actual invariant.

This moves the decision from "whichever worker's `SELECT` ran first" (a race) to "whichever
worker's `INSERT` commits first" (not a race — the database serializes concurrent writers to
the same index at the storage-engine level; exactly one of two concurrent transactions
inserting a conflicting key can ever win). The loser's `SaveChangesAsync` throws a
`DbUpdateException` wrapping a real Postgres unique-violation.

`ITriageUnitOfWork.TrySaveChangesAsync` catches specifically that (`PostgresException` with
`SqlState == PostgresErrorCodes.UniqueViolation`, nothing broader) and returns `false` instead
of throwing. `TicketCreatedIntegrationEventHandler` treats a `false` result as a safe no-op —
logs it plainly at `Information` level (this is an expected outcome under concurrency, not an
error) and returns. Because `OutboxAppender.AppendEventsToOutbox` runs *inside* the same
`SaveChangesAsync` call that just failed, the `TicketTriaged` event that would have been queued
alongside the losing `TriageRecord` insert rolls back in the same transaction — **the loser
never publishes**, not because of extra application logic, but because the outbox row and the
business row were never two separate commits to begin with.

## Why this design, not the alternatives

- **A distributed lock (Redis, Postgres advisory lock) around the whole handler.** Rejected:
  adds an external coordination point and a whole new failure mode (what happens if the lock
  holder crashes mid-triage — a slow LLM call under a lock is exactly the kind of thing that
  makes lock timeout tuning painful) to solve a problem the database's own unique index already
  solves for free, using a mechanism (`INSERT` uniqueness) the database is designed to make
  correct under concurrency without any additional coordination.
- **`SELECT ... FOR UPDATE` / optimistic concurrency tokens.** These protect concurrent
  *updates* to an existing row. This problem is concurrent *inserts* of what should be a single
  logical row — there's nothing to lock or version until one of them exists, which is exactly
  the gap a unique constraint is for.
- **Catch every `DbUpdateException` as "probably a race, just swallow it."** Rejected in favor
  of matching the specific Postgres unique-violation `SqlState` — an unrelated failure (a
  `Summary` value exceeding its `HasMaxLength(2000)` column constraint, a connectivity blip
  mid-transaction) would be silently swallowed and misreported as "another worker won" if the
  catch were broad, hiding a genuinely different bug behind the wrong explanation.

## Verified, not just argued

- `tests/UnitTests/Triage.Tests/TriageRecordUniquenessTests.cs` runs against a real relational
  engine (SQLite in-memory — deliberately *not* EF Core's InMemory provider, which doesn't
  enforce unique indexes at all and would make this test pass regardless of whether the
  constraint actually worked): a second succeeded record for the same ticket throws
  `DbUpdateException`; a failed record followed by a succeeded one for the same ticket does
  not; two different tickets can each have their own succeeded record.
- `tests/UnitTests/Triage.Tests/TicketCreatedIntegrationEventHandlerTests.cs` (new case:
  `HandleAsync_treats_losing_the_unique_index_race_as_a_safe_no_op`) proves the handler's
  reaction to losing the race — given `TrySaveChangesAsync` reporting `false`, it doesn't throw,
  doesn't retry, and doesn't fall through to the failure path.
- Live-verified directly against this environment's real Postgres: the migration
  (`20260706235314_AddUniqueSucceededTriageRecordIndex`) applied cleanly, and a manual two-row
  `INSERT` inside a transaction reproduced the exact real error —
  `duplicate key value violates unique constraint "IX_triage_records_TicketId"` — confirming the
  constraint is live in the actual database this system runs against, not just declared in C#.
