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

        /// <summary>
        /// Zeigt einen Text an der angegebenen Position auf dem Desktop an.
        /// Läuft der gleiche Step (gleicher <paramref name="stepKey"/>) erneut und der Text ist noch sichtbar,
        /// wird nur der Timer verlängert und kein neues Item gezeichnet.
        /// Leerer <paramref name="text"/> entfernt das Item dieses Steps.
        /// </summary>
        void ShowText(string stepKey, string text, float fontSize,
                      byte r, byte g, byte b, byte a,
                      int desktopIndex, int offsetX, int offsetY,
                      int durationMs, bool clearOnJobEnd);

        /// <summary>
        /// Entfernt den aktuell angezeigten Text aus dem Overlay.
        /// </summary>
        void ClearText();

        /// <summary>
        /// Wird am Ende eines Job-Laufs aufgerufen.
        /// Entfernt Text-Items die mit <c>clearOnJobEnd = true</c> erstellt wurden.
        /// </summary>
        void OnJobEnded();
    }
}
