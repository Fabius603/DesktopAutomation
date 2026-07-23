using TaskAutomation.Jobs;
using TaskAutomation.Orchestration;
using TaskAutomation.Steps;

namespace TaskAutomation.Tests.Orchestration;

public sealed class JobDebugSessionTests
{
    [Fact]
    public async Task Step_PausesAgainBeforeFollowingStep()
    {
        var first = new TimeoutStep();
        var second = new TimeoutStep();
        var session = CreateSession(first, second);

        var firstWait = session.BeforeStepAsync(first, "Hauptphase", CancellationToken.None);
        Assert.Equal(JobDebugSessionState.Paused, session.State);
        Assert.Equal(JobStepDebugState.Waiting, first.DebugState);

        session.Step();
        await firstWait;
        session.MarkCompleted(first);

        var secondWait = session.BeforeStepAsync(second, "Hauptphase", CancellationToken.None);
        Assert.Equal(JobDebugSessionState.Paused, session.State);
        Assert.Equal(JobStepDebugState.Waiting, second.DebugState);

        session.Step();
        await secondWait;
    }

    [Fact]
    public async Task Continue_RunsUntilBreakpoint()
    {
        var first = new TimeoutStep();
        var second = new TimeoutStep();
        var session = CreateSession(first, second);

        var firstWait = session.BeforeStepAsync(first, "Hauptphase", CancellationToken.None);
        session.Continue();
        Assert.Equal(JobDebugSessionState.Running, session.State);
        await firstWait;
        session.MarkCompleted(first);
        second.IsBreakpoint = true;

        var breakpointWait = session.BeforeStepAsync(second, "Hauptphase", CancellationToken.None);
        Assert.Equal(JobDebugSessionState.Paused, session.State);
        Assert.False(breakpointWait.IsCompleted);

        session.Continue();
        await breakpointWait;
    }

    [Fact]
    public async Task IterationEnd_KeepsLastStepAndResultVisibleUntilResumed()
    {
        var step = new TimeoutStep();
        var session = CreateSession(step);
        session.SetIteration(2);

        var beforeStep = session.BeforeStepAsync(step, "Hauptphase", CancellationToken.None);
        session.Step();
        await beforeStep;
        session.MarkCompleted(
            step,
            result: new TimeoutResult { WasExecuted = true, Success = true });

        var iterationPause = session.PauseAfterIterationAsync(CancellationToken.None);

        Assert.Equal(JobDebugSessionState.Paused, session.State);
        Assert.True(session.IsAtIterationEnd);
        Assert.Equal(step.Id, session.CurrentStepId);
        Assert.NotNull(session.GetSnapshot(step.Id));
        Assert.False(iterationPause.IsCompleted);

        session.Step();
        await iterationPause;

        Assert.False(session.IsAtIterationEnd);
        Assert.Equal(step.Id, session.CurrentStepId);
        Assert.NotNull(session.GetSnapshot(step.Id));
    }

    [Fact]
    public async Task Continue_SuppressesUiChangesUntilBreakpoint()
    {
        var first = new TimeoutStep();
        var second = new TimeoutStep();
        var breakpoint = new TimeoutStep { IsBreakpoint = true };
        var session = CreateSession(first, second, breakpoint);
        var changeCount = 0;
        session.Changed += () => changeCount++;

        var firstWait = session.BeforeStepAsync(first, "Hauptphase", CancellationToken.None);
        session.Continue();
        await firstWait;
        var changesAfterContinue = changeCount;

        session.MarkCompleted(first);
        await session.BeforeStepAsync(second, "Hauptphase", CancellationToken.None);
        session.MarkCompleted(second);

        Assert.Equal(changesAfterContinue, changeCount);

        var breakpointWait = session.BeforeStepAsync(breakpoint, "Hauptphase", CancellationToken.None);

        Assert.Equal(changesAfterContinue + 1, changeCount);
        Assert.Equal(JobDebugSessionState.Paused, session.State);

        session.Continue();
        await breakpointWait;
    }

    [Fact]
    public async Task Continue_OnlyPublishesIterationWhileStepsAreRunning()
    {
        var first = new TimeoutStep();
        var second = new TimeoutStep();
        var session = CreateSession(first, second);
        var fullChanges = 0;
        var iterationChanges = 0;
        session.Changed += () => fullChanges++;
        session.IterationChanged += () => iterationChanges++;

        var firstWait = session.BeforeStepAsync(first, "Hauptphase", CancellationToken.None);
        session.Continue();
        await firstWait;
        var changesAfterContinue = fullChanges;
        var visibleStateAfterContinue = first.DebugState;
        Assert.Equal(JobStepDebugState.None, visibleStateAfterContinue);

        session.MarkCompleted(first, "first");
        await session.BeforeStepAsync(second, "Hauptphase", CancellationToken.None, "second");
        session.SetIteration(2);
        session.MarkCompleted(second, "done");

        Assert.Equal(changesAfterContinue, fullChanges);
        Assert.Equal(1, iterationChanges);
        Assert.Equal(visibleStateAfterContinue, first.DebugState);
        Assert.Equal(JobStepDebugState.None, second.DebugState);
        Assert.Null(second.DebugDetails);
        Assert.Equal(2, session.Iteration);
    }

