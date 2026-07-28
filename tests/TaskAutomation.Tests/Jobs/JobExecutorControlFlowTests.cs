using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;
using TaskAutomation.WindowsIntegration;
using TaskAutomation.Orchestration;
using TaskAutomation.Logging;

namespace TaskAutomation.Tests.Jobs;

public sealed class JobExecutorControlFlowTests
{
    [Fact]
    public async Task ExecuteJob_UserChoiceConditionComparesStableIdAndSelectsNamedBranch()
    {
        var choice = new UserChoiceStep
        {
            Id = "choice",
            Settings = new()
            {
                Title = "Environment",
                Question = "Choose",
                Options =
                [
                    new() { Id = "dev-id", Label = "Development" },
                    new() { Id = "prod-id", Label = "Production" }
                ]
            }
        };
        var condition = new StepCondition
        {
            SourceStepId = choice.Id,
            PropertyId = "selected_option_id",
            PropertyPath = nameof(UserChoiceResult.SelectedOptionId),
            Operator = ConditionOperator.Equals,
            Comparison = new() { Value = "prod-id" }
        };
        var job = new Job
        {
            Name = "choice branch",
            Steps =
            [
                choice,
                new IfStep { Settings = new() { Conditions = [condition] } },
                Text("production"),
                new ElseStep(),
                Text("development"),
                new EndIfStep()
            ]
        };
        var builder = new JobExecutorTestBuilder().WithJobs(job).WithUserChoice("prod-id");

        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);

