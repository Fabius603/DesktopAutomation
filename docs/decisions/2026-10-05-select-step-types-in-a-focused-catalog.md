---
date: 2026-10-05
status: accepted
supersedes:
superseded_by:
---

# Select step types in a focused catalog

## Context

The user selected the first Product Design mockup and explicitly requested implementation in the
existing WPF application. Ordinary steps must be added exclusively through the main Add step
button. Settings are edited in the job overview. The variable manager needs clearer editing and
visible actions while preserving its protected draft sessions.

## Decision

Present the catalog as three columns: category navigation, searchable type rows, and a description.
The footer states the insertion destination. Focus means the current step selection, retained while
the Add step button takes keyboard focus. With multiple selected rows, the last selected row owns
the target. Without a selected row, append to On each run regardless of the previously active phase.
Do not add inline insertion buttons. If and Else if headers each expose Add else and Add else if.
Both header and context-menu commands use ControlFlowEditRules.ResolveBranchInsertionIndex:
resolve the nearest complete conditional, insert Else if before Else, allow at most one final Else,
and reject malformed blocks. Insert and select a default branch without a configuration dialog.
ControlFlowEditRules.ResolveAddInsertionIndex owns the focused-row insertion index; the view model
maps selection to a phase and formats its localized destination.

The variable dialog uses a searchable master list and a value-first editor with visible duplicate
and delete actions. Keep type filtering, protected types, legacy editors, per-variable drafts,
explicit apply/discard, and per-usage detachment. Each detach button supplies its own usage to the
existing operation rather than relying on a separately selected row. The footer clearly states
that unapplied changes survive switching variables. Existing persistence and execution contracts
are unchanged. Native controls and canonical descriptors replace the mockup's illustrative icons
and copy; the application's themes remain authoritative.

## Alternatives considered

Inline insertion plus buttons violate the requested single entrypoint. Applying the largest
selected index loses the actual focus in reversed multi-selection. Keeping a modal condition editor
for Else if introduces a second editing workflow. Showing only icon menus obscures variable actions.

## Consequences

No migration is needed. New incomplete conditions are visible drafts and use normal job validation.
Variable changes are never applied merely by navigating. Existing branch order and stable IDs remain
intact. The prior connected-block rendering decision continues to apply, with explicit branch
buttons added to conditional headers.

## Verification

Cover nearest-block branch placement, duplicate Else rejection, malformed and incomplete structures,
focused insertion, phase fallback, direct branch selection and undo, catalog empty-search and category
states, and variable drafts and usage buttons. Render the actual native dialog contents with hidden
presentation sources at normal and minimum widths, alongside the existing isolated step-list scenes.
Compare the selected mockup with rendered controls and run eng/verify.ps1 -Mode Full.
