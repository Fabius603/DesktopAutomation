using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GameOverlay.Drawing;
using Graphics = GameOverlay.Drawing.Graphics;

namespace DesktopOverlay
{
    /// <summary>
    /// Interface für Overlay-Items mit Lebenszyklus-Hooks.
    /// </summary>
    public interface IOverlayItem : IDisposable
    {
        string Id { get; }
        bool Visible { get; set; }

        /// <summary>
        /// Legt DirectX-Ressourcen an oder neu an (bei Device-Verlust).
        /// Wird im SetupGraphics-Event aufgerufen.
        /// </summary>
        /// <param name="gfx">Graphics-Kontext</param>
        /// <param name="recreate">True, wenn Ressourcen nach Device-Verlust neu erstellt werden</param>
        void Setup(Graphics gfx, bool recreate);

        /// <summary>
        /// Zeichnet das Item.
        /// </summary>
        void Draw(Graphics gfx);
    }
}
