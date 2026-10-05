---
date: 2026-10-05
status: superseded
supersedes: 2026-10-05-hide-internal-block-ends-and-retain-editor-drafts.md
superseded_by: 2026-10-05-retain-coordinate-inputs-and-use-themed-focus-markers.md
---

# Render measured step scopes with directional sequences

## Context

The user requests correcting surfaces that only become current on hover, including after block
moves, and delegates a clearer flow design and its implementation. Return loops around alternative
branches obscure nesting and can imply that the alternatives execute sequentially.

## Decision

Retain every editor, persistence, diagnostic, picker, internal-marker and drag behavior from the
superseded record. Replace return loops with colored scope guides for each branch. Draw downward
arrows only between successive executable steps in the same sequence, never from the end of one
branch to its alternative. A complete block has a single compact neutral exit in its existing
footer lane. An incomplete block has no fabricated exit. A collapsed block connects as one card.

Measure entry ports from the rendered drag icons and card edges. Round an orthogonal elbow when
successive ports differ horizontally; use a straight connection when they align. Nested surfaces
are painted before guides. Observe changes in realized container geometry, source, version and
viewport after layout, and redraw only on change. Hover must never repair the presentation.
Collection changes also invalidate the presentation. Unloading detaches the layout subscription;
loading restores it. The same projection continues to own depth and internal closure margins.

## Alternatives considered

Keeping bypass and return loops requires several overlapping lanes around each nested block.
Redrawing on every layout notification can create a render/layout cycle. Fixed pixel ports cease
to fit when templates or layout change. Using hover to trigger redraw leaves opening and moves
visually stale.

## Consequences

This is a structured editor rather than a complete runtime flowchart. Existing branch names and
indentation identify alternatives; arrowheads exclusively mean the next step in that sequence.
Stable layout produces no repeated invalidations. Stored jobs and drop indices are unchanged.

## Verification

Render ten isolated WPF scenarios without desktop automation, including complete nested block
moves, collapse/reopen, narrower layout, consecutive closures, empty blocks and drag previews.
Compare automatic drawings with an explicit repaint: repainting must not correct stale geometry.
Inspect the saved nested, consecutive-closure and moved-block images and run the full repository
verification gate.
