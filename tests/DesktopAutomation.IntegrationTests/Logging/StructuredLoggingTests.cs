using System.IO.Compression;
using System.Text.Json;
using DesktopAutomation.Application.Logging;
using TaskAutomation.Automations;
using TaskAutomation.Jobs;
using TaskAutomation.Logging;
using TaskAutomation.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;

namespace TaskAutomation.Tests.Logging;

public sealed class StructuredLoggingTests
{
    [Fact]
    public async Task AlternatingBranches_BoundSummaryRangesAndKeepExactCounts()
    {
        using var directory = new TemporaryDirectory();
        using var repository = new LogRepository(directory.Path);
        var id = Guid.NewGuid();
        repository.SaveRun(new LogRun
        {
            Id = id,
            StartedAt = DateTimeOffset.UtcNow,
            Steps = [new("step", "ShowText", 1, "Main", true)]
        });
        for (var iteration = 1; iteration <= 1000; iteration++)
            repository.Append(new LogEvent
            {
                Context = new(id, StepId: "step", StepExecutionId: Guid.NewGuid()),
                Phase = "Main",
                Iteration = iteration,
                Code = LogCodes.StepSkipped,
                Parameters = new() { ["Reason"] = iteration % 2 == 0 ? "Disabled" : "InactiveBranch" }
            });
        await repository.FlushAsync();
        var details = (await new LogQueryService(repository).RunAsync(id))!;
        Assert.Equal(2, details.Steps.Count);
        Assert.Equal(1000, details.StepCounts![StepLogOutcome.Skipped]);
        Assert.All(details.Run.StepSummaries, summary =>
        {
            Assert.Equal(500, summary.Count);
            Assert.Equal(256, summary.Iterations.Count);
            Assert.False(summary.IterationCoverageComplete);
        });
        Assert.Equal(2, repository.QueryAll(new(RunId: id)).Entries.Count);
    }

    [Fact]
    public async Task RepeatedSteps_KeepWarningAndCancellationDetailsAndExportSanitizedSummaries()
    {
        using var directory = new TemporaryDirectory();
        using var repository = new LogRepository(directory.Path);
        repository.Privacy.RegisterSecrets(["private-value"]);
        var id = Guid.NewGuid();
        repository.SaveRun(new LogRun
        {
            Id = id,
            StartedAt = DateTimeOffset.UtcNow,
            Steps = [new("step", "ShowText", 1, "Main", true)]
        });
        for (var iteration = 1; iteration <= 4; iteration++)
        {
            var context = new LogContext(id, StepId: "step", StepExecutionId: Guid.NewGuid());
            repository.Append(new LogEvent { Context = context, Phase = "Main", Iteration = iteration, Code = LogCodes.StepStarted });
            if (iteration == 2)
                repository.Append(new LogEvent
                {
                    Context = context,
                    Phase = "Main",
                    Iteration = iteration,
                    Level = ExecutionLogLevel.Warning,
                    Code = LogCodes.Message,
                    ProblemId = Guid.NewGuid()
                });
            repository.Append(new LogEvent
            {
                Context = context,
                Phase = "Main",
                Iteration = iteration,
                Code = LogCodes.StepPaths,
                Paths = [new("TargetFile", @"C:\private-value\output.png")]
            });
            repository.Append(new LogEvent
            {
                Context = context,
                Phase = "Main",
                Iteration = iteration,
                Code = iteration == 4 ? LogCodes.StepCancelled : LogCodes.StepCompleted,
                DurationMs = 10
            });
        }
        var run = repository.ReadRuns().Single();
        repository.SaveRun(run with { EndedAt = DateTimeOffset.UtcNow, Outcome = LogOutcome.Stopped });
        await repository.FlushAsync();
        var queries = new LogQueryService(repository);
        var details = (await queries.RunAsync(id))!;
        Assert.Equal(3, details.Steps.Count);
        Assert.Equal(2, details.StepCounts![StepLogOutcome.Successful]);
        Assert.Equal(1, details.StepCounts[StepLogOutcome.Warning]);
        Assert.Equal(1, details.StepCounts[StepLogOutcome.Cancelled]);
        Assert.Equal(1, details.Run.WarningCount);
        var summary = Assert.Single(details.Run.StepSummaries);
        Assert.Equal(20, summary.TotalDurationMs);
        Assert.Equal(new[] { new LogIterationRange(1, 1), new(3, 3) }, summary.Iterations);
        Assert.Contains("[redacted]", Assert.Single(summary.LastEvent.Paths).Value);
        var events = repository.QueryAll(new(RunId: id)).Entries;
        Assert.DoesNotContain(events, entry => entry.Iteration == 3);
        Assert.Single(events, entry => entry.Iteration == 2 && entry.Code == LogCodes.StepStarted);
        Assert.Single(events, entry => entry.Iteration == 4 && entry.Code == LogCodes.StepStarted);
        Assert.NotNull(await queries.EventDetailsAsync(summary.LastEvent.Id));
        var target = Path.Combine(directory.Path, "summaries.zip");
        Assert.True((await new LogExportService(repository, queries).ExportAsync(new(RunId: id), target, "test")).IsComplete);
        using var zip = ZipFile.OpenRead(target);
        using var reader = new StreamReader(zip.GetEntry("runs.json")!.Open());
        var json = reader.ReadToEnd();
        Assert.DoesNotContain("private-value", json);
        Assert.Equal(2, Assert.Single(JsonSerializer.Deserialize<LogRun[]>(json, LogRepository.JsonOptions)!).StepSummaries.Single().Count);
    }

