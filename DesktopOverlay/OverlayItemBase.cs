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
    /// Abstrakte Basisklasse mit Standard-Implementierung von Id und Sichtbarkeit.
    /// </summary>
    public abstract class OverlayItemBase : IOverlayItem
    {
        public string Id { get; }
        public bool Visible { get; set; } = true;

        protected OverlayItemBase(string id)
        {
            Id = id;
        }

        public virtual void Setup(Graphics gfx, bool recreate)
        {
            // Standard: keine Ressourcen.
            // Konkrete Items überschreiben diesen Hook.
        }

        public abstract void Draw(Graphics gfx);
        public abstract void Dispose();
    }
}
