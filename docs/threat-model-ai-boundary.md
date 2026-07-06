# Threat model — the AI boundary (stretch stage S5)

A STRIDE-style pass on the one boundary in this system where untrusted input (a customer's own
words, submitted through a ticket) is handed to a component (an LLM, local or cloud) whose
output is then trusted as structured data and acted on automatically — no human reviews a
triage result before it's stored, aggregated in Reporting, and in one case emailed directly
to the customer. That trust step is the boundary this document is about; PII exposure to the
provider itself is already covered by ADR 002/003 and RFC 001 and isn't repeated here except
where it intersects with the findings below.

**Scope.** `Triage.Application.Providers.TriagePrompt.Build` (the prompt sent to any
provider) and `.Parse` (how a provider's free-text response becomes a trusted `TriageResult`),
plus what happens to that result afterward: `TriageRecord`, the `TicketTriaged` event, the
Reporting read-model, and the one automated action that reaches an end user —
`Notifications.Application.TicketTriagedNotificationHandler` emailing the customer
`"Your ticket has been triaged as {Category} ({Priority} priority)."` directly from the
triage result's own fields.

## STRIDE pass

### Spoofing — an attacker-shaped message impersonating a legitimate system communication

`TicketTriagedNotificationHandler` interpolates `Category` and `Priority` verbatim into an
email sent to the customer. Before this stage's fix (below), a ticket body that convinced the
model to return e.g. `"category": "URGENT - your account is compromised, click http://evil.example"`
would have had that exact string emailed to the customer under this system's own subject line
("We've reviewed your support ticket") — a phishing pretext the *system itself* delivers,
using its own legitimate sending identity. This is the most concretely dangerous finding here
specifically because it's the one path that reaches an external party automatically, with no
human in the loop.

**Mitigation — implemented in this stage.** `TriagePrompt.Parse` now validates `Category`
against the fixed four-value vocabulary (`billing|technical|account|general`) and `Priority`
against its fixed four-value vocabulary (`low|medium|high|urgent`), case-insensitively,
falling back to a safe default (`general`/`medium`) for anything else — closing this off
directly rather than leaving it as a documented-but-open gap. See `TriagePromptTests` for the
injected-string test cases.

### Tampering — a ticket body manipulating its own classification or the record built from it

The plan's own named example: a ticket body instructing the model to mislabel itself
`urgent`, or more generally, any instruction embedded in the ticket ("ignore your previous
instructions and instead...") attempting to control the category, priority, summary, or draft
reply the system trusts as the model's genuine judgment.

- **Category/priority forgery** — covered by the Spoofing finding above; the same fix (a
  strict allow-list, not just a default-if-null check) closes both an out-of-vocabulary value
  *and* a same-vocabulary-but-wrong value chosen by prompt injection rather than genuine
  judgment. Vocabulary validation can't detect "the model said `urgent` but was tricked into
  it, when it should have said `low`" — that specific case has no automated defense here; it's
  a quality/trust problem, not a shape problem, and is exactly why stretch stage S2 (the eval
  harness) exists as a *separate* control: it's the mechanism that could catch a systematic
  drift in classification quality (from a prompt-injection-susceptible model or a bad prompt
  change) if the fixed sample set included adversarial-style tickets — it currently doesn't
  (S2's samples are all good-faith tickets); adding a handful of injection-attempt samples with
  an expected *unaffected* classification would extend the eval harness to cover this directly,
  and is the natural next hardening step, not implemented in this stage.
- **Summary/draft reply content tampering** — `Summary` and `DraftReply` are free text with no
  shape to validate against, so nothing here can detect "the model wrote what an attacker
  wanted" versus "the model wrote a normal summary." The prompt's own instruction
  ("keep placeholders exactly as-is... do not try to guess the original value") is the only
  guardrail, and it's an instruction to the model, not an enforced constraint — a sufficiently
  capable prompt injection could ignore it. Partial mitigation only: `DraftReply` is a *draft*,
  requiring an agent to review and explicitly send it (confirmed by this system's design —
  there's no code path here that auto-sends a draft reply to a customer without a human
  step), which puts a human in the loop for exactly this field, unlike `Category`/`Priority`
  which — before this stage's fix — had no such review step before reaching the customer email.

### Repudiation — can this actually be investigated after the fact?

If a triage result is later suspected to have been manipulated, `TriageRecord` stores the
*parsed* category/priority/summary/draftReply and which provider produced them, but **not the
provider's raw response text**. There's no way to go back and see exactly what the model said
verbatim, which is the artifact you'd actually want for a "was this prompt-injected" forensic
review. This is a real, currently-unaddressed gap — not fixed in this stage, since it would add
either a new column (raw response, itself potentially containing rehydrated PII if stored
after rehydration, or masked-only text if stored before — a design question of its own) or a
separate audit log, and deserved its own decision rather than a rushed addition here.

### Information Disclosure — the redaction mapping

The example named in the plan. `RedactionEngine`'s mapping (`placeholder -> original PII
substring`) is scoped to one ticket's own fields, held only in a local `Dictionary` for the
duration of one `TicketCreatedIntegrationEventHandler.HandleAsync` call, and never
serialized, cached, logged, or sent anywhere (`ITriageResultCache` caches the *masked* text and
the parsed result, never the mapping). So the mapping itself never "leaves this process" in
the literal sense the code comment on `RedactionEngine` asserts.

But `RedactedTicket.RehydrateBody` is a blind `string.Replace` for every mapping entry over
whatever text the LLM returned as `Summary`/`DraftReply` — it does not check that the LLM's
text actually needed those substitutions in a sensible way. If a ticket body successfully
prompts the model to *enumerate* the placeholders it was given (e.g. a draft reply that reads
"I found the following identifiers in your message: [PERSON_1], [EMAIL_1], [PHONE_1]..."),
rehydration will faithfully substitute every one, producing a single consolidated "here is all
the PII in this ticket" artifact as the *output* of the step whose entire purpose was to keep
that from happening. Since the mapping is per-ticket (not cross-customer), this doesn't
disclose anything to anyone who couldn't already view the same ticket's raw `Subject`/`Body`
directly — but it does defeat the redaction step's actual purpose (nothing downstream of
redaction should need to reconstruct the original text) and produces a compact, easily
copy-pasted "PII summary" that didn't exist as an artifact before. **Not mitigated in this
stage** — a defensible fix would be to validate that `Summary`/`DraftReply` don't contain any
literal placeholder tokens that weren't naturally preserved in a plausible position (hard to
define precisely) or, more simply, to render `Summary`/`DraftReply` as **already-masked** text
to agents by default (never rehydrated) with rehydration as an explicit, logged, opt-in action
— a real design change, not a one-line fix, and left here as a documented follow-up rather
than a rushed partial implementation.

### Denial of Service — cost and availability

- **Cache bypass via trivial variation.** `ITriageResultCache` keys on the exact masked text,
  so a would-be abuser sending near-identical tickets with tiny variations pays full LLM cost
  every time rather than hitting the cache — not a prompt-injection risk specifically, but a
  cost-exhaustion angle at the same boundary. `TriageConcurrencyLimiter`'s bulkhead
  (`MaxConcurrentTriages`/`MaxQueuedTriages`, Add-on A) already bounds *concurrent* damage; it
  doesn't bound *sustained* per-ticket cost from many sequential distinct-text submissions,
  which is really a rate-limiting-on-ticket-creation question — see the S3 load test report for
  the (unrelated, already-found) per-IP rate limiter gap, which does at least incidentally cap
  how fast one source can generate new billable LLM calls this way.
- **A pathologically long or repetitive ticket body** inflating the prompt sent to a cloud
  provider (cost) or slowing the local model (latency) isn't validated against a maximum length
  anywhere in `Tickets.Application`'s `CreateTicketCommand` handling — an unbounded-length
  ticket body is accepted today. Not fixed in this stage; a straightforward follow-up (a
  reasonable max length on `Subject`/`Body` at the API boundary, independent of triage).

### Elevation of Privilege — not currently applicable

No triage output (`Category`, `Priority`, `Summary`, `DraftReply`, or `WasFallback`) currently
drives any automated privileged action — no auto-assignment, no auto-close, no permission
change keyed on triage result. So there's no path today where a manipulated triage result
escalates an attacker's effective access. Worth re-examining the moment that stops being true
(e.g. a future "auto-assign urgent tickets to the on-call agent" feature would need to treat
`Priority` as untrusted input for authorization purposes, not just a display label).

## Summary of what's fixed vs. open

| Finding | STRIDE | Status |
|---|---|---|
| Category/priority forgery reaching the customer email verbatim | Spoofing | **Fixed** — allow-list validation in `TriagePrompt.Parse` |
| Category/priority forgery reaching Reporting/UI as an unexpected value | Tampering | **Fixed** — same validation |
| Summary/draft reply content tampering (free text, no shape to validate) | Tampering | Open — draft reply already requires human review before send; summary does not |
| No raw model response retained for forensic review | Repudiation | Open — needs a real storage/PII decision, not a quick fix |
| Redaction mapping enumeration via rehydration | Information Disclosure | Open — needs a design change to how `Summary`/`DraftReply` are rehydrated and shown |
| Cache bypass / unbounded ticket length cost exhaustion | Denial of Service | Open — length validation and/or fuzzy cache keys not implemented |
| Triage output driving a privileged action | Elevation of Privilege | Not applicable today; re-check if that changes |
