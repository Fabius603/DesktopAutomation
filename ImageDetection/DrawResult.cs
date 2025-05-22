using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageDetection.Algorithms.TemplateMatching;
using OpenCvSharp;

namespace ImageDetection
{
    public static class DrawResult
    {
        public static Mat DrawTemplateMatchingResult(Mat mat, TemplateMatchingResult result, Size templateSize)
        {
            var output = mat.Clone();

            if (!result.Success)
                return output;

            Scalar color = result.MultiplePoints ? Scalar.LimeGreen : Scalar.Red;
            int thickness = 2;

            if (result.MultiplePoints && result.Points != null)
            {
                foreach (var center in result.Points)
                {
                    var rect = GetRectFromCenter(center, templateSize);
                    Cv2.Rectangle(output, rect, color, thickness);
                }
            }
            else
            {
                var rect = GetRectFromCenter(result.CenterPoint, templateSize);
                Cv2.Rectangle(output, rect, color, thickness);
            }

            return output;
        }

        private static Rect GetRectFromCenter(Point center, Size size)
        {
            int x = center.X - size.Width / 2;
            int y = center.Y - size.Height / 2;
            return new Rect(x, y, size.Width, size.Height);
        }
    }
}
