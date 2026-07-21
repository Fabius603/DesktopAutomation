using System.Drawing;
using System.Drawing.Drawing2D;

namespace TaskAutomation.Steps
{
    internal static class DetectionResultDrawing
    {
        private static readonly Color ColorBest = Color.OrangeRed;
        private static readonly Color ColorOther = Color.LimeGreen;
        private const int Thickness = 2;
        private const int Radius = 5;

        public static Bitmap Draw(Bitmap source, IReadOnlyList<DetectionItem> detections, Point captureOffset)
        {
            var bitmap = (Bitmap)source.Clone();
            if (detections.Count == 0)
                return bitmap;

            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            for (var i = 1; i < detections.Count; i++)
                DrawSingle(graphics, detections[i].Center, detections[i].BoundingBox, captureOffset, ColorOther);

            DrawSingle(graphics, detections[0].Center, detections[0].BoundingBox, captureOffset, ColorBest);
            return bitmap;
        }

        private static void DrawSingle(Graphics graphics, Point globalCenter, Rectangle? globalBoundingBox, Point captureOffset, Color color)
        {
            if (globalBoundingBox.HasValue)
            {
                var box = globalBoundingBox.Value;
                box.Offset(-captureOffset.X, -captureOffset.Y);
                using var pen = new Pen(color, Thickness);
                graphics.DrawRectangle(pen, box);
            }

            var localCenter = new Point(globalCenter.X - captureOffset.X, globalCenter.Y - captureOffset.Y);
            using var brush = new SolidBrush(color);
            graphics.FillEllipse(
                brush,
                localCenter.X - Radius,
                localCenter.Y - Radius,
                Radius * 2,
                Radius * 2);
        }
    }
}
