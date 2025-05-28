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
        public Point CenterPointOnDesktop { get; set; } = new Point(0, 0);
        public List<Point> PointsOnDesktop { get; set; } = new List<Point>();

        public void Dispose()
        {
            Points?.Clear();
            CenterPoint = new Point(0, 0);
            Success = false;
            Confidence = 0;
            CenterPointOnDesktop = new Point(0, 0);
            PointsOnDesktop.Clear();
        }
    }
}
