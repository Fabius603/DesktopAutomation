---
date: 2026-10-02
status: superseded
supersedes: 2026-10-01-present-job-steps-as-connected-blocks.md
superseded_by: 2026-10-02-preview-step-moves-and-simplify-flow-routes.md
---

# Layer step lists and separate gutter actions

## Context

The user requested a complete correction of the step-list design: separate step numbers and
breakpoints, remove the inspector breakpoint switch, simplify cards, make nested routes legible,
and repair drag and drop. The user explicitly excluded Computer Use from verification.

## Decision

Keep the persisted flat sequence and existing step IDs. Cards contain one colored icon, the
localized name and a short value summary. Numbers, breakpoint hit targets and drag grips occupy
independent gutter columns; collapse controls sit outside cards. Keyboard shortcuts and context
menus expose the remaining actions, including unwrapping conditions.

Nested containers have 44 DIP left indentation and a 16 DIP right inset, distinct opaque surfaces,
shadows and highlights. Paint all surfaces before their routes so children cannot erase ancestor
connections. Routes show direction and branch convergence; incomplete groups remain open.

Job dragging uses stable insertion indicators instead of sorting the bound view during hover.
`ControlFlowEditRules` owns complete move units and insertion slots at branch boundaries. Horizontal
outdenting crosses closing markers only. Presentation maps measured row coordinates onto those shared
slots, including collapsed groups and explicit branch insertion areas. Hover and drop use the same
validator, and the captured source selection remains authoritative until drop. A click on an existing
multiple selection is deferred until mouse-up so it remains intact when dragging. Collapsed blocks
expand after 700 ms of hover without changing selection or dirty state. Macro live previews
retain their existing presentation policy.

## Alternatives considered

Sorting job collection views during hover shifts hit targets and selection beneath the pointer.
Equal right edges flatten nested groups. Persistent tree conversion would unnecessarily change job
storage and execution contracts.

## Consequences

Existing jobs remain backward compatible. Drop rejection never mutates collections; valid drops
retain undo and dependency checks. Rendering and structural editing share the existing analyzer;
business rules remain in `TaskAutomation`. Off-screen rendering tests run in an isolated WPF process
with fake application services and no user data, avoiding desktop interaction and global test state.

## Verification

Test branch boundaries, collapsed groups, nested outdenting, empty phases, complete block moves and
captured selections. Render the actual WPF view to assert gutter separation, compact card content
and insets on both sides. Inspect the saved renderings, then run `eng/verify.ps1 -Mode Full`.
