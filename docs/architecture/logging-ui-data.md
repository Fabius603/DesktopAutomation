# Log UI data mapping

This is the current application contract for the four log views. It documents implemented backend
capabilities, not a historical mockup specification. UI formatting/localization and interaction
remain in WPF. Historical log files stay excluded. Missing retained data is explicitly partial or
unavailable; no recovery of uncaptured fields is attempted.

## Overview and job execution history

Use `LogQueryService.OverviewAsync(RunQuery)` and `RunsAsync(RunQuery)`.

| UI element | Backend data | Navigation/action |
|---|---|---|
| Attention banner | `NewProblemRunCount`, `NewProblemRuns`, `LogAttentionService.NextNewAsync` | Run ID plus selected unseen problem; multiple runs use `OnlyNewProblems` |
| Recent/list rows | `Name`, `ExecutionNumber`, `Source`, `Outcome`, `StartedAt`, `DurationMs`, errors/warnings | Open exact run ID |
| Origin | `Origin`, `OriginName`, `OriginId`, historical `Trigger` | Automation ID or parent run ID |
| Active duration | `StartedAt`, active outcome, optional ended timestamp | UI clock formats elapsed time without persisting an invented terminal duration |
| Filters/counts | `Source`, `SourceId`, outcome, time, text, `OnlyProblems`, `OnlyNewProblems`; `Total`, `NextOffset`, checkpoint | Preserve filter and checkpoint on pagination/back navigation |
| Recent automations | `AutomationHistoryAsync` with page size 20 and chosen time range | Trigger selection; related runs as below |
| Export history | `ExportRunsAsync` with the same effective run filter/checkpoint | ZIP; all matching runs, not just the displayed page |

`Source=Job` excludes macro runs. Supported outcomes include warnings, errors, paused, interrupted
and unknown; never render all of them as failed. Counts are scoped to the same selected catalog.

## Run and step details

Use `RunAsync(runId)`, `DisplaySteps`, `StepCounts`, `PrimaryProblem` and `Navigation` with the current
job catalog. Step display titles/icons come from the canonical step descriptor, not invented
historical custom names.
Repeated main-phase successes/skips use optional `LogStepExecution.Summary`: one row shows the
execution count, total duration and average duration per execution in milliseconds, while details
explicitly describe its last observation. `LogStepSummary.AverageDurationMs` derives the average
from existing total/count facts without persisting another value. A measured zero remains zero;
an empty summary or an unmeasured structural skip has no average.
`StepCounts` weights these rows by summary count. `LogRun.CompletedIterations` counts completed
rounds; interrupted partial rounds do not acquire completion. Individual problems remain navigable.
`EventDetailsAsync` also resolves the last sanitized summary observation from run metadata. Older
v2 histories retain their detailed rows; summary coverage is not a missing-evidence error.

| UI element | Backend data | Navigation/action |
|---|---|---|
| Header | Run name/number/outcome/time/duration and origin snapshot | Back to saved list; job/automation/parent IDs |
| Timeline | Stable step ID/type/position, phase, iteration, execution ID, outcome, reason, duration | Select exact observation, not only the step definition |
| Safe description | `LogStepDisplay.Summary` key and approved arguments | Localization only |
| Cause and impact | `LogStepCause`, `LogStepDisplay.Impact`, explicit flow effects | Highlight originating observation |
| Error panel | `EventDetailsAsync` and `LogPresentation.Event`, diagnostic cause/help keys | Open run/step and approved `CheckPath` actions |
| Paths | `LogPath.Kind` and normalized value; source/target/directory/file roles | Invalid/missing/redacted paths cannot be opened |
| Technical details | Code, diagnostic code, timestamp, correlation, sanitized details/parameters | Clipboard copy needs no separate logging API |
| Footer/export | Outcome counts, read state/issues; `ExportAsync(LogQuery(RunId))` | Export this run; missing/deleted targets disable navigation with reason |

A main failure does not imply that end-phase cleanup was skipped. A cancelled user choice does not
claim whole-job cancellation. Dispatch-only steps do not claim child execution completion.

## Automation history

Use `AutomationChoicesAsync` and `AutomationHistoryAsync`. The former describes the current
selection catalog; the latter exclusively uses historical observations.

