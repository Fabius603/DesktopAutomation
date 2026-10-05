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
