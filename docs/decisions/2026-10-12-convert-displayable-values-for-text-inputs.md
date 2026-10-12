---
date: 2026-10-12
status: accepted
supersedes:
superseded_by:
---

# Convert displayable values for text inputs

## Context

The user requested that inputs accepting text also accept nearly every variable and display its
value as text. Strict scalar text contracts excluded numbers, collections and structured values.
Text displays and overlays also used different conversion rules.

## Decision

`TextValueFormatter` owns accepted displayable source shapes and conversion. Plain and multiline
text fields, nested text members, text overlays and text comparison operands consume this policy.
Non-image scalar values and collections are accepted. Collections use one value per line;
structured JSON objects retain their contents as JSON. Numbers use the current culture.
Show Text consumes all collection elements instead of silently displaying only the first.

Image data is excluded. Numeric, color, path and closed enum inputs retain their typed contracts.
Provider restrictions remain independent of conversion; legacy references remain readable.
Stored variables and bindings retain their original kinds and values. Conversion happens when
authoring known values or materializing handler inputs, never as a persistence migration.

## Alternatives considered

Keeping strict text-only inputs requires users to create redundant intermediate strings.
Broadening every input would lose the guarantees of numeric, geometry and identity contracts.

## Consequences

New text sources are selectable through shared contracts. Schema exports expose accepted source
shapes for nested members as well as their target kind. Text comparisons convert only their right
operand; numeric and enum comparisons remain typed.

## Verification

Contract tests cover all plain and multiline text descriptors and nested text schemas. Behavior
tests cover authoring, runtime materialization, current and legacy binding trees, collections,
cultures, the source picker and executed text comparison branches.
