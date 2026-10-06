using System.Text.Json;
using System.Text.Json.Nodes;
using DesktopAutomation.Application.Logging;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;
using TaskAutomation.Logging;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Logging;

public sealed class StepLoggingCompletenessTests
{
    [Fact]
    public async Task IntentionalActionSkip_HasTypedReasonAndNoRunProblem()
    {
        using var directory = new TemporaryDirectory();
        using var repository = new LogRepository(directory.Path);
        using var logs = new ExecutionLogService(repository);
        using var application = new ApplicationLogService(repository);
        using var serilog = new Serilog.LoggerConfiguration().WriteTo.Sink(application).CreateLogger();
        using var factory = new Serilog.Extensions.Logging.SerilogLoggerFactory(serilog, false);
        var step = new KlickOnPoint3DStep();
        var job = new Job { Steps = [step] };
        var run = logs.BeginJob(job.Id, "empty input");
        logs.InitializeRun(run, job, Guid.NewGuid());
        using var context = LogAmbient.Push(new(run.Id));
        using (var scope = new StepLogScope(logs, run, step, "Main", 1))
        {
            factory.CreateLogger("test").LogInformation("Expected empty detection");
            scope.Complete(result: new KlickOnPoint3DResult { WasExecuted = true, Success = false, SkipReason = "NoInput" });
        }
        logs.Finish(run, LogOutcome.Successful, "Completed");
        await repository.FlushAsync();
        var stored = Assert.Single(repository.ReadRuns());
        Assert.Equal(0, stored.WarningCount);
        Assert.Equal(0, stored.ErrorCount);
        var entries = repository.QueryAll(new(RunId: stored.Id)).Entries;
        var terminal = Assert.Single(entries, entry => entry.Code == LogCodes.StepSkipped);
        Assert.Equal("NoInput", terminal.Parameters["Reason"]);
        Assert.Equal("Log.Summary.NoInput", LogPresentation.Summary(terminal).Key);
        Assert.Equal(LogArea.Execution, Assert.Single(entries, entry => entry.Source == LogSource.Application).Category);
    }

