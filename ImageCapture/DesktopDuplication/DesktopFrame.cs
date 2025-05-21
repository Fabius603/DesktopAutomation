using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageCapture.DesktopDuplication
{
    public class DesktopFrame : IDisposable
    {
        public Bitmap DesktopImage { get; set; }
        public MovedRegion[] MovedRegions { get; set; }
        public Rectangle[] UpdatedRegions { get; set; }
        public bool CursorVisible { get; set; }
        public Point CursorLocation { get; set; }

        public void Dispose()
        {
            DesktopImage?.Dispose();
            DesktopImage = null;
            MovedRegions = null;
            UpdatedRegions = null;
            CursorLocation = Point.Empty;
        }

        public DesktopFrame Clone()
        {
            return new DesktopFrame
            {
                DesktopImage = DesktopImage,
                MovedRegions = MovedRegions != null ? (MovedRegion[])MovedRegions.Clone() : null,
                UpdatedRegions = UpdatedRegions != null ? (Rectangle[])UpdatedRegions.Clone() : null,
                CursorVisible = CursorVisible,
                CursorLocation = CursorLocation
            };
        }
    }
}
