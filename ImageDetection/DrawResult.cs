using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using ImageDetection.Model;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace ImageDetection
{
    public static class DrawResult
    {
        /// <summary>
        /// Zeichnet ein DetectionResult (BoundingBox oder Rechteck um den CenterPoint) direkt auf ein Bitmap.
        /// </summary>
        public static Bitmap DrawDetectionResult(Bitmap bitmap, IDetectionResult result)
        {
            if (result == null || !result.Success || bitmap == null)
                return bitmap;

            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                
                var color = Color.LimeGreen;
                int thickness = 2;
                int radius = 5;

                if (result.CenterPoint != null)
                {
                    // Mittelpunkt zeichnen
                    using (var brush = new SolidBrush(color))
                    {
                        var centerX = result.CenterPoint.X - radius;
                        var centerY = result.CenterPoint.Y - radius;
                        graphics.FillEllipse(brush, centerX, centerY, radius * 2, radius * 2);
                    }
                }

                if (result.BoundingBox.HasValue)
                {
                    // BoundingBox zeichnen
                    var bb = result.BoundingBox.Value;
                    using (var pen = new Pen(color, thickness))
                    {
                        graphics.DrawRectangle(pen, bb);
                    }
                }
            }

            return bitmap;
        }

        /// <summary>
        /// Legacy-Methode für Kompatibilität mit OpenCV Mat - konvertiert zu Bitmap, zeichnet und konvertiert zurück.
        /// Warnung: Diese Methode ist weniger effizient aufgrund der Konvertierungen.
        /// </summary>
        [Obsolete("Diese Methode ist weniger effizient. Verwenden Sie die Bitmap-Version für bessere Performance.")]
        public static Mat DrawDetectionResult(Mat mat, IDetectionResult result)
        {
            if (result == null || !result.Success || mat == null)
                return mat;

            using (var bitmap = mat.ToBitmap())
            {
                DrawDetectionResult(bitmap, result);
                return bitmap.ToMat();
            }
        }
    }
}
