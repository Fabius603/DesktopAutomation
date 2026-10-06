---
date: 2026-10-06
status: accepted
supersedes:
superseded_by:
---

# Summarize successful step repetitions

## Context

Fast repeating jobs generated large histories, saturated the bounded writer and delayed the log UI.
The user explicitly chose always summarizing successful repetitions while retaining individual
warnings and errors, without a full-detail diagnostic option.

## Decision

Keep execution observation in `StepLogScope`; `LogStepAggregation` at the repository boundary owns
capture reduction. Retain the first successful/skipped main-phase observation as representative
evidence. Further repetitions update optional run summaries with exact counts, total duration and
the last sanitized observation. Buffer repeated starts; retain their individual lifecycle when a
warning, error, cancellation or phase stop occurs. Start/end phases stay detailed. Late background
problems remain individual events. Completed rounds update summary counters instead of debug events.

Bound iteration ranges to 256 per summary and explicitly mark incomplete range coverage. Do not
invent missing-step outcomes in an aggregated historical interval whose membership is unavailable.
Queue summary metadata at most once per second per run and persist current summaries on completion,
flush and shutdown. Abrupt termination remains interrupted/incomplete with no invented completion.

## Alternatives considered

- Collapse rows only in the UI: retains the high capture, storage and read cost.
- Retain every execution behind a diagnostic option: contradicts the selected always-summary policy.
- Drop all routine observations: loses execution counts and representative result evidence.

## Consequences

Earlier schema-v2 runs keep their original evidence. New optional fields default to empty/zero;
no job, macro, automation or settings migration occurs. Summary details refer to the last observation,
and cannot answer arbitrary historical per-iteration questions. Event searches contain individual
retained records; run exports include summary metadata and readable counters. Failures and real
storage loss remain visible independently of intentional aggregation.

## Verification

Contract tests cover round-trip/defaults. Integration tests cover thousands of iterations, exact
counts, warning/cancellation recovery, privacy, exports and restart. WPF rendering verifies summary
rows, count labels, last-observation detail and existing navigation. The full repository gate applies.
