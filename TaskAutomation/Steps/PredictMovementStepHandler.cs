using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    public sealed class PredictMovementStepHandler : JobStepHandler<PredictMovementStep, PredictMovementResult>
    {
        private const int MaxSamples = 12;
        private const double RegressionEpsilon = 1e-12;
        private const double MinimumRobustScale = 1.0;
        private const double DefaultActionLeadMs = 8.0;
        private const double MinimumActionLeadMs = 2.0;
        private const double MaximumActionLeadMs = 33.0;

        protected override Task<PredictMovementResult> ExecuteCoreAsync(
            PredictMovementStep step,
            IStepPipelineContext ctx,
            CancellationToken ct)
        {
            var resolved = ResultBindingResolver.ResolvePoints(ctx.Results, step.Settings.PointsSource);
            var source = resolved.SourceResult as IDetectionStepResult;
            if (source is null)
                return Task.FromResult(new PredictMovementResult { WasExecuted = true, Found = false });
            var state = GetState(ctx, step.Id);
            var sampleTimestamp = source.SourceCaptureTimestampUtc;

            PruneOldSamplesAndTracks(state, sampleTimestamp, step.Settings.MaxSampleAgeMs);

            if (!resolved.IsSuccess)
            {
                // A single missed detection must not destroy an otherwise stable track. Old
                // tracks are removed by MaxSampleAgeMs and can therefore survive brief gaps.
                ctx.Logger.LogInformation("PredictMovementStepHandler: Kein Quellpunkt verfuegbar, History beibehalten.");
                return Task.FromResult(new PredictMovementResult { WasExecuted = true, Found = false });
            }

            var detections = resolved.Values.Select((point, index) => new DetectionItem
            {
                Center = point,
                BoundingBox = source.AllDetections.ElementAtOrDefault(index)?.BoundingBox
                              ?? (index == 0 ? source.BoundingBox : null),
                Confidence = source.AllDetections.ElementAtOrDefault(index)?.Confidence ?? source.Confidence
            }).ToArray();

            AssignDetectionsToTracks(state, detections, sampleTimestamp, step.Settings.ResetDistanceThreshold);

            var actionLeadMs = EstimateActionLeadMs(state);
            var predictedFor = Max(sampleTimestamp, DateTime.UtcNow).AddMilliseconds(actionLeadMs);
            var predictions = new List<TrackPrediction>();

            foreach (var (trackId, track) in state.Tracks)
            {
                PruneOldSamples(track, sampleTimestamp, step.Settings.MaxSampleAgeMs);
                if (track.Samples.Count < step.Settings.MinSamples)
                    continue;

                if (TryPredict(track, predictedFor, step.Settings.PredictionModel, out var point, out var box, out var error, out var model)
                    && IsPredictionWithinLimits(track, point, error, step.Settings))
                    predictions.Add(new TrackPrediction(trackId, point, box, track.Samples.Count, error, model));
            }

            if (predictions.Count == 0)
            {
                ctx.Logger.LogInformation(
                    "PredictMovementStepHandler: Noch nicht genug stabile Samples. Benoetigt={MinSamples}, Tracks={Tracks}.",
                    step.Settings.MinSamples,
                    state.Tracks.Count);
                return Task.FromResult(new PredictMovementResult { WasExecuted = true, Found = false });
            }

            var ordered = predictions
                .OrderBy(p => p.Error)
                .ThenByDescending(p => p.SampleCount)
                .ToList();

            var best = ordered[0];
            ctx.Logger.LogInformation(
                "PredictMovementStepHandler: {Model}-Vorhersage mit automatisch geschaetztem Vorlauf {ActionLeadMs:F1}ms bei ({X},{Y}), Track={TrackId}, Samples={Samples}, Fehler={Error:F2}.",
                best.Model,
                actionLeadMs,
                best.Center.X,
                best.Center.Y,
                best.TrackId,
                best.SampleCount,
                best.Error);

            var confidence = CalculatePredictionConfidence(source.Confidence, best.Error, step.Settings.ResetDistanceThreshold, best.SampleCount);
            if (confidence < step.Settings.MinimumConfidence)
            {
                ctx.Logger.LogInformation(
                    "PredictMovementStepHandler: Confidence {Confidence:F3} liegt unter Minimum {Minimum:F3}.",
                    confidence,
                    step.Settings.MinimumConfidence);
                return Task.FromResult(new PredictMovementResult { WasExecuted = true, Found = false });
            }

            return Task.FromResult(new PredictMovementResult
            {
                WasExecuted = true,
                Found = true,
                Point = best.Center,
                BoundingBox = best.BoundingBox,
                Confidence = confidence,
                SourceCaptureIsFresh = source.SourceCaptureIsFresh,
                SourceCaptureTimestampUtc = source.SourceCaptureTimestampUtc,
                IsPredicted = true,
                PredictedForUtc = predictedFor,
                AllDetections = ordered.Select(p => new DetectionItem
                {
                    Center = p.Center,
                    BoundingBox = p.BoundingBox,
                    Confidence = confidence
                }).ToList()
            });
        }

        protected override PredictMovementResult CreateDefault() => PredictMovementResult.Default;

        private static PredictMovementState GetState(IStepPipelineContext ctx, string stepId)
        {
            if (!ctx.PredictMovementStates.TryGetValue(stepId, out var state))
            {
                state = new PredictMovementState();
                ctx.PredictMovementStates[stepId] = state;
            }

            return state;
        }

        private static void AssignDetectionsToTracks(
            PredictMovementState state,
            IReadOnlyList<DetectionItem> detections,
            DateTime sampleTimestamp,
            double resetDistanceThreshold)
        {
            var maxDistance = resetDistanceThreshold > 0 ? resetDistanceThreshold : double.MaxValue;
            var candidates = new List<TrackMatchCandidate>();

            for (var detectionIndex = 0; detectionIndex < detections.Count; detectionIndex++)
            {
                foreach (var (trackId, track) in state.Tracks)
                {
                    if (track.Samples.Count == 0)
                        continue;

                    var expected = TryPredict(track, sampleTimestamp, "Linear", out var predicted, out _, out _, out _)
                        ? predicted
                        : track.Samples.Last().Center;

                    var distance = Distance(expected, detections[detectionIndex].Center);
                    if (distance <= maxDistance)
                        candidates.Add(new TrackMatchCandidate(detectionIndex, trackId, distance));
                }
            }

            // Globally prefer the shortest associations. This removes the dependency on the
            // detector's result order and reduces track swaps when objects are close together.
            var matchedDetections = new HashSet<int>();
            var matchedTracks = new HashSet<int>();
            foreach (var candidate in candidates.OrderBy(c => c.Distance))
            {
                if (matchedDetections.Contains(candidate.DetectionIndex) || matchedTracks.Contains(candidate.TrackId))
                    continue;

                matchedDetections.Add(candidate.DetectionIndex);
                matchedTracks.Add(candidate.TrackId);
                AddSample(state.Tracks[candidate.TrackId], detections[candidate.DetectionIndex], sampleTimestamp);
            }

            for (var detectionIndex = 0; detectionIndex < detections.Count; detectionIndex++)
            {
                if (matchedDetections.Contains(detectionIndex))
                    continue;

                var trackId = CreateTrack(state);
                AddSample(state.Tracks[trackId], detections[detectionIndex], sampleTimestamp);
            }
        }

        private static int CreateTrack(PredictMovementState state)
        {
            var trackId = state.NextTrackId++;
            state.Tracks[trackId] = new PredictMovementTrack();
            return trackId;
        }

        private static void AddSample(
            PredictMovementTrack track,
            DetectionItem detection,
            DateTime timestampUtc)
        {
            if (track.Samples.Count > 0 && track.Samples.Last().TimestampUtc == timestampUtc)
                return;

            track.Samples.Enqueue(new PredictMovementSample(detection.Center, detection.BoundingBox, timestampUtc));
            track.LastUpdateUtc = timestampUtc;

            while (track.Samples.Count > MaxSamples)
                track.Samples.Dequeue();
        }

        private static void PruneOldSamplesAndTracks(PredictMovementState state, DateTime nowUtc, int maxSampleAgeMs)
        {
            foreach (var track in state.Tracks.Values)
                PruneOldSamples(track, nowUtc, maxSampleAgeMs);

            foreach (var trackId in state.Tracks.Where(kv => kv.Value.Samples.Count == 0).Select(kv => kv.Key).ToList())
                state.Tracks.Remove(trackId);
        }

        private static void PruneOldSamples(PredictMovementTrack track, DateTime nowUtc, int maxSampleAgeMs)
        {
            if (maxSampleAgeMs <= 0)
                return;

            var maxAge = TimeSpan.FromMilliseconds(maxSampleAgeMs);
            while (track.Samples.Count > 0 && nowUtc - track.Samples.Peek().TimestampUtc > maxAge)
                track.Samples.Dequeue();
        }

        private static bool TryPredict(
            PredictMovementTrack track,
            DateTime targetUtc,
            string predictionModel,
            out Point point,
            out Rectangle? boundingBox,
            out double error,
            out string selectedModel)
        {
            point = default;
            boundingBox = null;
            error = double.MaxValue;
            selectedModel = "Linear";

            var samples = track.Samples.ToArray();
            if (samples.Length == 0)
                return false;

            if (samples.Length == 1)
            {
                point = samples[0].Center;
                boundingBox = samples[0].BoundingBox;
                error = 0;
                selectedModel = "Stationary";
                return true;
            }

            var origin = samples[0].TimestampUtc;
            var targetSeconds = (targetUtc - origin).TotalSeconds;

            if (!TrySelectTrajectory(samples, origin, predictionModel, targetSeconds,
                    out var predictedX, out var predictedY, out error, out selectedModel))
                return false;
            if (!double.IsFinite(predictedX) || !double.IsFinite(predictedY) ||
                predictedX < int.MinValue || predictedX > int.MaxValue ||
                predictedY < int.MinValue || predictedY > int.MaxValue)
                return false;

            point = new Point((int)Math.Round(predictedX), (int)Math.Round(predictedY));

            var lastBox = samples[^1].BoundingBox;
            if (lastBox.HasValue)
            {
                boundingBox = new Rectangle(
                    point.X - lastBox.Value.Width / 2,
                    point.Y - lastBox.Value.Height / 2,
                    lastBox.Value.Width,
                    lastBox.Value.Height);
            }

            return true;
        }

        private static bool IsPredictionWithinLimits(
            PredictMovementTrack track,
            Point prediction,
            double fitError,
            PredictMovementSettings settings)
        {
            if (settings.MaxFitError > 0 && fitError > settings.MaxFitError)
                return false;

            if (settings.MaxPredictionDistance <= 0 || track.Samples.Count == 0)
                return true;

            return Distance(track.Samples.Last().Center, prediction) <= settings.MaxPredictionDistance;
        }

        private static DateTime Max(DateTime left, DateTime right) => left >= right ? left : right;

        private static double EstimateActionLeadMs(PredictMovementState state)
        {
            var intervals = state.Tracks.Values
                .SelectMany(track => track.Samples.Zip(track.Samples.Skip(1),
                    (left, right) => (right.TimestampUtc - left.TimestampUtc).TotalMilliseconds))
                .Where(milliseconds => milliseconds > 0 && double.IsFinite(milliseconds))
                .ToArray();

            if (intervals.Length == 0)
                return DefaultActionLeadMs;

            // Roughly half a frame covers scheduling and action dispatch without predicting
            // a complete additional frame into the future.
            return Math.Clamp(Median(intervals) / 2.0, MinimumActionLeadMs, MaximumActionLeadMs);
        }

        private static bool TrySelectTrajectory(
            IReadOnlyList<PredictMovementSample> samples,
            DateTime origin,
            string requestedModel,
            double targetSeconds,
            out double x,
            out double y,
            out double error,
            out string selectedModel)
        {
            x = y = 0;
            error = double.MaxValue;
            selectedModel = "Linear";
            var candidates = new List<ModelPrediction>();

            if (TryFitTrajectory(samples, origin, out var ax, out var bx, out var ay, out var by))
            {
                candidates.Add(new ModelPrediction(
                    "Linear",
                    ax + bx * targetSeconds,
                    ay + by * targetSeconds,
                    CalculateFitError(samples, origin, ax, bx, ay, by),
                    1.0));
            }

            if (samples.Count >= 5 && TryFitAcceleration(samples, origin, targetSeconds, out var acceleration))
                candidates.Add(acceleration);

            if (samples.Count >= 3 && TryPredictKalman(samples, origin, targetSeconds, out var kalman))
                candidates.Add(kalman);

            var requested = requestedModel?.Trim() ?? "Linear";
            ModelPrediction? chosen = null;
            if (!requested.Equals("Automatic", StringComparison.OrdinalIgnoreCase))
                chosen = candidates
                    .Where(c => c.Model.Equals(requested, StringComparison.OrdinalIgnoreCase))
                    .Select(c => (ModelPrediction?)c)
                    .FirstOrDefault();

            if (chosen is null && candidates.Count > 0)
                chosen = candidates.MinBy(c => c.Error * c.ComplexityPenalty);
            if (chosen is null)
                return false;

            x = chosen.Value.X;
            y = chosen.Value.Y;
            error = chosen.Value.Error;
            selectedModel = chosen.Value.Model;
            return true;
        }

        private static bool TryFitAcceleration(
            IReadOnlyList<PredictMovementSample> samples,
            DateTime origin,
            double targetSeconds,
            out ModelPrediction prediction)
        {
            prediction = default;
            var weights = Enumerable.Range(0, samples.Count)
                .Select(i => 0.35 + 0.65 * Math.Pow((i + 1.0) / samples.Count, 2))
                .ToArray();

            if (!TryFitQuadratic(samples, origin, p => p.X, weights, out var ax, out var bx, out var cx) ||
                !TryFitQuadratic(samples, origin, p => p.Y, weights, out var ay, out var by, out var cy))
                return false;

            var error = CalculateFitError(samples, origin,
                t => (ax + bx * t + cx * t * t, ay + by * t + cy * t * t));
            prediction = new ModelPrediction(
                "Acceleration",
                ax + bx * targetSeconds + cx * targetSeconds * targetSeconds,
                ay + by * targetSeconds + cy * targetSeconds * targetSeconds,
                error,
                1.12);
            return true;
        }

        private static bool TryFitQuadratic(
            IReadOnlyList<PredictMovementSample> samples,
            DateTime origin,
            Func<Point, double> selector,
            IReadOnlyList<double> weights,
            out double a,
            out double b,
            out double c)
        {
            var matrix = new double[3, 4];
            for (var i = 0; i < samples.Count; i++)
            {
                var t = (samples[i].TimestampUtc - origin).TotalSeconds;
                var t2 = t * t;
                var basis = new[] { 1.0, t, t2 };
                var value = selector(samples[i].Center);
                for (var row = 0; row < 3; row++)
                {
                    for (var column = 0; column < 3; column++)
                        matrix[row, column] += weights[i] * basis[row] * basis[column];
                    matrix[row, 3] += weights[i] * basis[row] * value;
                }
            }

            if (!Solve3x3(matrix, out a, out b, out c))
                return false;
            return double.IsFinite(a) && double.IsFinite(b) && double.IsFinite(c);
        }

        private static bool Solve3x3(double[,] matrix, out double a, out double b, out double c)
        {
            a = b = c = 0;
            for (var pivot = 0; pivot < 3; pivot++)
            {
                var bestRow = pivot;
                for (var row = pivot + 1; row < 3; row++)
                    if (Math.Abs(matrix[row, pivot]) > Math.Abs(matrix[bestRow, pivot]))
                        bestRow = row;

                if (Math.Abs(matrix[bestRow, pivot]) < RegressionEpsilon)
                    return false;
                if (bestRow != pivot)
                    for (var column = pivot; column < 4; column++)
                        (matrix[pivot, column], matrix[bestRow, column]) = (matrix[bestRow, column], matrix[pivot, column]);

                var divisor = matrix[pivot, pivot];
                for (var column = pivot; column < 4; column++)
                    matrix[pivot, column] /= divisor;
                for (var row = 0; row < 3; row++)
                {
                    if (row == pivot) continue;
                    var factor = matrix[row, pivot];
                    for (var column = pivot; column < 4; column++)
                        matrix[row, column] -= factor * matrix[pivot, column];
                }
            }

            a = matrix[0, 3]; b = matrix[1, 3]; c = matrix[2, 3];
            return true;
        }

        private static bool TryPredictKalman(
            IReadOnlyList<PredictMovementSample> samples,
            DateTime origin,
            double targetSeconds,
            out ModelPrediction prediction)
        {
            prediction = default;
            var xFilter = new Kalman1D(samples[0].Center.X);
            var yFilter = new Kalman1D(samples[0].Center.Y);
            var residualSum = 0.0;
            var previousSeconds = 0.0;

            for (var i = 1; i < samples.Count; i++)
            {
                var seconds = (samples[i].TimestampUtc - origin).TotalSeconds;
                var dt = seconds - previousSeconds;
                if (dt <= 0) continue;
                residualSum += Distance(
                    new Point((int)Math.Round(xFilter.Predict(dt)), (int)Math.Round(yFilter.Predict(dt))),
                    samples[i].Center);
                xFilter.Update(samples[i].Center.X, dt);
                yFilter.Update(samples[i].Center.Y, dt);
                previousSeconds = seconds;
            }

            var horizon = Math.Max(0, targetSeconds - previousSeconds);
            prediction = new ModelPrediction(
                "Kalman",
                xFilter.Predict(horizon),
                yFilter.Predict(horizon),
                residualSum / Math.Max(1, samples.Count - 1),
                1.05);
            return true;
        }

        private static bool TryFitTrajectory(
            IReadOnlyList<PredictMovementSample> samples,
            DateTime origin,
            out double ax,
            out double bx,
            out double ay,
            out double by)
        {
            ax = bx = ay = by = 0;
            var weights = Enumerable.Range(0, samples.Count)
                .Select(i => 0.5 + 0.5 * (i + 1.0) / samples.Count)
                .ToArray();

            if (!TryFitLine(samples, origin, p => p.X, weights, out ax, out bx))
                return false;
            if (!TryFitLine(samples, origin, p => p.Y, weights, out ay, out by))
                return false;

            // One robust reweighting pass (Huber): large residuals retain a small influence
            // instead of pulling the complete trajectory towards a false detection.
            var residuals = new double[samples.Count];
            for (var i = 0; i < samples.Count; i++)
            {
                var sample = samples[i];
                var t = (sample.TimestampUtc - origin).TotalSeconds;
                var dx = sample.Center.X - (ax + bx * t);
                var dy = sample.Center.Y - (ay + by * t);
                residuals[i] = Math.Sqrt(dx * dx + dy * dy);
            }
            var scale = Math.Max(MinimumRobustScale, Median(residuals) * 1.4826);
            var huberLimit = 1.5 * scale;

            for (var i = 0; i < weights.Length; i++)
                weights[i] *= residuals[i] <= huberLimit ? 1.0 : huberLimit / residuals[i];

            return TryFitLine(samples, origin, p => p.X, weights, out ax, out bx) &&
                   TryFitLine(samples, origin, p => p.Y, weights, out ay, out by);
        }

        private static bool TryFitLine(
            IReadOnlyList<PredictMovementSample> samples,
            DateTime origin,
            Func<Point, double> selector,
            IReadOnlyList<double> weights,
            out double a,
            out double b)
        {
            a = 0;
            b = 0;

            var n = samples.Count;
            if (n == 0)
                return false;

            var sumW = 0.0;
            var sumWT = 0.0;
            var sumWV = 0.0;
            var sumWTT = 0.0;
            var sumWTV = 0.0;

            for (var i = 0; i < samples.Count; i++)
            {
                var sample = samples[i];
                var weight = weights[i];
                var t = (sample.TimestampUtc - origin).TotalSeconds;
                var v = selector(sample.Center);
                sumW += weight;
                sumWT += weight * t;
                sumWV += weight * v;
                sumWTT += weight * t * t;
                sumWTV += weight * t * v;
            }

            var denominator = sumW * sumWTT - sumWT * sumWT;
            if (Math.Abs(denominator) < RegressionEpsilon)
            {
                a = sumW > 0 ? sumWV / sumW : 0;
                b = 0;
                return true;
            }

            b = (sumW * sumWTV - sumWT * sumWV) / denominator;
            a = (sumWV - b * sumWT) / sumW;
            return true;
        }

        private static double Median(double[] values)
        {
            if (values.Length == 0)
                return 0;

            var ordered = values.OrderBy(v => v).ToArray();
            var middle = ordered.Length / 2;
            return ordered.Length % 2 == 0
                ? (ordered[middle - 1] + ordered[middle]) / 2.0
                : ordered[middle];
        }

        private static double CalculateFitError(
            IReadOnlyList<PredictMovementSample> samples,
            DateTime origin,
            double ax,
            double bx,
            double ay,
            double by)
        {
            if (samples.Count <= 1)
                return 0;

            var error = 0.0;
            foreach (var sample in samples)
            {
                var t = (sample.TimestampUtc - origin).TotalSeconds;
                var dx = sample.Center.X - (ax + bx * t);
                var dy = sample.Center.Y - (ay + by * t);
                error += Math.Sqrt(dx * dx + dy * dy);
            }

            return error / samples.Count;
        }

        private static double CalculateFitError(
            IReadOnlyList<PredictMovementSample> samples,
            DateTime origin,
            Func<double, (double X, double Y)> predictor)
        {
            var error = 0.0;
            foreach (var sample in samples)
            {
                var expected = predictor((sample.TimestampUtc - origin).TotalSeconds);
                var dx = sample.Center.X - expected.X;
                var dy = sample.Center.Y - expected.Y;
                error += Math.Sqrt(dx * dx + dy * dy);
            }
            return error / Math.Max(1, samples.Count);
        }

        private static double CalculatePredictionConfidence(double sourceConfidence, double fitError, double resetDistanceThreshold, int sampleCount)
        {
            var threshold = resetDistanceThreshold > 0 ? resetDistanceThreshold : 250;
            var stability = 1.0 - Math.Clamp(fitError / threshold, 0.0, 1.0);
            var sampleFactor = Math.Clamp(sampleCount / 6.0, 0.3, 1.0);
            return Math.Clamp(sourceConfidence * stability * sampleFactor, 0.0, 1.0);
        }

        private static double Distance(Point a, Point b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private readonly record struct TrackPrediction(
            int TrackId,
            Point Center,
            Rectangle? BoundingBox,
            int SampleCount,
            double Error,
            string Model);

        private readonly record struct ModelPrediction(
            string Model,
            double X,
            double Y,
            double Error,
            double ComplexityPenalty);

        private readonly record struct TrackMatchCandidate(
            int DetectionIndex,
            int TrackId,
            double Distance);

        /// <summary>One-dimensional constant-velocity Kalman filter.</summary>
        private sealed class Kalman1D
        {
            private const double MeasurementVariance = 16.0;
            private const double AccelerationVariance = 100.0;
            private double _position;
            private double _velocity;
            private double _p00 = 100;
            private double _p01;
            private double _p10;
            private double _p11 = 1000;

            public Kalman1D(double initialPosition) => _position = initialPosition;

            public double Predict(double dt) => _position + _velocity * Math.Max(0, dt);

            public void Update(double measurement, double dt)
            {
                dt = Math.Max(1e-6, dt);
                _position += _velocity * dt;

                var dt2 = dt * dt;
                var dt3 = dt2 * dt;
                var dt4 = dt2 * dt2;
                var p00 = _p00 + dt * (_p10 + _p01) + dt2 * _p11 + AccelerationVariance * dt4 / 4;
                var p01 = _p01 + dt * _p11 + AccelerationVariance * dt3 / 2;
                var p10 = _p10 + dt * _p11 + AccelerationVariance * dt3 / 2;
                var p11 = _p11 + AccelerationVariance * dt2;

                var innovationVariance = p00 + MeasurementVariance;
                var k0 = p00 / innovationVariance;
                var k1 = p10 / innovationVariance;
                var innovation = measurement - _position;
                _position += k0 * innovation;
                _velocity += k1 * innovation;

                // Joseph-equivalent scalar update; keep symmetry despite rounding.
                _p00 = (1 - k0) * p00;
                _p01 = (1 - k0) * p01;
                _p10 = p10 - k1 * p00;
                _p11 = p11 - k1 * p01;
                var offDiagonal = (_p01 + _p10) / 2;
                _p01 = _p10 = offDiagonal;
            }
        }
    }
}
