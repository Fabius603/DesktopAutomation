using System.Collections.Generic;
using System.Drawing;

namespace TaskAutomation.Events
{
    /// <summary>
    /// Zeichnet alle Ergebnisse eines Detection-Steps (BoundingBox + Mittelpunkt)
    /// direkt auf den Desktop via transparentem Overlay-Fenster.
    /// </summary>
    public interface IDesktopResultOverlay : IDisposable
    {
        /// <summary>
        /// Zeigt alle Erkennungsergebnisse auf dem Desktop an.
        /// Index 0 wird als bestes Ergebnis (Akzentfarbe) hervorgehoben.
        /// Ersetzt alle vorher angezeigten Ergebnisse.
        /// </summary>
        void ShowResult(IReadOnlyList<(Point Center, Rectangle? BoundingBox)> allDetections);

        /// <summary>
        /// Entfernt alle aktuell angezeigten Erkennungsergebnis-Items aus dem Overlay.
        /// </summary>
        void Clear();
    }
}
