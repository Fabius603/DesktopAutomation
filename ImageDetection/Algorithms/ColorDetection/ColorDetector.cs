using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using ImageDetection.Model;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using CvRect = OpenCvSharp.Rect;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;

namespace ImageDetection.Algorithms.ColorDetection
{
    public sealed class ColorDetectionOptions
    {
        public string ColorHex { get; set; } = "#FF0000";
        public double ConfidenceThreshold { get; set; } = 0.90;
        public int MinSize { get; set; } = 25;
        public int MaxSize { get; set; } = int.MaxValue;
        public CvRect ROI { get; set; } = new(0, 0, 0, 0);
        public bool EnableROI { get; set; }
    }

    public sealed class ColorDetector
    {
        public IDetectionResult Detect(Bitmap bitmap, ColorDetectionOptions options, CancellationToken ct = default)
        {
            if (bitmap is null)
                return new DetectionResult { Success = false, Confidence = 0 };

            using var sourceMat = BitmapConverter.ToMat(bitmap);
            return Detect(sourceMat, options, ct);
        }

        public IDetectionResult Detect(Mat sourceMat, ColorDetectionOptions options, CancellationToken ct = default)
        {
            if (sourceMat is null || sourceMat.Empty())
                return new DetectionResult { Success = false, Confidence = 0 };

            ValidateOptions(options);

            using var sourceBgr = ToBgr(sourceMat);
            var roi = ResolveRoi(options, sourceBgr.Width, sourceBgr.Height);
            using var roiMat = new Mat(sourceBgr, roi);

            var matches = DetectColor(roiMat, roi, options);

            if (matches.Count == 0)
                return new DetectionResult { Success = false, Confidence = 0 };

            var allResults = matches
                .Select(m => new DetectionResult
                {
                    Success = true,
                    CenterPoint = m.Center,
                    BoundingBox = m.BoundingBox,
                    Confidence = (float)m.Confidence
                })
                .Cast<IDetectionResult>()
                .ToList();

            var best = (DetectionResult)allResults[0];
            best.AllResults = allResults;
            return best;
        }

        private static void ValidateOptions(ColorDetectionOptions options)
        {
            if (options.ConfidenceThreshold is < 0 or > 1)
                throw new InvalidOperationException("ColorDetection threshold must be between 0 and 1.");

            if (options.MinSize <= 0)
                throw new InvalidOperationException("ColorDetection MinSize must be greater than 0.");

            if (options.MaxSize <= 0)
                throw new InvalidOperationException("ColorDetection MaxSize must be greater than 0.");
            
            if (options.MaxSize < options.MinSize)
                throw new InvalidOperationException("ColorDetection MaxSize must be greater than or equal to MinSize.");
        }

        private static CvRect ResolveRoi(ColorDetectionOptions options, int imageWidth, int imageHeight)
        {
            if (!options.EnableROI || options.ROI.Width <= 0 || options.ROI.Height <= 0)
                return new CvRect(0, 0, imageWidth, imageHeight);

            var x = Math.Clamp(options.ROI.X, 0, imageWidth - 1);
            var y = Math.Clamp(options.ROI.Y, 0, imageHeight - 1);
            var right = Math.Clamp(options.ROI.X + options.ROI.Width, x + 1, imageWidth);
            var bottom = Math.Clamp(options.ROI.Y + options.ROI.Height, y + 1, imageHeight);
            return new CvRect(x, y, right - x, bottom - y);
        }

        private static Mat ToBgr(Mat source)
        {
            var bgr = new Mat();

            switch (source.Channels())
            {
                case 1:
                    Cv2.CvtColor(source, bgr, ColorConversionCodes.GRAY2BGR);
                    break;

                case 3:
                    source.CopyTo(bgr);
                    break;

                case 4:
                    Cv2.CvtColor(source, bgr, ColorConversionCodes.BGRA2BGR);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported image channel count: {source.Channels()}");
            }

            return bgr;
        }

