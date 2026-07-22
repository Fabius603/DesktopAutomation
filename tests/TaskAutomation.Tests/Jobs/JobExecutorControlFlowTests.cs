using TaskAutomation.Jobs;
using TaskAutomation.Tests.TestDoubles;
using TaskAutomation.WindowsIntegration;

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
            .WithWindowsStates(new WindowsStateSnapshot { IsMuted = muted });
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
            .WithWindowsStates(new WindowsStateSnapshot { IsMuted = true });
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
            new WindowsStateSnapshot { IsMuted = false }, new WindowsStateSnapshot { IsMuted = true });
        builder.Overlay.OnShowText = _ => { if (builder.Overlay.TextCalls.Count == 2) cts.Cancel(); };
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id, cts.Token);
        Assert.Equal(["audible", "muted"], builder.Overlay.TextCalls.Select(call => call.Text));
    }

    private static IfConditionSettings Settings(ConditionOperator op) => new() { Conditions = [new StepCondition
        { SourceStepId = "audio", PropertyPath = "IsMuted", Operator = op }] };
    private static ShowTextStep Text(string text) => new() { Settings = new() { Text = text, ClearOnJobEnd = false } };
}
