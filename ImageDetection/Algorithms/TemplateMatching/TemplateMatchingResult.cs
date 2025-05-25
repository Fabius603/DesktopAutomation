using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace ImageDetection.Algorithms.TemplateMatching
{
    public class TemplateMatchingResult : IDisposable
    {
        public bool Success { get; set; } = false;
        public double Confidence { get; set; } = 0;
        public Point CenterPoint { get; set; } = new Point(0, 0);
        public List<Point> Points { get; set; } = new List<Point>();
        public bool MultiplePoints { get; set; } = false;
        public Size TemplateSize { get; set; } = new Size(0, 0);

        public void Dispose()
        {
            Points?.Clear();
            Points = null;
            CenterPoint = new Point(0, 0);
            Success = false;
            Confidence = 0;
        }
    }
}
