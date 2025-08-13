using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DesktopOverlay.OverlayItems
{
    public interface ITimedOverlayItem : IOverlayItem
    {
        /// <summary>Aktualisiert den Animationszustand. t in Sekunden seit Start des Playbacks.</summary>
        void Update(double t);
    }
}
