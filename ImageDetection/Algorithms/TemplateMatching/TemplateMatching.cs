using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageDetection.Algorithms.TemplateMatching
{
    public class TemplateMatching
    {
        private readonly TemplateMatchModes _templateMatchMode;
        private Mat _template { get; set; }
        private bool _multiplePoints = false;
        private double _threshold = 0.9;
        private int _suppressionRadius = 10;

        public TemplateMatching(TemplateMatchModes templateMatchMode)
        {
            _templateMatchMode = templateMatchMode;
        }

        public TemplateMatchingResult Detect(Mat rawSource)
        {
            using var sourceMat = NormalizeImage(rawSource);
            using var resultMat = new Mat();

            Cv2.MatchTemplate(sourceMat, _template, resultMat, _templateMatchMode);

            return !_multiplePoints
                ? DetectSinglePoint(resultMat)
                : DetectMultiplePoints(resultMat);
        }

        private TemplateMatchingResult DetectSinglePoint(Mat resultMat)
        {
            try
            {
                Cv2.MinMaxLoc(resultMat, out double minVal, out double maxVal, out Point minLoc, out Point maxLoc);

                var x = 0;
                var y = 0;

                // Extract pattern location 
                if (_templateMatchMode == TemplateMatchModes.SqDiff ||
                    _templateMatchMode == TemplateMatchModes.SqDiffNormed)
                {
                    x = minLoc.X;
                    y = minLoc.Y;
                }
                else
                {
                    x = maxLoc.X;
                    y = maxLoc.Y;
                }

                int pointX = (int)(x + Convert.ToDouble(_template.Width) / 2);
                int pointY = (int)(y + Convert.ToDouble(_template.Height) / 2);

                return new TemplateMatchingResult
                {
                    Success = maxVal >= _threshold,
                    CenterPoint = new Point(pointX, pointY),
                    Confidence = maxVal * 100,
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return new TemplateMatchingResult
                {
                    Success = false
                };
            }
        }

        private TemplateMatchingResult DetectMultiplePoints(Mat resultMat)
        {
            var candidates = new List<(Point pt, float score)>();

            for (int y = 0; y < resultMat.Rows; y++)
            {
                for (int x = 0; x < resultMat.Cols; x++)
                {
                    float value = resultMat.At<float>(y, x);
                    if (value >= _threshold)
                        candidates.Add((new Point(x, y), value));
                }
            }

            var finalMatches = new List<Point>();

            foreach (var (pt, _) in candidates.OrderByDescending(c => c.score))
            {
                bool suppressed = finalMatches.Any(existing =>
                    Math.Abs(existing.X - pt.X) <= _suppressionRadius &&
                    Math.Abs(existing.Y - pt.Y) <= _suppressionRadius);

                var centerPoint = new Point(
                    pt.X + _template.Width / 2,
                    pt.Y + _template.Height / 2
                );
                finalMatches.Add(centerPoint);
            }

            return new TemplateMatchingResult
            {
                Success = finalMatches.Count > 0,
                CenterPoint = finalMatches.Count > 0 ? new Point(finalMatches[0].X, finalMatches[0].Y) : new Point(0, 0),
                Points = finalMatches,
                MultiplePoints = true,
                Confidence = (finalMatches.Count > 0 ? candidates.Max(c => c.score) : 0) * 100,
            };
        }

        public void SetThreshold(double threshold)
        {
            if (threshold < 0 || threshold > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be between 0 and 1.");
            }
            this._threshold = threshold;
        }

        public void SetMultiplePoints(bool multiplePoints)
        {
            this._multiplePoints = multiplePoints;
        }

        public void SetTemplate(Mat template)
        {
            _template?.Dispose();

            _template = NormalizeImage(template);
        }


        private Mat NormalizeImage(Mat input)
        {
            Mat intermediate = input;

            if (input.Depth() != MatType.CV_8U)
            {
                intermediate = new Mat();
                input.ConvertTo(intermediate, MatType.CV_8U);
            }

            if (intermediate.Channels() > 1)
            {
                var gray = new Mat();
                Cv2.CvtColor(intermediate, gray, ColorConversionCodes.BGR2GRAY);
                if (intermediate != input)
                    intermediate.Dispose();
                return gray;
            }

            return intermediate != input ? intermediate : input.Clone();
        }
    }
}
