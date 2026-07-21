using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Steps
{
    internal static class PredictionTimingHelper
    {
        public static async Task WaitUntilPredictionTimeAsync(
            IDetectionStepResult? detection,
            ILogger logger,
            CancellationToken ct)
        {
            if (detection is not PredictMovementResult { IsPredicted: true, PredictedForUtc: { } predictedForUtc })
                return;

            var remaining = predictedForUtc - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
                return;

            logger.LogDebug(
                "PredictionTiming: Warte {DelayMs:F0}ms bis zur vorhergesagten Zielzeit.",
                remaining.TotalMilliseconds);
            await Task.Delay(remaining, ct).ConfigureAwait(false);
        }
    }
}
