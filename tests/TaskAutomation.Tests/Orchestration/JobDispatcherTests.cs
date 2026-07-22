using Microsoft.Extensions.Logging.Abstractions;
using TaskAutomation.Jobs;
using TaskAutomation.Logging;
using TaskAutomation.Orchestration;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Orchestration;

public sealed class JobDispatcherTests
{
    [Fact]
    public void StartJob_UnknownDefinition_ReturnsEmptyId()
    {
        var executor = new ControllableJobExecutor([]);
        using var dispatcher = new JobDispatcher(executor, NullLogger<JobDispatcher>.Instance);
        Assert.Equal(Guid.Empty, dispatcher.StartJob(Guid.NewGuid()));
        Assert.Empty(dispatcher.RunningJobInstances);
    }

    [Fact]
    public void StartJob_DefinitionWithoutActiveSteps_RaisesErrorAndDoesNotRegisterInstance()
    {
        var job = new Job { Name = "empty" };
        var executor = new ControllableJobExecutor([job]);
        using var dispatcher = new JobDispatcher(executor, NullLogger<JobDispatcher>.Instance);
        var errors = 0;
        dispatcher.JobErrorOccurred += (_, _) => errors++;
        Assert.Equal(Guid.Empty, dispatcher.StartJob(job.Id));
        Assert.Equal(1, errors);
        Assert.Empty(executor.SnapshotInvocations());
    }

    [Fact]
    public async Task StartJob_AllowsParallelInstancesWithUniqueIds()
    {
        var job = ExecutableJob("parallel");
        var executor = new ControllableJobExecutor([job]);
        using var dispatcher = new JobDispatcher(executor, NullLogger<JobDispatcher>.Instance);
        var first = dispatcher.StartJob(job.Id);
        var second = dispatcher.StartJob(job.Id);
        Assert.NotEqual(Guid.Empty, first);
        Assert.NotEqual(first, second);
        await WaitUntilAsync(() => executor.SnapshotInvocations().Length == 2);
        Assert.Equal(2, dispatcher.RunningJobInstances.Count);
        Assert.Equal([job.Id], dispatcher.RunningJobIds);
        CompleteAll(executor);
        await WaitUntilAsync(() => dispatcher.RunningJobInstances.Count == 0);
    }

    [Fact]
    public async Task CancelJob_StopsOnlySelectedInstance()
    {
        var job = ExecutableJob("cancel-one");
        var executor = new ControllableJobExecutor([job]);
        using var dispatcher = new JobDispatcher(executor, NullLogger<JobDispatcher>.Instance);
        var firstId = dispatcher.StartJob(job.Id);
        var secondId = dispatcher.StartJob(job.Id);
        await WaitUntilAsync(() => executor.SnapshotInvocations().Length == 2);
        dispatcher.CancelJob(firstId);
        await WaitUntilAsync(() => dispatcher.RunningJobInstances.Single(x => x.InstanceId == firstId).State == JobExecutionState.StopRequested);
        Assert.Equal(JobExecutionState.Starting,
            dispatcher.RunningJobInstances.Single(x => x.InstanceId == secondId).State);
        CompleteAll(executor);
        await WaitUntilAsync(() => dispatcher.RunningJobInstances.Count == 0);
    }

    [Fact]
    public async Task CancelJobsByDefinition_StopsEveryInstanceOfOnlyThatDefinition()
    {
        var first = ExecutableJob("first");
        var second = ExecutableJob("second");
        var executor = new ControllableJobExecutor([first, second]);
        using var dispatcher = new JobDispatcher(executor, NullLogger<JobDispatcher>.Instance);
        dispatcher.StartJob(first.Id);
        dispatcher.StartJob(first.Id);
        dispatcher.StartJob(second.Id);
        await WaitUntilAsync(() => executor.SnapshotInvocations().Length == 3);
        dispatcher.CancelJobsByDefinition(first.Id);
        await WaitUntilAsync(() => dispatcher.RunningJobInstances.Count(x =>
            x.JobId == first.Id && x.State == JobExecutionState.StopRequested) == 2);
        Assert.Equal(JobExecutionState.Starting, dispatcher.RunningJobInstances.Single(x => x.JobId == second.Id).State);
        CompleteAll(executor);
        await WaitUntilAsync(() => dispatcher.RunningJobInstances.Count == 0);
    }

    [Fact]
    public async Task ForceStopJob_CancelsBothTokens()
    {
        var job = ExecutableJob("force");
        var executor = new ControllableJobExecutor([job]);
        using var dispatcher = new JobDispatcher(executor, NullLogger<JobDispatcher>.Instance);
        var instanceId = dispatcher.StartJob(job.Id);
        await WaitUntilAsync(() => executor.SnapshotInvocations().Length == 1);
        dispatcher.ForceStopJob(instanceId);
        await WaitUntilAsync(() => executor.SnapshotInvocations()[0].Cancellation.IsForceStopRequested);
        var cancellation = executor.SnapshotInvocations()[0].Cancellation;
        Assert.True(cancellation.ExecutionToken.IsCancellationRequested);
        Assert.True(cancellation.EndPhaseToken.IsCancellationRequested);
        CompleteAll(executor);
        await WaitUntilAsync(() => dispatcher.RunningJobInstances.Count == 0);
    }

    [Fact]
    public async Task StartJob_ForwardsStartContextAndRemovesCompletedInstance()
    {
        var job = ExecutableJob("context");
        var executor = new ControllableJobExecutor([job]);
        using var dispatcher = new JobDispatcher(executor, NullLogger<JobDispatcher>.Instance);
        var context = new JobStartContext(JobStartSource.Automation, "automation", Guid.NewGuid());
        dispatcher.StartJob(job.Id, context);
        await WaitUntilAsync(() => executor.SnapshotInvocations().Length == 1);
        Assert.Equal(context, executor.SnapshotInvocations()[0].Context);
        CompleteAll(executor);
        await WaitUntilAsync(() => dispatcher.RunningJobInstances.Count == 0);
    }

    [Fact]
    public void ExecutorErrors_AreForwardedByDispatcher()
    {
        var executor = new ControllableJobExecutor([]);
        using var dispatcher = new JobDispatcher(executor, NullLogger<JobDispatcher>.Instance);
        JobErrorEventArgs? forwarded = null;
        dispatcher.JobErrorOccurred += (_, error) => forwarded = error;
        var original = new JobErrorEventArgs("job", new InvalidOperationException("boom"));
        executor.RaiseJobError(original);
        Assert.Same(original, forwarded);
    }

    private static Job ExecutableJob(string name) => new() { Name = name, Steps = [new TimeoutStep()] };
    private static void CompleteAll(ControllableJobExecutor executor)
    {
        foreach (var invocation in executor.SnapshotInvocations()) invocation.Completion.TrySetResult();
    }
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition())
        {
            timeout.Token.ThrowIfCancellationRequested();
            await Task.Yield();
        }
    }
}
