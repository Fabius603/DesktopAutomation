using ImageHelperMethods;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDetection.Model
{
    public sealed class YoloDetectionResult : IDetectionResult
    {
        public bool Success { get; set; }
        public Point CenterPointInImage { get; set; }
        public Point CenterPointOnDesktop { get; set; }
        public Rectangle? BoundingBox { get; set; }
        public float Confidence { get; set; }

        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Hilfsmethode, um aus Bildkoordinaten absolute virtuelle Koordinaten (0..65535) zu machen.
        /// </summary>
        public (double absX, double absY) ToAbsoluteVirtualFromGlobalTopLeft(Point globalTopLeft)
        {
            var globalX = globalTopLeft.X + CenterPointInImage.X;
            var globalY = globalTopLeft.Y + CenterPointInImage.Y;
            return ScreenHelper.ToAbsoluteVirtual(globalX, globalY);
        }
    }

}