    [Fact]
    public async Task LargeRun_RepetitionsAreSummarizedWithoutLosingCountsOnRestart()
    {
        using var directory = new TemporaryDirectory();
        var id = Guid.NewGuid();
        const int count = 12000;
        using (var repository = new LogRepository(directory.Path))
        {
            repository.SaveRun(new LogRun
            {
                Id = id,
                StartedAt = DateTimeOffset.UtcNow,
                Steps = [new("step", "ShowText", 1, "Main", true)]
            });
            var executions = Enumerable.Range(0, count / 2).Select(_ => Guid.NewGuid()).ToArray();
            for (var index = 0; index < count; index++)
                repository.Append(new LogEvent
                {
                    Context = new(id, StepId: "step", StepExecutionId: executions[index / 2]),
                    Code = index % 2 == 0 ? LogCodes.StepStarted : LogCodes.StepCompleted,
                    Phase = "Main",
                    Iteration = index / 2,
                    Message = "step observation"
                });
            var checkpoint = repository.SnapshotSequence;
            repository.Append(new LogEvent { Context = new(id), Message = "later" });
            var run = repository.ReadRuns().Single();
            repository.SaveRun(run with { EndedAt = DateTimeOffset.UtcNow, Outcome = LogOutcome.Successful });
            await repository.FlushAsync();
            var frozen = repository.QueryAll(new(RunId: id, SnapshotSequence: checkpoint));
            Assert.Equal(2, frozen.Entries.Count);
            Assert.Equal(2, frozen.Entries.Select(entry => entry.Id).Distinct().Count());
            Assert.DoesNotContain(frozen.Entries, entry => entry.Message == "later");
            Assert.Null(frozen.NextBeforeSequence);
            Assert.True(repository.ReadRuns().Single().IsComplete);
            var steps = (await new LogQueryService(repository).RunAsync(id))!.Steps;
            var step = Assert.Single(steps);
            Assert.Equal(StepLogOutcome.Successful, step.Outcome);
            Assert.Equal(count / 2, step.Summary!.Count);
            Assert.Equal(new LogIterationRange(0, count / 2 - 1), Assert.Single(step.Summary.Iterations));
            Assert.Equal(count / 2, (await new LogQueryService(repository).RunAsync(id))!.StepCounts![StepLogOutcome.Successful]);
        }
        using var restored = new LogRepository(directory.Path);
        Assert.Equal(3, restored.QueryAll(new(RunId: id)).Entries.Count);
        Assert.Equal(count / 2, Assert.Single(restored.ReadRuns().Single().StepSummaries).Count);
        Assert.Equal(LogReadState.Available, (await new LogQueryService(restored).RunAsync(id))!.State);
    }

