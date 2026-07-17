using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    /// <summary>
    /// Zeigt alle Ergebnisse eines Detection-Steps (BoundingBox + Mittelpunkt)
    /// direkt auf dem Desktop über ein transparentes Overlay-Fenster an –
    /// analog zu <see cref="ShowImageStepHandler"/>, jedoch ohne eigenes Vorschaufenster.
    /// Bestes Ergebnis (Index 0) wird farblich hervorgehoben, alle weiteren in Grün.
    /// </summary>
    public sealed class ShowOnDesktopStepHandler : JobStepHandler<ShowOnDesktopStep, OutputResult>
    {
        protected override Task<OutputResult> ExecuteCoreAsync(
            ShowOnDesktopStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var detection = string.IsNullOrEmpty(step.Settings.SourceDetectionStepId)
                ? DetectionResult.Default
                : ctx.Results.GetById<DetectionResult>(step.Settings.SourceDetectionStepId);

            if (!detection.Found || detection.Point is null)
            {
                ctx.DesktopResultOverlay.Clear();
                ctx.Logger.LogInformation(
                    "ShowOnDesktopStepHandler: Kein Treffer im Quell-Step {SourceStepId}; Overlay wurde geleert.",
                    step.Settings.SourceDetectionStepId);
                return Task.FromResult(new OutputResult { WasExecuted = true, Success = true });
            }

            // AllDetections bevorzugen; Fallback auf Einzel-Ergebnis
            IReadOnlyList<(Point Center, Rectangle? BoundingBox)> items = detection.AllDetections.Count > 0
                ? detection.AllDetections
                : new[] { (Center: detection.Point.Value, detection.BoundingBox) };

            ctx.DesktopResultOverlay.ShowResult(items);
            ctx.Logger.LogInformation(
                "ShowOnDesktopStepHandler: {Count} Treffer aus Quell-Step {SourceStepId} auf dem Desktop angezeigt.",
                items.Count, step.Settings.SourceDetectionStepId);

            return Task.FromResult(new OutputResult { WasExecuted = true, Success = true });
        }

        protected override OutputResult CreateDefault() => OutputResult.Default;
    }
}
