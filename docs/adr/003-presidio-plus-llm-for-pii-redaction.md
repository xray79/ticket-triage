# 003 — Presidio + Ollama union for PII redaction, not either alone

## Context

Structured PII (emails, phone numbers, card numbers) is well handled by
regex/NER tools. Free-text, indirect mentions ("my daughter Sarah's account")
are not — they need semantic understanding of the sentence, which is exactly
what an LLM is good at and a regex engine is not. A single miss here is a real
PII leak, which is a materially worse failure than redacting a few words that
didn't need it.

## Decision

Run two independent detectors over every field of every ticket and union their
detected spans before masking (`Triage.Application.Redaction.RedactionEngine`):

- **Microsoft Presidio** — deterministic regex/NER, precision-tuned for
  structured PII. Always runs first.
- **Ollama** — a supplementary pass asked to find contextual/indirect PII
  mentions Presidio's patterns don't cover. Asked to return literal matched
  substrings rather than character offsets (LLMs are unreliable at
  character-accurate positions), which the engine then locates itself via
  string search.

Overlapping spans from the two detectors are merged into one placeholder rather
than double-counted. A span reported outside a field's actual bounds (bad
detector output, wrong field) is dropped rather than allowed to crash the whole
redaction pass — see the `RemoveAll` bounds check and its regression test in
`RedactionEngineTests`, added after exactly this crash surfaced while testing
the engine against a short "Subject" field.

## Tradeoffs considered

- **Presidio alone.** Rejected: misses indirect mentions entirely — the stated
  gap this design exists to close.
- **LLM alone, no Presidio.** Rejected: LLMs are non-deterministic and can miss
  even structured PII a regex catches reliably; Presidio's precision on the
  "obvious" cases is worth keeping as the floor.
- **LLM redaction pass only on ticket types known to have more free-text PII,
  or only when Presidio's confidence is low.** This is explicitly an open
  question in the plan (§ "Open questions worth deciding early"). Starting with
  "always run both" is the safer default; optimizing to skip the Ollama pass
  in some cases is a measurement-driven follow-up, not a day-one decision.

## Why this one won

Union-of-detectors directly implements "a missed PII leak is worse than
over-redacting a harmless word" as an actual engineering rule, not just a
principle in the README — every span either detector flags gets masked, full
stop.
