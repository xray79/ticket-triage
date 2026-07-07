# RFC 004 — Adopting a utility CSS framework and component library for the agent console

**Status:** Decided — see ADR 007.

## Problem

`agent-console`'s styling today is entirely hand-rolled, component-scoped CSS: every component
(`PriorityBadgeComponent`, `ProviderBadgeComponent`, the `app-root` topnav, every page) writes
its own `styles: [...]` block with its own hex colors, spacing values, and border radii. `src/
styles.scss` — the one place global styles could live — is empty. There is no shared color
palette, spacing scale, or typography scale; `#fde2e1` and `#7a1717` in the priority badge and
`#e6eefc`/`#163b7a` in the provider badge were each chosen independently and can't be reused or
audited for consistency (e.g. for contrast) as a set.

This was fine while the app had two badge components and a topnav, but it doesn't scale: every
new page (Reporting charts, admin screens, org policy settings) either re-derives the same
"pill badge," "card," and "button" patterns from scratch or copies an existing component's
inline styles and drifts from it over time. The question is what to standardize on instead.

## Option A — Keep hand-rolled CSS, extract shared SCSS variables/mixins

Move the repeated values (badge colors, spacing, radii) into `src/styles.scss` as SCSS
variables/mixins and reference them from component `styles:` blocks instead of inlining hex
values.

**Pros**
- Zero new dependencies, zero new peer-version risk — directly relevant given how much of this
  session was spent unwinding Angular-family Dependabot breakage; anything added to
  `package.json` is another thing that can go stale or conflict.
- Full control over every pixel; no framework conventions to learn.

**Cons**
- Doesn't solve the actual problem, just relabels it: someone still has to invent, by hand,
  every "card," "button," "input," "modal," and "table" pattern a growing admin/reporting
  surface will need, and there's no library of accessible, tested components to draw on (focus
  trapping in a future modal, keyboard nav in a future dropdown, etc. — all bespoke).
- No enforced consistency mechanism; a variable *can* be bypassed by writing a raw hex value
  again, and nothing catches it.

## Option B — Tailwind CSS only

Add Tailwind for utility-class styling (spacing, color, layout) but keep hand-writing every
interactive component (buttons, badges, future dropdowns/modals/tables) as bespoke Angular
components styled with Tailwind classes instead of custom CSS.

**Pros**
- Solves the "inconsistent ad hoc values" problem directly — a constrained token set
  (`bg-red-100`, `text-red-900`, `rounded-full`, `px-2`) replaces arbitrary hex/rem values, and
  Angular CLI 18's builder has first-class PostCSS support so wiring it in is a small,
  well-trodden config change (`tailwind.config.js` + `postcss.config.js` + one `@tailwind`
  import in `styles.scss`), not a build-system rewrite.
- No Angular-version coupling at all — Tailwind is a PostCSS plugin, not an Angular library, so
  it carries none of the peer-dependency risk that just broke four separate Dependabot PRs.
- Low commitment: adopting Tailwind for new/touched components doesn't require touching
  anything that isn't already being changed.

**Cons**
- Still leaves "build an accessible dropdown/modal/date-picker from scratch" as a standing cost
  every time the app needs one — Tailwind is a styling primitive, not a component library, so it
  doesn't reduce the amount of custom interactive-widget code the team has to write and
  accessibility-test itself.

## Option C — A component library only (Taiga UI), no utility CSS

Adopt Taiga UI (or Angular Material) for ready-made accessible components — buttons, badges,
alerts, tables — and keep using component-scoped SCSS for anything the library doesn't cover,
without Tailwind.

**Pros**
- Gets real, accessible, tested widgets (the actual gap Option A/B don't close) with comparably
  little integration work — Taiga UI ships as standalone Angular components with its own
  design-token CSS, importable per-component so unused components don't bloat the bundle.

**Cons**
- Any one-off styling not covered by a library component (page layout, spacing between a Taiga
  component and app-specific content) is back to hand-written CSS with the same "no shared
  scale" problem Option A has — the library only solves the component half, not the layout/
  utility half.

## Option D — Tailwind CSS + Taiga UI together

Use Taiga UI for interactive/accessible components (badges, buttons, alerts, tables) and
Tailwind utility classes for layout, spacing, and one-off styling around them, replacing the
component-scoped hex-value CSS currently in `PriorityBadgeComponent`/`ProviderBadgeComponent`/
`app.component` incrementally as those files are touched — not as a single big-bang rewrite.

**Pros**
- Closes both gaps at once: Taiga UI supplies the accessible component primitives Option B
  lacks; Tailwind supplies the consistent layout/spacing scale Option C lacks, and each tool is
  used for the job it's actually good at rather than stretching one to cover both.
- Taiga UI's `v4-lts` line (`@taiga-ui/core@4.x`) declares `@angular/core: ">=16.0.0"` as its
  peer requirement — confirmed directly against the npm registry — so it installs cleanly
  against this app's current `@angular/core@^18.2.0` with no forced Angular major bump, unlike
  the several individual Angular-family Dependabot PRs closed earlier in this same effort for
  exactly that reason. (Taiga UI's `latest`/`v5` line requires `@angular/core >=19`, which this
  app is not on — `v4-lts` is the version actually compatible today.)
- Tailwind has no Angular peer dependency at all, so it adds no version-coupling risk on top of
  Taiga UI's.
- Matches this codebase's existing incremental-adoption instinct (ADR 005's graceful-fallback
  precedent: add a capability so it's available and used going forward, without requiring every
  existing call site to migrate on day one) — new components can use Tailwind + Taiga UI
  immediately; the two existing badge components can be migrated as a small, separate,
  low-risk follow-up rather than blocking this change on rewriting working code.

**Cons**
- Two new dependency families instead of one, which is two things to keep patched — mitigated
  by the Dependabot grouping change landed alongside this RFC (major/minor grouped per
  ecosystem), which is precisely what makes taking on a second frontend dependency family
  tractable now when it wasn't before.
- A short learning curve for whoever writes the next component (Tailwind's utility-class
  vocabulary, Taiga UI's component API) — judged worth it against the alternative of every
  contributor re-deriving badge/button/modal patterns by hand indefinitely.

## Recommendation

**Option D.** Options A-C each solve only half of what's actually missing (a shared
layout/spacing vocabulary and a library of accessible interactive components); Option D solves
both, and the peer-dependency check that sank the Angular-family Dependabot PRs earlier in this
work is exactly the check that clears Taiga UI's `v4-lts` line against this app's real Angular
version. Adopting both incrementally — new/touched components first, existing badges as a
follow-up — avoids a risky big-bang rewrite while still moving the codebase toward a consistent
design vocabulary.

## Outcome

See ADR 007 for the recorded decision and its scope.
