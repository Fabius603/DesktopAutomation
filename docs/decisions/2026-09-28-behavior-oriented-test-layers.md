---
date: 2026-09-28
status: accepted
supersedes:
superseded_by:
---

# Separate tests by behavior boundary

## Context

The former `TaskAutomation.Tests` assembly mixed domain, persistence, Windows, application, and UI
tests. This hid dependencies and encouraged tests to be classified by source folder instead of the
behavioral boundary they prove.

## Decision

Maintain distinct Unit, Contract, Architecture, Integration, UI, and End-to-End projects. Tests
assert observable outcomes or durable invariants. Shared fixtures live in `tests/TestInfrastructure`.

## Alternatives considered

- Keep one assembly and use traits only.
- Mirror every production class with a test class.

## Consequences

Each test layer can have different dependencies, execution requirements, and CI diagnostics.
Adding production code does not automatically require a one-to-one test; changing behavior does
require coverage at the narrowest sufficient level.

## Verification

The test manifest and repository checks validate the project set; the full gate executes every
deterministic level.