    [Fact]
    public async Task Step_ReenablesUiChangesAfterContinuedRunReachesBreakpoint()
    {
        var first = new TimeoutStep();
        var breakpoint = new TimeoutStep { IsBreakpoint = true };
        var following = new TimeoutStep();
        var session = CreateSession(first, breakpoint, following);
        var changeCount = 0;
        session.Changed += () => changeCount++;

        var firstWait = session.BeforeStepAsync(first, "Hauptphase", CancellationToken.None);
        session.Continue();
        await firstWait;
        session.MarkCompleted(first);

        var breakpointWait = session.BeforeStepAsync(breakpoint, "Hauptphase", CancellationToken.None);
        session.Step();
        await breakpointWait;
        session.MarkCompleted(breakpoint);
        var changesBeforeFollowingStep = changeCount;

        var followingWait = session.BeforeStepAsync(following, "Hauptphase", CancellationToken.None);

        Assert.True(changeCount > changesBeforeFollowingStep);
        Assert.Equal(JobDebugSessionState.Paused, session.State);

        session.Step();
        await followingWait;
    }

    [Fact]
    public async Task Continue_DoesNotPauseAtIterationEnd()
    {
        var step = new TimeoutStep();
        var session = CreateSession(step);

        var beforeStep = session.BeforeStepAsync(step, "Hauptphase", CancellationToken.None);
        session.Continue();
        await beforeStep;
        session.MarkCompleted(step);

        var iterationEnd = session.PauseAfterIterationAsync(CancellationToken.None);

        Assert.True(iterationEnd.IsCompleted);
        Assert.Equal(JobDebugSessionState.Running, session.State);
        Assert.False(session.IsAtIterationEnd);
    }

    [Fact]
    public void SetIteration_UpdatesVisibleSessionValue()
    {
        var session = CreateSession(new TimeoutStep());

        session.SetIteration(7);

        Assert.Equal(7, session.Iteration);
    }

    [Fact]
    public void SetIteration_RemovesSnapshotsWithoutRetainedStoreResult()
    {
        var retained = new TimeoutStep { Id = "retained" };
        var removed = new TimeoutStep { Id = "removed" };
        var session = CreateSession(retained, removed);
        session.MarkCompleted(retained, result: new TimeoutResult { WasExecuted = true, Success = true });
        session.MarkCompleted(removed, result: new TimeoutResult { WasExecuted = true, Success = true });

        session.SetIteration(2, [retained.Id]);

        Assert.NotNull(session.GetSnapshot(retained.Id));
        Assert.Null(session.GetSnapshot(removed.Id));
        Assert.Single(session.GetSnapshots());
    }

    [Fact]
    public void MarkSkipped_StoresReasonOnStep()
    {
        var step = new TimeoutStep();
        var session = CreateSession(step);

        session.MarkSkipped(step, "Inaktiver Zweig");

        Assert.Equal(JobStepDebugState.Skipped, step.DebugState);
        Assert.Equal("Inaktiver Zweig", step.DebugDetails);
    }

    [Fact]
    public async Task Snapshot_PreservesInputsAndResultForInspector()
    {
        var step = new TimeoutStep();
        var session = CreateSession(step);
        session.SetIteration(3);

        var wait = session.BeforeStepAsync(step, "Hauptphase", CancellationToken.None, "DauerMs=250");
        var before = session.GetSnapshot(step.Id);

        Assert.NotNull(before);
        Assert.Equal("Hauptphase", before.Phase);
        Assert.Equal(3, before.Iteration);
        Assert.Equal("DauerMs=250", before.InputDetails);
        Assert.Equal(JobStepDebugState.Waiting, before.State);

        session.Step();
        await wait;
        session.MarkCompleted(step, "Success=True");

        var after = session.GetSnapshot(step.Id);
        Assert.Equal("DauerMs=250", after!.InputDetails);
        Assert.Equal("Success=True", after.ResultDetails);
        Assert.Equal(JobStepDebugState.Completed, after.State);
        Assert.NotNull(after.Duration);
    }

    [Fact]
    public void GetSnapshots_IncludesSkippedContextStep()
    {
        var step = new TimeoutStep();
        var session = CreateSession(step);

        session.MarkSkipped(step, "Inaktiver Zweig");

        var snapshot = Assert.Single(session.GetSnapshots());
        Assert.Equal(step.Id, snapshot.StepId);
        Assert.Equal(JobStepDebugState.Skipped, snapshot.State);
        Assert.Equal("Inaktiver Zweig", snapshot.ResultDetails);
    }

    [Fact]
    public void MarkCompleted_FormatsNestedResultAsExpandableValueTree()
    {
        var step = new GetProcessStep();
        var session = CreateSession(step);
        var result = new GetProcessResult
        {
            WasExecuted = true,
            Found = true,
            Process = new RuntimeProcessReference
            {
                ProcessId = 42,
                ProcessName = "Editor",
                ExecutablePath = @"C:\Tools\Editor.exe",
                WindowHandle = 1234
            }
        };

        session.MarkCompleted(step, result: result);

        var values = session.GetSnapshot(step.Id)!.OutputValues;
        Assert.Contains(values, node => node.Name == "Found" && node.DisplayValue == "True");
        Assert.DoesNotContain(values, node => node.Name == "Was Executed");
        var process = Assert.Single(values, node => node.Name == "Process");
        Assert.Equal("Process", process.PropertyPath);
        Assert.Contains(process.Children, node => node.Name == "Process ID" && node.DisplayValue == "42");
        Assert.Contains(process.Children,
            node => node.PropertyPath == "Process.ProcessId" && node.DisplayValue == "42");
        Assert.Contains(process.Children, node => node.Name == "Process Name" && node.DisplayValue == "Editor");
    }

    private static JobDebugSession CreateSession(params JobStep[] steps)
    {
        var job = new Job { Name = "Debug test", Steps = [.. steps] };
        return new JobDebugSession(Guid.NewGuid(), job);
    }
}
