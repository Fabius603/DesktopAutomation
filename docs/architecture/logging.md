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
Action handlers set optional `IActionExecutionResult.SkipReason` for expected empty inputs or
deliberately rejected cached frames. `StepLogScope` records an informational `step.skipped` with
that reason; `Success` retains its original technical meaning. Missing properties, unexecuted
sources and type mismatches remain warnings rather than being relabeled as empty detections.
Application diagnostics inherit the active step's canonical area when no specific area exists.
For main-phase iterations, `LogStepAggregation` at the repository capture boundary retains the first
successful/skipped observation as representative evidence and summarizes repetitions in optional
`LogRun.StepSummaries`. Counts, total duration and the last sanitized observation remain available.
Repeated starts are held transiently until completion; warnings, errors, cancellation and phase-stop
effects restore the buffered start and retain individual evidence. Normal messages/output/path
observations within a buffered repetition are suppressed; approved paths join its last observation.
Late background problems remain individual events. Start/end phases retain their existing detail.
Completed rounds update run counters instead of emitting per-round debug messages. Summary metadata
is queued at most once per second per run, and completion/flush/shutdown persist the current summary.
Sequence gaps due to intentional aggregation are not lost entries. Abrupt exit still leaves an
interrupted incomplete run; unflushed in-memory summaries cannot be reconstructed.
Iteration ranges are capped at 256 per summary; `IterationCoverageComplete=false` explicitly marks
that exact historical iteration membership is no longer available. Counts and the last observation
remain exact. Timeline projection does not fabricate missing-step outcomes for that older interval.
Earlier v2 runs default to empty summaries and keep their original detailed events. Run exports
include summaries in `runs.json` and `report.txt`; event-only searches contain retained individual
events. See [the aggregation decision](../decisions/2026-10-06-summarize-successful-step-repetitions.md).
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

Events append in batches of up to 256 queued requests to rotated JSONL segments (approximately 10 MB).
Each batch persists affected run metadata once and then repository health; flush barriers preserve
queue order and confirm both events and summaries. Run metadata and repository health
use temporary-file replacement. The producer queue is bounded to 8192 requests; failed or refused
writes mark affected runs incomplete. Normal reads merge persisted and pending events by ID.
Corrupt files produce partial/unavailable states without discarding readable entries.
Run-scoped reads report their own `run.incomplete` summary, rather than inheriting historical
`storage.overload` or `storage.write-failed` issues from unrelated runs. Cross-source diagnostics
retain these repository health issues; corruption and inaccessible evidence remain visible.

Retention keeps event segments for up to 30 days and approximately 500 MiB; segments with active
runs are protected and can exceed that bound. Completed old run snapshots are removed, interrupted
old snapshots age out by start time, and execution counters survive event deletion. On restart,
open runs become interrupted and incomplete, with no invented end timestamp.

Cleanup runs at startup, on segment rotation and hourly even when no new events arrive. A new
segment is opened on the next write after each UTC date change as well as at the size limit;
routine new messages cannot keep an old segment alive indefinitely. Age is measured by the last
append, so a segment can retain its earliest events for up to one additional day.
The `TimeProvider` timer only queues a coalesced maintenance request; the existing background writer
serializes cleanup with persistence and reads. When protected evidence becomes eligible at run
completion, cleanup is queued immediately. The oldest eligible segments are removed first until
the event budget is met; protected, unreadable or corrupt files can temporarily exceed it.
Counters and repository sequence are persisted before deletion. Missing-evidence flags are saved
before segment deletion (conservatively incomplete if deletion fails), and survive restart.
Old run snapshots remain while their events are retained or their segment membership is unknown.
Per-file access failures do not discard new queued observations; cleanup retries on the next
maintenance pass and clears `storage.retention-failed` after successful recovery. Corrupt content
is preserved because it may contain protected evidence. Segment cache entries are evicted at deletion.

`StorageChanged` is raised outside repository locks after maintenance, including unchanged passes
so previously failed acknowledgement cleanup can retry.
The log workspace coalesces it into its existing refresh path. `LogAttentionService` removes only
acknowledgements of expired runs through its existing atomic update path, both on initial load
and on storage changes. Failed acknowledgement cleanup retains the previous state and is visible.
Retained-run acknowledgements survive event deletion; unread status never extends log retention.
Shutdown stops the timer and drains already queued work. Historical logs outside v2 and user
exports are untouched. The 500 MiB limit covers event segments, not metadata or exported archives.

