# Structured user logs

The backend uses one schema-v2 repository under `AppPaths.StructuredLogsDirectory` (`Logs/v2`).
Historical log files remain on disk, but are never read, merged, migrated, or displayed by the new model.
The schema is declared in `TaskAutomation.Contracts/Logging/LogContracts.cs` without frontend dependencies.

## Capture and ownership

`LogRepository` assigns event IDs/sequences, sanitizes capture, stores immutable run snapshots and
problem counts, and queues writes. `ExecutionLogService`, `AutomationLogService`, and
`ApplicationLogService` adapt domain events and Serilog to that owner. The old application text
parser, independent automation/job file writers, reflection-based result dumps, and tail-storage
service have been removed. Compatibility projections remain for the existing UI until the new
views are connected.

Runs contain a source snapshot, origin, dispatcher instance, automation/trigger context, parent-run
link, ordered step snapshots, timestamps, duration, outcome, warning/error counts, and completeness.
Step observations carry phase, iteration, and execution identity. Missing steps are projected from
the snapshot rather than guessed from German message text. `LogOutcomeRules` separates completion
from quality; `LogTimeline` distinguishes disabled/inactive steps, unexecuted steps, cleanup, and
missing evidence. Application diagnostics inside a step share its problem identity.

`StepLogScope` is the canonical lifecycle owner for handlers and control operations. Actual
executions have a start and one terminal event under the same execution ID with measured duration;
disabled/inactive/suppressed steps have explicit skip reasons. EndJob settings are resolved before
completion. Structured branch decisions contain states and operators without raw comparison values.
`StepLogResults` covers all built-in result families with an explicit privacy allowlist, enforced by
contract coverage tests. A user-cancelled choice is distinct from cancellation of the whole job.

Non-waiting execution records acknowledge dispatch. Background script events and output observations
retain the original execution context; output records contain stream and character count only.
Late problems update completed-run quality without replacing lifecycle completion records. Run-bound
exception diagnostics preserve exception types, HRESULTs, and stacks while excluding input-bearing
exception messages. Script/process arguments and displayed text are not logged.

Automations record trigger observation, target, policy decision, related instance IDs, and earliest
eligibility where known. A dispatcher start request is not evidence of successful completion.
Macro dispatches also receive run identity and completion summaries; individual low-level recorded
mouse/key commands are not exposed as job-step rows.

## Read use cases

`LogQueryService` supplies overview, run history, run detail timelines, navigation, full retained
event search, and grouping for the planned views. `LogQuery` supports source/run/trigger, time,
severity, area, text, problems, and sequence cursors. Event pages hold a fixed sequence checkpoint;
`AfterSequence` enables new-event counts and catch-up. Run pages hold a catalog checkpoint so newly
created runs do not displace pages; mutable outcomes can change filter membership.

`LogExportService` writes a ZIP with manifest, run metadata, JSONL, and a readable report. It flushes
capture first, honors cancellation, reports loss/incompleteness, re-sanitizes output, and never
overwrites an existing destination. Completeness is relative to retained v2 data; active runs are
listed in the manifest. New view layouts are not implemented by this backend change.

## Storage and recovery

Events append to rotated JSONL segments (approximately 10 MB). Run metadata and repository health
use temporary-file replacement. The producer queue is bounded to 8192 requests; failed or refused
writes mark affected runs incomplete. Normal reads merge persisted and pending events by ID.
Corrupt files produce partial/unavailable states without discarding readable entries.

Retention keeps event segments for up to 30 days and approximately 500 MiB; segments with active
runs are protected and can exceed that bound. Completed old run snapshots are removed, interrupted
old snapshots age out by start time, and execution counters survive event deletion. On restart,
open runs become interrupted and incomplete, with no invented end timestamp.

Current storage uses local file scans rather than a database index. Large retained searches and
exports therefore cost memory and I/O proportional to retained data. Automatic repair/retry,
remote storage, retrospective root-cause inference, and reconstruction of historical run/step data
are not provided. No model can recover events that were never captured or were lost on abrupt exit.

## Privacy and diagnostics

`LogPrivacy` is the common capture/export boundary. Known loaded secret values and recognized
credential assignments are redacted; sensitive parameter keys are suppressed. Result metadata
uses an allowlist. OCR text, arbitrary request bodies, settings dumps, and reflected result objects
are excluded. Unknown secrets in third-party free text cannot be guaranteed detectable.

Typed exceptions map to stable file/access/unknown codes. Cause and help resource keys exist in
German and English; actions are offered only when the necessary context is known. Current job and
step existence is checked before navigation. Human explanations never claim an unproven cause.

The operational workflow is in the [logging skill](../../.agents/skills/maintain-logging/SKILL.md).
The design rationale is recorded in the [logging decision](../decisions/2026-10-05-use-structured-user-logs.md).
