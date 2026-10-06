using ImageCapture.DesktopDuplication;
using Microsoft.Extensions.Logging;
using TaskAutomation.Geometry;

namespace TaskAutomation.Steps;

/// <summary>Owns capture policy and one serialized session per monitor identity.</summary>
public sealed class DesktopCaptureService : IDesktopCaptureService
{
    private sealed class MonitorSession
    {
        public readonly SemaphoreSlim Gate = new(1, 1);
        public IDesktopDuplicationSession? Capture;
    }
    private readonly ILogger<DesktopCaptureService> _logger;
    private readonly IDesktopDuplicationSessionFactory _factory;
    private readonly TimeProvider _time;
    private readonly object _lifetime = new();
    private readonly Dictionary<string, MonitorSession> _sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly CancellationTokenSource _shutdown = new();
    private bool _disposed;
    private int _activeCaptures;

    public DesktopCaptureService(ILogger<DesktopCaptureService> logger)
        : this(logger, new DesktopDuplicationSessionFactory(), TimeProvider.System) { }

    public DesktopCaptureService(ILogger<DesktopCaptureService> logger,
        IDesktopDuplicationSessionFactory factory, TimeProvider? timeProvider = null)
    {
        _logger = logger;
        _factory = factory;
        _time = timeProvider ?? TimeProvider.System;
    }

    public Task<CaptureFrame> CaptureAsync(int monitorIdx, CancellationToken ct, bool captureCursor = false)
        => CaptureAsync(new DesktopCaptureRequest(monitorIdx, CaptureCursor: captureCursor), ct);

    public async Task<CaptureFrame> CaptureAsync(DesktopCaptureRequest request, CancellationToken ct)
    {
        request.Validate();
        lock (_lifetime)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _activeCaptures++;
        }
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _shutdown.Token);
            // Native acquisition and GPU readback must never block the WPF dispatcher.
            return await Task.Run(() => CaptureCoreAsync(request, linked.Token), linked.Token).ConfigureAwait(false);
        }
        finally
        {
            lock (_lifetime)
            {
                _activeCaptures--;
                Monitor.PulseAll(_lifetime);
            }
        }
    }

    private async Task<CaptureFrame> CaptureCoreAsync(DesktopCaptureRequest request, CancellationToken ct)
    {
        long started = _time.GetTimestamp();
        var monitor = _factory.ResolveMonitor(request.MonitorIndex, request.MonitorDeviceName);
        MonitorSession session;
        lock (_lifetime)
        {
            if (!_sessions.TryGetValue(monitor.DeviceName, out session!))
                _sessions.Add(monitor.DeviceName, session = new MonitorSession());
        }
        int queueBudget = request.TimeoutMilliseconds - (int)_time.GetElapsedTime(started).TotalMilliseconds;
        if (queueBudget <= 0) throw new TimeoutException("Desktop capture timed out resolving the monitor.");
        using var deadline = new CancellationTokenSource(TimeSpan.FromMilliseconds(queueBudget), _time);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(ct, deadline.Token);
        try { await session.Gate.WaitAsync(budget.Token).ConfigureAwait(false); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { throw new TimeoutException("Desktop capture timed out waiting for the monitor."); }
        try
        {
            bool fresh = false;
            int recoveries = 0;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                int remaining = request.TimeoutMilliseconds - (int)_time.GetElapsedTime(started).TotalMilliseconds;
                if (remaining <= 0) break;
                try
                {
                    var current = _factory.ResolveMonitor(request.MonitorIndex, monitor.DeviceName);
                    if (session.Capture != null && session.Capture.Bounds != current.Bounds) Reset(session);
                    session.Capture ??= _factory.Create(current);
                    // Initialization may exceed the waiting budget; only probe without waiting afterward.
                    remaining = Math.Max(0, request.TimeoutMilliseconds - (int)_time.GetElapsedTime(started).TotalMilliseconds);
                    int acquireTimeout = !request.WaitForNewFrame && session.Capture.HasImage ? 0 : Math.Min(16, remaining);
                    fresh = session.Capture.UpdateFrame(acquireTimeout);
                    ct.ThrowIfCancellationRequested();
                    if (session.Capture.HasImage && (fresh || !request.WaitForNewFrame)) break;
                }
                catch (Exception ex) when (ex is DesktopDuplicationException or ObjectDisposedException)
                {
                    Reset(session);
                    if (++recoveries > 2) throw;
                    _logger.LogDebug(ex, "Desktop capture session lost; recreating monitor session (attempt {Attempt}).", recoveries);
                }
                int delayBudget = request.TimeoutMilliseconds - (int)_time.GetElapsedTime(started).TotalMilliseconds;
                if (delayBudget > 0)
                    await Task.Delay(TimeSpan.FromMilliseconds(Math.Min(4, delayBudget)), _time, ct).ConfigureAwait(false);
            }
            ct.ThrowIfCancellationRequested();
            if (session.Capture?.HasImage != true || (!fresh && request.WaitForNewFrame && !request.AllowCachedFallback))
                throw new TimeoutException("No suitable desktop image was available within the capture time limit.");

            using var frame = session.Capture.CopyFrame(request.CaptureCursor);
            if (frame.DesktopImage == null || frame.DesktopImage.Width != frame.Bounds.Width || frame.DesktopImage.Height != frame.Bounds.Height)
            {
                Reset(session);
                throw new DesktopDuplicationException("The desktop image and monitor bounds do not match.");
            }
            ct.ThrowIfCancellationRequested();
            var result = new CaptureFrame
            {
                Image = frame.DesktopImage,
                Bounds = frame.Bounds.ToPixelRegion(),
                Offset = frame.Bounds.Location.ToPixelPoint(),
                IsFresh = fresh,
                CaptureTimestampUtc = frame.CaptureTimestampUtc,
                FrameVersion = frame.FrameVersion,
                FrameTimestamp = frame.FrameTimestamp
            };
            frame.DesktopImage = null;
            return result;
        }
        finally { session.Gate.Release(); }
    }

    private void Reset(MonitorSession session)
    {
        var capture = session.Capture;
        session.Capture = null;
        try { capture?.Dispose(); }
        catch (Exception ex) { _logger.LogDebug(ex, "Failed to dispose a desktop capture session."); }
    }

    public void Dispose()
    {
        lock (_lifetime)
        {
            if (_disposed) return;
            _disposed = true;
        }
        _shutdown.Cancel();
        lock (_lifetime)
        {
            while (_activeCaptures > 0) Monitor.Wait(_lifetime);
            foreach (var session in _sessions.Values)
            {
                try { Reset(session); }
                finally { session.Gate.Dispose(); }
            }
            _sessions.Clear();
            _shutdown.Dispose();
        }
    }
}