Current storage uses local file scans rather than a database index. `QueryAll` reads a complete
filtered snapshot in one pass; run-list checkpoints read the repository sequence without scanning
events. A segment cache validates file length and last-write time on each read and removes deleted
files; its budget is 64 MiB of source JSONL (deserialized objects require additional memory).
Large retained searches and exports still cost memory proportional to retained data, and cold reads
require proportional I/O. `LogTimeline` indexes lifecycle and execution IDs once per projection.
Live WPF notifications coalesce into one pending refresh flag rather than queueing dispatcher work
for each observation. Automatic repair/retry,
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

## UI-facing structures (2026-10-06)

The additive v2 contracts preserve existing records and do not migrate historical logs. Missing
trigger snapshots, flow effects, or path roles remain unknown. No job/automation format changed.
See [the UI data mapping](logging-ui-data.md) and
[the decision rationale](../decisions/2026-10-06-capture-log-ui-facts-at-their-owner.md).

- `LogTriggerSnapshot` captures trigger kind, observed event kind, normalized watched directory,
  target ID/kind/name at observation time. `JobStartContext` carries the same snapshot into jobs
  and macros. Never reconstruct these facts from a currently edited definition.
- `RunQuery.Source` and `OnlyProblems`, `LogQueryService.IsProblem` and `SelectRuns` are the single
  filter owners for run lists, overview counts and run export. Preview lists may have 20 rows;
  `ProblemRunCount` and `TotalRuns` count the entire selected catalog. The catalog sequence fixes
  membership against new arrivals; mutable outcomes are evaluated at read/export time.
- `AutomationHistoryAsync` joins retained trigger/decision records by trigger ID before applying
  search and time filters, and resolves each started/related instance into `LogRunLink`. A missing
  run stays `RunUnavailable`; a requested start is called started only after run evidence exists.
  `EligibleAt` is not a promised future trigger. `AutomationChoicesAsync` includes retained entries
  for deleted definitions and explicitly distinguishes current-definition context from history.
- `LogFlowEffect` is recorded at the execution owner when failure/cancellation stops a phase,
  EndJob ends a phase, or ContinueJob advances an iteration. Continued cleanup failures do not
  claim to prevent following steps. `LogTimeline` projects `LogStepCause` only for a provably
  prevented step in the same phase/iteration, for main work prevented by start-phase termination,
  or for explicitly suppressed cleanup. Partial runs do not acquire invented causal links.
- `StepLogPaths` supplies normalized approved path roles without filesystem I/O. SaveImage has a
  target directory and the actual unique output file on success. Filesystem operations record
  already resolved source/target paths before I/O via `step.paths`; arbitrary settings/values are
  never dumped. Invalid paths remain unavailable. Redacted paths are not actionable.
- `LogPresentation` owns semantic areas, titles, safe result summaries and impact resource keys.
  WPF only resolves the supplied keys/arguments and step catalog icons. German and English keys
  are added together. An unsupported query does not claim the resource is absent. Technical events
  with unknown semantics stay generic and retain their sanitized technical evidence.
- `RunAsync` adds display steps, grouped outcome counts and the primary problem.
  `EventDetailsAsync` joins safe paths from the same execution and resolves the related run and
  snapshot step position. `LogQuery.Category` supports frontend-neutral area filtering.
- `ExportRunsAsync` exports all runs matching a `RunQuery` and all retained events correlated to
  those runs, across sources. Offset/page size do not restrict exports to the visible page. The
  manifest carries the effective filter, checkpoint and explicit run IDs, including metadata-only
  runs. Event exports keep their existing `LogQuery` semantics.
- Repository `RunChanged` also announces newly counted late problems so finished-run quality and
  overview counters can refresh without reading legacy adapter caches.


## Persistent attention state

`LogAttentionService` in the application layer projects unread problems separately from execution
quality. `LogOverview.NewProblemRunCount` covers the complete filtered catalog; `ProblemRunCount`
keeps its original meaning. `RunQuery.OnlyNewProblems` shares list/overview/export membership.
Neu/Gesehen/Erledigt acknowledgements live in the local user profile under
`AppPaths.LogAttentionDirectory`, using the canonical atomic JSON repository. No log is changed
or removed by viewing a problem. Known duplicates remain seen; new IDs or increased severity
reopen attention. Missing evidence requires an explicit summary acknowledgement, and failed
persistence does not publish state. See the
[accepted attention decision](../decisions/2026-10-06-store-log-attention-separately.md) and
[behaviour details](../concepts/log-attention.md).
