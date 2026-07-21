using System;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    public sealed record CaptureFrame : ICaptureStepResult
    {
        public System.Drawing.Bitmap? Image { get; init; }
        public System.Drawing.Rectangle Bounds { get; init; }
        public System.Drawing.Point Offset { get; init; }
        public bool IsFresh { get; init; } = true;
        public DateTime CaptureTimestampUtc { get; init; } = DateTime.UtcNow;
        public bool HasImage => Image is not null;
        public static readonly CaptureFrame Default = new();
    }

    /// <summary>
    /// Anwendungsweiter Singleton-Dienst für den Desktop-Screenshot über DXGI Desktop Duplication.
    /// Verwaltet intern genau eine <see cref="ImageCapture.DesktopDuplication.DesktopDuplicator"/>-Instanz
    /// pro Monitor-Index und serialisiert gleichzeitige Zugriffe thread-safe.
    /// </summary>
    public interface IDesktopCaptureService : IDisposable
    {
        /// <summary>
        /// Nimmt einen Frame vom angegebenen Monitor auf und gibt das Ergebnis zurück.
        /// Blockiert, bis ein Frame verfügbar ist (max. ~50 ms je Retry-Versuch).
        /// Wenn <paramref name="captureCursor"/> true ist, wird der aktuelle Mauszeiger
        /// in das Bild eingeblendet.
        /// </summary>
        Task<CaptureFrame> CaptureAsync(int monitorIdx, CancellationToken ct, bool captureCursor = false);
    }
}
