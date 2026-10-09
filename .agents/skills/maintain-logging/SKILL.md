---
name: maintain-logging
description: Add, change, review, or troubleshoot DesktopAutomation structured logging, run and step history, automation correlation, diagnostics, retention, privacy, queries, and support exports. Use whenever execution or application logging behavior changes.
---

# Maintain structured logging

Read `../../instructions/architecture.md`, `../../instructions/testing.md`, and
[the current logging architecture](../../../docs/architecture/logging.md).
Respect the repository authority order and inspect git status before editing.

## Canonical owners

- `TaskAutomation.Contracts/Logging/LogContracts.cs`: frontend-neutral schema-v2 records and stable codes.
- `TaskAutomation/Logging/LogRepository.cs`: identity, sequencing, counters, privacy boundary, queued persistence, reads, and retention.
- `LogOutcomeRules`, `LogTimeline`, `LogDiagnostics`: outcome, timeline, and evidence-based diagnostics.
- `StepLogScope`: the shared execution lifecycle for handlers, condition markers, EndJob, and ContinueJob.
- `StepLogResults`: explicit allowlisted summaries of every built-in result family; `StepLogEvents` builds the records.
- `ExecutionLogService`, `AutomationLogService`, `ApplicationLogService`: domain and Serilog adapters over the same repository.
- `DesktopAutomation.Application/Logging`: overview, run history, details, search, grouping, navigation, and ZIP export use cases.
- WPF owns formatting, translations, interaction, and dispatcher marshaling. Never parse messages or files in a view model to infer business state.

## Required capture workflow

1. Define the observable event, correlation, severity, and final outcome before adding behavior.
2. Reuse or add a stable `LogCodes` token. Put reasoning in typed parameters and use `Record`, not a new text-file writer.
3. Create a run before executing. Snapshot stable step IDs, canonical type IDs, position, phase, and enabled state without copying mutable settings or input values.
4. Propagate `LogContext` via `LogAmbient.Push` and dispose the scope. Carry run, dispatcher instance, automation, trigger, parent-run, step, and step-execution IDs as applicable. Each actual step execution gets a new ID; iterations remain explicit.
5. Record started, completed, failed, skipped, or cancelled at the owner that observes the transition. A stop request differs from confirmed cancellation. A rejected start has no fabricated successful run.
6. Use `LogOutcomeRules.Complete`. Errors and warnings affect quality even if the loop reaches its end. Failed, stopped, and interrupted remain distinct. Reuse a problem ID across domain and application observations of the same failure to avoid counting it twice.
7. For branches, disabled steps, loops, debug pauses, and cleanup, preserve phase and iteration. Missing evidence in an incomplete run means unknown, never successful or skipped. Do not invent an end timestamp after a process interruption.

## Step integration requirements

