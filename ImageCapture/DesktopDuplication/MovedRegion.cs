using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageCapture.DesktopDuplication
{
    /// <summary>
    /// Describes the movement of an image rectangle within a desktop frame.
    /// </summary>
    /// <remarks>
    /// Move regions are always non-stretched regions so the source is always the same size as the destination.
    /// </remarks>
    public struct MovedRegion
    {
        public Point Source;
        public Rectangle Destination;
    }
}