        private static IReadOnlyList<ColorDetectionMatch> DetectColor(
            Mat roiMat,
            CvRect roi,
            ColorDetectionOptions options)
        {
            var target = ParseColor(options.ColorHex);
            var maxDistance = Math.Sqrt(3 * 255 * 255);

            using var mask = BuildFastColorMask(roiMat, target, options.ConfidenceThreshold);

            Cv2.FindContours(
                mask,
                out OpenCvSharp.Point[][] contours,
                out _,
                RetrievalModes.External,
                ContourApproximationModes.ApproxSimple);

            var matches = new List<ColorDetectionMatch>();

            foreach (var contour in contours)
            {
                var boundsLocal = Cv2.BoundingRect(contour);

                if (boundsLocal.Width <= 0 || boundsLocal.Height <= 0)
                    continue;

                using var componentMask = new Mat(mask, boundsLocal);
                var area = Cv2.CountNonZero(componentMask);

                if (area < options.MinSize)
                    continue;

                if (area > options.MaxSize)
                    continue;

                using var componentImage = new Mat(roiMat, boundsLocal);
                var mean = Cv2.Mean(componentImage, componentMask);
                var confidence = CalculateColorConfidence(mean, target, maxDistance);
                var moments = Cv2.Moments(contour);

                var bounds = new DrawingRectangle(
                    boundsLocal.X + roi.X,
                    boundsLocal.Y + roi.Y,
                    boundsLocal.Width,
                    boundsLocal.Height);

                var center = moments.M00 > 0
                    ? new DrawingPoint(
                        (int)Math.Round(moments.M10 / moments.M00) + roi.X,
                        (int)Math.Round(moments.M01 / moments.M00) + roi.Y)
                    : new DrawingPoint(
                        bounds.X + bounds.Width / 2,
                        bounds.Y + bounds.Height / 2);

                matches.Add(new ColorDetectionMatch(center, bounds, confidence, area));
            }

            return matches
                .OrderByDescending(m => m.Confidence)
                .ThenByDescending(m => m.Area)
                .ToList();
        }

        private static Mat BuildFastColorMask(Mat image, DrawingColor target, double threshold)
        {
            var tolerance = (int)Math.Round((1.0 - threshold) * 255);

            var lower = new Scalar(
                Math.Max(0, target.B - tolerance),
                Math.Max(0, target.G - tolerance),
                Math.Max(0, target.R - tolerance));

            var upper = new Scalar(
                Math.Min(255, target.B + tolerance),
                Math.Min(255, target.G + tolerance),
                Math.Min(255, target.R + tolerance));

            var mask = new Mat();
            Cv2.InRange(image, lower, upper, mask);
            return mask;
        }

        private static double CalculateColorConfidence(Scalar mean, DrawingColor target, double maxDistance)
        {
            var distance = Math.Sqrt(
                Math.Pow(mean.Val0 - target.B, 2) +
                Math.Pow(mean.Val1 - target.G, 2) +
                Math.Pow(mean.Val2 - target.R, 2));

            return 1.0 - Math.Clamp(distance / maxDistance, 0.0, 1.0);
        }

        private static DrawingColor ParseColor(string colorHex)
        {
            var value = colorHex.Trim();

            if (value.StartsWith("#", StringComparison.Ordinal))
                value = value[1..];

            if (value.Length == 8)
                value = value[2..];

            if (value.Length != 6 ||
                !int.TryParse(value[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) ||
                !int.TryParse(value.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) ||
                !int.TryParse(value.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
            {
                throw new InvalidOperationException($"Invalid color value '{colorHex}'. Expected #RRGGBB.");
            }

            return new DrawingColor(r, g, b);
        }

        private readonly record struct DrawingColor(int R, int G, int B);

        private sealed record ColorDetectionMatch(
            DrawingPoint Center,
            DrawingRectangle BoundingBox,
            double Confidence,
            int Area);
    }
}
