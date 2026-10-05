---
date: 2026-10-05
status: proposed
supersedes:
superseded_by:
---

# Use structured user logs with an isolated v2 store

## Context

The new log views require correlated runs, step timelines, automation decisions, actionable
diagnostics, retained search, and support exports. Existing text logs cannot reliably reconstruct
these facts. The user explicitly requested excluding historical logs, implementing the backend,
removing unused code, and documenting future logging in a repository skill.

## Decision

Implement frontend-neutral schema-v2 contracts and a canonical queued repository in TaskAutomation.
Store events and run metadata in an isolated `Logs/v2` directory. Leave historical files untouched
and invisible. Adapt job, automation, and application logging to that owner; provide view use cases
and exports in DesktopAutomation.Application. Keep presentation localization in the WPF shell.
Use stable identity, explicit lifecycle events, evidence-based diagnostics, shared privacy rules,
and explicit incompleteness rather than inferred success.

The historical exclusion and backend implementation are directly authorized. This record remains
proposed for the implementation-specific storage and retention choices until separately accepted;
it does not impose an approval step on the authorized implementation.

## Alternatives considered

- Parse old messages: cannot establish reliable step identity or missing transitions and conflicts
  with the user's historical-log exclusion.
- Maintain separate stores: duplicates privacy, retention, search, and correlation rules.
- Introduce a database: provides indexed search but adds a dependency and migration/repair burden.
  Local schema-v2 files are sufficient for the present backend and preserve an abstraction for a
  later indexed repository.

## Consequences

Existing UI projections continue to work against new records while new layouts can consume typed
use cases. Logging does not change persisted job, macro, automation, or settings formats. Search
cost grows with retained data; active runs may exceed quotas. Redaction of unknown third-party
free-text secrets is not guaranteed. Missing historical evidence, arbitrary root causes, and
confirmed completion after abrupt process exit cannot be reconstructed.

## Verification

Logging unit/integration tests cover outcomes, timelines, stable pagination, live identity,
correlation, privacy, restart interruption, corrupted files, writer failure, and ZIP exports.
Contract tests cover structured serialization. The required `eng/verify.ps1 -Mode Full` gate
checks architecture, resources, documentation, formatting, release build, and all test layers.
