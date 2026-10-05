---
date: 2026-10-05
status: superseded
supersedes: 2026-10-02-preview-step-moves-and-simplify-flow-routes.md
superseded_by: 2026-10-05-hide-internal-block-ends-and-retain-editor-drafts.md
---

# Follow the drag cursor without insert cards

## Context

The user requested removal of all additional Add step cards and reported that the floating drag
preview stays at the upper-left corner. Native OLE dragging does not reliably update WPF mouse
coordinates. Computer Use remains excluded.

## Decision

Retain icon-only dragging, source ghosts, visual-only target gaps, snap thresholds, smooth scrolling,
collapse controls, layered branch routes and compact summaries from the superseded decision.
Remove branch and empty-phase Add step cards, their visual style and reserved branch space. The
main New step command and existing insertion rules remain. Empty phases retain a drop surface;
empty branches use their natural header/closure boundary without a permanent insert card.

`StepDragPreviewSession` owns cursor coordinates for the preview and timer-driven scrolling. During
native dragging read the screen cursor through GetCursorPos and convert it through the connected
WPF surface with PointFromScreen, respecting window offset and DPI. GiveFeedback refreshes the
cursor as well as the existing timer; DragOver can also supply its event coordinates. Missing
presentation sources retain the last valid position instead of forcing the preview to zero. Hover
expansion uses the same tracked coordinates. Structural validity remains in ControlFlowEditRules.

## Alternatives considered

Polling WPF Mouse.GetPosition inside OLE dragging can leave stale coordinates. Subtracting window
origins manually ignores DPI transforms. Retaining invisible insertion buttons leaves unexplained
space and stale hit targets.

## Consequences

Job storage and runtime behavior are unchanged. Permanent cards are replaced by ordinary list
boundaries, while transient previews still reserve space on valid targets. The source drag handler
attaches and detaches GiveFeedback with the other owned drag events.

## Verification

Render actual WPF cards, empty phases and nested branches without insertion buttons. Check cursor
movement through an offset hidden native presentation source with injected screen positions; never
move the user's cursor or show a window. Cover empty branch drops, cancellation cleanup and stable
preview order. Inspect saved renderings and run eng/verify.ps1 -Mode Full.
