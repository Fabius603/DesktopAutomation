---
date: 2026-10-02
status: superseded
supersedes: 2026-10-02-layer-step-lists-and-separate-gutter-actions.md
superseded_by: 2026-10-05-follow-the-drag-cursor-without-insert-cards.md
---

# Preview step moves and simplify flow routes

## Context

The user explicitly requested icon-only dragging, a source ghost, previews that snap into valid
positions, a floating preview elsewhere, smooth edge scrolling, clearer collapse actions and
shorter understandable summaries. Activation must no longer be editable in the inspector.
Computer Use is excluded by the user.

## Decision

Preserve flat persisted jobs, stable IDs and the shared control-flow analyzer. Only the colored
step icon starts a job drag. Cards still contain icon, localized name and summary; numbers and
breakpoints retain separate hit regions. Activation remains in the list context menu and keyboard
shortcut. A permanently visible 32 DIP collapse button has a circled chevron and state-specific
localized tooltip and accessibility name.

A `StepDragPreviewSession` owns only transient WPF state. Capture the original card before dimming
source rows; keep a faded copy in the logical source position. A valid nearby target reserves a
76 DIP visual gap and shows the card with a subtle accent outline. Elsewhere a compact card follows
the pointer. Target acquisition uses 30 DIP proximity and retention uses 44 DIP, preventing flicker.
The preview never sorts or mutates collections. Measured targets remove preview displacement,
restore transforms and minimum height before changing targets, and revalidate on drop. Escape,
unload and completed or rejected drops release all transient state. Multiple selection and whole
If blocks use the existing canonical `ControlFlowEditRules` and captured selection. Existing macro
preview behavior remains explicitly separate presentation policy.

A dispatcher timer updates the cursor preview and scroll offsets every 16 ms. Scroll speed ramps
quadratically near viewport edges, caps at 600 DIP/s, and uses elapsed time with a bounded timestep.
After scrolling, update layout before resolving and validating the target. The existing delayed
expansion opens collapsed groups without changing selection or persisted job state.

Nested surfaces and muted side brackets describe membership. Curved connections between symbols
show execution order with a small arrow only at the destination. Branches merge through inset
right-side return lanes; nested groups reconnect through their own closure. An incomplete block
has no invented bottom closure. Paint surfaces before paths; keep paths outside card text.

`JobStepDetailsProvider` remains the summary owner. Show at most two important values, normalize
multiline text, shorten long text, keep duration units, use human-facing monitor numbers, show
collection and answer counts, and use concise source names and actual enum labels. Missing sources
and secrets do not reveal internal identifiers. Visible source positions use the same localization
helper as gutter numbering; detailed inspector formatting remains available.

## Alternatives considered

Sorting collection views during hover makes selection and target coordinates unstable. Moving
collections before a confirmed drop makes cancellation unsafe. Dense arrow rails to every child
confuse membership with execution. Full settings or serialized objects overwhelm compact cards.

## Consequences

No persistence migration or runtime rule change is needed. Structural validity and mutation remain
owned by `ControlFlowEditRules`; frontend helpers handle geometry and presentation only. Source
and target visual state must be restored on every termination path. An isolated WPF process renders
actual templates without opening windows or loading user data.

## Verification

Cover source handle hit regions, rejected targets, snap retention, bounded edge scrolling, stable
collection order and preview cleanup. Render ordinary, nested, collapsed, snapped and floating
states; inspect saved images. Check real collapse hit sizes and localized automation names, inspector
switch removal, gutter separation, compact card content and nested insets. Existing structural tests
cover complete moves, empty branches, phase changes, collapsed targets, outdenting and undo. Run
`eng/verify.ps1 -Mode Full` before handoff. Native operating-system drag input is not automated.
