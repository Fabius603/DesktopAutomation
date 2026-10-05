---
date: 2026-10-01
status: superseded
supersedes:
superseded_by: 2026-10-02-layer-step-lists-and-separate-gutter-actions.md
---

# Present job steps as connected blocks

## Context

The user selected the first connected-block design and requested its implementation. Jobs remain
flat persisted step sequences; deeply nested conditions and their branch boundaries must stay clear.

## Decision

Use one overview card template for start, run and end phases. Show localized names, ordinal numbers,
icons and descriptor-based summaries. Edit settings in the right inspector. Render conditional
sections as connected containers with 44 DIP indentation and a readable minimum width, allowing
horizontal scrolling for deep nesting. Collapse state is transient and keyed by existing step IDs.

`ControlFlowStructureAnalyzer` remains the canonical structural owner. `ControlFlowEditRules`
defines complete selection units and the outer markers removed when unwrapping a condition.
The UI shares a cached projection for rendering geometry. Deleting a block includes its content;
unwrapping is an explicit, confirmed action that preserves nested conditions and combines branches
in their existing order. Invalid incomplete blocks must not acquire an invented closing marker.

## Alternatives considered

The compact timeline design and flattened nested cards were not selected. Persisting a new tree
representation would require a migration without improving execution semantics.

## Consequences

Existing jobs keep their storage format and IDs. Collapsing does not dirty a job. Selecting a hidden
child reveals its ancestors. Summary formatting continues to use the existing step descriptors and
field formatters rather than adding a separate step registry.

## Verification

Unit tests cover structural editing, overlapping selection and incomplete blocks. UI tests cover
geometry, preview positions, collapsing, accessible actions and deep nesting. Verify the running
Release application and run `eng/verify.ps1 -Mode Full` before handoff.
