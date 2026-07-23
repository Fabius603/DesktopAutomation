using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;
using TaskAutomation.WindowsIntegration;
using TaskAutomation.Orchestration;
using TaskAutomation.Logging;

namespace TaskAutomation.Tests.Jobs;

public sealed class JobExecutorControlFlowTests
{
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
