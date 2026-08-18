namespace DesktopAutomationApp.ViewModels;

/// <summary>
/// Derives an editor's dirty state from its current persisted state and the last accepted baseline.
/// Only the newest asynchronous comparison may publish a result.
/// </summary>
internal sealed class EditorChangeTracker<TState> : IDisposable
{
    private readonly Func<TState, TState, CancellationToken, Task<bool>> _equalsAsync;
    private readonly Action<bool> _publishDirtyState;
    private readonly SynchronizationContext? _synchronizationContext;
    private readonly TimeSpan _debounce;
    private readonly object _sync = new();
    private TState _baseline;
    private CancellationTokenSource? _comparisonCts;
    private Task _pendingComparison = Task.CompletedTask;
    private long _revision;
    private bool _disposed;

    public EditorChangeTracker(
        TState baseline,
        Func<TState, TState, CancellationToken, Task<bool>> equalsAsync,
        Action<bool> publishDirtyState,
        TimeSpan? debounce = null)
    {
        _baseline = baseline;
        _equalsAsync = equalsAsync ?? throw new ArgumentNullException(nameof(equalsAsync));
        _publishDirtyState = publishDirtyState ?? throw new ArgumentNullException(nameof(publishDirtyState));
        _synchronizationContext = SynchronizationContext.Current;
        _debounce = debounce ?? TimeSpan.Zero;
    }

    public void Accept(TState baseline)
    {
        CancellationTokenSource? previous;
        lock (_sync)
        {
            ThrowIfDisposed();
            _baseline = baseline;
            _revision++;
            previous = _comparisonCts;
            _comparisonCts = null;
            _pendingComparison = Task.CompletedTask;
        }

        previous?.Cancel();
        previous?.Dispose();
        Publish(isDirty: false, revision: null);
    }

    public void Evaluate(TState current, bool markDirtyImmediately = true)
    {
        CancellationTokenSource? previous;
        CancellationTokenSource currentCts;
        TState baseline;
        long revision;

        lock (_sync)
        {
            ThrowIfDisposed();
            revision = ++_revision;
            baseline = _baseline;
            previous = _comparisonCts;
            currentCts = new CancellationTokenSource();
            _comparisonCts = currentCts;
        }

        previous?.Cancel();
        previous?.Dispose();
        if (markDirtyImmediately)
            Publish(isDirty: true, revision);

        var comparison = CompareAndPublishAsync(baseline, current, revision, currentCts.Token);
        lock (_sync)
        {
            if (!_disposed && revision == _revision)
                _pendingComparison = comparison;
        }
    }

    internal Task WhenIdleAsync()
    {
        lock (_sync)
            return _pendingComparison;
    }

    private async Task CompareAndPublishAsync(
        TState baseline,
        TState current,
        long revision,
        CancellationToken cancellationToken)
    {
        try
        {
            if (_debounce > TimeSpan.Zero)
                await Task.Delay(_debounce, cancellationToken).ConfigureAwait(false);

            var statesMatch = await _equalsAsync(baseline, current, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            await PublishAsync(!statesMatch, revision).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private void Publish(bool isDirty, long? revision)
        => _ = PublishAsync(isDirty, revision);

    private Task PublishAsync(bool isDirty, long? revision)
    {
        void Apply()
        {
            lock (_sync)
            {
                if (_disposed || revision.HasValue && revision.Value != _revision)
                    return;
            }

            _publishDirtyState(isDirty);
        }

        if (_synchronizationContext is null || ReferenceEquals(_synchronizationContext, SynchronizationContext.Current))
        {
            Apply();
            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _synchronizationContext.Post(_ =>
        {
            try
            {
                Apply();
                completion.SetResult();
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        }, null);
        return completion.Task;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        CancellationTokenSource? comparisonCts;
        lock (_sync)
        {
            if (_disposed)
                return;

            _disposed = true;
            _revision++;
            comparisonCts = _comparisonCts;
            _comparisonCts = null;
        }

        comparisonCts?.Cancel();
        comparisonCts?.Dispose();
    }
}
