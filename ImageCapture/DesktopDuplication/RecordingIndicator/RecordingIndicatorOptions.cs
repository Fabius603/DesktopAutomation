using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading.Tasks;

namespace ImageCapture.DesktopDuplication.RecordingIndicator
{
    public sealed class RecordingIndicatorOptions
    {
        public int MonitorIndex = 0;
        public GameOverlay.Drawing.Color Color { get; set; } = new(255, 64, 64, 220);
        public float BorderThickness { get; set; } = 2f;
        public RecordingIndicatorMode Mode { get; set; } = RecordingIndicatorMode.RedBorder;
        public Corner BadgeCorner { get; set; } = Corner.TopLeft;
        public string Label { get; set; } = "REC";
    }
}
