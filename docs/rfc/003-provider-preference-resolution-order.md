# RFC 003 — How org policy, a per-ticket request, and a user's standing preference should resolve

**Status:** Decided — implemented as `TicketsEndpoints.ResolveEffectiveProviderAsync` (Add-on A).
No dedicated ADR covers this specific ordering decision; it's small enough to have been made
inline, which is exactly why it's worth writing down explicitly here.

## Problem

By the time Add-on A landed, there were three independent sources of "which provider should
triage this ticket," and they can disagree:

1. **Org-wide policy** — an Admin can force local-only for the whole organization (e.g. a
   customer with a strict no-cloud-egress requirement).
2. **A per-ticket request** — the agent creating a ticket can explicitly ask for a specific
   provider on that one ticket (`CreateTicketRequest.RequestedProvider`).
3. **A user's standing preference** — an agent can set "I generally prefer OpenAI" as a
   default that applies whenever they don't say otherwise.

When more than one of these is set, something has to win. Three orderings are plausible:

## Option A — User preference always wins (most specific to the individual)

The agent's own standing preference takes priority over org policy and even overrides an
explicit per-ticket request (on the theory that if they bothered to set a preference, they
meant it every time).

**Rejected outright, without needing much analysis:** an org's force-local-only policy exists
specifically to satisfy a compliance/contractual requirement ("this customer's data never
goes to a third party") that cannot be something an individual agent's personal setting is
allowed to override. If a user preference could beat org policy, the org policy wouldn't be a
policy, it would be a suggestion — and the one scenario org policy exists for (an auditor or
customer asking "can you guarantee no ticket ever left local infrastructure") would have no
actual guarantee behind it.

## Option B — Explicit per-ticket request always wins (most specific to the moment)

Whatever the agent explicitly asks for *on this ticket* overrides both their own standing
preference and org policy — the reasoning being that an explicit, in-the-moment choice is the
most deliberate signal available.

**Pros**
- Respects the agent's immediate judgment call — e.g. "this one ticket has no PII at all and
  I specifically want the higher-quality cloud model for it."

**Cons**
- Same fatal problem as Option A, one level down: if a per-ticket request can override org
  force-local-only, the org policy is still not actually a guarantee — it's a default that any
  agent can opt out of on any ticket just by asking. A policy an individual contributor can
  silently bypass isn't the compliance control it's meant to be.

## Option C — Org policy > explicit per-ticket request > user preference > hard-coded local floor

Org-wide force-local-only is checked first and short-circuits everything else if set. Only if
org policy allows cloud at all does an explicit per-ticket request get honored; only if
neither of those apply does the user's standing preference kick in; and if nothing at all is
set, the system defaults to `"local"` rather than leaving the choice ambiguous.

**Pros**
- Org policy is an actual guarantee, not a default — nothing below it in the chain can
  override it, which is the one property Options A and B both broke.
- Within whatever org policy allows, the ordering still respects the intuitive "more specific
  wins" principle: an explicit choice for *this ticket* is more specific than a general
  standing preference, so it should (and does) take priority over it.
- A sensible, safe default (`"local"`) exists at the bottom of the chain, so an agent who has
  never touched any of these settings still gets privacy-preserving behavior with zero
  configuration — consistent with ADR 002's "local-first" stance holding even when nobody
  has made an explicit choice at all.

**Cons**
- Four things to reason about instead of one flat setting is more surface area, and the
  ordering itself has to be documented somewhere an agent or admin can find it — without that,
  "why did my preference get ignored" is a legitimate support question the system needs a good
  answer to (this RFC, and the code comment at `ResolveEffectiveProviderAsync`, are that
  answer).
- An agent who doesn't realize org policy is active may be confused when their explicit
  per-ticket request to use a cloud provider is silently downgraded to local — the resolution
  is correct, but it's invisible unless the UI surfaces *why* a ticket ended up on a given
  provider (this codebase's provider badge shows *which* provider produced a result and
  whether fallback occurred, but doesn't separately indicate "org policy overrode your
  request" as a distinct reason from "the requested provider failed and fell back").

## Recommendation

**Option C.** The org-policy-first ordering is not really a judgment call — any ordering that
lets a narrower scope (a per-ticket request, or an individual's preference) override the
organization's own compliance guarantee defeats the reason force-local-only exists at all. The
remaining question — request vs. standing preference — is a genuine judgment call, and
"more specific wins" is the right default there: a one-off explicit choice reflects more
present, deliberate intent than a general setting the agent may have forgotten they even have.

## Outcome

Implemented exactly as Option C in `TicketsEndpoints.ResolveEffectiveProviderAsync`: org
force-local-only checked first and short-circuits; else an explicit per-request provider;
else the user's stored preference; else `"local"`. Live-verified during Add-on A that org
policy correctly overrides a user's cloud preference end to end.

The UI gap noted above (Option C's one real con — no distinct "org policy overrode your
choice" signal) is an open, undecided follow-up, not something this RFC resolves; flagging it
here rather than silently dropping it.
