# Load test report — concurrent ticket ingestion (stretch stage S3)

## What this is

The delivery plan's stage S3 asks for a load test against the Stage 0 deploy — concurrent
ticket ingestion, SQS queue depth, Ollama latency under concurrency, and the point a
resilience mechanism actually trips — with a real write-up of what broke, not an assertion
that it "should" hold up. This is that write-up, run against the real stack in this
environment (no live AWS deploy available here, so against a local `dotnet run
--project src/Host` + local Postgres instead — see "Scope and limitations" below for
exactly what that does and doesn't cover).

## Environment

- `src/Host` running via `dotnet run` (not containerized) against a local Postgres,
  `ASPNETCORE_ENVIRONMENT=Development`.
- LocalStack (SQS) and Ollama were **not** running — this sandbox can't reach Docker Hub to
  pull either (documented elsewhere in this repo). This matters directly for what the test
  could and couldn't observe; see below.
- Load generator: [`loadtest/ticket-ingestion.js`](../loadtest/ticket-ingestion.js)
  (autocannon), run from the same machine as the API — i.e. the load and the API share one
  outbound IP, which turns out to be the whole story.

## What actually happened

| Connections | Req/s | p50 latency | p99 latency | 2xx | 429 | Other errors |
|---|---|---|---|---|---|---|
| 5   | 11,269 | 0 ms | 1 ms | 198 | 89,953  | 0 |
| 20  | 16,606 | 1 ms | 3 ms | 0   | 132,837 | 0 |
| 50  | 17,740 | 2 ms | 5 ms | 0   | 141,909 | 0 |
| 100 | 18,816 | 5 ms | 8 ms | 0   | 150,509 | 0 |

**The rate limiter trips almost immediately — well before Postgres, EF Core, or anything
else in the request path becomes the bottleneck.** `Program.cs` registers a global
`PartitionedRateLimiter` keyed on `RemoteIpAddress`, `PermitLimit = 100` requests per
`1-minute` fixed window, `QueueLimit = 0` (no queueing — an over-limit request is rejected
immediately with `429`, not delayed). At even 5 concurrent connections hammering the
endpoint continuously, the first ~100 requests within the current window succeed and every
request after that gets `429` until the window rolls over a minute later. Across an 8-second
test window that's a tiny sliver of 2xx responses and tens of thousands of 429s — and it gets
*more* lopsided at higher concurrency, not less, since more connections just means more
requests arrive (and get rejected) per second, while the 100/minute ceiling stays fixed.
Latency stayed in single-digit milliseconds throughout, including at 100 connections — the
app never got slow, because the rate limiter rejects a request in-process before it reaches
the database at all.

**What broke, plainly:** a single office/NAT IP with more than one agent creating tickets
concurrently — a completely ordinary scenario for a support team, not an attack — would see
the same 429 wall a deliberate abuser would. The rate limiter can't distinguish "10 agents
behind one corporate IP filing tickets during a busy morning" from "one bad actor." At Stage
0 review time this was a reasonable placeholder (anonymous-abuse protection is table stakes
for a public-facing endpoint), but this load test is the first time its behavior under
*legitimate* concurrent traffic was actually exercised, and it fails that case badly.

**The fix I'd make:** partition the limiter by authenticated user id (or a per-org key) for
routes that require auth, instead of by `RemoteIpAddress` — `POST /api/tickets` already runs
behind `RequireAuthorization()`, so the caller's identity is known before the limiter needs to
decide, and per-user limits don't penalize a whole office for sharing a NAT gateway. Keep the
IP-based limiter only in front of the unauthenticated routes (`/api/auth/login`), where it's
actually doing the job it was meant for — throttling anonymous credential-stuffing attempts,
not throttling ticket creation by paying customers' own support staff. A secondary, smaller
fix: `QueueLimit = 0` means a legitimate short burst gets rejected outright instead of
smoothed; a small queue (a few seconds' worth) would absorb a brief spike without changing the
steady-state ceiling.

## A second finding, incidental to the SQS gap

With LocalStack unreachable, every ticket's outbox row failed to publish, and
`OutboxDispatcherHostedService` retries the same oldest 20 unprocessed rows every 5 seconds
**forever, with no backoff and no dead-lettering** (confirmed by reading
`OutboxDispatcherHostedService.DispatchPendingAsync` — a failed publish sets `Error` on the
row but never `ProcessedOnUtc`, so it's picked up again on the very next poll). After this
load test, `tickets.outbox_messages` held 209 rows, 208 unprocessed — nearly all of them —
and the API log filled with one `SqsIntegrationEventConsumer`/publish failure roughly every
5 seconds for as long as the process ran. That's not itself dangerous (the poll is cheap and
bounded to a batch of 20), but it means a sustained outage or one permanently-poisoned message
costs the same fixed retry forever with no escalation — worth a backoff/max-attempt policy
if this ever needs to survive a multi-hour outage unattended, which the plan's own
architecture (outbox decoupling `POST /tickets` from a slow downstream) was designed to
tolerate gracefully, just not indefinitely as implemented today.

## Scope and limitations (documented, not hidden)

- **What this test measured:** the synchronous half of ticket creation only —
  `POST /api/tickets` writing the ticket row + outbox row inside one transaction, gated by
  auth and the rate limiter. That's the part actually reachable in this sandbox.
- **What it did not, and could not, measure here:** real SQS queue depth under load (no
  reachable LocalStack), Ollama latency under concurrent triage calls (no reachable Ollama),
  and the point the Polly circuit breaker around the *cloud* provider clients trips (needs a
  live OpenAI/Anthropic/Gemini key plus enough failure volume to open the breaker — neither
  available here). All three require the async consumer side of the pipeline actually running
  against real infrastructure, which — consistent with every other async/SQS-dependent claim
  in this repo (Stage 0, Add-on C) — wasn't available in this environment.
- If a future session has Docker/AWS access, the same `loadtest/ticket-ingestion.js` script
  works unmodified against a full `docker compose up` stack or a real deploy; the queue-depth
  and Ollama-latency observations just need `awslocal sqs get-queue-attributes
  --attribute-names ApproximateNumberOfMessages` and the OTel traces/console exporter already
  wired up in Add-on B watched during the same run.
