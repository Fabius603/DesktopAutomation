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
        public void Dispose() { }
    }
}
