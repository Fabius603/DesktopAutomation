using System.Collections.Generic;
using System.Drawing;

namespace TaskAutomation.Events
{
    /// <summary>
    /// No-op-Implementierung von <see cref="IDesktopResultOverlay"/> für Nicht-WPF-Umgebungen (Tests, Console).
    /// </summary>
    public sealed class NullDesktopResultOverlay : IDesktopResultOverlay
    {
        public void ShowResult(IReadOnlyList<(Point Center, Rectangle? BoundingBox)> allDetections) { }
        public void Clear() { }
        public void ShowText(string stepKey, string text, float fontSize,
                             byte r, byte g, byte b, byte a,
                             int desktopIndex, int offsetX, int offsetY,
                             int durationMs, bool clearOnJobEnd) { }
        public void ClearText() { }
        public void OnJobEnded() { }
        public void Dispose() { }
    }
}
