using System.IO.Compression;
using System.Text.Json;
using DesktopAutomation.Application.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TaskAutomation.Automations;
using TaskAutomation.Jobs;
using TaskAutomation.Logging;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Logging;

public sealed class LogUiBackendTests
{
    private static LogRun SaveRun(LogRepository repository, LogSource source, string name, LogOutcome outcome, Guid? instance = null)
    {
        var id = Guid.NewGuid();
        return repository.SaveRun(new LogRun
        {
            Id = id,
            InstanceId = instance ?? id,
            SourceId = Guid.NewGuid(),
            Source = source,
            Name = name,
            Outcome = outcome,
            StartedAt = DateTimeOffset.UtcNow,
            EndedAt = DateTimeOffset.UtcNow
        });
    }
    [Fact]
    public async Task OverviewCountsAndExport_UseIdenticalRunFiltersBeyondFirstPage()
    {
        using var directory = new TemporaryDirectory();
        using var repository = new LogRepository(directory.Path);
        for (var index = 0; index < 25; index++)
        {
            var run = SaveRun(repository, LogSource.Job, "selected", LogOutcome.Failed);
            repository.Append(new LogEvent { Context = new(run.Id), Message = "selected event" });
        }
        var excluded = SaveRun(repository, LogSource.Makro, "selected", LogOutcome.Failed);
        repository.Append(new LogEvent { Context = new(excluded.Id), Message = "must not export" });
        SaveRun(repository, LogSource.Job, "other", LogOutcome.Successful);
        var queries = new LogQueryService(repository);
        var query = new RunQuery(Source: LogSource.Job, Search: "selected", OnlyProblems: true, Outcome: LogOutcome.Failed, PageSize: 2);
        var overview = await queries.OverviewAsync(query);
        Assert.Equal(25, overview.ProblemRunCount);
        Assert.Equal(25, overview.TotalRuns);
        Assert.Equal(20, overview.ProblemRuns.Count);
        var page = await queries.RunsAsync(query);
        Assert.Equal(25, page.Total);
        Assert.Equal(2, page.Runs.Count);
        query = query with { SnapshotSequence = page.SnapshotSequence };
        SaveRun(repository, LogSource.Job, "selected", LogOutcome.Failed);
        var path = Path.Combine(directory.Path, "runs.zip");
        await new LogExportService(repository, queries).ExportRunsAsync(query, path, "test");
        using var archive = ZipFile.OpenRead(path);
        using var reader = new StreamReader(archive.GetEntry("runs.json")!.Open());
        var runs = JsonSerializer.Deserialize<LogRun[]>(await reader.ReadToEndAsync(), LogRepository.JsonOptions)!;
        Assert.Equal(25, runs.Length);
        Assert.All(runs, run => Assert.Equal(LogSource.Job, run.Source));
        using var events = new StreamReader(archive.GetEntry("events.jsonl")!.Open());
        Assert.DoesNotContain("must not export", await events.ReadToEndAsync());
    }
    [Fact]
    public async Task AutomationSnapshotAndLinks_SurviveDefinitionChangesAndRequireRealRunEvidence()
    {
        using var directory = new TemporaryDirectory();
        using var repository = new LogRepository(directory.Path);
        using var logs = new AutomationLogService(repository);
        var definition = new AutomationDefinition
        {
            Name = "watch",
            Trigger = new FileSystemAutomationTrigger { DirectoryPath = "C:/original" },
            Action = new() { JobId = Guid.NewGuid(), Name = "target" }
        };
        var dispatcher = new RecordingJobDispatcher();
        var engine = new AutomationEngine(new AutomationRepository(definition), dispatcher,
            [new ManualAutomationTriggerProvider(AutomationTriggerKind.FileSystemEvent)], NullLogger<AutomationEngine>.Instance, logs);
        await engine.StartAsync();
        var trigger = new AutomationTriggerContext(definition.Id, Guid.NewGuid(), DateTimeOffset.UtcNow, "Created", new() { ["FileName"] = "invoice.pdf" });
        await engine.TriggerAsync(trigger);
        var start = Assert.Single(dispatcher.StartedJobs);
        Assert.Equal("Created", start.Context!.Trigger!.EventKind);
        Assert.Equal(Path.GetFullPath("C:/original"), start.Context.Trigger.WatchedDirectory);
        definition.Trigger = new FileSystemAutomationTrigger { DirectoryPath = "C:/changed" };
        var queries = new LogQueryService(repository);
        var first = Assert.Single((await queries.AutomationHistoryAsync(new(Search: "invoice.pdf"))).Items);
        Assert.Equal("Log.Automation.Requested", first.TitleKey);
        Assert.Equal(Path.GetFullPath("C:/original"), first.Trigger!.WatchedDirectory);
        Assert.Equal("RunUnavailable", Assert.Single(first.StartedRuns).UnavailableReason);
        var instance = Assert.Single(first.StartedRuns).InstanceId;
        using var execution = new ExecutionLogService(repository);
        var session = execution.BeginJob(definition.Action.JobId!.Value, "target", start.Context with { InstanceId = instance });
        var run = Assert.Single(repository.ReadRuns());
        Assert.Equal("Created", run.Trigger!.EventKind);
        Assert.Equal(Path.GetFullPath("C:/original"), run.Trigger.WatchedDirectory);
        var confirmed = Assert.Single((await queries.AutomationHistoryAsync(new())).Items);
        Assert.Equal("Log.Automation.Started", confirmed.TitleKey);
        Assert.Equal(run.Id, Assert.Single(confirmed.StartedRuns).RunId);
        var choice = Assert.Single(await queries.AutomationChoicesAsync([]));
        Assert.False(choice.DefinitionExists);
        Assert.Equal(Path.GetFullPath("C:/original"), choice.WatchedDirectory);
        await engine.StopAsync();
    }
    [Fact]
    public async Task AlreadyRunning_ResolvesEveryRelatedInstanceWithoutInventingAnotherStart()
    {
        using var directory = new TemporaryDirectory();
        using var repository = new LogRepository(directory.Path);
        var automation = Guid.NewGuid();
        var trigger = Guid.NewGuid();
        var one = SaveRun(repository, LogSource.Job, "first", LogOutcome.Running);
        var two = SaveRun(repository, LogSource.Job, "second", LogOutcome.Running);
        repository.Append(new LogEvent { Source = LogSource.Automation, SourceId = automation, Code = LogCodes.Trigger, Context = new(TriggerId: trigger, AutomationId: automation) });
        repository.Append(new LogEvent
        {
            Source = LogSource.Automation,
            SourceId = automation,
            Code = LogCodes.AutomationDecision,
            Context = new(TriggerId: trigger, AutomationId: automation),
            Parameters = new() { ["Reason"] = "AlreadyRunning" },
            RelatedInstanceIds = [one.InstanceId, two.InstanceId]
        });
        var row = Assert.Single((await new LogQueryService(repository).AutomationHistoryAsync(new())).Items);
        Assert.Empty(row.StartedRuns);
        Assert.Equal(2, row.RelatedRuns.Count);
        Assert.All(row.RelatedRuns, link => Assert.NotNull(link.RunId));
        Assert.Equal("Log.Automation.AlreadyRunning", row.TitleKey);
    }
    [Fact]
    public async Task StoppingMainFailure_PreventsFollowingMainWorkButContinuedCleanupFailureDoesNot()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "script.ps1");
        File.WriteAllText(path, "test");
        using var repository = new LogRepository(Path.Combine(directory.Path, "logs"));
        using var logs = new ExecutionLogService(repository);
        var failure = new ScriptExecutionStep { Settings = new() { ScriptPath = path, WaitForExit = true } };
        var prevented = new ShowTextStep();
        var cleanupFailure = new ScriptExecutionStep { Settings = new() { ScriptPath = path, WaitForExit = true } };
        var cleanup = new ShowTextStep();
        var job = new Job { Name = "causal evidence", Steps = [failure, prevented], EndSteps = [cleanupFailure, cleanup] };
        var builder = new JobExecutorTestBuilder().WithJobs(job).WithLogs(logs);
        builder.Scripts.Execute = (_, _, _) => throw new InvalidOperationException("private failure");
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);
        var detail = (await new LogQueryService(repository).RunAsync(Assert.Single(repository.ReadRuns()).Id))!;
        Assert.Equal(failure.Id, Assert.Single(detail.Steps, item => item.Step.Id == prevented.Id).Cause!.StepId);
        Assert.Equal(StepLogOutcome.Successful, Assert.Single(detail.Steps, item => item.Step.Id == cleanup.Id).Outcome);
        Assert.Null(Assert.Single(detail.Steps, item => item.Step.Id == cleanup.Id).Cause);
        var failureInCleanup = Assert.Single(detail.Steps, item => item.Step.Id == cleanupFailure.Id);
        Assert.Null(Assert.Single(failureInCleanup.Events, entry => entry.Code == LogCodes.StepFailed).FlowEffect);
        Assert.Equal(1, detail.StepCounts![StepLogOutcome.NotExecuted]);
    }
    [Fact]
    public async Task ErrorDetails_JoinApprovedPathsAndExposeSemanticAreaWithoutLeakingSecrets()
    {
        using var directory = new TemporaryDirectory();
        using var repository = new LogRepository(directory.Path);
        repository.Privacy.RegisterSecrets(["private-directory"]);
        var run = SaveRun(repository, LogSource.Job, "job", LogOutcome.Failed);
        var execution = Guid.NewGuid();
        var context = new LogContext(run.Id, run.InstanceId, StepId: "step", StepExecutionId: execution);
        repository.Append(new LogEvent { Code = LogCodes.StepPaths, Context = context, Paths = [new("Source", "C:/private-directory/file.txt"), new("Target", "C:/safe/file.txt")] });
        var error = repository.Append(new LogEvent
        {
            Source = LogSource.Application,
            Context = context,
            DiagnosticCode = LogCodes.FileUnavailable,
            Level = ExecutionLogLevel.Error,
            Details = "technical evidence"
        });
        await repository.FlushAsync();
        var queries = new LogQueryService(repository);
        var details = (await queries.EventDetailsAsync(error.Id))!;
        Assert.Equal(LogArea.FileAccess, details.Display.Category);
        Assert.Equal("Log.Title.LOG-FILE-004", details.Display.Title.Key);
        Assert.DoesNotContain("private-directory", JsonSerializer.Serialize(details));
        Assert.Single(details.Display.Diagnostic!.Actions, action => action.Kind == "CheckPath");
        Assert.Equal(error.Id, Assert.Single((await queries.SearchAsync(new(Category: LogArea.FileAccess))).Entries).Id);
    }
}