- Route every actual execution through `StepLogScope`, including control operations and materialization. Start and terminal records share one execution ID, phase, and iteration; the terminal record has a measured duration. Exceptions and cancellation each produce one terminal event. Never mark EndJob complete before its settings are resolved.
- Main-phase repetition persistence is owned by `LogStepAggregation` inside `LogRepository`. Keep the first successful/skipped observation as representative evidence; update optional run summaries for repetitions. Buffer repeated starts until outcome is known and restore them for warnings/errors/cancellation/phase stops. Suppress routine messages/output/path events only inside a buffered successful repetition, joining approved paths to its last sanitized observation. Completed rounds update counters. Preserve individual problems, late background observations and start/end-phase evidence. Summaries are intentional coverage, not storage loss; cap iteration ranges and keep `IterationCoverageComplete` explicit. Never reconstruct per-iteration successes from a count or incomplete range coverage. Do not add a full-detail option: the user requested always summarizing successful repetitions.
- Use `StepLogScope.Skip` for disabled steps, inactive branches, and explicitly suppressed end phases. Skips have a unique observation ID and stable reason, with no fabricated execution duration. Preserve the existing structural semantics of condition markers.
- Record `LogBranchDecision` on IF, ELSE-IF, ELSE, and block-end observations. Copy only parent/branch activity, reason, match mode, condition positions, states, and operators. Never serialize `ConditionDebugEvaluation`, condition definitions, actual/expected values, or their formatted details. Debug rendering is separate from persisted logging.
- Maintain `StepLogResults` for every result contract. Include useful statuses, booleans, counts, sizes, approved paths, process identity, and capture freshness. Exclude OCR words/lines/text, clipboard text, window titles, names and item lists from Windows queries, selected labels/values, error-message payloads, applied setting values, images, command arguments, and arbitrary user inputs. Do not replace the allowlist with property reflection.
- Keep `StepLogResultCoverageTests` passing. A new result family must explicitly declare a safe summary before the step is complete. Unknown external result types are marked unsupported without dumping their values.
- An unsuccessful action is a warning; query/detection results that simply find nothing are normal results. Handlers identify intentional non-actions through `IActionExecutionResult.SkipReason`; `StepLogScope` records these as informational skips, preserving `Success` semantics. Empty/null resolved values are normal; broken references and mismatched types remain problems. Run-bound application diagnostics inherit their step area when no more specific category exists. A cancelled user-choice dialog records `UserCancelled` without claiming that the entire job was cancelled.
- A non-waiting script/job step reports `CompletionScope=Dispatch`; it cannot claim execution completion. Background script events and output observations preserve the original context and remain separate from lifecycle events. Late problems update finished-run quality. `LogTimeline` uses only the five lifecycle codes to determine terminal state.
- Script output logs contain stream and character count, never raw output. Capture the original context explicitly for process callbacks. Script and process launch logs omit arguments; shown-text logs omit text. Use `LogDiagnostics.ExceptionDetails` for run-bound exceptions so messages containing materialized values do not escape through diagnostics; keep type, HRESULT, stack, and typed diagnostic codes.
- Run-bound application messages render only approved string metadata and primitive facts. Keep arbitrary string properties, objects, and collections out of the rendered message as well as the parameter dictionary. Preserve the current step phase and iteration for application, script-output, and background observations.

## Persistence and privacy invariants

- Only `AppPaths.StructuredLogsDirectory` (`Logs/v2`) is searchable. Historical `.log` and legacy JSON logs stay untouched and invisible. Do not add migration, fallback parsing, regex reconstruction, or legacy-counter import.
- Events have stable IDs and a repository sequence. Never use timestamps as identity. Live and persisted projections use the same sanitized record.
- Use the bounded asynchronous writer. Producers must not perform disk I/O; consumers must handle partial or unavailable results. Queue overload and I/O failures must leave evidence of loss, never fabricate completeness. Dispose the repository at application shutdown.
- Metadata writes use atomic replacement. Keep persistent counters and completion summaries independent of event retention. Protect active runs during retention; deleting retained evidence makes affected details incomplete. Defaults are 30 days, 500 MiB of event segments, and rotation near 10 MB; active runs may exceed the quota.
- Register loaded secret values before use. `LogPrivacy` is the shared boundary for metadata, live events, disk, and exports. Allowlist useful primitive result metadata. Never log secret stores, credentials, authorization headers, request bodies, OCR text, arbitrary input values, full settings, or reflected result dumps.
- Redaction cannot identify every unknown secret in third-party free text. Keep such content out of capture rather than relying on regexes. Export re-sanitizes retained entries and run metadata.

## Query and UI contract

- Use `LogQueryService` and `LogExportService`, not adapter tail limits, for new views.
- Search covers retained v2 segments. Time ranges use inclusive `From` and exclusive `Until`; minimum severity is inclusive.
- Carry `SnapshotSequence` and `NextBeforeSequence` across event pages. Use `AfterSequence` for new-event counts and live catch-up; use IDs to merge. Carry the run catalog checkpoint across run pages. Outcomes of active runs can still change.
- Use `TriggerId` to show a trigger and its decisions and related executions. `EligibleAt` means earliest allowed execution, not a guaranteed future trigger.
- Grouping preserves event IDs and counts. Do not hide the underlying records or replace them with an invented common root cause.
- Navigation uses the run snapshot and current job existence. Deleted jobs or steps must produce a reason; missing links must not crash or silently open a different step.
- Diagnostics derive from typed exceptions and approved parameters. Unknown errors remain unknown. Add cause/help keys to both resource files. Never infer a missing path from a substring of an exception message.
- ZIP exports include manifest, run snapshots, JSONL events, readable report, scope, version, and completeness issues. Respect cancellation, do not overwrite an existing destination, and remove temporary files on failure.