| UI element | Backend data | Navigation/action |
|---|---|---|
| Selection/context | Automation ID/name, `DefinitionExists`; row trigger snapshot directory and target | Open current automation/target only if still present |
| Timeline | Trigger ID, observed timestamp, `TitleKey`, reason, grouped events, completeness | Select trigger ID; paging uses count/offset/checkpoint |
| Recognized file | Retained trigger parameters `FileName`/`Path`, explicit snapshot event kind | No file content or request body |
| Eligibility | `EligibleAt` | Display earliest permitted start, not an appointment |
| New execution | `StartedRuns` with resolved run ID/source/name/number/outcome | Missing run leaves request pending/unavailable; no false started label |
| Already-running executions | Every `RelatedRuns` link; no new run is invented | Select among multiple related instances |
| Technical details | Grouped events and safe trigger/decision parameters | Event or automation-source export |

Definition edits/deletion do not rewrite history. A trigger lacking decision evidence remains
partial/unknown. Event-kind details are unavailable for older observations that never captured them.

## Application diagnostics

Use cross-source `SearchAsync(LogQuery)`, `LogQuery.Category`, `LogPresentation.Event` and
`EventDetailsAsync`. Filtering only `Source=Application` intentionally excludes domain events.

| UI element | Backend data | Navigation/action |
|---|---|---|
| Table | Timestamp, semantic category/area key, severity, title key | Select stable event ID |
| Details | Safe summary, diagnostic cause/help, correlated paths and technical evidence | Approved diagnostic actions and related run link |
| Execution link | Run link plus stored step position/ID/execution ID | Run details with selected observation; editor navigation checks current existence |
| Search/counts | `MatchCount`, `AvailableCount`, issues/read state | Available count is the whole retained checkpoint, not the filtered count |
| Live/paging | `AfterSequence`, `SnapshotSequence`, `NextBeforeSequence`, repository notifications | Pause scrolling only; capture continues; merge by event ID |
| Export/copy | `ExportAsync` with identical event query; already sanitized detail object | ZIP or clipboard |

Unknown third-party text cannot be guaranteed secret-free by regexes; rely on capture allowlists and
known-secret masking, never promise that every possible confidential string can be recognized.

## WPF implementation

`LogsHomeViewModel` owns the shared workspace and transient selection/filter state. `LogsHomeView`
hosts `LogRunListView`, `ExecutionLogsView`, `AutomationLogsView` and `ApplicationLogsView`; each
detail surface uses `LogDetailPanel`. They consume only the v2 application queries and exports.
The former adapter-based view models and tail-follow behavior have been removed.

Editor navigation uses `LogQueryService.Navigation` and rechecks current definitions before opening.
The run/step overload also supports retained steps that did not execute. `LogDiagnostics.PathActions`
is the canonical path-action allowlist, including successful results, while the WPF adapter opens
folders or selects files in Explorer and reports unreachable paths. It never executes captured files.

Event selection uses stable event IDs; trigger selection uses trigger IDs; step selection preserves
execution ID, phase and iteration. Loading further pages freezes their checkpoint. Run exports
preserve that checkpoint and cover all matches. Reading or scrolling application events pauses
visible updates while recording continues; new matching events are counted until explicitly loaded.
Advanced diagnostics expose source/category filters and repeated-event groups without hiding records.
Definition changes, partial history, deleted targets and absent links remain explicit states.

Localization updates filter labels without clearing selected values. Theme brushes and the existing
step icon/category presentation are reused. Isolated Release WPF rendering and interaction coverage
is in `LogScreenRenderingTests`; screenshots include all four views, empty/compact/light states.


### Navigation and refresh follow-up

All log headings precede the shared tabs, including application diagnostics. Normal execution-tab
navigation opens with `OnlyProblems=false`; the attention action explicitly enables `OnlyNewProblems` instead.
A completed run-detail refresh consumes the pending dirty state, so returning from the step editor
does not start a new loading cycle on each timer tick. New repository notifications still trigger
refreshes. Selection preserves the exact step execution ID.

The attention banner uses `LogOverview.NewProblemRunCount` and `NewProblemRuns`, supplied by
`LogAttentionService`. `ProblemRunCount` still measures execution quality. `RunQuery.OnlyNewProblems`
shares membership with history and export. The [attention behaviour](../concepts/log-attention.md)
documents per-problem read state and the separate local persistence contract. A selected observation
is confirmed only on a real visit or selection change, not repeatedly during background refresh.


Beim Öffnen einer Ausführung werden bevorzugt noch nicht angesehene Probleme gewählt.
„Weiteres neues Problem ansehen“ führt gezielt zur nächsten Beobachtung, auch bei mehreren
Problemen desselben Steps. Gesehene Hauptfehler verdrängen dabei keine neue Warnung.
Fehlen Step-Belege für einen Run-bezogenen Fehler, wird dessen Anwendungsdiagnose geöffnet.
