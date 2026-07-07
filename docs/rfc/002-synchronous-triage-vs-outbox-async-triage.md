# RFC 002 — Synchronous inline triage vs. outbox + async SQS-triggered triage

**Status:** Decided — implemented in Stage 0; see the README's "Why these choices" section
("Outbox + async triage over synchronous inline calls") for the outcome summary. No dedicated
ADR exists for this one specifically (ADR 001 covers the modular-monolith boundary that made
it possible, not this choice on its own), which is part of why it's worth an RFC here.

## Problem

`POST /tickets` needs to end with the ticket triaged: category, priority, summary, and a
draft reply, produced by an LLM call that — local or cloud — is not fast, and not perfectly
reliable (a cloud provider can be rate-limited, degraded, or down; a local model call can be
slow under load). The question is when, relative to the HTTP request, that LLM call actually
happens.

## Option A — Triage inline, inside the `POST /tickets` request

The handler for `POST /tickets` calls the triage orchestrator directly, waits for the LLM
response, persists the result, and only then returns `201 Created` with the fully-triaged
ticket in the response body.

**Pros**
- Simplest possible mental model: the response *is* the finished state. No polling, no
  "triage pending" UI state, no separate mechanism that can fail independently of the request
  that triggered it.
- No outbox, no SQS, no dead-letter queue, no idempotency handling for a redelivered message —
  an entire category of distributed-systems failure modes simply doesn't exist.

**Cons**
- The HTTP request's latency becomes whatever the slowest LLM call in the fallback chain
  takes, multiplied by however much redaction (Presidio + a supplementary LLM pass, see
  RFC 001) costs on top. A cloud provider timeout (this codebase's `ConfigureCloudResilience`
  allows up to 15s before its own timeout trips, on top of two retries) turns directly into a
  slow `POST /tickets` — the caller (an agent's browser) is blocked the whole time.
- A single slow or failing LLM call ties up a request-handling thread/connection for its
  entire duration. Under any real concurrent ticket-creation volume (see the S3 load test),
  that's exactly the kind of resource a synchronous design can't isolate — a triage backlog
  becomes an HTTP-thread-pool backlog, and now ticket *creation itself* degrades because
  triage is slow, even though creating a ticket record doesn't inherently require an LLM call
  to succeed.
- No natural retry boundary. If the LLM call fails outright (not just falls back — actually
  fails), what does the API return? A `201` with a "triage failed" ticket, forcing the client
  to handle a partial-success shape? Or a `5xx`, even though the *ticket itself* was created
  successfully in the same request? Neither answer is clean, because the request conflates
  two operations (create the ticket; triage the ticket) that don't share a failure domain.

## Option B — Outbox + async SQS-triggered triage

`POST /tickets` writes the ticket row and a `TicketCreated` outbox row in one transaction,
then returns `201` immediately — before any LLM call has happened. A background dispatcher
polls the outbox and publishes to SQS; a consumer (the Triage module, and after stretch stage
S1, a wholly separate process) picks up `TicketCreated`, redacts, triages, and publishes
`TicketTriaged`/`TicketTriageFailed` back through the same outbox mechanism, which the Tickets
module consumes to update the ticket's displayed state.

**Pros**
- `POST /tickets` latency is decoupled from LLM latency entirely — the request only pays for
  a database write. This is the direct enabler of stretch stage S1 (extracting Triage into
  its own deployable): since the *only* channel between Tickets and Triage was already
  async/SQS, splitting the deployable required zero protocol change (see ADR 006).
  A synchronous design would have forced S1 to introduce a brand-new HTTP/gRPC boundary where
  none existed, exactly the harder version of that stage the plan's own text anticipated.
- Triage failure and ticket-creation failure are cleanly separate failure domains: a ticket
  always exists once `201` is returned, and `TicketTriageFailed` is a normal, expected outcome
  the UI can show ("triage failed, retry" — or simply visible via the provider badge) without
  it ever meaning the ticket itself is in a bad state.
- The outbox pattern gives an at-least-once delivery guarantee with a durable retry point (the
  unprocessed outbox row) that survives a process restart, unlike an in-flight synchronous
  call that's simply gone if the process crashes mid-request.

**Cons**
- Real complexity cost: an outbox table + dispatcher per module, SQS queues and DLQs, an
  idempotency story for redelivery (see stretch stage S7's write-up on this exact concern),
  and a UI that now needs a "triage pending" state instead of always having the final answer
  in the creation response.
- The S3 load test surfaced a concrete instance of this cost: `OutboxDispatcherHostedService`
  retries a failed batch every 5 seconds with no backoff or dead-lettering — a real gap that
  a purely synchronous design wouldn't have, because it wouldn't have an outbox to begin with.
- Harder to reason about "what's the state of this ticket right now" without either the UI
  polling or an additional push mechanism (this codebase doesn't add one — the Angular client
  re-fetches the ticket to see if triage has landed yet).

## Recommendation

**Option B.** The plan's own framing already treats a slow LLM call as a given, not a defect
to engineer away — cloud provider latency and even local model latency under load are outside
this system's control. Given that constraint, decoupling ticket creation from triage isn't
optional polish, it's the only way to keep `POST /tickets` fast regardless of what triage is
doing, and it's the precondition for treating "triage degraded/slow/failed" as a normal,
recoverable state instead of an HTTP-request-shaped emergency. The complexity Option B adds
(outbox, SQS, idempotency, a pending UI state) is real, but it's complexity in service of an
actual requirement (a slow or failing LLM must never block ticket creation), not complexity
for its own sake — and Option A's apparent simplicity is a mirage the moment the LLM call is
slow or unavailable, which — per this system's own design goals around provider fallback and
resilience — is treated as the expected case, not the exception.

## Outcome

Implemented as Stage 0's core design (outbox → SQS → Triage consumer → outbox → SQS → Tickets/
Notifications/Reporting consumers), and directly responsible for stretch stage S1's extraction
of Triage into its own deployable requiring no protocol change (ADR 006).
