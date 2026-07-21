using ImageCapture.DesktopDuplication;
using ImageHelperMethods;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    /// <summary>
    /// Implementierung von <see cref="IDesktopCaptureService"/>.
    /// Eine Instanz dieser Klasse wird als Singleton registriert und teilt sich genau eine
    /// <see cref="DesktopDuplicator"/>-Instanz pro Monitor-Index über alle gleichzeitig
    /// laufenden Jobs hinweg.
    /// </summary>
    public sealed class DesktopCaptureService : IDesktopCaptureService
    {
        private readonly ILogger<DesktopCaptureService> _logger;

        // pro Monitor-Index: ein Semaphor (serialisiert Zugriffe) und ein Duplicator
        private readonly ConcurrentDictionary<int, SemaphoreSlim>   _semaphores  = new();
        private readonly ConcurrentDictionary<int, DesktopDuplicator> _duplicators = new();

        // ── Cursor-Overlay via P/Invoke ───────────────────────────────────────
        private const uint CURSOR_SHOWING = 0x00000001;

        [StructLayout(LayoutKind.Sequential)]
        private struct CursorPoint { public int X, Y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public int    cbSize;
            public uint   flags;
            public IntPtr hCursor;
            public CursorPoint ptScreenPos;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(ref CURSORINFO pci);

        [DllImport("user32.dll")]
        private static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        private static void OverlayCursor(Bitmap bmp, Rectangle monitorBounds)
        {
            var ci = new CURSORINFO { cbSize = Marshal.SizeOf<CURSORINFO>() };
            if (!GetCursorInfo(ref ci) || (ci.flags & CURSOR_SHOWING) == 0) return;

            int cx = ci.ptScreenPos.X - monitorBounds.Left;
            int cy = ci.ptScreenPos.Y - monitorBounds.Top;

            // Cursor liegt außerhalb des aufgenommenen Monitors?
            if (cx < -64 || cy < -64 || cx > bmp.Width + 64 || cy > bmp.Height + 64) return;

            using var g = Graphics.FromImage(bmp);
            IntPtr hdc = g.GetHdc();
            try   { DrawIcon(hdc, cx, cy, ci.hCursor); }
            finally { g.ReleaseHdc(hdc); }
        }

        public DesktopCaptureService(ILogger<DesktopCaptureService> logger)
            => _logger = logger;

        public async Task<CaptureFrame> CaptureAsync(int monitorIdx, CancellationToken ct, bool captureCursor = false)
        {
            var sem = _semaphores.GetOrAdd(monitorIdx, _ => new SemaphoreSlim(1, 1));
            await sem.WaitAsync(ct).ConfigureAwait(false);
            DesktopFrame? frame = null;
            DesktopFrame? cachedFallbackFrame = null;
            try
            {
                ct.ThrowIfCancellationRequested();

                // Lazy-Init des Duplicators (nur beim ersten Aufruf pro Monitor)
                if (!_duplicators.TryGetValue(monitorIdx, out var duplicator))
                {
                    _logger.LogDebug(
                        "DesktopCaptureService: Erstelle DesktopDuplicator für Monitor {MonitorIndex}", monitorIdx);
                    duplicator = CreateDuplicator(monitorIdx);
                    // 16 ms ≈ 60 fps: DXGI blockiert intern bis ein neuer Frame verfügbar ist.
                    _duplicators[monitorIdx] = duplicator;
                    // Warm-up-Pause: DXGI braucht ~100 ms bis der erste Frame bereit ist.
                    await Task.Delay(100, ct).ConfigureAwait(false);
                    ct.ThrowIfCancellationRequested();
                }

                int retryCount  = 0;
                const int maxRetries = 6;

                while (frame == null && retryCount < maxRetries)
                {
                    ct.ThrowIfCancellationRequested();
                    try
                    {
                        frame = duplicator.GetLatestFrame();
                        if (frame?.DesktopImage == null)
                        {
                            frame?.Dispose();
                            frame = null;
                            retryCount++;
                            _logger.LogWarning(
                                "DesktopCaptureService: Kein Bild in Versuch {Attempt}/{Max} (Monitor {MonitorIndex})",
                                retryCount, maxRetries, monitorIdx);
                            await Task.Delay(16, ct).ConfigureAwait(false);
                            continue;
                        }

                        if (!frame.IsFresh)
                        {
                            cachedFallbackFrame?.Dispose();
                            cachedFallbackFrame = frame;
                            frame = null;
                            retryCount++;
                            _logger.LogDebug(
                                "DesktopCaptureService: Gecachten Frame in Versuch {Attempt}/{Max} erhalten, warte auf frischen Frame (Monitor {MonitorIndex})",
                                retryCount, maxRetries, monitorIdx);
                            await Task.Delay(16, ct).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex) when (retryCount < maxRetries - 1)
                    {
                        retryCount++;
                        frame?.Dispose();
                        frame = null;
                        _logger.LogWarning(ex,
                            "DesktopCaptureService: Capture fehlgeschlagen in Versuch {Attempt}/{Max} (Monitor {MonitorIndex})",
                            retryCount, maxRetries, monitorIdx);

                        if (ex is ObjectDisposedException || ex is DesktopDuplicationException)
                        {
                            duplicator = RecreateDuplicator(monitorIdx);
                            await Task.Delay(100, ct).ConfigureAwait(false);
                        }

                        await Task.Delay(50, ct).ConfigureAwait(false);
                    }
                }

                if (frame == null && cachedFallbackFrame != null)
                {
                    frame = cachedFallbackFrame;
                    cachedFallbackFrame = null;
                    _logger.LogWarning(
                        "DesktopCaptureService: Kein frischer Frame nach {Max} Versuchen, verwende letzten gecachten Frame (Monitor {MonitorIndex})",
                        maxRetries, monitorIdx);
                }
                else
                {
                    cachedFallbackFrame?.Dispose();
                }

                if (frame?.DesktopImage == null)
                {
                    frame?.Dispose();
                    throw new InvalidOperationException(
                        $"Kein Desktop-Bild nach {maxRetries} Versuchen für Monitor {monitorIdx}.");
                }

                using (frame)
                {
                    // Eigentumsübertragung: frame.DesktopImage wird direkt übernommen.
                    var bitmap = frame.DesktopImage;
                    frame.DesktopImage = null;

                    var screenBounds = ScreenHelper.GetDesktopBounds(monitorIdx);
                    var offset       = new System.Drawing.Point(screenBounds.Left, screenBounds.Top);

                    if (captureCursor)
                        OverlayCursor(bitmap, screenBounds);

                    _logger.LogInformation(
                        "DesktopCaptureService: Aufgenommen {W}x{H} bei Offset ({X},{Y})",
                        bitmap.Width, bitmap.Height, offset.X, offset.Y);

                    return new CaptureFrame
                    {
                        Image       = bitmap,
                        Bounds      = screenBounds,
                        Offset      = offset,
                        IsFresh     = frame.IsFresh,
                        CaptureTimestampUtc = frame.CaptureTimestampUtc == DateTime.MinValue
                            ? DateTime.UtcNow
                            : frame.CaptureTimestampUtc
                    };
                }
            }
            finally
            {
                frame?.Dispose();
                cachedFallbackFrame?.Dispose();
                sem.Release();
            }
        }

        private DesktopDuplicator CreateDuplicator(int monitorIdx)
        {
            _logger.LogDebug(
                "DesktopCaptureService: Erstelle DesktopDuplicator fÃ¼r Monitor {MonitorIndex}", monitorIdx);

            var duplicator = new DesktopDuplicator(monitorIdx);
            duplicator.SetFrameTimeout(16);
            return duplicator;
        }

        private DesktopDuplicator RecreateDuplicator(int monitorIdx)
        {
            if (_duplicators.TryRemove(monitorIdx, out var oldDuplicator))
            {
                try { oldDuplicator.Dispose(); } catch { /* best-effort */ }
            }

            _logger.LogWarning(
                "DesktopCaptureService: DesktopDuplicator fÃ¼r Monitor {MonitorIndex} wird neu erstellt.",
                monitorIdx);

            var duplicator = CreateDuplicator(monitorIdx);
            _duplicators[monitorIdx] = duplicator;
            return duplicator;
        }

        public void Dispose()
        {
            foreach (var d in _duplicators.Values)
                try { d.Dispose(); } catch { /* best-effort */ }
            foreach (var s in _semaphores.Values)
                try { s.Dispose(); } catch { /* best-effort */ }
            _duplicators.Clear();
            _semaphores.Clear();
        }
    }
}
