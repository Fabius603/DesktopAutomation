using System;
using System.Threading;

namespace TaskAutomation.Orchestration;

/// <summary>
/// Steuert den kontrollierten Stop und den davon getrennten ForceStop einer Job-Instanz.
/// Ein normaler Stop kann nur einmal angefordert werden und beendet niemals die Endphase.
/// </summary>
public sealed class JobExecutionCancellation : IDisposable
{
    private readonly CancellationTokenSource _executionCts;
    private readonly CancellationTokenSource _endPhaseCts = new();
    private readonly CancellationTokenRegistration _externalRegistration;
    private int _state = (int)JobExecutionState.Starting;
    private int _forceStopRequested;

    public event Action<JobExecutionState>? StateChanged;

    public JobExecutionCancellation(CancellationToken externalToken = default)
    {
        _executionCts = new CancellationTokenSource();
        _externalRegistration = externalToken.CanBeCanceled
            ? externalToken.Register(() => RequestStop())
            : default;
    }

    public CancellationToken ExecutionToken => _executionCts.Token;
    public CancellationToken EndPhaseToken => _endPhaseCts.Token;
    public JobExecutionState State => (JobExecutionState)Volatile.Read(ref _state);
    public bool IsEndPhase => State == JobExecutionState.RunningEndSteps;
    public bool IsForceStopRequested => Volatile.Read(ref _forceStopRequested) != 0;

    /// <summary>Fordert genau einmal den kontrollierten Wechsel in die Endphase an.</summary>
    public bool RequestStop()
    {
        while (true)
        {
            var current = State;
            if (!current.CanRequestStop())
                return false;
            if (Interlocked.CompareExchange(
                    ref _state,
                    (int)JobExecutionState.StopRequested,
                    (int)current) != (int)current)
                continue;

            CancelSafely(_executionCts);
            StateChanged?.Invoke(JobExecutionState.StopRequested);
            return true;
        }
    }

    /// <summary>Bricht Haupt- und Endphase unmittelbar ab. Darf mehrfach aufgerufen werden.</summary>
    public bool ForceStop()
    {
        while (true)
        {
            var current = State;
            if (current.IsTerminal() || current == JobExecutionState.ForceStopRequested)
                return false;
            if (Interlocked.CompareExchange(
                    ref _state,
                    (int)JobExecutionState.ForceStopRequested,
                    (int)current) != (int)current)
                continue;
            StateChanged?.Invoke(JobExecutionState.ForceStopRequested);
            break;
        }

        var firstRequest = Interlocked.Exchange(ref _forceStopRequested, 1) == 0;
        CancelSafely(_executionCts);
        CancelSafely(_endPhaseCts);
        return firstRequest;
    }

    internal void EnterStartPhase() => SetActivePhase(JobExecutionState.RunningStartSteps);
    internal void EnterRunPhase() => SetActivePhase(JobExecutionState.RunningSteps);

    internal bool BeginEndPhase()
    {
        while (true)
        {
            var current = State;
            if (current == JobExecutionState.ForceStopRequested || current.IsTerminal())
                return false;
            if (current == JobExecutionState.RunningEndSteps)
                return true;
            if (Interlocked.CompareExchange(
                    ref _state,
                    (int)JobExecutionState.RunningEndSteps,
                    (int)current) != (int)current)
                continue;
            StateChanged?.Invoke(JobExecutionState.RunningEndSteps);
            return true;
        }
    }

    internal void MarkCompleted(JobExecutionState finalState)
    {
        if (!finalState.IsTerminal())
            throw new ArgumentOutOfRangeException(nameof(finalState));
        SetState(finalState);
    }

    private void SetActivePhase(JobExecutionState phase)
    {
        while (true)
        {
            var current = State;
            if (current is JobExecutionState.StopRequested or JobExecutionState.ForceStopRequested || current.IsTerminal())
                return;
            if (Interlocked.CompareExchange(ref _state, (int)phase, (int)current) != (int)current)
                continue;
            if (current != phase)
                StateChanged?.Invoke(phase);
            return;
        }
    }

    private void SetState(JobExecutionState state)
    {
        var previous = (JobExecutionState)Interlocked.Exchange(ref _state, (int)state);
        if (previous != state)
            StateChanged?.Invoke(state);
    }

    private static void CancelSafely(CancellationTokenSource cts)
    {
        try { cts.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    public void Dispose()
    {
        _externalRegistration.Dispose();
        _executionCts.Dispose();
        _endPhaseCts.Dispose();
    }
}
