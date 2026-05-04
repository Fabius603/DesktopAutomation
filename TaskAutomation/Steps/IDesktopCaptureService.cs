using System;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
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
        Task<CaptureResult> CaptureAsync(int monitorIdx, CancellationToken ct, bool captureCursor = false);
    }
}
