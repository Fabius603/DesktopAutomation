using TaskAutomation.Jobs;
using TaskAutomation.Orchestration;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;
using TaskAutomation.Logging;
using System.Text.Json.Nodes;

namespace TaskAutomation.Tests.Jobs;

public sealed class JobExecutionOwnershipTests
{
    private static ShowTextStep Text(string text) => new() { Settings = new() { Text = text } };

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ParallelScript_MainCanContinue_ButEndPhaseWaitsForCompletion(bool endPhaseScript)
    {
        var path = Path.GetTempFileName();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var script = new ScriptExecutionStep { Settings = new() { ScriptPath = path, WaitForExit = false } };
            var job = new Job
            {
                Name = "owned",
                Steps = endPhaseScript ? [Text("main")] : [script, Text("main")],
                EndSteps = endPhaseScript ? [script, Text("end")] : [Text("end")]
            };
            var builder = new JobExecutorTestBuilder().WithJobs(job);
            builder.Scripts.Execute = async (_, _, token) => { started.TrySetResult(); await release.Task; token.ThrowIfCancellationRequested(); };
            using var executor = await builder.BuildAsync();
            var run = executor.ExecuteJob(job.Id);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(run.IsCompleted);
            Assert.Contains(builder.Overlay.TextCalls, call => call.Text == "main");
            if (!endPhaseScript) Assert.DoesNotContain(builder.Overlay.TextCalls, call => call.Text == "end");
            Assert.Empty(builder.Logs.Completions);
            release.SetResult();
            await run.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(Assert.Single(builder.Logs.Completions).Success);
            Assert.Contains(builder.Overlay.TextCalls, call => call.Text == "end");
        }
        finally { release.TrySetResult(); File.Delete(path); }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FailedChild_FailsParent_AndStillExecutesCleanup(bool wait)
    {
        var child = new Job { Name = "child", Steps = [new JobExecutionStep { Settings = new() { JobId = Guid.NewGuid() } }] };
        var parent = new Job { Name = "parent", Steps = [new JobExecutionStep { Settings = new() { JobId = child.Id, WaitForCompletion = wait } }], EndSteps = [Text("cleanup")] };
        var builder = new JobExecutorTestBuilder().WithJobs(parent, child);
        using var executor = await builder.BuildAsync();
        using var cancellation = new JobExecutionCancellation();
        var outcome = await executor.ExecuteDefinitionAsync(JobStepsSnapshotService.CaptureExecution(parent), JobStartContext.Manual, cancellation);
        Assert.Equal(JobExecutionState.Failed, outcome.State);
        Assert.Contains(builder.Overlay.TextCalls, call => call.Text == "cleanup");
        Assert.All(builder.Logs.Completions, completion => Assert.False(completion.Success));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Stop_PropagatesToOwnedScript_AndCleanupRunsOnlyAfterItsExit(bool force)
    {
        var path = Path.GetTempFileName();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            var job = new Job { Name = "stop", Steps = [new ScriptExecutionStep { Settings = new() { ScriptPath = path, WaitForExit = false } }], EndSteps = [Text("cleanup")] };
            var builder = new JobExecutorTestBuilder().WithJobs(job);
            builder.Scripts.Execute = async (_, _, token) =>
            {
                using var registration = token.Register(() => stopped.TrySetResult());
                started.TrySetResult();
                await release.Task;
                token.ThrowIfCancellationRequested();
            };
            using var executor = await builder.BuildAsync();
            using var cancellation = new JobExecutionCancellation();
            var run = executor.ExecuteJob(job.Id, JobStartContext.Manual, cancellation);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            if (force) cancellation.ForceStop(); else cancellation.RequestStop();
            await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.False(run.IsCompleted);
            Assert.Empty(builder.Overlay.TextCalls);
            release.SetResult();
            await run.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(JobExecutionState.Cancelled, cancellation.State);
            if (force) Assert.Empty(builder.Overlay.TextCalls);
            else Assert.Equal("cleanup", Assert.Single(builder.Overlay.TextCalls).Text);
        }
        finally { release.TrySetResult(); File.Delete(path); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DispatcherParent_RemainsRegisteredUntilNestedChildHasExited(bool force)
    {
        var path = Path.GetTempFileName();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        JobDispatcher? dispatcher = null;
        try
        {
            var child = new Job { Name = "nested", Steps = [new ScriptExecutionStep { Settings = new() { ScriptPath = path, WaitForExit = false } }], EndSteps = [Text("child cleanup")] };
            var parent = new Job { Name = "parent", Steps = [new JobExecutionStep { Settings = new() { JobId = child.Id, WaitForCompletion = false } }], EndSteps = [Text("parent cleanup")] };
            var builder = new JobExecutorTestBuilder().WithJobs(parent, child)
                .WithLauncher(new Lazy<IJobLauncher>(() => dispatcher!));
            builder.Scripts.Execute = async (_, _, token) =>
            {
                using var registration = token.Register(() => stopped.TrySetResult());
                started.TrySetResult(); await release.Task; token.ThrowIfCancellationRequested();
            };
            using var executor = await builder.BuildAsync();
            dispatcher = new JobDispatcher(executor, Microsoft.Extensions.Logging.Abstractions.NullLogger<JobDispatcher>.Instance);
            var id = dispatcher.StartJob(parent.Id);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(2, dispatcher.RunningJobInstances.Count);
            if (force) dispatcher.ForceStopJob(id); else dispatcher.CancelJob(id);
            await stopped.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(2, dispatcher.RunningJobInstances.Count);
            Assert.Empty(builder.Logs.Completions);
            release.SetResult();
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (dispatcher.RunningJobInstances.Count != 0 && DateTime.UtcNow < deadline) await Task.Delay(5);
            Assert.Empty(dispatcher.RunningJobInstances);
            Assert.Equal(2, builder.Logs.Completions.Count);
            if (force) Assert.Empty(builder.Overlay.TextCalls);
            else Assert.Equal(["child cleanup", "parent cleanup"], builder.Overlay.TextCalls.Select(call => call.Text));
        }
        finally { release.TrySetResult(); dispatcher?.Dispose(); File.Delete(path); }
    }

    [Fact]
    public async Task MainFailure_StopsParallelWork_ButKeepsFailedOutcome()
    {
        var path = Path.GetTempFileName();
        try
        {
            var job = new Job
            {
                Name = "failure",
                Steps = [new ScriptExecutionStep { Settings = new() { ScriptPath = path, WaitForExit = false } },
                new JobExecutionStep { Settings = new() { JobId = Guid.NewGuid(), WaitForCompletion = true } }],
                EndSteps = [Text("cleanup")]
            };
            var builder = new JobExecutorTestBuilder().WithJobs(job);
            builder.Scripts.Execute = (_, _, token) => Task.Delay(Timeout.Infinite, token);
            using var executor = await builder.BuildAsync();
            using var cancellation = new JobExecutionCancellation();
            await executor.ExecuteJob(job.Id, JobStartContext.Manual, cancellation).WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(JobExecutionState.Failed, cancellation.State);
            Assert.False(Assert.Single(builder.Logs.Completions).Success);
            Assert.Equal("cleanup", Assert.Single(builder.Overlay.TextCalls).Text);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task Macro_JobAndFollowingStepsWaitForItsCompletion()
    {
        var macro = new TaskAutomation.Makros.Makro { Name = "macro" };
        var macros = new ControllableMakroExecutor();
        var job = new Job { Name = "macro owner", Steps = [new MakroExecutionStep { Settings = new() { MakroId = macro.Id } }, Text("after macro")] };
        var builder = new JobExecutorTestBuilder().WithJobs(job).WithMakros(macros, macro);
        using var executor = await builder.BuildAsync();
        var run = executor.ExecuteJob(job.Id);
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (macros.Snapshot().Length == 0 && DateTime.UtcNow < deadline) await Task.Delay(5);
        var invocation = Assert.Single(macros.Snapshot());
        Assert.False(run.IsCompleted);
        Assert.Empty(builder.Overlay.TextCalls);
        invocation.Completion.SetResult();
        await run.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("after macro", Assert.Single(builder.Overlay.TextCalls).Text);
    }

    [Fact]
    public async Task BrokenCleanupCondition_SuppressesItsBlock_ButRunsIndependentCleanup()
    {
        var condition = new IfStep();
        condition.Inputs["conditions"] = new ResultBinding { ProviderId = ValueProviderIds.JobVariable, SourceId = Guid.NewGuid().ToString() };
        var job = new Job
        {
            Name = "cleanup condition",
            Steps = [Text("main")],
            EndSteps = [condition, Text("unsafe branch"), new ElseStep(), Text("unsafe else"), new EndIfStep(), Text("safe cleanup")]
        };
        var builder = new JobExecutorTestBuilder().WithJobs(job);
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);
        Assert.Equal(["main", "safe cleanup"], builder.Overlay.TextCalls.Select(call => call.Text));
        Assert.False(Assert.Single(builder.Logs.Completions).Success);
    }

    [Fact]
    public async Task EmptyChild_IsRejected_AndParentDoesNotContinue()
    {
        var child = new Job { Name = "empty" };
        var parent = new Job { Name = "parent", Steps = [new JobExecutionStep { Settings = new() { JobId = child.Id, WaitForCompletion = true } }, Text("after")], EndSteps = [Text("cleanup")] };
        var builder = new JobExecutorTestBuilder().WithJobs(parent, child);
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(parent.Id);
        Assert.DoesNotContain(builder.Overlay.TextCalls, call => call.Text == "after");
        Assert.False(Assert.Single(builder.Logs.Completions).Success);
    }

    [Fact]
    public async Task BrokenEndJobInput_DoesNotPreventFollowingCleanup()
    {
        var end = new EndJobStep();
        end.Inputs["skip_end_steps"] = new ResultBinding { ProviderId = ValueProviderIds.JobVariable, SourceId = Guid.NewGuid().ToString() };
        var job = new Job { Name = "cleanup", Steps = [Text("main")], EndSteps = [end, Text("after bad end")] };
        var builder = new JobExecutorTestBuilder().WithJobs(job);
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);
        Assert.Contains(builder.Overlay.TextCalls, call => call.Text == "after bad end");
        Assert.False(Assert.Single(builder.Logs.Completions).Success);
    }

    [Fact]
    public async Task InvalidStructure_IsRejectedBeforeAnySideEffect()
    {
        var job = new Job { Name = "invalid", Steps = [Text("before"), new IfStep()] };
        var builder = new JobExecutorTestBuilder().WithJobs(job);
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(job.Id);
        Assert.Empty(builder.Overlay.TextCalls);
        Assert.False(Assert.Single(builder.Logs.Completions).Success);
    }

    [Fact]
    public async Task ExecutionSnapshot_DoesNotObserveChangesToDefinition()
    {
        var job = new Job { Name = "snapshot", Steps = [Text("original")] };
        var builder = new JobExecutorTestBuilder().WithJobs(job);
        using var executor = await builder.BuildAsync();
        var snapshot = JobStepsSnapshotService.CaptureExecution(job);
        job.Steps = [Text("edited")];
        using var cancellation = new JobExecutionCancellation();
        await executor.ExecuteDefinitionAsync(snapshot, JobStartContext.Manual, cancellation);
        Assert.Equal("original", Assert.Single(builder.Overlay.TextCalls).Text);
    }

    [Fact]
    public async Task ParallelInstances_UseDifferentOverlayKeysForTheSameDefinition()
    {
        var job = new Job { Name = "overlay", Steps = [Text("same")] };
        var builder = new JobExecutorTestBuilder().WithJobs(job);
        using var executor = await builder.BuildAsync();
        await Task.WhenAll(executor.ExecuteJob(job.Id), executor.ExecuteJob(job.Id));
        Assert.Equal(2, builder.Overlay.TextCalls.Select(call => call.StepKey).Distinct().Count());
    }
}
