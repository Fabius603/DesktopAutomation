---
date: 2026-10-12
status: accepted
supersedes:
superseded_by:
---

# Share field and nested source compatibility

## Context

The user requested repairs for inconsistent nested color compatibility, hidden optional single
sources, invalid in-memory numeric defaults and structured variables hidden by the picker.
Field descriptors and compound schemas had independently derived their accepted source shapes.
The picker also used variable editor support to decide whether an existing source was selectable.

## Decision

`StepInputContractRegistry.AcceptedSourceShapes` and `LegacySourceShapes` own source type and
cardinality compatibility for fields and nested members. Scalar targets accept Single and
OptionalSingle of the expected kind; collection targets remain collections. Display text retains
its dedicated conversion policy. Directory paths retain their typed text path shape.
Provider policies and target constraints remain independent and unchanged.

Existing text color and file-path references use explicit legacy shapes. New selections continue
to use typed Color and FilePath sources. A source is selectable whenever its input contract
accepts it; the presence of a direct variable editor only limits creation and editing.

`JobVariableValueRules` accepts finite float Number values consistently with the JSON runtime
reader. NaN and infinities remain invalid. Stored values and source identifiers are not migrated.

## Alternatives considered

Patching each overlay or nested editor would preserve competing compatibility rules. Restricting
source selection to editable variable kinds would continue hiding valid existing typed values.

## Consequences

Schema exports expose current and legacy source shapes. Optional references can be selected but
must still provide a usable value when consumed; accepting their cardinality does not make a
missing required value valid. Creation remains limited to supported variable editor kinds.

## Verification

Contract matrices compare field and nested compatibility. Behavior tests validate and materialize
legacy text colors, reject invalid color tokens and non-finite numbers, and cover float JSON
round trips. UI tests select whole structured and optional sources and add a text overlay row
through the live editor with valid numeric defaults.
