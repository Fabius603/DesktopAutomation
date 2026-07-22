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
    public void SetIteration_UpdatesVisibleSessionValue()
    {
        var session = CreateSession(new TimeoutStep());

        session.SetIteration(7);

        Assert.Equal(7, session.Iteration);
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
        Assert.Contains(process.Children, node => node.Name == "Process ID" && node.DisplayValue == "42");
        Assert.Contains(process.Children, node => node.Name == "Process Name" && node.DisplayValue == "Editor");
    }

    private static JobDebugSession CreateSession(params JobStep[] steps)
    {
        var job = new Job { Name = "Debug test", Steps = [.. steps] };
        return new JobDebugSession(Guid.NewGuid(), job);
    }
}
