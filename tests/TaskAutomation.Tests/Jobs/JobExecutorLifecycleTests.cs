using TaskAutomation.Jobs;
using TaskAutomation.Orchestration;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Jobs;

public sealed class JobExecutorLifecycleTests
{
    [Fact]
    public async Task ExecuteJob_RunsStartMainAndEndPhasesInOrder()
    {
        var job = Job("phases", start: [Text("start")], run: [Text("run")], end: [Text("end")]);
        var builder = new JobExecutorTestBuilder().WithJobs(job);
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);
        Assert.Equal(["start", "run", "end"], builder.Overlay.TextCalls.Select(call => call.Text));
        Assert.Null(executor.CurrentJob);
        var completion = Assert.Single(builder.Logs.Completions);
        Assert.True(completion.Success);
        Assert.False(completion.Cancelled);
    }

    [Fact]
    public async Task ExecuteJob_DisabledStepsAreSkippedInEveryPhase()
    {
        var job = Job("disabled", start: [Text("start", false)],
            run: [Text("run"), Text("disabled", false)], end: [Text("end", false)]);
        var builder = new JobExecutorTestBuilder().WithJobs(job);
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);
        Assert.Equal(["run"], builder.Overlay.TextCalls.Select(call => call.Text));
    }

    [Fact]
    public async Task ExecuteJob_EndJobStopsRemainingMainStepsButRunsEndPhase()
    {
        var job = Job("end", run: [Text("before"), new EndJobStep(), Text("after")], end: [Text("cleanup")]);
        var builder = new JobExecutorTestBuilder().WithJobs(job);
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);
        Assert.Equal(["before", "cleanup"], builder.Overlay.TextCalls.Select(call => call.Text));
    }

    [Fact]
    public async Task ExecuteJob_EndJobCanSkipConfiguredEndPhase()
    {
        var job = Job("end", run: [Text("before"), new EndJobStep { Settings = new() { SkipEndSteps = true } }],
            end: [Text("cleanup")]);
        var builder = new JobExecutorTestBuilder().WithJobs(job);
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);
        Assert.Equal(["before"], builder.Overlay.TextCalls.Select(call => call.Text));
    }

    [Fact]
    public async Task ExecuteJob_ContinueStartsFreshIteration()
    {
        using var cts = new CancellationTokenSource();
        var job = Job("continue", run: [Text("run"), new ContinueJobStep()], end: [Text("end")]);
        var builder = new JobExecutorTestBuilder().WithJobs(job);
        builder.Overlay.OnShowText = call =>
        {
            if (call.Text == "run" && builder.Overlay.TextCalls.Count(item => item.Text == "run") == 3) cts.Cancel();
        };
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id, cts.Token);
        Assert.Equal(3, builder.Overlay.TextCalls.Count(call => call.Text == "run"));
        Assert.True(Assert.Single(builder.Logs.Completions).Cancelled);
    }

    [Fact]
    public async Task ExecuteJob_RepeatingJobRunsUntilCancellation()
    {
        using var cts = new CancellationTokenSource();
        var job = Job("repeat", run: [Text("run")]);
        job.Repeating = true;
        var builder = new JobExecutorTestBuilder().WithJobs(job);
        builder.Overlay.OnShowText = call =>
        {
            if (builder.Overlay.TextCalls.Count == 4) cts.Cancel();
        };
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id, cts.Token);
        Assert.Equal(4, builder.Overlay.TextCalls.Count);
        Assert.True(Assert.Single(builder.Logs.Completions).Cancelled);
    }

    [Fact]
    public async Task ExecuteJob_UnknownIdRaisesJobErrorWithoutStartingSession()
    {
        var builder = new JobExecutorTestBuilder();
        using var executor = await builder.BuildAsync();
        var errors = new List<JobErrorEventArgs>();
        executor.JobErrorOccurred += (_, error) => errors.Add(error);
        await executor.ExecuteJob(Guid.NewGuid());
        Assert.Single(errors);
        Assert.Empty(builder.Logs.MutableSessions);
    }

    [Fact]
    public async Task ExecuteJob_JobWithoutActiveStepsRaisesJobError()
    {
        var job = Job("empty", run: [Text("disabled", false)]);
        var builder = new JobExecutorTestBuilder().WithJobs(job);
        using var executor = await builder.BuildAsync();
        var errors = 0;
        executor.JobErrorOccurred += (_, _) => errors++;
        await executor.ExecuteJob(job.Id);
        Assert.Equal(1, errors);
        Assert.Empty(builder.Overlay.TextCalls);
    }

    private static Job Job(string name, JobStep[]? start = null, JobStep[]? run = null, JobStep[]? end = null) => new()
        { Name = name, StartSteps = start?.ToList() ?? [], Steps = run?.ToList() ?? [], EndSteps = end?.ToList() ?? [] };
    private static ShowTextStep Text(string text, bool enabled = true) => new()
        { IsEnabled = enabled, Settings = new() { Text = text, ClearOnJobEnd = false } };
}