    [Fact]
    public async Task ConditionsAndOutputs_AreStructuredWithoutPersistingPrivateValues()
    {
        const string privateValue = "unregistered-private-text-9842";
        using var directory = new TemporaryDirectory();
        using var repository = new LogRepository(directory.Path);
        using var logs = new ExecutionLogService(repository);
        using var application = new ApplicationLogService(repository);
        using var serilog = new Serilog.LoggerConfiguration().MinimumLevel.Debug().WriteTo.Sink(application).CreateLogger();
        using var factory = new Serilog.Extensions.Logging.SerilogLoggerFactory(serilog, false);
        var variable = new JobVariable { Name = "input", Scope = JobVariableScope.Shared, ValueKind = ResultValueKind.Text, Value = JsonValue.Create(privateValue) };
        var condition = new StepCondition
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = variable.Id.ToString("D"),
            Operator = ConditionOperator.Equals,
            Comparison = new() { Value = privateValue }
        };
        var chosen = new ShowTextStep { Settings = new() { Text = privateValue } };
        var skipped = new ShowTextStep { Settings = new() { Text = "else" } };
        var job = new Job
        {
            Name = "private condition",
            Variables = [variable],
            Steps = [
            new IfStep { Settings = new() { Conditions = [condition] } }, chosen,
            new ElseIfStep { Settings = new() { Conditions = [condition] } }, skipped, new ElseStep(), new EndIfStep()]
        };
        var builder = new JobExecutorTestBuilder().WithJobs(job).WithLogs(logs).WithLogger(factory.CreateLogger<JobExecutor>());
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);
        await repository.FlushAsync();
        var entries = new LogQueryService(repository).ReadAll(new(), default, out _, out _);
        Assert.DoesNotContain(privateValue, JsonSerializer.Serialize(entries, LogRepository.JsonOptions));
        Assert.Equal(privateValue, Assert.Single(builder.Overlay.TextCalls).Text);
        var decision = Assert.Single(entries, entry => entry.Code == LogCodes.StepCompleted && entry.Context.StepId == job.Steps[0].Id);
        Assert.True(decision.BranchDecision!.BranchActive);
        Assert.Equal("Met", Assert.Single(decision.BranchDecision.Conditions).State);
        var alternative = Assert.Single(entries, entry => entry.Code == LogCodes.StepCompleted && entry.Context.StepId == job.Steps[2].Id);
        Assert.Equal("PreviousBranchMatched", alternative.BranchDecision!.Reason);
        Assert.False(alternative.BranchDecision.BranchActive);
        Assert.Contains(entries, entry => entry.Context.StepId == skipped.Id && entry.Code == LogCodes.StepSkipped);
        AssertLifecycle(entries, job.Steps.Where(step => step != skipped).Select(step => step.Id));
        var output = Path.Combine(directory.Path, "support.zip");
        await new LogExportService(repository, new LogQueryService(repository)).ExportAsync(new(), output, "test");
        using var archive = System.IO.Compression.ZipFile.OpenRead(output);
        foreach (var entry in archive.Entries)
        {
            using var reader = new StreamReader(entry.Open());
            Assert.DoesNotContain(privateValue, await reader.ReadToEndAsync());
        }
    }

    [Theory]
    [InlineData("Start")]
    [InlineData("Main")]
    [InlineData("End")]
    public async Task EndJob_HasMeasuredLifecycleInEveryPhaseAndExplicitlySkippedCleanup(string phase)
    {
        using var directory = new TemporaryDirectory();
        using var repository = new LogRepository(directory.Path);
        using var logs = new ExecutionLogService(repository);
        var end = new EndJobStep { Settings = new() { SkipEndSteps = true } };
        var cleanup = new ShowTextStep();
        var job = new Job { Name = "end marker", Steps = [new ShowTextStep()], EndSteps = [cleanup] };
        if (phase == "Start") job.StartSteps = [end];
        else if (phase == "Main") job.Steps = [end];
        else job.EndSteps = [end, cleanup];
        using var executor = await new JobExecutorTestBuilder().WithJobs(job).WithLogs(logs).BuildAsync();
        await executor.ExecuteJob(job.Id);
        var queries = new LogQueryService(repository);
        var events = queries.ReadAll(new(), default, out _, out _);
        AssertLifecycle(events, [end.Id]);
        var terminal = Assert.Single(events, entry => entry.Code == LogCodes.StepCompleted && entry.Context.StepId == end.Id);
        Assert.Equal(phase, terminal.Phase);
        Assert.Equal("EndJob", terminal.Parameters["Reason"]);
        var detail = await queries.RunAsync(Assert.Single(repository.ReadRuns()).Id);
        var remaining = Assert.Single(detail!.Steps, step => step.Step.Id == cleanup.Id);
        Assert.Equal(phase == "End" ? StepLogOutcome.NotExecuted : StepLogOutcome.Skipped, remaining.Outcome);
    }

    [Fact]
    public async Task ContinueJob_SummarizesSuccessfulRepetitionsAndDisabledSteps()
    {
        using var directory = new TemporaryDirectory();
        using var repository = new LogRepository(directory.Path);
        using var logs = new ExecutionLogService(repository);
        using var cancellation = new CancellationTokenSource();
        var body = new ShowTextStep();
        var next = new ContinueJobStep();
        var disabled = new ShowTextStep { IsEnabled = false };
        var job = new Job { Name = "loop", Steps = [disabled, body, next] };
        var builder = new JobExecutorTestBuilder().WithJobs(job).WithLogs(logs);
        builder.Overlay.OnShowText = _ => { if (builder.Overlay.TextCalls.Count == 2) cancellation.Cancel(); };
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id, cancellation.Token);
        var entries = new LogQueryService(repository).ReadAll(new(), default, out _, out _);
        AssertLifecycle(entries, [body.Id, next.Id]);
        var repeated = entries.Where(entry => entry.Code == LogCodes.StepCompleted && entry.Context.StepId == body.Id).ToArray();
        Assert.Equal(1, Assert.Single(repeated).Iteration);
        var summary = Assert.Single(repository.ReadRuns().Single().StepSummaries, item => item.StepId == body.Id);
        Assert.Equal(2, summary.Count);
        Assert.Equal(new LogIterationRange(1, 2), Assert.Single(summary.Iterations));
        Assert.Equal(2, Assert.Single(repository.ReadRuns().Single().StepSummaries, item => item.StepId == disabled.Id).Count);
        Assert.Equal(1, repository.ReadRuns().Single().CompletedIterations);
        Assert.DoesNotContain(entries, entry => entry.Code == LogCodes.IterationCompleted);
        Assert.Equal("NextIteration", Assert.Single(entries, entry => entry.Code == LogCodes.StepCompleted && entry.Context.StepId == next.Id).Parameters["Reason"]);
        Assert.All(entries.Where(entry => entry.Context.StepId == disabled.Id), entry =>
        { Assert.Equal(LogCodes.StepSkipped, entry.Code); Assert.NotNull(entry.Context.StepExecutionId); Assert.Equal("Disabled", entry.Parameters["Reason"]); });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ScriptFailureOrCancellation_HasOneTerminalEventAndNoInputInDiagnostics(bool cancelled)
    {
        const string privateValue = "private-exception-input-8374";
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "script.ps1");
        File.WriteAllText(path, "test");
        using var repository = new LogRepository(Path.Combine(directory.Path, "logs"));
        using var logs = new ExecutionLogService(repository);
        var step = new ScriptExecutionStep { Settings = new() { ScriptPath = path, WaitForExit = true } };
        var cleanup = new ShowTextStep();
        var job = new Job { Name = "failure", Steps = [step], EndSteps = [cleanup] };
        var builder = new JobExecutorTestBuilder().WithJobs(job).WithLogs(logs);
        builder.Scripts.Execute = (_, _, _) => throw (cancelled ? new OperationCanceledException(privateValue) : new InvalidOperationException(privateValue));
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);
        var entries = new LogQueryService(repository).ReadAll(new(), default, out _, out _);
        var terminal = Assert.Single(entries, entry => entry.Context.StepId == step.Id && entry.Code != LogCodes.StepStarted);
        Assert.Equal(cancelled ? LogCodes.StepCancelled : LogCodes.StepFailed, terminal.Code);
        Assert.NotNull(terminal.DurationMs);
        Assert.DoesNotContain(privateValue, JsonSerializer.Serialize(entries));
        AssertLifecycle(entries, [cleanup.Id]);
        Assert.Equal(cancelled ? LogOutcome.Stopped : LogOutcome.Failed, Assert.Single(repository.ReadRuns()).Outcome);
    }

    [Fact]
    public async Task BackgroundScript_PreservesCorrelationAndReportsLateFailureWithoutLoggingOutput()
    {
        const string privateValue = "private-script-output-2387";
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "script.ps1");
        File.WriteAllText(path, "test");
        using var repository = new LogRepository(Path.Combine(directory.Path, "logs"));
        using var logs = new ExecutionLogService(repository);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observed = new TaskCompletionSource<LogEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        repository.EntryWritten += (_, entry) => { if (entry.Code == LogCodes.StepBackgroundFailed) observed.TrySetResult(entry); };
        var step = new ScriptExecutionStep { Settings = new() { ScriptPath = path, WaitForExit = false } };
        var job = new Job { Name = "background", Steps = [step, new ShowTextStep()] };
        var builder = new JobExecutorTestBuilder().WithJobs(job).WithLogs(logs);
        builder.Scripts.ExecuteWithOutput = async (_, _, _, output) =>
        {
            started.TrySetResult();
            await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
            output!(privateValue, false);
            output(privateValue, true);
            throw new InvalidOperationException(privateValue);
        };
        using var executor = await builder.BuildAsync();
        try
        {
            await executor.ExecuteJob(job.Id);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal(LogOutcome.Successful, Assert.Single(repository.ReadRuns()).Outcome);
        }
        finally { release.TrySetResult(); }
        var failure = await observed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var entries = new LogQueryService(repository).ReadAll(new(), default, out _, out _);
        var dispatch = Assert.Single(entries, entry => entry.Code == LogCodes.StepCompleted && entry.Context.StepId == step.Id);
        Assert.Equal("Dispatch", dispatch.Parameters["CompletionScope"]);
        Assert.Equal(dispatch.Context, failure.Context);
        Assert.Equal(dispatch.Phase, failure.Phase);
        Assert.Equal(dispatch.Iteration, failure.Iteration);
        Assert.Equal(2, entries.Count(entry => entry.Code == LogCodes.StepOutput));
        Assert.All(entries.Where(entry => entry.Code == LogCodes.StepOutput), entry =>
        {
            Assert.Equal(dispatch.Context, entry.Context);
            Assert.Equal(dispatch.Phase, entry.Phase);
            Assert.Equal(dispatch.Iteration, entry.Iteration);
            Assert.Equal(privateValue.Length.ToString(System.Globalization.CultureInfo.InvariantCulture), entry.Parameters["CharacterCount"]);
        });
        Assert.DoesNotContain(privateValue, JsonSerializer.Serialize(entries));
        Assert.Equal(LogOutcome.WithErrors, Assert.Single(repository.ReadRuns()).Outcome);
        AssertLifecycle(entries, [step.Id]);
    }

    [Fact]
    public async Task CancelledChoice_EndsStepWithoutStoppingJob()
    {
        using var directory = new TemporaryDirectory();
        using var repository = new LogRepository(directory.Path);
        using var logs = new ExecutionLogService(repository);
        var choice = new UserChoiceStep { Settings = new() { Title = "Choice", Question = "Choose", Options = [new() { Label = "First" }, new() { Label = "Second" }] } };
        var following = new ShowTextStep();
        var job = new Job { Name = "choice", Steps = [choice, following] };
        var builder = new JobExecutorTestBuilder().WithJobs(job).WithLogs(logs);
        builder.UserChoices.CancelChoice = true;
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);
        var entries = new LogQueryService(repository).ReadAll(new(), default, out _, out _);
        Assert.Equal("UserCancelled", Assert.Single(entries, entry => entry.Code == LogCodes.StepCancelled).Parameters["Reason"]);
        Assert.Equal(LogOutcome.Successful, Assert.Single(repository.ReadRuns()).Outcome);
        AssertLifecycle(entries, [choice.Id, following.Id]);
    }

    private static void AssertLifecycle(IReadOnlyList<LogEvent> entries, IEnumerable<string> ids)
    {
        foreach (var id in ids)
        {
            var lifecycles = entries.Where(entry => entry.Context.StepId == id && entry.Source == LogSource.Job
                && entry.Code is LogCodes.StepStarted or LogCodes.StepCompleted or LogCodes.StepFailed or LogCodes.StepCancelled)
                .GroupBy(entry => entry.Context.StepExecutionId).ToArray();
            Assert.NotEmpty(lifecycles);
            foreach (var lifecycle in lifecycles)
            {
                Assert.NotNull(lifecycle.Key);
                Assert.Single(lifecycle, entry => entry.Code == LogCodes.StepStarted);
                var terminal = Assert.Single(lifecycle, entry => entry.Code != LogCodes.StepStarted);
                Assert.True(terminal.DurationMs >= 0);
            }
        }
    }
}

