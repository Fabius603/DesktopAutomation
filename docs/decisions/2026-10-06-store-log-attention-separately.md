---
date: 2026-10-06
status: accepted
supersedes:
superseded_by:
---

# Store log attention separately from execution evidence

## Context

Problem outcomes kept reappearing in the overview after the user had read the warning. The user
explicitly requested implementing the documented Neu/Gesehen/Erledigt concept on 2026-10-06.
Changing log severity or deleting evidence would make history and support reports misleading.

## Decision

`LogAttentionService` in the application layer owns identity, read-state projection and transitions.
A separate versioned document lives under `AppPaths.LogAttentionDirectory` in the local Windows
user profile. It contains run/problem/event IDs, state, acknowledged severity, sequence and a
summary revision, never copied messages or paths. The existing `JsonRepository` owns atomic writes.

A successfully selected problem detail is Seen; explicit actions resolve or reopen it. Background
refresh of the same selection does not override an explicit reopening. Correlated duplicates stay
seen, a new problem or increased severity reopens attention. Incomplete/missing evidence gets an
explicit summary acknowledgement, revision-bound to counters/outcome/completeness. Bulk actions
acknowledge captured problem records only, not later arrivals. Mutations are serialized and cache
state changes only after a successful write. Unreadable state is surfaced and defaults to New.

Query, paging, overview and run export consume the same new-problem filter. Original problem
counts, outcome rules, immutable logs and unfiltered exports keep their semantics. Acknowledgements
are not added to support exports. Obsolete run acknowledgements are pruned on successful writes;
retained run acknowledgements outlive event retention. Legacy logs remain excluded.

## Alternatives considered

- Mutate severity or remove logs after viewing: loses evidence and changes execution quality.
- Keep only an in-memory dismissed banner: warns again on restart and cannot distinguish new problems.
- Confirm an entire run on any visit: silently acknowledges unviewed problems and later arrivals.
- Create a second writer or duplicate rules in each view model: conflicts with canonical persistence
  and gives inconsistent behaviour between overview and diagnostic links.

## Consequences

A new optional, versioned local file is introduced without migrating jobs, settings or v1 logs.
Missing state conservatively means New. Read and write failures are visible; failed writes do not
publish acknowledgements. The service uses worker tasks for reads/writes and shared v2 query paging.
The WPF layer owns only selection, local text, dispatcher handling and commands.

## Verification

`LogAttentionTests` covers persistence, identity, severity changes, explicit transitions, partial
history, frozen bulk scope, concurrent writes, I/O failures and export membership. Release WPF
rendering covers automatic viewing, explicit actions, reopening through refresh, default filters,
heading order and editor-return timer stability. The full repository gate remains mandatory.
