using ImageDetection.Model;
using OpenCvSharp;
using OpenCvSharp.Features2D;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace ImageDetection.Algorithms.KeyPointMatching
{
    /// <summary>
    /// SIFT-based keypoint matcher using a FLANN index for fast descriptor search.
    /// The template is loaded once; its SIFT descriptors are cached until the path changes.
    /// </summary>
    public class KeyPointMatcher : IDisposable
    {
        private readonly int    _minMatchCount;
        private readonly double _lowesRatioThreshold;

        private bool _useROI;
        private Rect _roi;

        private string        _currentTemplatePath = string.Empty;
        private Mat?          _trainDescriptors;
        private KeyPoint[]?   _trainKeyPoints;
        private OpenCvSharp.Size _templateSize;

        private readonly SIFT         _sift;
        private FlannBasedMatcher?    _flannMatcher;
        private bool _disposed;

        public KeyPointMatcher(int minMatchCount = 10, double lowesRatioThreshold = 0.75)
        {
            _minMatchCount       = minMatchCount;
            _lowesRatioThreshold = lowesRatioThreshold;
            _sift = SIFT.Create();
        }

        /// <summary>
        /// Loads the template image and precomputes its SIFT keypoints and descriptors.
        /// Rebuilds the FLANN index. No-op if the same path is given again.
        /// </summary>
        public void SetTemplate(string templatePath)
        {
            if (string.IsNullOrEmpty(templatePath) || templatePath == _currentTemplatePath)
                return;

            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Template-Datei nicht gefunden: '{templatePath}'");

            using var templateMat = Cv2.ImRead(templatePath, ImreadModes.Grayscale);
            if (templateMat.Empty())
                throw new InvalidOperationException($"Template-Bild konnte nicht geladen werden: '{templatePath}'");

            _trainDescriptors?.Dispose();
            _trainDescriptors = new Mat();

            _sift.DetectAndCompute(templateMat, null, out _trainKeyPoints, _trainDescriptors);
            _templateSize        = templateMat.Size();
            _currentTemplatePath = templatePath;

            // Rebuild FLANN index for the new template
            _flannMatcher?.Dispose();
            _flannMatcher = null;

            if (!_trainDescriptors.Empty() && _trainKeyPoints?.Length > 0)
            {
                _flannMatcher = new FlannBasedMatcher();
                _flannMatcher.Add(new[] { _trainDescriptors });
                _flannMatcher.Train();
            }
        }

        public void SetROI(Rect roi) => _roi  = roi;
        public void EnableROI()               => _useROI = true;
        public void DisableROI()              => _useROI = false;

        /// <summary>
        /// Searches <paramref name="rawSource"/> for the previously set template.
        /// Returns <see cref="DetectionResult.Success"/> = false when not enough good matches are found.
        /// </summary>
        public IDetectionResult Detect(Mat rawSource)
        {
            if (_flannMatcher == null || _trainKeyPoints == null || _trainKeyPoints.Length < 4)
                return new DetectionResult { Success = false, Confidence = 0 };

            if (rawSource == null || rawSource.Empty())
                return new DetectionResult { Success = false, Confidence = 0 };

            Mat? graySource = null;
            Mat? subRegion  = null;
            var  roiOffset  = new OpenCvSharp.Point(0, 0);

            try
            {
                graySource = rawSource.Channels() == 1
                    ? rawSource.Clone()
                    : rawSource.CvtColor(ColorConversionCodes.BGR2GRAY);

                Mat imageForDetection = graySource;

                bool useValidatedROI = _useROI
                    && _roi.Width > 0 && _roi.Height > 0
                    && _roi.X >= 0   && _roi.Y >= 0
                    && _roi.X + _roi.Width  <= graySource.Cols
                    && _roi.Y + _roi.Height <= graySource.Rows;

                if (useValidatedROI)
                {
                    try
                    {
                        subRegion         = new Mat(graySource, _roi);
                        imageForDetection = subRegion;
                        roiOffset         = _roi.Location;
                    }
                    catch (OpenCVException)
                    {
                        imageForDetection = graySource;
                        roiOffset         = new OpenCvSharp.Point(0, 0);
                    }
                }

                using var queryDescriptors = new Mat();
                _sift.DetectAndCompute(imageForDetection, null, out var queryKeyPoints, queryDescriptors);

                if (queryKeyPoints.Length < 2 || queryDescriptors.Empty())
                    return new DetectionResult { Success = false, Confidence = 0 };

                // KNN match (k=2) against the pre-built FLANN index
                var knnMatches = _flannMatcher.KnnMatch(queryDescriptors, _trainDescriptors, 2);

                // Lowe's ratio test
                var goodMatches = new List<DMatch>();
                foreach (var m in knnMatches)
                {
                    if (m.Length >= 2 && m[0].Distance < _lowesRatioThreshold * m[1].Distance)
                        goodMatches.Add(m[0]);
                }

                if (goodMatches.Count < _minMatchCount)
                    return new DetectionResult { Success = false, Confidence = 0 };

                // Corresponding points: template (train) → query image
                var templatePts = new List<Point2f>(goodMatches.Count);
                var queryPts    = new List<Point2f>(goodMatches.Count);
                foreach (var m in goodMatches)
                {
                    templatePts.Add(_trainKeyPoints[m.TrainIdx].Pt);
                    queryPts   .Add(queryKeyPoints [m.QueryIdx].Pt);
                }

                // Homography with RANSAC – returns inlier mask for each point
                using var inlierMask = new Mat();
                using var H = Cv2.FindHomography(
                    InputArray.Create(templatePts.ToArray()),
                    InputArray.Create(queryPts   .ToArray()),
                    HomographyMethods.Ransac,
                    5.0,
                    inlierMask);

                if (H == null || H.Empty())
                    return new DetectionResult { Success = false, Confidence = 0 };

                // Only RANSAC-consistent inliers count – this filters accidental Lowe-matches
                int inlierCount = inlierMask.Empty() ? 0 : Cv2.CountNonZero(inlierMask);
                if (inlierCount < _minMatchCount)
                    return new DetectionResult { Success = false, Confidence = 0 };

                // Project template corners into the query image
                var templateCorners = new[]
                {
                    new Point2f(0,                   0),
                    new Point2f(_templateSize.Width, 0),
                    new Point2f(_templateSize.Width, _templateSize.Height),
                    new Point2f(0,                   _templateSize.Height),
                };

                Point2f[] projectedCorners;
                try
                {
                    projectedCorners = Cv2.PerspectiveTransform(templateCorners, H);
                }
                catch
                {
                    return new DetectionResult { Success = false, Confidence = 0 };
                }

                // Center of projected quad + ROI offset
                float cx = 0, cy = 0;
                foreach (var pt in projectedCorners) { cx += pt.X; cy += pt.Y; }
                cx /= projectedCorners.Length;
                cy /= projectedCorners.Length;

                int globalX = (int)cx + roiOffset.X;
                int globalY = (int)cy + roiOffset.Y;

                // Axis-aligned bounding box from projected corners
                float minX = float.MaxValue, minY = float.MaxValue;
                float maxX = float.MinValue, maxY = float.MinValue;
                foreach (var pt in projectedCorners)
                {
                    float px = pt.X + roiOffset.X;
                    float py = pt.Y + roiOffset.Y;
                    if (px < minX) minX = px;
                    if (py < minY) minY = py;
                    if (px > maxX) maxX = px;
                    if (py > maxY) maxY = py;
                }
                var boundingBox = new Rectangle(
                    (int)minX, (int)minY,
                    (int)(maxX - minX), (int)(maxY - minY));

                // Confidence based on RANSAC inliers, not raw good-match count
                float confidence = (float)inlierCount / Math.Max(1, _trainKeyPoints.Length);

                var result = new DetectionResult
                {
                    Success     = true,
                    CenterPoint = new System.Drawing.Point(globalX, globalY),
                    BoundingBox = boundingBox,
                    Confidence  = confidence,
                };
                result.AllResults = new[] { (IDetectionResult)result };
                return result;
            }
            finally
            {
                subRegion ?.Dispose();
                graySource?.Dispose();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _trainDescriptors?.Dispose();
            _flannMatcher    ?.Dispose();
            _sift              .Dispose();
        }
    }
}
