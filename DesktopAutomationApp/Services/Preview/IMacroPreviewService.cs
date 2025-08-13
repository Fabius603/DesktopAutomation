using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Makros;
using System.Drawing;

namespace DesktopAutomationApp.Services.Preview
{
    public interface IMacroPreviewService
    {
        /// <summary>Erzeugt Items für Übersicht/Playback.</summary>
        MacroPreviewService.PreviewResult Build(Makro makro, Rectangle virtualBounds, Rectangle overlayBounds);
    }
}