    [Fact]
    public async Task HistoricalStorageLoss_DoesNotMakeUnrelatedCompleteRunPartial()
    {
        using var directory = new TemporaryDirectory();
        File.WriteAllText(Path.Combine(directory.Path, "health.json"),
            "{\"Sequence\":0,\"Issues\":[\"storage.overload\",\"storage.write-failed\"],\"Counters\":{}}");
        using var repository = new LogRepository(directory.Path);
        var complete = repository.SaveRun(new LogRun
        {
            Id = Guid.NewGuid(),
            StartedAt = DateTimeOffset.UtcNow,
            EndedAt = DateTimeOffset.UtcNow,
            Outcome = LogOutcome.Successful
        });
        var damaged = repository.SaveRun(new LogRun
        {
            Id = Guid.NewGuid(),
            StartedAt = DateTimeOffset.UtcNow,
            EndedAt = DateTimeOffset.UtcNow,
            Outcome = LogOutcome.Successful,
            IsComplete = false,
            LostEntries = 5
        });
        repository.Append(new LogEvent { Context = new(complete.Id) });
        repository.Append(new LogEvent { Context = new(damaged.Id) });
        await repository.FlushAsync();
        var queries = new LogQueryService(repository);
        var details = await queries.RunAsync(complete.Id);
        Assert.Equal(LogReadState.Available, details!.State);
        Assert.Empty(details.Issues);
        Assert.False(LogQueryService.IsProblem(details.Run));
        var missing = await queries.RunAsync(damaged.Id);
        Assert.Equal(LogReadState.Partial, missing!.State);
        Assert.Contains("run.incomplete", missing.Issues);
        Assert.Contains("storage.overload", repository.Query(new()).Issues);
    }

    [Fact]
    public async Task RetainedReadCache_ObservesFileAppendReplacementAndDeletion()
    {
        using var directory = new TemporaryDirectory();
        using var repository = new LogRepository(directory.Path);
        repository.Append(new LogEvent { Message = "original" });
        await repository.FlushAsync();
        Assert.Single(repository.QueryAll(new()).Entries);
        var path = Directory.GetFiles(directory.Path, "*.events.jsonl").Single();
        var appended = new LogEvent { Sequence = repository.SnapshotSequence, Message = "appended" };
        File.AppendAllText(path, JsonSerializer.Serialize(appended, LogRepository.JsonOptions) + "\n");
        Assert.Equal(2, repository.QueryAll(new()).Entries.Count);
        File.WriteAllText(path, JsonSerializer.Serialize(appended, LogRepository.JsonOptions) + "\n");
        Assert.Equal("appended", Assert.Single(repository.QueryAll(new()).Entries).Message);
        File.Delete(path);
        Assert.Empty(repository.QueryAll(new()).Entries);
    }

    [Fact]
    public async Task Retention_ExpiresOldEvidenceButPreservesExecutionCounter()
    {
        using var directory = new TemporaryDirectory();
        var source = Guid.NewGuid();
        var old = DateTimeOffset.UtcNow.AddDays(-40);
        using (var repository = new LogRepository(directory.Path))
        {
            var id = Guid.NewGuid();
            repository.SaveRun(new LogRun
            {
                Id = id,
                SourceId = source,
                StartedAt = old,
                EndedAt = old.AddSeconds(1),
                ExecutionNumber = repository.NextExecutionNumber(source),
                Outcome = LogOutcome.Successful
            });
            repository.Append(new LogEvent { Context = new(id), Timestamp = old });
            await repository.FlushAsync();
        }
        foreach (var path in Directory.EnumerateFiles(directory.Path, "*.events.jsonl"))
            File.SetLastWriteTimeUtc(path, old.UtcDateTime);
        using var reloaded = new LogRepository(directory.Path);
        Assert.Empty(reloaded.ReadRuns());
        Assert.Empty(reloaded.Query(new()).Entries);
        Assert.Equal(LogReadState.Empty, reloaded.Query(new()).State);
        Assert.Equal(2, reloaded.NextExecutionNumber(source));
    }

