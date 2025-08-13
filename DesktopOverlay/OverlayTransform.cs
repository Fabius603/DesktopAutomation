using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopOverlay
{
    public readonly struct OverlayTransform
    {
        public static readonly OverlayTransform Identity = new OverlayTransform(1f, 1f, 0f, 0f);

        public readonly float Sx; // Scale X (Virtual → Overlay-Local)
        public readonly float Sy; // Scale Y
        public readonly float Ox; // Offset X (Virtual → Overlay-Local)
        public readonly float Oy; // Offset Y

        public OverlayTransform(float sx, float sy, float ox, float oy)
        {
            Sx = sx; Sy = sy; Ox = ox; Oy = oy;
        }

        /// <summary>
        /// Mappt globale virtuelle Pixel (über alle Monitore) in lokale Overlay-Fensterkoordinaten.
        /// overlayBounds.Left/Top werden NICHT eingerechnet, da Graphics local zeichnet.
        /// </summary>
        public static OverlayTransform FromVirtualToOverlayLocal(Rectangle virtualBounds, Rectangle overlayBounds)
        {
            float sx = overlayBounds.Width <= 0 ? 1f : overlayBounds.Width / (float)virtualBounds.Width;
            float sy = overlayBounds.Height <= 0 ? 1f : overlayBounds.Height / (float)virtualBounds.Height;
            float ox = -virtualBounds.Left * sx;
            float oy = -virtualBounds.Top * sy;
            return new OverlayTransform(sx, sy, ox, oy);
        }

        public (float x, float y) Apply(float globalX, float globalY)
            => (globalX * Sx + Ox, globalY * Sy + Oy);
    }
}
