---
date: 2026-10-07
status: accepted
supersedes:
superseded_by:
---

# Own job child executions until termination

## Context

The user requested a review of orchestration, chose to keep a job active until its scripts,
macros and child jobs finish, and authorized implementation and behavior tests.
Detached script tasks and cancellation-only child-job tracking could report completion early.
Parallel instances also shared resource keys and model disposal without ownership.

## Decision

`JobExecutor` owns lifecycle and one sequence runner for all phases. `OwnedExecutionScope`
tracks parallel tasks before dispatch. A step's existing wait option controls only when the
next step may proceed. Main-phase children are joined before the end phase; end-phase children
are joined before resource disposal. Awaited macros already complete inside their steps.
No persisted job format or existing enum value changes. Append the waiting runtime state.

An execution snapshot retains job/step IDs and is captured at admission. Catalog reloads publish
complete immutable dictionaries. `JobDispatcher` reserves capacity atomically and executes the
captured definition. Waiting child starts return failure/cancellation through the typed executor
outcome; rejected starts cannot become successful steps.

A child failure fails the owning job and stops sibling work. Main-phase failure also stops
owned work. Ordinary stop allows cleanup; force stop propagates separately through the child tree.
The existing end-phase timeout supplies the child stop grace period. On expiry, escalate child
cancellation but retain visible activity and resources until actual termination. Normal joins have
no time limit. Cleanup-launched work remains subject to its phase cancellation. Video finalization
uses a cancellable process stop that kills FFmpeg and joins the writer. Cancellation cannot forcibly
terminate arbitrary in-process native code; such work stays active until it returns.

Overlay and preview keys include the execution owner. Input blocking retains independent owners
and safety deadlines. The single capture indicator follows the last active owner and restores
another owner's indicator on release. Shared YOLO usage is leased through `ModelLifetime`, including
inference. The last lease releases the model; one cancelled waiter cannot cancel shared loading.

Runtime structure errors reject execution before side effects. Cleanup control operations use the
same continue-after-error policy as ordinary cleanup steps. A failed cleanup condition suppresses
its block. Existing condition defaults and unavailable-value interpretation remain compatible;
this change does not silently introduce a new condition product policy.

Repeating rounds shorter than 1 ms use a cancellable 1 ms scheduling delay. More expensive rounds
only yield, preserving detection throughput. This prevents empty loops monopolizing a worker. Logging producers fetch one run by ID instead
of sorting the run catalog for every observation.

## Alternatives considered

Detached child execution contradicts the requested job lifecycle. Declaring a task finished when
its timeout expires would dispose resources still in use. Globally disabling parallel jobs would
hide ownership defects. A new persisted repeat setting is unnecessary for the scheduling safeguard.

## Consequences

Existing non-waiting scripts and child jobs now keep their owner alive. EndJob ends further main
steps and joins already launched work before optional cleanup. Child errors affect the owner.
Normal completion does not stop successful background children. IDs and persisted settings remain
compatible. Application-wide model consumers must acquire leases while using a session.

## Verification

Behavior tests cover ownership joins, failure propagation, stop/force stop, capacity races,
execution snapshots, cleanup control failures, independent resource keys and shared model leases.
The required completion gate is `powershell -NoProfile -ExecutionPolicy Bypass -File eng/verify.ps1 -Mode Full`.