    [Fact]
    public async Task LiveCatchUp_AndTriggerFilterDoNotMixEqualTimeEventsFromOtherTriggers()
    {
        using var directory = new TemporaryDirectory();
        using var repository = new LogRepository(directory.Path);
        var trigger = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;
        repository.Append(new LogEvent { Timestamp = time, Context = new(TriggerId: trigger) });
        var checkpoint = repository.Query(new(PageSize: 1)).SnapshotSequence;
        var wanted = repository.Append(new LogEvent { Timestamp = time, Context = new(TriggerId: trigger) });
        repository.Append(new LogEvent { Timestamp = time, Context = new(TriggerId: Guid.NewGuid()) });
        await repository.FlushAsync();
        Assert.Equal(wanted.Id, Assert.Single(repository.Query(new(AfterSequence: checkpoint, TriggerId: trigger)).Entries).Id);
    }

    [Fact]
    public async Task RunPaginationCheckpoint_ExcludesNewRunsAndKeepsMetadataSecretsRedacted()
    {
        using var directory = new TemporaryDirectory();
        using var repository = new LogRepository(directory.Path);
        repository.Privacy.RegisterSecrets(["private-value"]);
        var firstId = Guid.NewGuid();
        repository.SaveRun(new LogRun
        {
            Id = firstId,
            StartedAt = DateTimeOffset.UtcNow,
            CompletionReason = "failure private-value",
            Name = "first",
            EndedAt = DateTimeOffset.UtcNow
        });
        var queries = new LogQueryService(repository);
        var first = await queries.RunsAsync(new(PageSize: 1));
        repository.SaveRun(new LogRun { Id = Guid.NewGuid(), Name = "new", StartedAt = DateTimeOffset.UtcNow });
        var stable = await queries.RunsAsync(new(SnapshotSequence: first.SnapshotSequence));
        Assert.Equal(firstId, Assert.Single(stable.Runs).Id);
        await repository.FlushAsync();
        Assert.DoesNotContain("private-value", File.ReadAllText(Path.Combine(directory.Path, $"{firstId:N}.run.json")));
    }
    [Fact]
    public async Task SearchPages_IncludeRetainedFilesAndExcludeNewArrivalsFromSnapshot()
    {
        using var directory = new TemporaryDirectory();
        using var repository = new LogRepository(directory.Path);
        var timestamp = DateTimeOffset.UtcNow;
        for (var index = 0; index < 205; index++) repository.Append(new LogEvent { Timestamp = timestamp, Message = $"needle {index}" });
        await repository.FlushAsync();
        var first = repository.Query(new(Search: "needle", PageSize: 100));
        Assert.Equal(205, first.MatchCount);
        repository.Append(new LogEvent { Timestamp = timestamp, Message = "needle new" });
        var second = repository.Query(new(Search: "needle", PageSize: 100,
            BeforeSequence: first.NextBeforeSequence!.Value, SnapshotSequence: first.SnapshotSequence));
        var third = repository.Query(new(Search: "needle", PageSize: 100,
            BeforeSequence: second.NextBeforeSequence!.Value, SnapshotSequence: first.SnapshotSequence));
        Assert.Equal(205, first.Entries.Concat(second.Entries).Concat(third.Entries).Select(entry => entry.Id).Distinct().Count());
        Assert.DoesNotContain(second.Entries.Concat(third.Entries), entry => entry.Message == "needle new");
    }

    [Fact]
    public async Task Restart_PreservesCompletedSummaryAndMarksUnfinishedRunInterruptedWithoutInventedEndTime()
    {
        using var directory = new TemporaryDirectory();
        Guid completed, open;
        using (var repository = new LogRepository(directory.Path))
        using (var logs = new ExecutionLogService(repository))
        {
            var run = logs.BeginJob(Guid.NewGuid(), "completed"); completed = run.Id;
            logs.Record(run, new LogEvent { Level = ExecutionLogLevel.Warning, Message = "warning" });
            logs.Finish(run, LogOutcome.Successful, "Completed");
            open = logs.BeginJob(Guid.NewGuid(), "open").Id;
            await repository.FlushAsync();
        }
        using var reloaded = new LogRepository(directory.Path);
        var complete = Assert.Single(reloaded.ReadRuns(), run => run.Id == completed);
        Assert.Equal(LogOutcome.WithWarnings, complete.Outcome);
        Assert.Equal(1, complete.WarningCount);
        var interrupted = Assert.Single(reloaded.ReadRuns(), run => run.Id == open);
        Assert.Equal(LogOutcome.Interrupted, interrupted.Outcome);
        Assert.Null(interrupted.EndedAt);
        Assert.False(interrupted.IsComplete);
    }

