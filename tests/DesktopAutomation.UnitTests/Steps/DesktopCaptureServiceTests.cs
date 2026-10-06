using System.Drawing;
using ImageCapture.DesktopDuplication;
using Microsoft.Extensions.Logging.Abstractions;
using TaskAutomation.Steps;

namespace TaskAutomation.Tests.Steps;

public sealed class DesktopCaptureServiceTests
{
    [Fact]
    public async Task WaitingForImage_DoesNotCloneCachedImagesBetweenPolls()
    {
        var clock = new CaptureClock();
        var session = new FakeSession { Poll = _ => { clock.Advance(10); return false; } };
        using var service = Create(new FakeFactory(session), clock);
        var result = await service.CaptureAsync(new(0, TimeoutMilliseconds: 30), default);
        using var image = result.Image;
        Assert.Equal(1, session.Copies);
        Assert.False(result.IsFresh);
    }

    [Fact]
    public async Task ConcurrentRequestsForSameMonitor_AreSerializedAndQueueCancellationDoesNotAffectTheActiveCapture()
    {
        using var release = new ManualResetEventSlim();
        using var cancelled = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int active = 0;
        var session = new FakeSession
        {
            Poll = _ =>
        {
            Assert.Equal(1, Interlocked.Increment(ref active));
            entered.TrySetResult();
            Assert.True(release.Wait(TimeSpan.FromSeconds(5)));
            Interlocked.Decrement(ref active);
            return true;
        }
        };
        using var service = Create(new FakeFactory(session));
        var first = service.CaptureAsync(new(0, TimeoutMilliseconds: 5000), default);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = service.CaptureAsync(new(1, "DISPLAY-A", TimeoutMilliseconds: 5000), default);
        var queued = service.CaptureAsync(new(2, "DISPLAY-A"), cancelled.Token);
        cancelled.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued);
        release.Set();
        var results = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
        foreach (var result in results) result.Image!.Dispose();
        Assert.Equal(2, session.Copies);
    }

    [Fact]
    public async Task LatestAvailable_ReturnsCachedImageWithOriginalProvenanceAndIndependentOwnership()
    {
        var clock = new CaptureClock();
        var session = new FakeSession();
        var factory = new FakeFactory(session);
        using var service = Create(factory, clock);
        var result = await service.CaptureAsync(new(0, WaitForNewFrame: false), default);
        using var image = result.Image;
        Assert.False(result.IsFresh);
        Assert.Equal(17, result.FrameVersion);
        Assert.Equal(100, result.FrameTimestamp);
        Assert.Equal(session.Timestamp, result.CaptureTimestampUtc);
        Assert.Equal(-4, result.Offset.X);
        Assert.Equal(1, session.Copies);
        image!.SetPixel(0, 0, Color.Blue);
        var second = await service.CaptureAsync(new(0, WaitForNewFrame: false), default);
        using var secondImage = second.Image;
        Assert.Equal(Color.Red.ToArgb(), secondImage!.GetPixel(0, 0).ToArgb());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NewImageTimeout_UsesCacheOnlyWhenAllowed(bool allowFallback)
    {
        var clock = new CaptureClock();
        var session = new FakeSession { Poll = _ => { clock.Advance(250); return false; } };
        using var service = Create(new FakeFactory(session), clock);
        var request = new DesktopCaptureRequest(0, AllowCachedFallback: allowFallback);
        if (!allowFallback)
        {
            await Assert.ThrowsAsync<TimeoutException>(() => service.CaptureAsync(request, default));
            Assert.Equal(0, session.Copies);
        }
        else
        {
            var result = await service.CaptureAsync(request, default);
            using var image = result.Image;
            Assert.False(result.IsFresh);
            Assert.Equal(17, result.FrameVersion);
            Assert.Equal(1, session.Copies);
        }
    }

    [Fact]
    public async Task TimeoutWithoutImage_FailsInsteadOfReturningAnEmptySuccessfulCapture()
    {
        var clock = new CaptureClock();
        var session = new FakeSession { HasImage = false, Poll = _ => { clock.Advance(250); return false; } };
        using var service = Create(new FakeFactory(session), clock);
        await Assert.ThrowsAsync<TimeoutException>(() => service.CaptureAsync(new DesktopCaptureRequest(0), default));
    }

    [Fact]
    public async Task FreshImage_ForwardsCursorAndSnapshotGeometry()
    {
        var session = new FakeSession { Poll = _ => true };
        using var service = Create(new FakeFactory(session));
        var result = await service.CaptureAsync(new(0, CaptureCursor: true), default);
        using var image = result.Image;
        Assert.True(result.IsFresh);
        Assert.True(session.CursorRequested);
        Assert.Equal(new TaskAutomation.Contracts.Geometry.PixelRegion(-4, 8, 2, 3), result.Bounds);
    }

    [Fact]
    public async Task DeviceLoss_RecreatesSessionAndDoesNotReuseInvalidCache()
    {
        var lost = new FakeSession { Poll = _ => throw new DesktopDuplicationException("device lost") };
        var recovered = new FakeSession { Poll = _ => true };
        using var service = Create(new FakeFactory(lost, recovered));
        var result = await service.CaptureAsync(new DesktopCaptureRequest(0), default);
        using var image = result.Image;
        Assert.True(lost.Disposed);
        Assert.True(result.IsFresh);
        Assert.Equal(0, lost.Copies);
    }

    [Fact]
    public async Task UnrecoverableErrors_AreNotRetriedAsDeviceLoss()
    {
        var session = new FakeSession { Poll = _ => throw new ArgumentException("invalid native data") };
        using var service = Create(new FakeFactory(session));
        await Assert.ThrowsAsync<ArgumentException>(() => service.CaptureAsync(new DesktopCaptureRequest(0), default));
        Assert.Equal(0, session.Copies);
    }

    [Fact]
    public async Task MonitorIdentitySurvivesIndexChanges_AndBoundsChangesRecreateTheSession()
    {
        var first = new FakeSession { Poll = _ => true };
        var replacement = new FakeSession { Bounds = new(10, 20, 2, 3), Poll = _ => true };
        var factory = new FakeFactory(first, replacement);
        using var service = Create(factory);
        var result = await service.CaptureAsync(new(0, "DISPLAY-A"), default);
        result.Image!.Dispose();
        factory.Monitor = new("DISPLAY-A", replacement.Bounds);
        result = await service.CaptureAsync(new(2, "DISPLAY-A"), default);
        using var image = result.Image;
        Assert.True(first.Disposed);
        Assert.Equal(10, result.Offset.X);
        Assert.Equal(20, result.Offset.Y);
    }

    [Fact]
    public async Task DisconnectedSelectedMonitor_DoesNotFallBackToAnotherMonitor()
    {
        using var service = Create(new FakeFactory(new FakeSession()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CaptureAsync(new(0, "MISSING"), default));
    }

    [Fact]
    public async Task CancellationDuringNativePoll_ReturnsNoImage()
    {
        using var cancellation = new CancellationTokenSource();
        var session = new FakeSession { Poll = _ => { cancellation.Cancel(); return true; } };
        using var service = Create(new FakeFactory(session));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CaptureAsync(new DesktopCaptureRequest(0), cancellation.Token));
        Assert.Equal(0, session.Copies);
    }

    [Fact]
    public async Task DisposeDuringCapture_WaitsForNativePollAndCancelsPendingWork()
    {
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = new FakeSession
        {
            Poll = _ => { entered.TrySetResult(); Assert.True(release.Wait(TimeSpan.FromSeconds(5))); return true; }
        };
        var service = Create(new FakeFactory(session));
        var capture = service.CaptureAsync(new DesktopCaptureRequest(0), default);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var disposalStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var disposal = Task.Run(() => { disposalStarted.SetResult(); service.Dispose(); });
        await disposalStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        release.Set();
        try { (await capture).Image!.Dispose(); }
        catch (OperationCanceledException) { }
        await disposal.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(session.Disposed);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => service.CaptureAsync(new DesktopCaptureRequest(0), default));
    }

    private static DesktopCaptureService Create(FakeFactory factory, TimeProvider? clock = null)
        => new(NullLogger<DesktopCaptureService>.Instance, factory, clock);

    private sealed class CaptureClock : TimeProvider
    {
        private long _ticks;
        public override long TimestampFrequency => 1000;
        public override long GetTimestamp() => Interlocked.Read(ref _ticks);
        public void Advance(int milliseconds) => Interlocked.Add(ref _ticks, milliseconds);
    }

    private sealed class FakeFactory(params FakeSession[] sessions) : IDesktopDuplicationSessionFactory
    {
        private readonly Queue<FakeSession> _sessions = new(sessions);
        public DesktopMonitor Monitor { get; set; } = new("DISPLAY-A", new(-4, 8, 2, 3));
        public DesktopMonitor ResolveMonitor(int index, string? deviceName)
            => deviceName is null or "" || deviceName == Monitor.DeviceName ? Monitor : throw new InvalidOperationException("disconnected");
        public IDesktopDuplicationSession Create(DesktopMonitor monitor) => _sessions.Dequeue();
    }

    private sealed class FakeSession : IDesktopDuplicationSession
    {
        public Rectangle Bounds { get; init; } = new(-4, 8, 2, 3);
        public bool HasImage { get; set; } = true;
        public Func<int, bool> Poll { get; init; } = _ => false;
        public bool Disposed { get; private set; }
        public int Copies { get; private set; }
        public bool CursorRequested { get; private set; }
        public DateTime Timestamp { get; } = new(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);
        public bool UpdateFrame(int timeoutMilliseconds) => Poll(timeoutMilliseconds);
        public DesktopFrame CopyFrame(bool captureCursor)
        {
            Assert.False(Disposed);
            Copies++;
            CursorRequested = captureCursor;
            var image = new Bitmap(Bounds.Width, Bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            image.SetPixel(0, 0, Color.Red);
            return new DesktopFrame { DesktopImage = image, Bounds = Bounds, FrameVersion = 17, FrameTimestamp = 100, CaptureTimestampUtc = Timestamp };
        }
        public void Dispose() => Disposed = true;
    }
}
