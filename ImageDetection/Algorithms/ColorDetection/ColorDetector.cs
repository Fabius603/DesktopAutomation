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
        public int MinWidth { get; set; } = 1;
        public int MinHeight { get; set; } = 1;
        public int DownscaleFactor { get; set; } = 1;
        public CvRect ROI { get; set; } = new(0, 0, 0, 0);
        public bool EnableROI { get; set; }
    }

    public sealed class ColorDetector : IDisposable
    {
        private readonly Mat _bgr = new();
        private readonly Mat _scaled = new();
        private readonly Mat _mask = new();
        private readonly List<ColorDetectionMatch> _matches = new();
        private bool _disposed;

        public IDetectionResult Detect(Bitmap bitmap, ColorDetectionOptions options, CancellationToken ct = default)
        {
            if (bitmap is null)
                return new DetectionResult { Success = false, Confidence = 0 };

            ValidateOptions(options);
            var roi = ResolveRoi(options, bitmap.Width, bitmap.Height);
            var usesPartialRoi = roi.X != 0 || roi.Y != 0
                || roi.Width != bitmap.Width || roi.Height != bitmap.Height;

            if (usesPartialRoi)
            {
                using var cropped = bitmap.Clone(
                    new DrawingRectangle(roi.X, roi.Y, roi.Width, roi.Height),
                    bitmap.PixelFormat);
                var croppedOptions = CopyWithoutRoi(options);
                var result = Detect(cropped, croppedOptions, ct);
                OffsetResult(result, roi.X, roi.Y);
                return result;
            }

            using var sourceMat = BitmapConverter.ToMat(bitmap);
            return Detect(sourceMat, options, ct);
        }

        private static ColorDetectionOptions CopyWithoutRoi(ColorDetectionOptions source) => new()
        {
            ColorHex = source.ColorHex,
            ConfidenceThreshold = source.ConfidenceThreshold,
            MinSize = source.MinSize,
            MaxSize = source.MaxSize,
            MinWidth = source.MinWidth,
            MinHeight = source.MinHeight,
            DownscaleFactor = source.DownscaleFactor,
            EnableROI = false
        };

        private static void OffsetResult(IDetectionResult result, int offsetX, int offsetY)
        {
            var results = result.AllResults.Count > 0
                ? result.AllResults.Distinct()
                : new[] { result };
            foreach (var item in results)
            {
                item.CenterPoint = new DrawingPoint(item.CenterPoint.X + offsetX, item.CenterPoint.Y + offsetY);
                if (item.BoundingBox is DrawingRectangle box)
                {
                    box.Offset(offsetX, offsetY);
                    item.BoundingBox = box;
                }
            }
        }

        public IDetectionResult Detect(Mat sourceMat, ColorDetectionOptions options, CancellationToken ct = default)
        {
            if (sourceMat is null || sourceMat.Empty())
                return new DetectionResult { Success = false, Confidence = 0 };

            ValidateOptions(options);

            ToBgr(sourceMat, _bgr);
            var roi = ResolveRoi(options, _bgr.Width, _bgr.Height);
            using var roiMat = new Mat(_bgr, roi);

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

            if (options.MinWidth <= 0)
                throw new InvalidOperationException("ColorDetection MinWidth must be greater than 0.");

            if (options.MinHeight <= 0)
                throw new InvalidOperationException("ColorDetection MinHeight must be greater than 0.");

            if (options.DownscaleFactor <= 0)
                throw new InvalidOperationException("ColorDetection DownscaleFactor must be greater than 0.");
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

        private static void ToBgr(Mat source, Mat destination)
        {
            switch (source.Channels())
            {
                case 1:
                    Cv2.CvtColor(source, destination, ColorConversionCodes.GRAY2BGR);
                    break;

                case 3:
                    source.CopyTo(destination);
                    break;

                case 4:
                    Cv2.CvtColor(source, destination, ColorConversionCodes.BGRA2BGR);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported image channel count: {source.Channels()}");
            }
        }

        private IReadOnlyList<ColorDetectionMatch> DetectColor(
            Mat roiMat,
            CvRect roi,
            ColorDetectionOptions options)
        {
            var target = ParseColor(options.ColorHex);
            var maxDistance = Math.Sqrt(3 * 255 * 255);
            var workingMat = roiMat;
            var scaleX = 1.0;
            var scaleY = 1.0;

            if (options.DownscaleFactor > 1 &&
                roiMat.Width >= options.DownscaleFactor * 2 &&
                roiMat.Height >= options.DownscaleFactor * 2)
            {
                var scaledWidth = Math.Max(1, roiMat.Width / options.DownscaleFactor);
                var scaledHeight = Math.Max(1, roiMat.Height / options.DownscaleFactor);
                Cv2.Resize(
                    roiMat,
                    _scaled,
                    new OpenCvSharp.Size(scaledWidth, scaledHeight),
                    0,
                    0,
                    InterpolationFlags.Nearest);
                workingMat = _scaled;
                scaleX = roiMat.Width / (double)workingMat.Width;
                scaleY = roiMat.Height / (double)workingMat.Height;
            }

            BuildFastColorMask(workingMat, target, options.ConfidenceThreshold, _mask);

            Cv2.FindContours(
                _mask,
                out OpenCvSharp.Point[][] contours,
                out _,
                RetrievalModes.External,
                ContourApproximationModes.ApproxSimple);

            _matches.Clear();

            foreach (var contour in contours)
            {
                var boundsScaled = Cv2.BoundingRect(contour);
                var boundsLocal = ScaleBounds(boundsScaled, roiMat.Width, roiMat.Height, scaleX, scaleY);

                if (boundsLocal.Width <= 0 || boundsLocal.Height <= 0)
                    continue;

                if (boundsLocal.Width < options.MinWidth || boundsLocal.Height < options.MinHeight)
                    continue;

                using var componentMask = new Mat(_mask, boundsScaled);
                var area = (int)Math.Round(Cv2.CountNonZero(componentMask) * scaleX * scaleY);

                if (area < options.MinSize)
                    continue;

                if (area > options.MaxSize)
                    continue;

                using var componentImage = new Mat(workingMat, boundsScaled);
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
                        (int)Math.Round((moments.M10 / moments.M00) * scaleX) + roi.X,
                        (int)Math.Round((moments.M01 / moments.M00) * scaleY) + roi.Y)
                    : new DrawingPoint(
                        bounds.X + bounds.Width / 2,
                        bounds.Y + bounds.Height / 2);

                _matches.Add(new ColorDetectionMatch(center, bounds, confidence, area));
            }

            return _matches
                .OrderByDescending(m => m.Confidence)
                .ThenByDescending(m => m.Area)
                .ToList();
        }

        private static DrawingRectangle ScaleBounds(CvRect bounds, int maxWidth, int maxHeight, double scaleX, double scaleY)
        {
            var x = (int)Math.Floor(bounds.X * scaleX);
            var y = (int)Math.Floor(bounds.Y * scaleY);
            var right = (int)Math.Ceiling((bounds.X + bounds.Width) * scaleX);
            var bottom = (int)Math.Ceiling((bounds.Y + bounds.Height) * scaleY);

            x = Math.Clamp(x, 0, maxWidth - 1);
            y = Math.Clamp(y, 0, maxHeight - 1);
            right = Math.Clamp(right, x + 1, maxWidth);
            bottom = Math.Clamp(bottom, y + 1, maxHeight);

            return new DrawingRectangle(x, y, right - x, bottom - y);
        }

        private static void BuildFastColorMask(Mat image, DrawingColor target, double threshold, Mat mask)
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

            Cv2.InRange(image, lower, upper, mask);
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

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _bgr.Dispose();
            _scaled.Dispose();
            _mask.Dispose();
        }
    }
}
