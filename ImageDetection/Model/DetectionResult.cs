using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDetection.Model
{
    public class DetectionResult : IDetectionResult
    {
        public bool Success { get; set; }
        public Point CenterPoint { get; set; }
        public Rectangle? BoundingBox { get; set; }
        public float Confidence { get; set; }

        public DetectionResult()
        {
            Success = false;
            CenterPoint = Point.Empty;
            BoundingBox = null;
            Confidence = 0f;
        }
    }
}
