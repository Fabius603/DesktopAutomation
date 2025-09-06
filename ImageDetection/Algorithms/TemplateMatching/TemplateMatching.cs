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
            {
                throw new InvalidOperationException("Template not set or is empty. Call SetTemplate first.");
            }
            if (rawSource == null) // Check for null rawSource before normalization
            {
                return new DetectionResult { Success = false, Confidence = 0 };
            }
            var sourceMat = NormalizeImage(rawSource);

            var resultMat = new Mat();
            Mat subRegion = null;
            OpenCvSharp.Point currentRoiOffset = new OpenCvSharp.Point(0, 0);
            Mat imageForMatching = sourceMat;

            try
            {

                bool useValidatedROI = _useROI && _roi != null && _roi.Width > 0 && _roi.Height > 0 &&
                                       _roi.X >= 0 && _roi.Y >= 0 &&
                                       _roi.X + _roi.Width <= sourceMat.Cols &&
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
                        // subRegion remains null, no specific disposal needed here for it
                    }
                }

                if (imageForMatching.Width < _template.Width || imageForMatching.Height < _template.Height)
                {
                    return new DetectionResult { Success = false, Confidence = 0 };
                }

                Cv2.MatchTemplate(imageForMatching, _template, resultMat, _templateMatchMode);

                IDetectionResult result = DetectSinglePoint(resultMat, currentRoiOffset);

                // Convert to global desktop coordinates using the same coordinate system as ScreenHelper.GetScreens()
                //ConvertToGlobalDesktopCoordinates(result, globalOffset);

                return result;
            }
            finally
            {
                imageForMatching?.Dispose();
                sourceMat?.Dispose();
                resultMat?.Dispose();
                subRegion?.Dispose();
            }
        }

        private IDetectionResult DetectSinglePoint(Mat resultMat, OpenCvSharp.Point roiOffset)
        {
            if (resultMat == null)
                return new DetectionResult { Success = false };
            try
            {
                Cv2.MinMaxLoc(resultMat, out double minVal, out double maxVal, out OpenCvSharp.Point minLoc, out OpenCvSharp.Point maxLoc);

                double actualScore = _templateMatchMode == TemplateMatchModes.SqDiffNormed ? minVal : maxVal;
                OpenCvSharp.Point matchLoc = _templateMatchMode == TemplateMatchModes.SqDiffNormed ? minLoc : maxLoc;

                int pointX = matchLoc.X + _template.Width / 2 + roiOffset.X;
                int pointY = matchLoc.Y + _template.Height / 2 + roiOffset.Y;

                bool success;
                double confidencePercent;

                if (_templateMatchMode == TemplateMatchModes.SqDiffNormed)
                {
                    success = actualScore <= (1.0 - _confidenceThreshold);
                    confidencePercent = (1.0 - actualScore) * 100.0;
                }
                else // CcoeffNormed or CCorrNormed (guaranteed by constructor)
                {
                    success = actualScore >= _confidenceThreshold;
                    confidencePercent = actualScore * 100.0;
                }

                confidencePercent = Math.Max(0.0, Math.Min(100.0, confidencePercent));

                return new DetectionResult
                {
                    Success = success,
                    CenterPoint = new System.Drawing.Point(pointX, pointY),
                    Confidence = (float)confidencePercent,
                    BoundingBox = ToRect(new System.Drawing.Point(pointX, pointY), ClassConverter.ToDrawing(_templateSize)),
                };
            }
            catch (OpenCVException)
            {
                return new DetectionResult { Success = false };
            }
        }

        Rectangle ToRect(System.Drawing.Point center, System.Drawing.Size size)
        {
            int x = center.X - size.Width / 2;
            int y = center.Y - size.Height / 2;
            return new System.Drawing.Rectangle(x, y, size.Width, size.Height);
        }

        //private TemplateMatchingResult DetectMultiplePoints(Mat resultMat, Point roiOffset)
        //{
        //    if (resultMat == null)
        //        return new TemplateMatchingResult { Success = false, Points = new List<Point>() };

        //    var candidates = new List<(Point pt, float score)>();

        //    for (int y = 0; y < resultMat.Rows; y++)
        //    {
        //        for (int x = 0; x < resultMat.Cols; x++)
        //        {
        //            float value = resultMat.At<float>(y, x);
        //            bool isMatch;

        //            if (_templateMatchMode == TemplateMatchModes.SqDiffNormed)
        //            {
        //                isMatch = value <= (1.0 - _confidenceThreshold);
        //            }
        //            else // CcoeffNormed or CCorrNormed
        //            {
        //                isMatch = value >= _confidenceThreshold;
        //            }

        //            if (isMatch)
        //                candidates.Add((new Point(x, y), value));
        //        }
        //    }

        //    var finalMatchCenters = new List<Point>();
        //    var scoresOfFinalMatches = new List<float>();

        //    var orderedCandidates = _templateMatchMode == TemplateMatchModes.SqDiffNormed
        //        ? candidates.OrderBy(c => c.score)
        //        : candidates.OrderByDescending(c => c.score);

        //    foreach (var (candidateTopLeft, candidateScore) in orderedCandidates)
        //    {
        //        var currentMatchCenter = new Point(
        //            candidateTopLeft.X + _template.Width / 2 + roiOffset.X,
        //            candidateTopLeft.Y + _template.Height / 2 + roiOffset.Y
        //        );

        //        bool isSuppressed = false;
        //        foreach (Point existingMatchCenter in finalMatchCenters)
        //        {
        //            if (Math.Abs(existingMatchCenter.X - currentMatchCenter.X) <= _suppressionRadius &&
        //                Math.Abs(existingMatchCenter.Y - currentMatchCenter.Y) <= _suppressionRadius)
        //            {
        //                isSuppressed = true;
        //                break;
        //            }
        //        }

        //        if (!isSuppressed)
        //        {
        //            finalMatchCenters.Add(currentMatchCenter);
        //            scoresOfFinalMatches.Add(candidateScore);
        //        }
        //    }

        //    double overallConfidence = 0;
        //    if (scoresOfFinalMatches.Any())
        //    {
        //        double bestFinalScore = _templateMatchMode == TemplateMatchModes.SqDiffNormed
        //            ? scoresOfFinalMatches.Min()
        //            : scoresOfFinalMatches.Max();

        //        if (_templateMatchMode == TemplateMatchModes.SqDiffNormed)
        //        {
        //            overallConfidence = (1.0 - bestFinalScore) * 100.0;
        //        }
        //        else // CcoeffNormed or CCorrNormed
        //        {
        //            overallConfidence = bestFinalScore * 100.0;
        //        }
        //        overallConfidence = Math.Max(0.0, Math.Min(100.0, overallConfidence));
        //    }

        //    return new TemplateMatchingResult
        //    {
        //        Success = finalMatchCenters.Count > 0,
        //        CenterPoint = finalMatchCenters.Count > 0 ? finalMatchCenters[0] : new Point(0, 0),
        //        Points = finalMatchCenters,
        //        MultiplePoints = true,
        //        Confidence = overallConfidence,
        //        TemplateSize = _templateSize
        //    };
        //}

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