    [Fact]
    public async Task SameProblemAcrossJobAndDiagnostic_CountsOnceAndStaysRunScoped()
    {
        using var directory = new TemporaryDirectory();
        using var repository = new LogRepository(directory.Path);
        using var logs = new ExecutionLogService(repository);
        var first = logs.BeginJob(Guid.NewGuid(), "first");
        var second = logs.BeginJob(Guid.NewGuid(), "second");
        var problem = Guid.NewGuid();
        logs.Record(first, new LogEvent { Code = LogCodes.StepFailed, Level = ExecutionLogLevel.Error, ProblemId = problem });
        repository.Append(new LogEvent
        {
            Source = LogSource.Application,
            Level = ExecutionLogLevel.Error,
            Context = new(first.Id),
            ProblemId = problem
        });
        logs.Finish(first, LogOutcome.Failed, "StepFailed");
        logs.Finish(second, LogOutcome.Successful, "Completed");
        await repository.FlushAsync();
        Assert.Equal(1, repository.ReadRuns().Single(run => run.Id == first.Id).ErrorCount);
        Assert.Equal(0, repository.ReadRuns().Single(run => run.Id == second.Id).ErrorCount);
    }

    [Fact]
    public async Task Export_MasksSecretsInLiveDiskAndArchiveAndNeverOverwritesDestination()
    {
        using var directory = new TemporaryDirectory();
        using var repository = new LogRepository(directory.Path);
        repository.Privacy.RegisterSecrets(["private-value"]);
        var live = repository.Append(new LogEvent
        {
            Message = "private-value password=other",
            Details = "Bearer abc123",
            Parameters = new() { ["Token"] = "raw", ["Path"] = "private-value" }
        });
        Assert.DoesNotContain("private-value", JsonSerializer.Serialize(live));
        var queries = new LogQueryService(repository);
        var exports = new LogExportService(repository, queries);
        var target = Path.Combine(directory.Path, "report.zip");
        var result = await exports.ExportAsync(new(), target, "1.0.0");
        Assert.True(result.IsComplete);
        using (var archive = ZipFile.OpenRead(target))
        {
            foreach (var entry in archive.Entries)
            {
                using var reader = new StreamReader(entry.Open());
                var text = reader.ReadToEnd();
                Assert.DoesNotContain("private-value", text);
                Assert.DoesNotContain("abc123", text);
                Assert.DoesNotContain("password=other", text);
            }
        }
        await Assert.ThrowsAsync<IOException>(() => exports.ExportAsync(new(), target, "1.0.0"));
        Assert.Empty(Directory.GetFiles(directory.Path, "*.tmp"));
    }

