using System.Collections.Generic;
using System.Drawing;

namespace ImageDetection.Model
{
    public class DetectionResult : IDetectionResult
    {
        public bool Success { get; set; }
        public Point CenterPoint { get; set; }
        public Rectangle? BoundingBox { get; set; }
        public float Confidence { get; set; }
        public IReadOnlyList<IDetectionResult> AllResults { get; set; } = [];

        public DetectionResult()
        {
            Success = false;
            CenterPoint = Point.Empty;
            BoundingBox = null;
            Confidence = 0f;
        }
    }
}
