using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace TaskAutomation.Timing;

public sealed class WindowsPreciseDelayService : IPreciseDelayService, IDisposable
{
    private sealed class DelayRequest
    {
        private readonly TaskCompletionSource _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private CancellationTokenRegistration _cancellationRegistration;

        public DelayRequest(long targetTimestamp) => TargetTimestamp = targetTimestamp;

        public long TargetTimestamp { get; }
        public Task Task => _completion.Task;
        public bool IsCompleted => _completion.Task.IsCompleted;

        public void RegisterCancellation(CancellationToken token, Action wakeScheduler)
        {
            if (!token.CanBeCanceled) return;
            var registration = token.UnsafeRegister(static state =>
            {
                var (request, cancellationToken, wake) =
                    ((DelayRequest, CancellationToken, Action))state!;
                request.Cancel(cancellationToken);
                wake();
            }, (this, token, wakeScheduler));
            _cancellationRegistration = registration;
            if (IsCompleted)
                registration.Unregister();
        }

        public void Complete()
        {
            if (_completion.TrySetResult())
                _cancellationRegistration.Unregister();
        }

        public void Cancel(CancellationToken token)
        {
            if (_completion.TrySetCanceled(token))
                _cancellationRegistration.Unregister();
        }

        public void CancelForShutdown()
        {
            if (_completion.TrySetCanceled())
                _cancellationRegistration.Unregister();
        }
    }

    private sealed class NativeTimerWaitHandle : WaitHandle
    {
        public NativeTimerWaitHandle(nint handle) =>
            SafeWaitHandle = new SafeWaitHandle(handle, ownsHandle: true);
    }

    private const uint CreateWaitableTimerHighResolution = 0x00000002;
    private const uint TimerModifyState = 0x0002;
    private const uint Synchronize = 0x00100000;
    private static readonly long FineWaitTicks =
        Math.Max(1, PreciseTime.AddMilliseconds(0, 2));

    private readonly object _gate = new();
    private readonly PriorityQueue<DelayRequest, long> _requests = new();
    private readonly AutoResetEvent _wakeEvent = new(false);
    private readonly NativeTimerWaitHandle? _timer;
    private readonly WaitHandle[]? _waitHandles;
    private readonly Thread _schedulerThread;
    private bool _disposed;

    public WindowsPreciseDelayService()
    {
        var timerHandle = CreateTimer(CreateWaitableTimerHighResolution);
        if (timerHandle == 0)
            timerHandle = CreateTimer(0);

        if (timerHandle != 0)
        {
            _timer = new NativeTimerWaitHandle(timerHandle);
            _waitHandles = [_wakeEvent, _timer];
        }

        _schedulerThread = new Thread(RunScheduler)
        {
            IsBackground = true,
            Name = "DesktopAutomation precise timer",
            Priority = ThreadPriority.AboveNormal
        };
        _schedulerThread.Start();
    }

    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay));
        if (delay == TimeSpan.Zero) return Task.CompletedTask;

        var delayTicks = delay.TotalSeconds * Stopwatch.Frequency;
        if (delayTicks >= long.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(delay));

        var target = checked(Stopwatch.GetTimestamp() + (long)Math.Ceiling(delayTicks));
        return DelayUntilAsync(target, cancellationToken);
    }

    public Task DelayUntilAsync(long targetTimestamp, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (targetTimestamp <= Stopwatch.GetTimestamp()) return Task.CompletedTask;

        var request = new DelayRequest(targetTimestamp);
        request.RegisterCancellation(cancellationToken, WakeScheduler);

        lock (_gate)
        {
            if (_disposed)
            {
                request.CancelForShutdown();
                throw new ObjectDisposedException(nameof(WindowsPreciseDelayService));
            }
            if (!request.IsCompleted)
                _requests.Enqueue(request, targetTimestamp);
        }

        WakeScheduler();
        return request.Task;
    }

    private void RunScheduler()
    {
        while (true)
        {
            DelayRequest? next;
            lock (_gate)
            {
                if (_disposed) break;
                RemoveCompletedRequestsLocked();
                next = _requests.Count > 0 ? _requests.Peek() : null;
            }

            if (next is null)
            {
                _wakeEvent.WaitOne();
                continue;
            }

            var now = Stopwatch.GetTimestamp();
            var remaining = next.TargetTimestamp - now;
            if (remaining <= 0)
            {
                CompleteDueRequests(now);
                continue;
            }

            if (remaining > FineWaitTicks)
            {
                WaitCoarsely(remaining - FineWaitTicks);
                continue;
            }

            SpinUntilDeadlineOrWake(next.TargetTimestamp);
        }

        CancelPendingRequests();
    }

    private void WaitCoarsely(long stopwatchTicks)
    {
        if (_timer != null && _waitHandles != null)
        {
            var dueTime = -Math.Max(1L, (long)Math.Ceiling(
                stopwatchTicks * 10_000_000d / Stopwatch.Frequency));
            if (SetWaitableTimerEx(
                    _timer.SafeWaitHandle,
                    ref dueTime,
                    0,
                    0,
                    0,
                    0,
                    0))
            {
                WaitHandle.WaitAny(_waitHandles);
                return;
            }
        }

        var milliseconds = (int)Math.Clamp(Math.Ceiling(
            stopwatchTicks * 1000d / Stopwatch.Frequency), 1, int.MaxValue);
        _wakeEvent.WaitOne(milliseconds);
    }

    private void SpinUntilDeadlineOrWake(long targetTimestamp)
    {
        var spinner = new SpinWait();
        while (Stopwatch.GetTimestamp() < targetTimestamp)
        {
            if (_wakeEvent.WaitOne(0)) return;
            spinner.SpinOnce(-1);
        }
        CompleteDueRequests(Stopwatch.GetTimestamp());
    }

    private void CompleteDueRequests(long now)
    {
        List<DelayRequest> due = [];
        lock (_gate)
        {
            RemoveCompletedRequestsLocked();
            while (_requests.Count > 0
                   && _requests.TryPeek(out var request, out var target)
                   && target <= now)
            {
                _requests.Dequeue();
                if (!request.IsCompleted) due.Add(request);
            }
        }

        foreach (var request in due)
            request.Complete();
    }

    private void RemoveCompletedRequestsLocked()
    {
        while (_requests.Count > 0 && _requests.Peek().IsCompleted)
            _requests.Dequeue();
    }

    private void CancelPendingRequests()
    {
        List<DelayRequest> pending = [];
        lock (_gate)
        {
            while (_requests.Count > 0)
                pending.Add(_requests.Dequeue());
        }
        foreach (var request in pending)
            request.CancelForShutdown();
    }

    private void WakeScheduler()
    {
        try { _wakeEvent.Set(); }
        catch (ObjectDisposedException) { }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
        }

        WakeScheduler();
        if (Thread.CurrentThread != _schedulerThread)
            _schedulerThread.Join(TimeSpan.FromSeconds(5));
        _timer?.Dispose();
        _wakeEvent.Dispose();
    }

    private static nint CreateTimer(uint flags) => CreateWaitableTimerExW(
        0,
        null,
        flags,
        TimerModifyState | Synchronize);

    [DllImport("kernel32.dll", EntryPoint = "CreateWaitableTimerExW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWaitableTimerExW(
        nint timerAttributes,
        string? timerName,
        uint flags,
        uint desiredAccess);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWaitableTimerEx(
        SafeWaitHandle timer,
        ref long dueTime,
        int period,
        nint completionRoutine,
        nint completionArgument,
        nint wakeContext,
        uint tolerableDelay);
}
