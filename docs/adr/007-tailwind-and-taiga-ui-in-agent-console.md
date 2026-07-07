# 007 — Tailwind CSS + Taiga UI (v4-lts) in the agent console, adopted incrementally

## Context

`agent-console`'s styling is entirely hand-rolled, component-scoped CSS with no shared color,
spacing, or typography scale (`src/styles.scss` is empty; every component invents its own hex
values). See RFC 004 for the full comparison of alternatives.

## Decision

Add two frontend dependencies to `agent-console` and use them for new and touched UI going
forward, without rewriting existing working components in the same change:

- **Tailwind CSS** (`tailwindcss` + `postcss`/`autoprefixer`), wired into the existing
  `@angular-devkit/build-angular:application` builder via `postcss.config.js` and
  `tailwind.config.js`, with `@tailwind base/components/utilities` added to `src/styles.scss`.
  Used for layout, spacing, and one-off styling.
- **Taiga UI**, pinned to the **`v4-lts` line** (`@taiga-ui/core@^4.90.0` and its companion
  packages), *not* `latest`/`v5`. `v4-lts` declares `@angular/core: ">=16.0.0"` as a peer
  requirement, verified directly against the npm registry, so it installs against this app's
  `@angular/core@^18.2.0` with no Angular version change. `latest` (`v5`) requires
  `@angular/core >=19` and is explicitly rejected here for the same reason four individual
  Angular-family Dependabot PRs were closed earlier in this effort: an isolated major bump this
  app's current Angular version can't satisfy. Used for accessible interactive components
  (alerts, buttons, badges going forward).

Scope of this change: wire up the build (Tailwind + PostCSS config, Taiga UI's global styles and
a root provider), and demonstrate the pattern by migrating the two existing ad hoc badge
components (`PriorityBadgeComponent`, `ProviderBadgeComponent`) as the first real usage. No
other pages are touched.

## Tradeoffs considered

- **Hand-rolled SCSS variables instead (RFC 004 Option A).** Rejected: adds no component
  library, so every future accessible widget (dropdown, modal, table) is still built and
  accessibility-tested from scratch.
- **Tailwind only (Option B) or Taiga UI only (Option C).** Rejected: each solves only one of
  the two gaps (utility/layout consistency vs. accessible component primitives); using both for
  the job each is actually good at was judged clearer than stretching one to cover both.
- **Taiga UI `latest`/v5.** Rejected outright — requires Angular ≥19, which would force exactly
  the kind of unplanned, untested major Angular migration this session already declined to
  accept via Dependabot for the same version-compatibility reason.
- **Rewriting all existing components in this change.** Rejected: an unscoped rewrite is a much
  larger, riskier change than the actual ask (make the new tools available and prove they work),
  and this codebase's own precedent (ADR 005) favors additive capability over forced migration
  of every existing call site on day one.

## Why this one won

It closes the real gap (no shared design vocabulary, no accessible component library) with
dependencies that are actually compatible with this app's current Angular version — checked
against the registry rather than assumed, which is the exact diligence this session's earlier
Dependabot work established was necessary before taking on any new Angular-adjacent package.
Scoping the change to build wiring plus two component migrations (instead of a full rewrite)
keeps the change reviewable and keeps existing, working pages untouched.
