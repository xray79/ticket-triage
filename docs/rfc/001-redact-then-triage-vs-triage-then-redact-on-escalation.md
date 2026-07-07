# RFC 001 — Redact-then-triage locally vs. triage raw locally, redact only on cloud escalation

**Status:** Decided — see [ADR 002](../adr/002-local-first-llm-with-opt-in-cloud-fallback.md)
and [ADR 003](../adr/003-presidio-plus-llm-for-pii-redaction.md) for the outcomes this RFC
led to.

## Problem

Every ticket may contain PII — a customer's name, account number, or an indirect mention
("my daughter Sarah's account"). The triage pipeline needs a category, priority, summary,
and draft reply from an LLM. Redaction (Presidio + a supplementary LLM pass, see RFC-adjacent
ADR 003) isn't free: it's an extra Presidio HTTP call plus a second LLM call per ticket, adds
latency to the pipeline, and — because the redaction LLM pass can itself miss something —
isn't a 100% guarantee, just a strong mitigation.

Two designs are both plausible before you've picked one:

## Option A — Redact always, before any triage call, local or cloud

Every ticket gets the full Presidio + LLM union redaction pass before the masked text is
ever sent to *any* triage provider, local or cloud. The local Ollama triage call, exactly
like a cloud call, only ever sees masked text.

**Pros**
- One code path. Local and cloud triage are identical from the redaction layer's point of
  view — no "is this going to a provider that needs redaction" branch to get wrong.
- Consistent behavior end to end: a ticket triaged locally today and re-triaged against a
  cloud provider tomorrow (e.g. after a user opts in) sees the same masked text either time,
  so results are comparable and the redaction mapping never has to be recomputed per provider.
- The redaction pass itself gets exercised on every ticket, which means bugs in it surface
  immediately in the common case (local triage), not only on the rarer cloud-opt-in path.

**Cons**
- Pays the extra Presidio + LLM redaction latency on *every* ticket, including the ones that
  only ever touch the local model and arguably didn't strictly need redaction to stay on this
  machine (Ollama runs on infrastructure the org already controls).
- If the redaction pass has a bug that mangles ticket text (over-redaction, a bad span), it
  now affects 100% of tickets instead of only the subset that opted into cloud.

## Option B — Triage raw locally, redact only when escalating to cloud

Local (Ollama) triage runs against the raw, unredacted ticket text — since Ollama is
self-hosted infrastructure the org already controls, the argument goes, there's no "leaving
the building" boundary being crossed. Redaction only runs as a gate immediately before a
cloud provider call, when the agent (or org config) has opted a specific ticket into
OpenAI/Anthropic/Gemini.

**Pros**
- Saves the redaction cost (latency, an extra service dependency on Presidio, a second LLM
  call) for the majority of tickets that never leave local infrastructure.
- Bugs in the redaction pass have a smaller blast radius — they only affect tickets that
  actually escalate to cloud, which is presumably the minority, opt-in path.

**Cons**
- Two different code paths depending on which provider ends up being called — "local" and
  "cloud" no longer mean "same pipeline, different endpoint," they mean genuinely different
  request flows, which is more branching to test and reason about.
- "Ollama runs on infrastructure we control" is true today, but it's an assumption about
  deployment topology (self-hosted, same trust boundary) baked into the security model. If
  Ollama ever moved to a shared or less-trusted host — or if the definition of "who can see
  raw ticket text" needs to satisfy a compliance requirement that doesn't care whether the
  LLM host is "ours" — this design would need to change to catch up, whereas Option A's
  invariant ("no provider ever sees raw text") doesn't depend on where any provider is hosted.
- Provider preference can change per ticket, and — per RFC 003 below — an org can force
  local-only or a user's preference can differ from what was true when the ticket was first
  triaged. A ticket triaged locally today under Option B, then manually re-triaged against a
  cloud provider after a preference change, needs the redaction pass run retroactively at
  that point — a code path that only exists for the "changed my mind" case and is easy to
  undertest precisely because it's rare.
- Whichever detector runs at redaction time (Presidio + Ollama's contextual pass) is itself
  doing real, valuable work independent of *which* provider ends up seeing the result — an
  audit trail or downstream read-model that only shows masked text for cloud-triaged tickets
  and raw text for locally-triaged ones is a harder thing to reason about and explain than
  "every ticket is masked, always."

## Recommendation

**Option A.** The cost — one extra Presidio + LLM call per ticket — is small and constant,
and buys a system with exactly one behavior to explain, test, and audit: nothing downstream
of the redaction boundary, ever, sees raw PII, regardless of which provider a ticket happens
to use today or is switched to tomorrow. Option B's savings are real but they're an
optimization on top of a correctness property, and optimizing away a safety margin before
there's a measured cost problem inverts the right order of operations for something whose
failure mode is "a customer's PII leaked." If redaction latency does become a measured
bottleneck, the better lever is skipping the *supplementary* LLM detector pass selectively
(exactly the "measurement-driven follow-up" ADR 003 already calls out as future work) — not
skipping redaction entirely for an entire provider tier.

## Outcome

Implemented as ADR 002 (local-first, cloud opt-in, always-redact) and ADR 003 (Presidio +
Ollama union). This RFC exists to show the reasoning that led there, since the two decisions
were made together and the tradeoff against Option B is the load-bearing part neither ADR
spells out on its own.