## UI backend facts

Read [the current UI data mapping](../../../docs/architecture/logging-ui-data.md) when integrating
or changing a log screen. Keep these rules at their canonical owners:

- `SelectRuns`/`IsProblem` determine list, overview and export membership. Pass `Source=Job` for
  job-only history. Full attention counts never come from the 20-row preview. `ExportRunsAsync`
  uses the effective filter/checkpoint and all matching runs, ignoring visible-page offset/size.
- Capture `LogTriggerSnapshot` on observation and carry it through `JobStartContext`. Include
  observed event kind and watched directory; never reconstruct historical context from edited
  definitions. `AutomationHistoryAsync` groups by trigger ID before filtering and resolves every
  related instance. Use its title key; no real run means requested, never started.
- Record `LogFlowEffect` only when the owner observes a phase stop/iteration change. Continued
  end-phase failures have no stop effect. `LogTimeline` owns cause links and suppresses invented
  causality when evidence is incomplete. Keep cleanup independent of main-phase failure.
- Use `StepLogPaths` for approved normalized path roles. At filesystem boundaries capture already
  resolved paths before I/O. Separate target directory from actual unique output file. Invalid or
  redacted paths are unavailable for navigation; never parse exception messages for a path.
- `LogPresentation` supplies semantic areas and localized title/summary/impact keys with safe
  arguments. Add keys in both languages. Unknown/unsupported queries cannot become resource-absent
  claims. Use `EventDetailsAsync` to join paths and the exact related run/step position.
- New contract fields are optional for earlier v2 records. Keep their defaults empty/unknown;
  neither migrate legacy logs nor backfill facts from current definitions. Preserve shared privacy
  sanitization for snapshots and paths and `RunChanged` notifications for late problems.

## Verification

The WPF workspace is owned by `LogsHomeViewModel`; the shared views use `LogDetailPanel`.
Keep filter/export checkpoints, ID-based selection, locale changes, paused live catch-up and
current-definition navigation covered by `LogScreenRenderingTests`. Normal execution-tab navigation
starts without problem filters; the attention action enables `OnlyNewProblems`. A run-detail refresh
must consume its pending dirty state so returning from the editor cannot replay loading every tick.
`LogAttentionService` owns Neu/Gesehen/Erledigt in `AppPaths.LogAttentionDirectory`, separate from
immutable logs. The overview counts new problems; quality counts keep their original meaning.
Confirm only the selected observation after successful loading; preserve explicit reopening across
background refresh. Duplicate problem IDs stay acknowledged; new IDs or increased severity reopen.
Missing evidence uses an explicitly confirmed run summary revision. Bulk confirmation uses captured
problem records/checkpoints. Publish state only after atomic persistence succeeds, keep failures
visible, and preserve unfiltered history/export evidence. Prefer an unseen observation when opening a run; expose the next unseen problem even within
the same step. Cover restart, concurrent writes, exact-event navigation and failures. Add actions through the
canonical `LogDiagnostics.PathActions` allowlist; never execute a captured file. A run/step can
be navigated from its retained snapshot even when no step event exists. Do not restore the old
adapter-based view models or add a parallel tail reader.

Use the repository `create-tests` skill for risk-based coverage. Verify correlation across parallel runs,
equal timestamps, stable pagination with live arrivals, final quality counts, branch and cleanup
timeline states, privacy across disk/live/export, corrupted or inaccessible files, writer loss,
retention, cancellation, and restart interruption when the respective behavior changes.
Contract changes require round-trip and compatibility checks. Keep historical-log exclusion covered.
Run the skill validator when editing this file. Select final checks according to
`../../instructions/testing.md` and use `../validate-repository/SKILL.md` for commands.

Also follow [website documentation maintenance](../../instructions/website-documentation.md):
update affected explanations and examples in the same task, regenerate the reference, and run
the `website-docs` verification check. Existing deferred explanations are not a waiver for new ones.