        Assert.Equal(["production"], builder.Overlay.TextCalls.Select(call => call.Text));
    }

    [Theory]
    [InlineData(true, "if")]
    [InlineData(false, "else")]
    public async Task ExecuteJob_ChoosesIfOrElseFromCurrentWindowsState(bool muted, string expected)
    {
        var audio = new WindowsStateQueryStep { Id = "audio", Settings = new() { QueryType = "audio.volume" } };
        var job = new Job { Name = "branch", Steps = [audio,
            new IfStep { Settings = Settings(ConditionOperator.IsTrue) }, Text("if"), new ElseStep(), Text("else"), new EndIfStep()] };
        var builder = new JobExecutorTestBuilder().WithJobs(job)
            .WithWindowsStates(new AudioVolumeQueryResult { IsMuted = muted });
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);
        Assert.Equal([expected], builder.Overlay.TextCalls.Select(call => call.Text));
    }

    [Fact]
    public async Task ExecuteJob_FirstMatchingElseIfWinsAndLaterBranchesAreSkipped()
    {
        var audio = new WindowsStateQueryStep { Id = "audio", Settings = new() { QueryType = "audio.volume" } };
        var job = new Job { Name = "elseif", Steps = [audio,
            new IfStep { Settings = Settings(ConditionOperator.IsFalse) }, Text("if"),
            new ElseIfStep { Settings = Settings(ConditionOperator.IsTrue) }, Text("elseif"),
            new ElseStep(), Text("else"), new EndIfStep()] };
        var builder = new JobExecutorTestBuilder().WithJobs(job)
            .WithWindowsStates(new AudioVolumeQueryResult { IsMuted = true });
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);
        Assert.Equal(["elseif"], builder.Overlay.TextCalls.Select(call => call.Text));
    }

    [Fact]
    public async Task ExecuteJob_WindowsSettingRunsOnlyInsideSelectedBranch()
    {
        var audio = new WindowsStateQueryStep
        {
            Id = "audio",
            Settings = new() { QueryType = "audio.volume" }
        };
        var skipped = new WindowsSettingChangeStep
        {
            Settings = new()
            {
                SettingId = "audio.master_volume",
                Parameters = new() { ["value"] = "20" }
            }
        };
        var executed = new WindowsSettingChangeStep
        {
            Settings = new()
            {
                SettingId = "audio.mute",
                Parameters = new() { ["state"] = "on" }
            }
        };
        var job = new Job
        {
            Name = "setting branch",
            Steps =
            [
                audio,
                new IfStep { Settings = Settings(ConditionOperator.IsFalse) },
                skipped,
                new ElseStep(),
                executed,
                new EndIfStep()
            ]
        };
        var builder = new JobExecutorTestBuilder()
            .WithJobs(job)
            .WithWindowsStates(new AudioVolumeQueryResult { IsMuted = true });

        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);

        var change = Assert.Single(builder.WindowsSettings.Changes);
        Assert.Equal("audio.mute", change.SettingId);
        Assert.Equal("on", change.Parameters["state"]);
    }

    [Fact]
    public async Task ExecuteJob_RepeatingConditionUsesFreshResultEachIteration()
    {
        using var cts = new CancellationTokenSource();
        var audio = new WindowsStateQueryStep { Id = "audio", Settings = new() { QueryType = "audio.volume" } };
        var job = new Job { Name = "fresh", Repeating = true, Steps = [audio,
            new IfStep { Settings = Settings(ConditionOperator.IsTrue) }, Text("muted"),
            new ElseStep(), Text("audible"), new EndIfStep()] };
        var builder = new JobExecutorTestBuilder().WithJobs(job).WithWindowsStates(
            new AudioVolumeQueryResult { IsMuted = false }, new AudioVolumeQueryResult { IsMuted = true });
        builder.Overlay.OnShowText = _ => { if (builder.Overlay.TextCalls.Count == 2) cts.Cancel(); };
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id, cts.Token);
        Assert.Equal(["audible", "muted"], builder.Overlay.TextCalls.Select(call => call.Text));
    }

    [Fact]
    public async Task Debugger_StoresStructuredIfEvaluationWithActualAndExpectedValues()
    {
        var audio = new WindowsStateQueryStep
        {
            Id = "audio",
            Settings = new() { QueryType = "audio.volume" }
        };
        var ifStep = new IfStep
        {
            Id = "if",
            Settings = Settings(ConditionOperator.IsTrue)
        };
        var job = new Job
        {
            Name = "debug condition",
            Steps = [audio, ifStep, Text("muted"), new EndIfStep()]
        };
        var builder = new JobExecutorTestBuilder()
            .WithJobs(job)
            .WithWindowsStates(new AudioVolumeQueryResult { IsMuted = true });
        using var executor = await builder.BuildAsync();
        using var cancellation = new JobExecutionCancellation(CancellationToken.None);
        var session = new JobDebugSession(Guid.NewGuid(), job);

        var execution = executor.ExecuteJob(job.Id, JobStartContext.Unknown, cancellation, session);
        session.Continue();
        await execution;

        var evaluation = session.GetSnapshot(ifStep.Id)!.ConditionEvaluation;
        Assert.NotNull(evaluation);
        Assert.Equal(ConditionDebugState.Met, evaluation.State);
        Assert.True(evaluation.BranchExecuted);
        var condition = Assert.Single(evaluation.Conditions);
        Assert.Equal(ConditionDebugState.Met, condition.State);
        Assert.Equal("true", condition.ActualValue);
        Assert.Equal("Festwert true", condition.ExpectedValue);
        Assert.Same(ifStep.Settings.Conditions[0], condition.Definition);
    }

    [Fact]
    public async Task Debugger_DistinguishesSkippedElseIfFromFalseCondition()
    {
        var audio = new WindowsStateQueryStep
        {
            Id = "audio",
            Settings = new() { QueryType = "audio.volume" }
        };
        var ifStep = new IfStep { Settings = Settings(ConditionOperator.IsTrue) };
        var elseIf = new ElseIfStep
        {
            Id = "else-if",
            Settings = Settings(ConditionOperator.IsFalse)
        };
        var job = new Job
        {
            Name = "skipped else-if",
            Steps = [audio, ifStep, Text("if"), elseIf, Text("else-if"), new EndIfStep()]
        };
        var builder = new JobExecutorTestBuilder()
            .WithJobs(job)
            .WithWindowsStates(new AudioVolumeQueryResult { IsMuted = true });
        using var executor = await builder.BuildAsync();
        using var cancellation = new JobExecutionCancellation(CancellationToken.None);
        var session = new JobDebugSession(Guid.NewGuid(), job);

        var execution = executor.ExecuteJob(job.Id, JobStartContext.Unknown, cancellation, session);
        session.Continue();
        await execution;

        var evaluation = session.GetSnapshot(elseIf.Id)!.ConditionEvaluation;
        Assert.NotNull(evaluation);
        Assert.Equal(ConditionDebugState.NotEvaluated, evaluation.State);
        Assert.Equal(ConditionDebugState.NotEvaluated, Assert.Single(evaluation.Conditions).State);
        Assert.False(evaluation.BranchExecuted);
    }

    private static IfConditionSettings Settings(ConditionOperator op) => new() { Conditions = [new StepCondition
        { SourceStepId = "audio", PropertyPath = "IsMuted", Operator = op }] };
    private static ShowTextStep Text(string text) => new() { Settings = new() { Text = text, ClearOnJobEnd = false } };
}
