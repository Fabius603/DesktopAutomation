---
date: 2026-10-05
status: superseded
supersedes: 2026-10-05-follow-the-drag-cursor-without-insert-cards.md
superseded_by: 2026-10-05-render-measured-step-scopes-with-directional-sequences.md
---

# Hide internal block ends and retain editor drafts

## Context

The user explicitly requires EndIf to remain an internal marker, persistent validation feedback,
working nested variable comparisons, and predictable picker dismissal. Opening and validating a
condition editor previously wrote references to uncommitted draft values into persisted locals.
Selecting another step then lost those values. Computer Use remains excluded.

## Decision

Retain the drag preview, icon-only handles, smooth scrolling, compact cards and layered surfaces
from the superseded decision. Preserve the flat persisted structure and stable IDs. Hide every
EndIf row, including orphaned markers; selection and diagnostic navigation resolve to a visible
owner. The debugger skips internal closing markers. Render block closures in noninteractive
footer margins shared by projection, routes and drop placement. Use rounded joins in the left
indentation corridor and a single outgoing flow connection, without closure cards or merge dots.

AddJobStepDialogViewModel owns working copies of existing local values. Validation may update
these copies, but cannot mutate persisted locals. Commit changed referenced values and new local
values together with a valid step. Keep invalid editor sessions by step ID across selection changes;
clear them on discard, deletion or snapshot restoration. Canonical JobValidation still owns validity
and accumulates input and structure errors. The UI combines canonical and draft diagnostics,
marks affected cards and phases, and reveals the affected step through the problem list.

ResultPathPicker toggles on a field click and closes on selection, Escape, outside input,
owner deactivation, hiding or unloading. Group expansion remains open. Its owned input and owner
subscriptions are removed on close. Condition rows use the same mutable source catalog as pickers.
Runtime materialization only happens when the enclosing branch and condition are evaluated.

## Alternatives considered

Keeping a tiny EndIf row creates invisible-looking hit targets and unwanted inspector selections.
Rebuilding an invalid editor loses user input. Updating persisted locals while checking a draft can
leave dangling references and damages undo. Returning branches across card widths obscures nesting.

## Consequences

Jobs remain backward compatible. Editor drafts consume memory only while editing and cannot be
saved or executed while invalid. Internal markers still travel with complete block moves, but
never enter the visible selection. Each consecutive closure reserves its own insertion lane.
Only changed working locals are committed, so unrelated values retain later edits.

## Verification

Cover repeated nested enum and variable reopening without persisted mutations, valid commits and
undo, invalid-draft retention and discard, canonical error accumulation, hidden markers and debug
behavior, and insertion positions without marker rows. Render nine real WPF scenarios off screen,
including empty blocks and consecutive closures. Exercise picker value selection, toggle, groups,
Escape, outside input and hiding inside an isolated invisible WPF surface. Inspect saved images
and run eng/verify.ps1 -Mode Full without desktop automation or changing the user's running app.
