using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDetection.Model
{
    public interface IDetectionResult
    {
        bool Success { get; set; }
        Point CenterPoint { get; set; }
        Rectangle? BoundingBox { get; set; }
        float Confidence { get; set; }
    }
}
