namespace TaskAutomation.Orchestration;

/// <summary>Owns parallel operations until they have actually terminated.</summary>
public sealed class OwnedExecutionScope : IDisposable
{
    private readonly object _gate = new();
    private readonly SemaphoreSlim _admission;
    private readonly List<Task> _tasks = [];
    private readonly CancellationTokenSource _stop = new();
    private readonly Action? _onFailure;
    private readonly CancellationTokenSource _force;
    private readonly TimeSpan? _stopBudget;
    private readonly CancellationTokenRegistration _forceObservation;
    public CancellationToken ForceToken => _force.Token;
    public bool StopEscalated { get; private set; }
    private Exception? _failure;
    public OwnedExecutionScope(Action? onFailure = null, CancellationToken forceToken = default, TimeSpan? stopBudget = null, int maxConcurrentExecutions = 32)
    {
        if (maxConcurrentExecutions < 1) throw new ArgumentOutOfRangeException(nameof(maxConcurrentExecutions));
        _admission = new SemaphoreSlim(maxConcurrentExecutions, maxConcurrentExecutions);
        _onFailure = onFailure;
        _stopBudget = stopBudget;
        _force = CancellationTokenSource.CreateLinkedTokenSource(forceToken);
        _forceObservation = _force.Token.Register(() =>
        {
            if (!forceToken.IsCancellationRequested) StopEscalated = true;
        });
    }
    public Exception? Failure { get { lock (_gate) return _failure; } }
    public int PendingCount { get { lock (_gate) return _tasks.Count(task => !task.IsCompleted); } }

    public async Task<Task> StartAsync(Func<CancellationToken, Task> operation, CancellationToken phaseToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(phaseToken, _stop.Token, ForceToken);
        await _admission.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            linked.Token.ThrowIfCancellationRequested();
            var completion = StartCore(operation, phaseToken);
            _ = completion.ContinueWith(_ => _admission.Release(), CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            return completion;
        }
        catch { _admission.Release(); throw; }
    }

    public Task Start(Func<CancellationToken, Task> operation, CancellationToken phaseToken) =>
        StartAsync(operation, phaseToken).Unwrap();

    private Task StartCore(Func<CancellationToken, Task> operation, CancellationToken phaseToken)
    {
        // Register the completion before scheduling so drain cannot miss a launch.
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_gate)
        {
            _tasks.RemoveAll(task => task.IsCompletedSuccessfully);
            _tasks.Add(completion.Task);
        }
        _ = RunAsync();
        return completion.Task;
        async Task RunAsync()
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(phaseToken, _stop.Token, ForceToken);
            using var escalation = linked.Token.Register(() =>
            {
                if (!_stopBudget.HasValue || _force.IsCancellationRequested) return;
                try { _force.CancelAfter(_stopBudget.Value); } catch (ObjectDisposedException) { }
            });
            try
            {
                await Task.Run(async () =>
                {
                    linked.Token.ThrowIfCancellationRequested();
                    await operation(linked.Token).ConfigureAwait(false);
                }, CancellationToken.None).ConfigureAwait(false);
                completion.TrySetResult();
            }
            catch (OperationCanceledException error) when (linked.IsCancellationRequested)
            { completion.TrySetCanceled(error.CancellationToken); }
            catch (Exception error)
            {
                lock (_gate) _failure ??= error;
                try { _stop.Cancel(); _onFailure?.Invoke(); }
                finally { completion.TrySetException(error); }
            }
        }
    }

    public async Task DrainAsync(TimeSpan? stopBudget = null, CancellationToken stopToken = default)

    {
        Task[] tasks;
        lock (_gate) tasks = _tasks.ToArray();
        try
        {
            var all = Task.WhenAll(tasks);
            if (stopBudget.HasValue && !all.IsCompleted)
            {
                using var waitStop = CancellationTokenSource.CreateLinkedTokenSource(stopToken, _stop.Token, ForceToken);
                var stop = Task.Delay(Timeout.Infinite, waitStop.Token);
                if (await Task.WhenAny(all, stop).ConfigureAwait(false) == stop)
                {
                    try { await all.WaitAsync(stopBudget.Value).ConfigureAwait(false); }
                    catch (TimeoutException)
                    {
                        StopEscalated = true;
                        _force.Cancel();
                        // Retain ownership and visible activity until the operation confirms termination.
                    }
                }
                waitStop.Cancel();
            }
            await all.ConfigureAwait(false);
        }
        finally { lock (_gate) _tasks.RemoveAll(task => task.IsCompleted); }
    }
    public void RequestStop() => _stop.Cancel();
    public void ThrowIfFailed()
    {
        if (Failure is { } failure) throw new InvalidOperationException("An owned execution failed.", failure);
    }
    public void Dispose() { _forceObservation.Dispose(); _stop.Dispose(); _force.Dispose(); }
}
