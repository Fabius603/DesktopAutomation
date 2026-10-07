---
date: 2026-10-07
status: accepted
supersedes:
superseded_by:
---

# Share context and overflow menus

## Context

The user selected the first compact popup design and explicitly requested project-wide
right-click coverage, matching three-dot menus, meaningful multiple selection, tests and
documentation that keeps new pages covered.

## Decision

Use one WPF popup style and one action factory per actionable surface. Right click, overflow
and keyboard context entry points share that factory. Preserve selection on selected rows;
otherwise target the clicked row. Batch menus expose supported actions with target counts.
Keep domain mutation and persistence rules in their existing owners. Require a maintained
surface inventory and agent instructions when adding pages or collections.

## Alternatives considered

Independent XAML menus would continue to diverge from code-created right-click menus.
A global window-wide handler would interfere with native text, password and picker controls.

## Consequences

Menu changes must cover all entry points, localization, selection, eligibility and accessibility.
New pages must update `docs/architecture/context-menus.md` and follow the current agent policy.
The native tray icon opens a WPF popup using the same presentation owner.

## Verification

Risk-based tests cover selection preservation, captured batch targets, independent definition
copies, collection limits and scoped exports. WPF render tests cover both themes. The required
repository completion gate remains `eng/verify.ps1 -Mode Full`.
