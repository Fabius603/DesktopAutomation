using ImageDetection.Model;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageHelperMethods;

namespace ImageDetection.Algorithms.TemplateMatching
{
    public class TemplateMatching : IDisposable
    {
        private TemplateMatchModes _templateMatchMode;
        private Mat _template;
        private OpenCvSharp.Size _templateSize;
        private string _templatePath = string.Empty;
        private bool _multiplePoints = false;
        private double _confidenceThreshold = 0.9;
        private int _suppressionRadius = 10;
        private Rect _roi;
        private bool _useROI = false;

        private readonly List<TemplateMatchModes> _allowedMatchModes = new List<TemplateMatchModes>
        {
            TemplateMatchModes.CCoeffNormed,
            TemplateMatchModes.CCorrNormed,
            TemplateMatchModes.SqDiffNormed
        };

        public TemplateMatching(TemplateMatchModes templateMatchMode)
        {
            if (_allowedMatchModes.Contains(templateMatchMode))
            {
                _templateMatchMode = templateMatchMode;
            }
            else
            {
                throw new ArgumentException("Invalid template match mode. Allowed modes are: " + string.Join(", ", _allowedMatchModes), nameof(templateMatchMode));
            }
        }

        public IDetectionResult Detect(Mat rawSource)
        {
            if (_template == null)
                throw new InvalidOperationException("Template not set or is empty. Call SetTemplate first.");
            if (rawSource == null)
                return new DetectionResult { Success = false, Confidence = 0 };

            var sourceMat = NormalizeImage(rawSource);
            var resultMat = new Mat();
            Mat? subRegion = null;
            OpenCvSharp.Point currentRoiOffset = new OpenCvSharp.Point(0, 0);
            Mat imageForMatching = sourceMat;

            try
            {
                bool useValidatedROI = _useROI && _roi.Width > 0 && _roi.Height > 0 &&
                                       _roi.X >= 0 && _roi.Y >= 0 &&
                                       _roi.X + _roi.Width  <= sourceMat.Cols &&
                                       _roi.Y + _roi.Height <= sourceMat.Rows;

                if (useValidatedROI)
                {
                    try
                    {
                        subRegion = new Mat(sourceMat, _roi);
                        imageForMatching = subRegion;
                        currentRoiOffset = _roi.Location;
                    }
                    catch (OpenCVException)
                    {
                        imageForMatching = sourceMat;
                        currentRoiOffset = new OpenCvSharp.Point(0, 0);
                    }
                }

                if (imageForMatching.Width < _template.Width || imageForMatching.Height < _template.Height)
                    return new DetectionResult { Success = false, Confidence = 0 };

                Cv2.MatchTemplate(imageForMatching, _template, resultMat, _templateMatchMode);

                var all = DetectAllPoints(resultMat, currentRoiOffset);

                if (all.Count == 0)
                    return new DetectionResult { Success = false };

                var best = (DetectionResult)all[0];
                best.AllResults = all;
                return best;
            }
            finally
            {
                subRegion?.Dispose();
                sourceMat?.Dispose();
                resultMat?.Dispose();
            }
        }

        private List<IDetectionResult> DetectAllPoints(Mat resultMat, OpenCvSharp.Point roiOffset)
        {
            if (resultMat == null) return [];

            bool isSqDiff = _templateMatchMode == TemplateMatchModes.SqDiffNormed;

            // ── Fast path: nur ein Ergebnis benötigt (MultiplePoints deaktiviert) ──────────────
            if (!_multiplePoints)
            {
                Cv2.MinMaxLoc(resultMat,
                    out double minVal, out double maxVal,
                    out OpenCvSharp.Point minLoc, out OpenCvSharp.Point maxLoc);

                double score = isSqDiff ? minVal : maxVal;
                OpenCvSharp.Point loc = isSqDiff ? minLoc : maxLoc;

                bool passes = isSqDiff
                    ? score <= (1.0 - _confidenceThreshold)
                    : score >= _confidenceThreshold;

                if (!passes) return [];

                double confidencePct = isSqDiff
                    ? Math.Clamp((1.0 - score) * 100.0, 0, 100)
                    : Math.Clamp(score * 100.0, 0, 100);

                int cx = loc.X + _template.Width  / 2 + roiOffset.X;
                int cy = loc.Y + _template.Height / 2 + roiOffset.Y;
                var center = new System.Drawing.Point(cx, cy);
                return [new DetectionResult
                {
                    Success     = true,
                    CenterPoint = center,
                    BoundingBox = ToRect(center, ClassConverter.ToDrawing(_templateSize)),
                    Confidence  = (float)confidencePct,
                }];
            }

            // ── Slow path: mehrere Ergebnisse (Suppression-Loop) ────────────────────────
            var results = new List<IDetectionResult>();

            // Arbeitskopie für iterative Suppression (nur im MultiplePoints-Modus)
            using var work = resultMat.Clone();

            while (true)
            {
                Cv2.MinMaxLoc(work, out double minVal, out double maxVal,
                              out OpenCvSharp.Point minLoc, out OpenCvSharp.Point maxLoc);

                double score   = isSqDiff ? minVal : maxVal;
                OpenCvSharp.Point loc = isSqDiff ? minLoc : maxLoc;

                bool passes = isSqDiff
                    ? score <= (1.0 - _confidenceThreshold)
                    : score >= _confidenceThreshold;

                if (!passes) break;

                double confidencePct = isSqDiff
                    ? Math.Clamp((1.0 - score) * 100.0, 0, 100)
                    : Math.Clamp(score * 100.0, 0, 100);

                int cx = loc.X + _template.Width  / 2 + roiOffset.X;
                int cy = loc.Y + _template.Height / 2 + roiOffset.Y;
                var center  = new System.Drawing.Point(cx, cy);
                var bbox    = ToRect(center, ClassConverter.ToDrawing(_templateSize));

                results.Add(new DetectionResult
                {
                    Success     = true,
                    CenterPoint = center,
                    BoundingBox = bbox,
                    Confidence  = (float)confidencePct,
                });

                // Region um den Match auf neutralen Wert setzen (Suppression)
                int r  = _suppressionRadius;
                int x0 = Math.Max(0, loc.X - r);
                int y0 = Math.Max(0, loc.Y - r);
                int x1 = Math.Min(work.Cols - 1, loc.X + r);
                int y1 = Math.Min(work.Rows - 1, loc.Y + r);
                var suppressRect = new Rect(x0, y0, x1 - x0 + 1, y1 - y0 + 1);

                using var roi = new Mat(work, suppressRect);
                roi.SetTo(isSqDiff ? Scalar.All(1.0) : Scalar.All(0.0));
            }

            return results;
        }

