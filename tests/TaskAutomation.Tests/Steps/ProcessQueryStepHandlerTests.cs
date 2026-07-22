using System.Diagnostics;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class ProcessQueryStepHandlerTests
{
    [Fact]
    public async Task ActiveProcess_CurrentProcessNameReturnsRunningReference()
    {
        using var current = Process.GetCurrentProcess();
        var context = new PipelineContextStub();
        var result = Assert.IsType<ActiveProcessResult>(await new ActiveProcessStepHandler().ExecuteAsync(
            new ActiveProcessStep { Id = "active", Settings = new() { Target = new() { ProcessName = current.ProcessName } } },
            context, default));
        Assert.True(result.IsRunning);
        Assert.True(result.MatchCount >= 1);
        Assert.NotNull(result.Process);
        Assert.True(result.Process.ProcessId > 0);
        Assert.Same(result, context.Results.GetRaw("active"));
    }

    [Fact]
    public async Task ActiveProcess_UnconfiguredTargetReturnsNeutralExecutedResult()
    {
        var result = Assert.IsType<ActiveProcessResult>(await new ActiveProcessStepHandler().ExecuteAsync(
            new ActiveProcessStep(), new PipelineContextStub(), default));
        Assert.True(result.WasExecuted);
        Assert.False(result.IsRunning);
        Assert.Equal(0, result.MatchCount);
    }

    [Fact]
    public async Task GetProcess_CurrentProcessWithExeSuffixIsFound()
    {
        using var current = Process.GetCurrentProcess();
        var result = Assert.IsType<GetProcessResult>(await new GetProcessStepHandler().ExecuteAsync(
            new GetProcessStep { Settings = new() { Query = new() { ProcessName = current.ProcessName + ".exe" } } },
            new PipelineContextStub(), default));
        Assert.True(result.Found);
        Assert.NotNull(result.Process);
        Assert.Equal(current.ProcessName, result.Process.ProcessName, ignoreCase: true);
    }

    [Fact]
    public async Task GetProcess_ImpossibleNameReturnsNotFound()
    {
        var result = Assert.IsType<GetProcessResult>(await new GetProcessStepHandler().ExecuteAsync(
            new GetProcessStep { Settings = new() { Query = new() { ProcessName = "DesktopAutomation_NoSuchProcess_" + Guid.NewGuid() } } },
            new PipelineContextStub(), default));
        Assert.False(result.Found);
        Assert.Null(result.Process);
    }

    [Fact]
    public async Task ActiveProcess_ValidBoundReferenceTakesPriorityOverFallbackName()
    {
        using var current = Process.GetCurrentProcess();
        var reference = ProcessTargetResolver.CreateReference(current);
        var context = new PipelineContextStub();
        context.Results.Set<GetProcessStep>(new GetProcessResult { WasExecuted = true, Found = true, Process = reference }, "source");
        var result = Assert.IsType<ActiveProcessResult>(await new ActiveProcessStepHandler().ExecuteAsync(
            new ActiveProcessStep { Settings = new() { Target = new() { ProcessName = "impossible",
                ProcessSource = new() { SourceStepId = "source", PropertyPath = "Process" } } } }, context, default));
        Assert.True(result.IsRunning);
        Assert.Equal(current.Id, result.Process!.ProcessId);
    }

    [Fact]
    public async Task ActiveProcess_StaleBoundReferenceIsRejected()
    {
        using var current = Process.GetCurrentProcess();
        var reference = ProcessTargetResolver.CreateReference(current) with { StartTimeUtc = DateTime.UtcNow.AddYears(-1) };
        var context = new PipelineContextStub();
        context.Results.Set<GetProcessStep>(new GetProcessResult { WasExecuted = true, Found = true, Process = reference }, "source");
        var result = Assert.IsType<ActiveProcessResult>(await new ActiveProcessStepHandler().ExecuteAsync(
            new ActiveProcessStep { Settings = new() { Target = new() { ProcessSource = new()
                { SourceStepId = "source", PropertyPath = "Process" } } } }, context, default));
        Assert.False(result.IsRunning);
    }

    [Fact]
    public async Task ProcessHandlers_HonorPreCancelledToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new GetProcessStepHandler().ExecuteAsync(
            new GetProcessStep(), new PipelineContextStub(), cts.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new ActiveProcessStepHandler().ExecuteAsync(
            new ActiveProcessStep(), new PipelineContextStub(), cts.Token));
    }
}