    [Fact]
    public async Task CorruptEvent_IsPartialInsteadOfEmptyAndDoesNotHideValidEvents()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "broken.events.jsonl");
        File.WriteAllText(path, "{unfinished\n" + JsonSerializer.Serialize(new LogEvent { Sequence = 1, Message = "valid" }, LogRepository.JsonOptions) + "\n");
        using var repository = new LogRepository(directory.Path);
        var page = repository.Query(new());
        Assert.Equal(LogReadState.Partial, page.State);
        Assert.Equal("valid", Assert.Single(page.Entries).Message);
        using var queriesRepository = new LogRepository(Path.Combine(directory.Path, "empty"));
        Assert.Equal(LogReadState.Empty, queriesRepository.Query(new()).State);
        await repository.FlushAsync();
    }

    [Fact]
    public async Task WriterFailure_MarksAffectedRunIncompleteAndSurvivesRestart()
    {
        using var directory = new TemporaryDirectory();
        using (var repository = new LogRepository(directory.Path))
        {
            var id = Guid.NewGuid();
            repository.SaveRun(new LogRun { Id = id, StartedAt = DateTimeOffset.UtcNow });
            await repository.FlushAsync();
            // Block event-file creation while allowing metadata writes, using an isolated directory ACL-independent failure.
            var moved = directory.Path + "-saved";
            Directory.Move(directory.Path, moved);
            File.WriteAllText(directory.Path, "not a directory");
            repository.Append(new LogEvent { Context = new(id) });
            await Assert.ThrowsAsync<IOException>(() => repository.FlushAsync());
            Assert.False(repository.ReadRuns().Single().IsComplete);
            File.Delete(directory.Path);
            Directory.Move(moved, directory.Path);
            await repository.FlushAsync();
        }
        using var restored = new LogRepository(directory.Path);
        Assert.False(restored.ReadRuns().Single().IsComplete);
        Assert.Contains("storage.write-failed", restored.Query(new()).Issues);
    }

    [Fact]
    public async Task AutomationTrigger_PreservesFileContextAndRelatedRunInstance()
    {
        using var directory = new TemporaryDirectory();
        using var repository = new LogRepository(directory.Path);
        using var logs = new AutomationLogService(repository);
        var definition = new AutomationDefinition
        {
            Name = "files",
            Trigger = new HotkeyAutomationTrigger { VirtualKeyCode = 65 },
            Action = new AutomationAction { Name = "sort", JobId = Guid.NewGuid(), ActionType = AutomationActionTarget.Job }
        };
        var dispatcher = new RecordingJobDispatcher();
        var engine = new AutomationEngine(new AutomationRepository(definition), dispatcher,
            [new ManualAutomationTriggerProvider(AutomationTriggerKind.Hotkey)], NullLogger<AutomationEngine>.Instance, logs);
        await engine.StartAsync();
        var trigger = new AutomationTriggerContext(definition.Id, Guid.NewGuid(), DateTimeOffset.UtcNow,
            "Created", new() { ["FileName"] = "invoice.pdf" });
        await engine.TriggerAsync(trigger);
        await repository.FlushAsync();
        var entries = repository.Query(new(Source: LogSource.Automation)).Entries;
        Assert.Contains(entries, entry => entry.Code == LogCodes.Trigger && entry.Parameters.GetValueOrDefault("FileName") == "invoice.pdf");
        Assert.Contains(entries, entry => entry.Code == LogCodes.AutomationDecision && entry.Context.InstanceId.HasValue);
        Assert.All(entries.Where(entry => entry.Code is LogCodes.Trigger or LogCodes.AutomationDecision), entry => Assert.Equal(trigger.TriggerId, entry.Context.TriggerId));
        Assert.Equal(trigger.TriggerId, Assert.Single(dispatcher.StartedJobs).Context!.TriggerId);
        await engine.StopAsync();
    }

    [Fact]
    public async Task RealJobExecution_ProducesSnapshotLifecycleAndStableDispatcherIdentity()
    {
        using var directory = new TemporaryDirectory();
        using var repository = new LogRepository(directory.Path);
        using var logs = new ExecutionLogService(repository);
        var job = new Job { Name = "capture metadata", Steps = [new ShowTextStep { Settings = new() { Text = "hello" } }] };
        var builder = new JobExecutorTestBuilder().WithJobs(job).WithLogs(logs);
        using var executor = await builder.BuildAsync();
        var instance = Guid.NewGuid();
        await executor.ExecuteJob(job.Id, JobStartContext.Manual with { InstanceId = instance });
        await repository.FlushAsync();
        var run = Assert.Single(repository.ReadRuns());
        Assert.Equal(instance, run.InstanceId);
        Assert.Single(run.Steps);
        Assert.Equal(LogOutcome.Successful, run.Outcome);
        var details = await new LogQueryService(repository).RunAsync(run.Id);
        var step = Assert.Single(details!.Steps);
        Assert.Equal(StepLogOutcome.Successful, step.Outcome);
        Assert.NotNull(step.ExecutionId);
        Assert.Contains(step.Events, entry => entry.Code == LogCodes.StepStarted);
        Assert.Contains(step.Events, entry => entry.Code == LogCodes.StepCompleted);
    }
}