        Rectangle ToRect(System.Drawing.Point center, System.Drawing.Size size)
        {
            int x = center.X - size.Width / 2;
            int y = center.Y - size.Height / 2;
            return new System.Drawing.Rectangle(x, y, size.Width, size.Height);
        }

        public void SetROI(Rect roi)
        {
            _roi = roi;
        }

        public void EnableROI()
        {
            _useROI = true;
        }

        /// <summary>
        /// Effizienter Bitmap-Overload für Template Matching - vermeidet unnötige Konvertierungen
        /// </summary>
        public IDetectionResult Detect(Bitmap bitmap)
        {
            if (bitmap == null)
            {
                return new DetectionResult { Success = false, Confidence = 0 };
            }

            using (var mat = bitmap.ToMat())
            {
                return Detect(mat);
            }
        }

        public void DisableROI()
        {
            _useROI = false;
        }

        public void SetTemplateMatchMode(TemplateMatchModes templateMatchMode)
        {
            if (_allowedMatchModes.Contains(templateMatchMode))
            {
                _templateMatchMode = templateMatchMode;
            }
            else
            {
                throw new ArgumentException("Invalid template match mode. Allowed modes are: " + string.Join(", ", _allowedMatchModes), nameof(templateMatchMode));
            }
        }

        public void SetThreshold(double threshold)
        {
            if (threshold < 0 || threshold > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(threshold), "Threshold must be between 0 and 1.");
            }
            _confidenceThreshold = threshold;
        }

        public void SetSuppressionRadius(int radius)
        {
            if (radius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(radius), "Suppression radius cannot be negative.");
            }
            _suppressionRadius = radius;
        }

        public void EnableMultiplePoints()
        {
            _multiplePoints = true;
        }

        public void DisableMultiplePoints()
        {
            _multiplePoints = false;
        }

        public void SetTemplate(string templatePath)
        {
            if(templatePath == _templatePath)
            {
                return; // No need to set the same template again.
            }

            Mat template = new Mat(templatePath);
            _template?.Dispose();
            _template = NormalizeImage(template);
            template.Dispose();
            _templatePath = templatePath;
            _templateSize = _template.Size();
        }

        private Mat NormalizeImage(Mat input)
        {
            if (input == null)
            {
                return new Mat(); // Return a new, empty Mat.
            }

            Mat matToProcess = input;
            Mat tempMat1 = null; // For depth conversion
            Mat tempMat2 = null; // For color conversion

            try
            {
                if (input.Depth() != MatType.CV_8U)
                {
                    tempMat1 = new Mat();
                    input.ConvertTo(tempMat1, MatType.CV_8U);
                    matToProcess = tempMat1;
                }

                if (matToProcess.Channels() > 1)
                {
                    tempMat2 = new Mat();
                    Cv2.CvtColor(matToProcess, tempMat2, ColorConversionCodes.BGR2GRAY);
                    tempMat1?.Dispose(); // Dispose intermediate depth conversion if it happened
                    return tempMat2;     // Ownership of tempMat2 transferred
                }

                if (tempMat1 != null) // Depth conversion happened, no color conversion needed
                {
                    return tempMat1; // Ownership of tempMat1 transferred
                }

                // No depth or color conversion was needed, input was already 8U and single channel.
                // Return a clone as the method contract is to return a new Mat that the caller (or class) owns.
                return input.Clone();
            }
            catch (OpenCVException) // Catch OpenCV specific exceptions during conversion
            {
                tempMat1?.Dispose();
                tempMat2?.Dispose();
                // Optionally log the exception here
                return new Mat(); // Return an empty Mat on conversion failure
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
             _template?.Dispose();
        }

        ~TemplateMatching()
        {
            Dispose(false);
        }
    }
}
