---
date: 2026-10-05
status: accepted
supersedes: 2026-10-05-render-measured-step-scopes-with-directional-sequences.md
superseded_by:
---

# Retain coordinate inputs and use themed focus markers

## Context

The user reports a broken Position detail field, requests an audit of related fields, removes
the small gray downward arrow at conditional block ends, and disables dotted outlines appearing
on keyboard actions such as Ctrl+V throughout the application.

## Decision

Retain the superseded record's scope guides, directional sequences, measured routing ports,
redraw policy, editor behavior, persistence, diagnostics, and internal-marker handling. Remove
only the decorative gray closing chevron. The block's logical exit port still routes a following
real step and never becomes a selectable step or insertion target.

A point pair without an optional whole-value source shows its individual coordinate inputs and
has no whole-source button. GeneratedStepPointFieldPairViewModel owns this presentation state
and forwards source-mode changes to bindings. Value validation and persistence stay in shared code.

Replace the application resource at SystemParameters.FocusVisualStyleKey with an empty focus
adorner template. This suppresses WPF's extra dotted keyboard-focus decoration globally, without
changing keyboard navigation, selection, or existing themed focus borders and foreground markers.

## Alternatives considered

Changing only the paste handler treats one trigger rather than the shared WPF decoration.
Clearing keyboard focus would break navigation. Per-control fixes would leave the same dotted
adorner active elsewhere. Treating a missing whole source as false hides ordinary coordinates.

## Consequences

Positions remain editable in direct and referenced modes. No stored job migration is needed.
Application-wide focus decoration has one resource owner. Scope exit spacing remains compatible
with existing drag placement; only the decorative glyph is removed.

## Verification

Render every editable built-in step with expanded detail sections, record binding errors, and
assert coordinate visibility. Inspect Position at normal and constrained widths and process
window placement. Check the global focus template and absence of the gray closing chevron.
Run the full repository verification entrypoint.
