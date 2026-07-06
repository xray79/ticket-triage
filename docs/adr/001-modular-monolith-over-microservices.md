# 001 — Modular monolith over microservices

## Context

The system has four cohesive areas (Identity, Tickets, Triage, and eventually
Notifications/Reporting) that all belong to one product surface, built and
operated by one person. Microservices would mean separate deployables, separate
databases (or at least separate connection pools/migrations), network calls
where in-process calls suffice, and a lot more operational surface (service
discovery, distributed tracing across process boundaries, per-service CI/CD) —
all before the system has enough scale or team size to need independent
deployability.

## Decision

Build one deployable (the `Host` composition root) containing four modules
(`Identity`, `Tickets`, `Triage`, and Shared Kernel/Abstractions/Infrastructure),
each with its own `Domain`/`Application`/`Infrastructure`/`Contracts` projects
and its own schema in a single Postgres database. Modules may only reference
another module's `Contracts` project or communicate via domain events published
through an outbox to SQS — never another module's `Domain`/`Application`/
`Infrastructure` directly. This boundary is enforced by a NetArchTest suite
(`tests/ArchitectureTests`) that fails CI on violation, not just a review
convention.

## Tradeoffs considered

- **Microservices from day one.** Rejected: no team-scaling or independent-deploy
  need yet, and the operational cost (service mesh, per-service pipelines,
  cross-service integration testing) isn't justified by the problem size.
- **A single unstructured project.** Rejected: without enforced module
  boundaries, a solo project this size tends to accrete cross-cutting references
  that make a later extraction much harder, and there'd be no CI signal when that
  starts happening.
- **Modular monolith with only a "logical" boundary (folders, no enforced
  rule).** Rejected in favor of the NetArchTest gate — a boundary that isn't
  enforced by CI erodes the first time someone's in a hurry.

## Why this one won

It gets almost all the benefit of clean boundaries (an extractable module, a
tested contract surface between modules) with almost none of the microservices
tax (no network hop for in-process calls, one CI pipeline, one database to
operate, one thing to deploy). Stretch stage S1 in the delivery plan — pulling
Triage out into its own service — is the concrete test of whether this boundary
actually holds up under extraction; the fact that it's plausible at all is this
decision paying off.
