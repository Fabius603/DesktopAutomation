using System;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using TaskAutomation.Contracts.Geometry;

namespace TaskAutomation.Steps
{
    public sealed record DesktopCaptureRequest(
        int MonitorIndex,
        string? MonitorDeviceName = null,
        bool CaptureCursor = false,
        bool WaitForNewFrame = true,
        int TimeoutMilliseconds = DesktopDuplicationSettings.DefaultTimeoutMilliseconds,
        bool AllowCachedFallback = true)
    {
        public void Validate()
        {
            if (MonitorIndex < 0) throw new ArgumentOutOfRangeException(nameof(MonitorIndex));
            if (TimeoutMilliseconds is < 1 or > DesktopDuplicationSettings.MaximumTimeoutMilliseconds) throw new ArgumentOutOfRangeException(nameof(TimeoutMilliseconds));
        }
    }
    public sealed record CaptureFrame : ICaptureStepResult
    {
        public System.Drawing.Bitmap? Image { get; init; }
        public PixelRegion Bounds { get; init; }
        public PixelPoint Offset { get; init; }
        public bool IsFresh { get; init; } = true;
        public DateTime CaptureTimestampUtc { get; init; } = DateTime.UtcNow;
        public long FrameVersion { get; init; }
        public long FrameTimestamp { get; init; }
        public bool HasImage => Image is not null;
        public static readonly CaptureFrame Default = new();
    }

    /// <summary>
    /// Anwendungsweiter Singleton-Dienst für den Desktop-Screenshot über DXGI Desktop Duplication.
    /// Verwaltet intern genau eine <see cref="ImageCapture.DesktopDuplication.DesktopDuplicator"/>-Instanz
    /// pro Monitor-Kennung und serialisiert gleichzeitige Zugriffe thread-safe.
    /// </summary>
    public interface IDesktopCaptureService : IDisposable
    {
        /// <summary>
        /// Nimmt einen Frame vom angegebenen Monitor auf und gibt das Ergebnis zurück.
        /// Wartet innerhalb des Standardzeitlimits auf ein neues Bild und erlaubt ein Cache-Fallback.
        /// Wenn <paramref name="captureCursor"/> true ist, wird der aktuelle Mauszeiger
        /// anhand der DXGI-Metadaten in das Bild eingeblendet.
        /// </summary>
        Task<CaptureFrame> CaptureAsync(int monitorIdx, CancellationToken ct, bool captureCursor = false)
            => CaptureAsync(new DesktopCaptureRequest(monitorIdx, CaptureCursor: captureCursor), ct);

        Task<CaptureFrame> CaptureAsync(DesktopCaptureRequest request, CancellationToken ct);
    }
}
