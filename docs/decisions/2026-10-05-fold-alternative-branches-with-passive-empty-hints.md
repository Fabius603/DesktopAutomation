---
date: 2026-10-05
status: accepted
supersedes:
superseded_by:
---

# Fold alternative branches with passive empty hints

## Context

The user requests plain fold arrows to the left of If, Else-if and Else symbols and small
information for empty branches that creates no extra insertion locations during dragging.

## Decision

Retain whole-block folding for If. Else-if and Else fold only their own contents, leaving
sibling headers visible. A plain right chevron means folded; a down chevron means expanded.
The arrow has a keyboard-accessible button hit target and a localized automation name.

ControlFlowBlock.SectionEndExclusive owns branch boundaries and IsSectionEmpty consumes it.
StepListProjection adapts these boundaries into visibility and reveals folded ancestors when
selecting a hidden child. Drop placement skips a folded alternative's contents using the same
shared boundaries. Persisted steps, IDs, and internal closing markers are unchanged.

Show an empty-branch hint below its header only when the branch is expanded and actually empty.
The hint belongs to the real header's template, has no hit target, and adds no list item, model,
command, simulated step, or insertion index. Preview projections update emptiness naturally.
Measured hint geometry extends the existing scope guide and keeps the block exit below the hint.

## Alternatives considered

A separate placeholder list item creates synthetic drop rows. A right-side circular fold icon
conflicts with the requested location and arrow-only appearance. Collapsing every alternative
as the entire If block hides unrelated branches.

## Consequences

Fold state remains transient. Existing whole-block moves and deletes keep their semantics.
Both languages contain the same informational key. There is still one canonical branch boundary
calculation and the existing measured drop adapter operates only on real persisted steps.

## Verification

Exercise folded alternatives, revealing hidden children, empty and preview-filled branches,
and drop placement over hint content. Inspect native Release renders of empty branches and
folded alternatives. Run the mandatory full repository verification entrypoint.
