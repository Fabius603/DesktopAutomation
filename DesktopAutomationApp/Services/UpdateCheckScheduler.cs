namespace DesktopAutomationApp.Services;

public sealed class UpdateCheckScheduler : IDisposable
{
    private readonly Func<Task> _checkAsync;
    private readonly TimeSpan _interval;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Task _runTask;

    public UpdateCheckScheduler(Func<Task> checkAsync, TimeSpan interval)
    {
        ArgumentNullException.ThrowIfNull(checkAsync);
        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval));

        _checkAsync = checkAsync;
        _interval = interval;
        _runTask = RunAsync(_cancellation.Token);
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _checkAsync();

            using var timer = new PeriodicTimer(_interval);
            while (await timer.WaitForNextTickAsync(cancellationToken))
                await _checkAsync();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
        GC.SuppressFinalize(this);
    }
}
