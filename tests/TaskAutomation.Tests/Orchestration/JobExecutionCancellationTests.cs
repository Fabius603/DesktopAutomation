using TaskAutomation.Orchestration;

namespace TaskAutomation.Tests.Orchestration;

public sealed class JobExecutionCancellationTests
{
    [Fact]
    public void NewInstance_StartsInStartingStateWithActiveTokens()
    {
        using var cancellation = new JobExecutionCancellation();
        Assert.Equal(JobExecutionState.Starting, cancellation.State);
        Assert.False(cancellation.ExecutionToken.IsCancellationRequested);
        Assert.False(cancellation.EndPhaseToken.IsCancellationRequested);
    }

    [Fact]
    public void RequestStop_CancelsExecutionButKeepsEndPhaseAvailable()
    {
        using var cancellation = new JobExecutionCancellation();
        cancellation.EnterRunPhase();
        Assert.True(cancellation.RequestStop());
        Assert.Equal(JobExecutionState.StopRequested, cancellation.State);
        Assert.True(cancellation.ExecutionToken.IsCancellationRequested);
        Assert.False(cancellation.EndPhaseToken.IsCancellationRequested);
        Assert.False(cancellation.RequestStop());
        Assert.True(cancellation.BeginEndPhase());
        Assert.Equal(JobExecutionState.RunningEndSteps, cancellation.State);
    }

    [Fact]
    public void ForceStop_CancelsExecutionAndEndPhaseAndIsIdempotent()
    {
        using var cancellation = new JobExecutionCancellation();
        cancellation.EnterStartPhase();
        Assert.True(cancellation.ForceStop());
        Assert.True(cancellation.ExecutionToken.IsCancellationRequested);
        Assert.True(cancellation.EndPhaseToken.IsCancellationRequested);
        Assert.True(cancellation.IsForceStopRequested);
        Assert.False(cancellation.ForceStop());
        Assert.False(cancellation.BeginEndPhase());
    }

    [Fact]
    public void ExternalCancellation_RequestsControlledStop()
    {
        using var external = new CancellationTokenSource();
        using var cancellation = new JobExecutionCancellation(external.Token);
        external.Cancel();
        Assert.Equal(JobExecutionState.StopRequested, cancellation.State);
        Assert.True(cancellation.ExecutionToken.IsCancellationRequested);
        Assert.False(cancellation.EndPhaseToken.IsCancellationRequested);
    }

    [Theory]
    [InlineData(JobExecutionState.Completed)]
    [InlineData(JobExecutionState.Cancelled)]
    [InlineData(JobExecutionState.Failed)]
    public void MarkCompleted_SetsTerminalStateAndPreventsStop(JobExecutionState state)
    {
        using var cancellation = new JobExecutionCancellation();
        cancellation.MarkCompleted(state);
        Assert.Equal(state, cancellation.State);
        Assert.False(cancellation.RequestStop());
        Assert.False(cancellation.ForceStop());
    }

    [Fact]
    public void StateChanged_ReportsTransitionsInOrder()
    {
        using var cancellation = new JobExecutionCancellation();
        var states = new List<JobExecutionState>();
        cancellation.StateChanged += states.Add;
        cancellation.EnterStartPhase();
        cancellation.EnterRunPhase();
        cancellation.RequestStop();
        cancellation.BeginEndPhase();
        cancellation.MarkCompleted(JobExecutionState.Cancelled);
        Assert.Equal([JobExecutionState.RunningStartSteps, JobExecutionState.RunningSteps,
            JobExecutionState.StopRequested, JobExecutionState.RunningEndSteps, JobExecutionState.Cancelled], states);
    }

    [Fact]
    public void MarkCompleted_RejectsNonTerminalState()
    {
        using var cancellation = new JobExecutionCancellation();
        Assert.Throws<ArgumentOutOfRangeException>(() => cancellation.MarkCompleted(JobExecutionState.RunningSteps));
    }
}
