using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using ImageDetection.Model;

namespace ImageDetection
{
    public static class DrawResult
    {
        private static readonly Color ColorBest  = Color.OrangeRed;
        private static readonly Color ColorOther = Color.LimeGreen;
        private const int Thickness = 2;
        private const int Radius    = 5;

        /// <summary>
        /// Zeichnet alle Ergebnisse aus <see cref="IDetectionResult.AllResults"/> in Grün
        /// und das beste Ergebnis (das übergebene <paramref name="result"/> selbst) in Orange-Rot.
        /// Falls AllResults leer ist, wird nur das Einzelergebnis gezeichnet.
        /// </summary>
        public static Bitmap DrawDetectionResult(Bitmap bitmap, IDetectionResult result)
        {
            if (result == null || !result.Success || bitmap == null)
                return bitmap;

            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var all = result.AllResults;

            if (all == null || all.Count == 0)
            {
                // Nur Einzelergebnis – als bestes zeichnen
                DrawSingle(graphics, result, ColorBest);
            }
            else
            {
                // Alle anderen zuerst (grün), dann bestes oben drauf (orange-rot)
                foreach (var r in all)
                    if (r != result)
                        DrawSingle(graphics, r, ColorOther);

                DrawSingle(graphics, result, ColorBest);
            }

            return bitmap;
        }

        private static void DrawSingle(Graphics g, IDetectionResult r, Color color)
        {
            if (r.BoundingBox.HasValue)
            {
                using var pen = new Pen(color, Thickness);
                g.DrawRectangle(pen, r.BoundingBox.Value);
            }

            using var brush = new SolidBrush(color);
            g.FillEllipse(brush,
                r.CenterPoint.X - Radius,
                r.CenterPoint.Y - Radius,
                Radius * 2, Radius * 2);
        }
    }
}
