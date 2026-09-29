---
name: create-tests
description: Design, scaffold, reorganize, or review DesktopAutomation automated tests across unit, contract, architecture, integration, UI, and end-to-end levels. Use when behavior or regression coverage changes.
---

# Create behavior-oriented tests

Read `../../instructions/testing.md`, `../../../docs/architecture/testing-strategy.md`, and
`references/test-selection.md`.

Describe the observable behavior and invariant before choosing a test level. Prefer the narrowest
level that proves the risk, then add broader coverage only for a distinct boundary. Use
`eng/new-test.ps1` to create a correctly located scaffold when useful; complete every generated
section with meaningful scenarios before verification.

Avoid assertions against source text, method-call choreography, private implementation details, or
the current algorithm unless that detail is itself a stable external contract.
