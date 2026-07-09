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
    public sealed class PredictMovementStepHandler : JobStepHandler<PredictMovementStep, DetectionResult>
    {
        private const int MaxSamples = 12;
        private const double MinimumElapsedSeconds = 0.001;

        protected override Task<DetectionResult> ExecuteCoreAsync(
            PredictMovementStep step,
            IStepPipelineContext ctx,
            CancellationToken ct)
        {
            var source = ctx.Results.GetById<DetectionResult>(step.Settings.SourceDetectionStepId);
            var state = GetState(ctx, step.Id);
            var sampleTimestamp = source.SourceCaptureTimestampUtc;

            PruneOldSamplesAndTracks(state, sampleTimestamp, step.Settings.MaxSampleAgeMs);

            if (!source.Found || source.Point is null)
            {
                state.Tracks.Clear();
                ctx.Logger.LogInformation("PredictMovementStepHandler: Kein Quellpunkt verfuegbar, History zurueckgesetzt.");
                return Task.FromResult(new DetectionResult { WasExecuted = true, Found = false });
            }

            var detections = source.AllDetections.Count > 0
                ? source.AllDetections
                : new[] { (Center: source.Point.Value, source.BoundingBox) };

            AssignDetectionsToTracks(state, detections, sampleTimestamp, step.Settings.ResetDistanceThreshold);

            var predictedFor = sampleTimestamp.AddMilliseconds(Math.Max(0, step.Settings.PredictionMs));
            var predictions = new List<TrackPrediction>();

            foreach (var (trackId, track) in state.Tracks)
            {
                PruneOldSamples(track, sampleTimestamp, step.Settings.MaxSampleAgeMs);
                if (track.Samples.Count < step.Settings.MinSamples)
                    continue;

                if (TryPredict(track, predictedFor, out var point, out var box, out var error))
                    predictions.Add(new TrackPrediction(trackId, point, box, track.Samples.Count, error));
            }

            if (predictions.Count == 0)
            {
                ctx.Logger.LogInformation(
                    "PredictMovementStepHandler: Noch nicht genug stabile Samples. Benoetigt={MinSamples}, Tracks={Tracks}.",
                    step.Settings.MinSamples,
                    state.Tracks.Count);
                return Task.FromResult(new DetectionResult { WasExecuted = true, Found = false });
            }

            var ordered = predictions
                .OrderBy(p => p.Error)
                .ThenByDescending(p => p.SampleCount)
                .ToList();

            var best = ordered[0];
            ctx.Logger.LogInformation(
                "PredictMovementStepHandler: Vorhersage fuer +{PredictionMs}ms bei ({X},{Y}), Track={TrackId}, Samples={Samples}, Fehler={Error:F2}.",
                step.Settings.PredictionMs,
                best.Center.X,
                best.Center.Y,
                best.TrackId,
                best.SampleCount,
                best.Error);

            return Task.FromResult(new DetectionResult
            {
                WasExecuted = true,
                Found = true,
                Point = best.Center,
                BoundingBox = best.BoundingBox,
                Confidence = CalculatePredictionConfidence(source.Confidence, best.Error, step.Settings.ResetDistanceThreshold, best.SampleCount),
                SourceCaptureIsFresh = source.SourceCaptureIsFresh,
                SourceCaptureTimestampUtc = source.SourceCaptureTimestampUtc,
                IsPredicted = true,
                PredictedForUtc = predictedFor,
                AllDetections = ordered.Select(p => (p.Center, p.BoundingBox)).ToList()
            });
        }

        protected override DetectionResult CreateDefault() => DetectionResult.Default;

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
            IReadOnlyList<(Point Center, Rectangle? BoundingBox)> detections,
            DateTime sampleTimestamp,
            double resetDistanceThreshold)
        {
            var unmatchedTrackIds = new HashSet<int>(state.Tracks.Keys);

            foreach (var detection in detections)
            {
                var match = FindBestTrack(state, unmatchedTrackIds, detection.Center, sampleTimestamp, resetDistanceThreshold);
                var trackId = match ?? CreateTrack(state);
                var track = state.Tracks[trackId];

                AddSample(track, detection, sampleTimestamp);
                unmatchedTrackIds.Remove(trackId);
            }
        }

        private static int? FindBestTrack(
            PredictMovementState state,
            HashSet<int> candidateTrackIds,
            Point detection,
            DateTime sampleTimestamp,
            double resetDistanceThreshold)
        {
            int? bestId = null;
            var bestDistance = double.MaxValue;
            var maxDistance = resetDistanceThreshold > 0 ? resetDistanceThreshold : double.MaxValue;

            foreach (var trackId in candidateTrackIds)
            {
                var track = state.Tracks[trackId];
                if (track.Samples.Count == 0)
                    continue;

                var expected = TryPredict(track, sampleTimestamp, out var predicted, out _, out _)
                    ? predicted
                    : track.Samples.Last().Center;

                var distance = Distance(expected, detection);
                if (distance > maxDistance || distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestId = trackId;
            }

            return bestId;
        }

        private static int CreateTrack(PredictMovementState state)
        {
            var trackId = state.NextTrackId++;
            state.Tracks[trackId] = new PredictMovementTrack();
            return trackId;
        }

        private static void AddSample(
            PredictMovementTrack track,
            (Point Center, Rectangle? BoundingBox) detection,
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
            out Point point,
            out Rectangle? boundingBox,
            out double error)
        {
            point = default;
            boundingBox = null;
            error = double.MaxValue;

            var samples = track.Samples.ToArray();
            if (samples.Length == 0)
                return false;

            if (samples.Length == 1)
            {
                point = samples[0].Center;
                boundingBox = samples[0].BoundingBox;
                error = 0;
                return true;
            }

            var origin = samples[0].TimestampUtc;
            var targetSeconds = (targetUtc - origin).TotalSeconds;

            if (!TryFitLine(samples, origin, p => p.X, out var ax, out var bx) ||
                !TryFitLine(samples, origin, p => p.Y, out var ay, out var by))
                return false;

            var predictedX = ax + bx * targetSeconds;
            var predictedY = ay + by * targetSeconds;
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

            error = CalculateFitError(samples, origin, ax, bx, ay, by);
            return true;
        }

        private static bool TryFitLine(
            IReadOnlyList<PredictMovementSample> samples,
            DateTime origin,
            Func<Point, double> selector,
            out double a,
            out double b)
        {
            a = 0;
            b = 0;

            var n = samples.Count;
            if (n == 0)
                return false;

            var sumT = 0.0;
            var sumV = 0.0;
            var sumTT = 0.0;
            var sumTV = 0.0;

            foreach (var sample in samples)
            {
                var t = (sample.TimestampUtc - origin).TotalSeconds;
                var v = selector(sample.Center);
                sumT += t;
                sumV += v;
                sumTT += t * t;
                sumTV += t * v;
            }

            var denominator = n * sumTT - sumT * sumT;
            if (Math.Abs(denominator) < MinimumElapsedSeconds)
            {
                a = sumV / n;
                b = 0;
                return true;
            }

            b = (n * sumTV - sumT * sumV) / denominator;
            a = (sumV - b * sumT) / n;
            return true;
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
            double Error);
    }
}
