using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class JobExecutionStepHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_ResolvesByNameCaseInsensitivelyAndUsesFallbackExecutor()
    {
        var parent = new Job { Name = "parent" };
        var child = new Job { Name = "Child" };
        Guid called = default;
        var context = Context(parent, child).WithExecution((id, _) => { called = id; return Task.CompletedTask; });
        var result = Assert.IsType<JobExecutionResult>(await new JobExecutionStepHandler().ExecuteAsync(
            new JobExecutionStep { Settings = new() { JobName = "child", WaitForCompletion = true } }, context, default));
        Assert.Equal(child.Id, called);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_WaitingPrefersDispatcherAsync()
    {
        var parent = new Job { Name = "parent" };
        var child = new Job { Name = "child" };
        var fallbackCalls = 0;
        Guid dispatcherId = default;
        var context = new PipelineContextStub { CurrentJob = parent, AllJobs = Jobs(parent, child),
            ExecuteJob = (_, _) => { fallbackCalls++; return Task.CompletedTask; },
            StartJobViaDispatcherAsync = (id, _) => { dispatcherId = id; return Task.CompletedTask; } };
        await new JobExecutionStepHandler().ExecuteAsync(new JobExecutionStep
            { Settings = new() { JobId = child.Id, WaitForCompletion = true } }, context, default);
        Assert.Equal(child.Id, dispatcherId);
        Assert.Equal(0, fallbackCalls);
    }

    [Fact]
    public async Task ExecuteAsync_FireAndForgetDispatcherStoresNonEmptyInstanceId()
    {
        var parent = new Job { Name = "parent" };
        var child = new Job { Name = "child" };
        var instanceId = Guid.NewGuid();
        var context = new PipelineContextStub { CurrentJob = parent, AllJobs = Jobs(parent, child),
            StartJobViaDispatcher = id => id == child.Id ? instanceId : Guid.Empty };
        var result = Assert.IsType<JobExecutionResult>(await new JobExecutionStepHandler().ExecuteAsync(
            new JobExecutionStep { Settings = new() { JobId = child.Id, WaitForCompletion = false } }, context, default));
        Assert.Equal([instanceId], context.ChildJobInstanceIds);
        Assert.True(result.Success);
    }

    [Fact]
    public async Task ExecuteAsync_FireAndForgetEmptyInstanceIdIsNotTracked()
    {
        var parent = new Job { Name = "parent" };
        var child = new Job { Name = "child" };
        var context = new PipelineContextStub { CurrentJob = parent, AllJobs = Jobs(parent, child),
            StartJobViaDispatcher = _ => Guid.Empty };
        await new JobExecutionStepHandler().ExecuteAsync(new JobExecutionStep
            { Settings = new() { JobId = child.Id, WaitForCompletion = false } }, context, default);
        Assert.Empty(context.ChildJobInstanceIds);
    }

    [Theory]
    [InlineData("missing-id")]
    [InlineData("missing-name")]
    [InlineData("unconfigured")]
    [InlineData("self")]
    public async Task ExecuteAsync_InvalidTargetThrows(string scenario)
    {
        var parent = new Job { Name = "parent" };
        var settings = scenario switch
        {
            "missing-id" => new JobExecutionStepSettings { JobId = Guid.NewGuid() },
            "missing-name" => new JobExecutionStepSettings { JobName = "missing" },
            "self" => new JobExecutionStepSettings { JobId = parent.Id },
            _ => new JobExecutionStepSettings()
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => new JobExecutionStepHandler().ExecuteAsync(
            new JobExecutionStep { Settings = settings }, new PipelineContextStub { CurrentJob = parent, AllJobs = Jobs(parent) }, default));
    }

    private static PipelineContextStub Context(Job parent, Job child) => new() { CurrentJob = parent, AllJobs = Jobs(parent, child) };
    private static Dictionary<string, Job> Jobs(params Job[] jobs) => jobs.ToDictionary(job => job.Id.ToString());
}

internal static class JobExecutionContextExtensions
{
    public static PipelineContextStub WithExecution(this PipelineContextStub original, Func<Guid, CancellationToken, Task> execute) => new()
    { CurrentJob = original.CurrentJob, AllJobs = original.AllJobs, ExecuteJob = execute };
}
