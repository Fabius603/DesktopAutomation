using System;
using System.Collections.Generic;
using ImageDetection.Model;
using OpenCvSharp;

namespace ImageDetection
{
    public static class DrawResult
    {
        /// <summary>
        /// Zeichnet ein DetectionResult (BoundingBox oder Rechteck um den CenterPoint).
        /// </summary>
        public static Mat DrawDetectionResult(Mat mat, IDetectionResult result)
        {
            if (result == null || !result.Success)
                return mat;

            Scalar color = Scalar.LimeGreen;
            int thickness = 2;
            int radius = 5;

            if (result.CenterPoint != null)
            {
                // Mittelpunkt zeichnen
                var center = new Point(result.CenterPoint.X, result.CenterPoint.Y);
                Cv2.Circle(mat, center, radius, color, -1);
            }

            if (result.BoundingBox.HasValue)
            {
                // BoundingBox liegt schon vor → direkt zeichnen
                var bb = result.BoundingBox.Value;
                var rect = new Rect(bb.X, bb.Y, bb.Width, bb.Height);
                Cv2.Rectangle(mat, rect, color, thickness);
            }

            return mat;
        }
    }
}
