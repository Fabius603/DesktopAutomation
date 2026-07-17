using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using Microsoft.Extensions.Logging;
using TaskAutomation.Events;

namespace TaskAutomation.Steps
{
    public sealed class ShowImageStepHandler : JobStepHandler<ShowImageStep, OutputResult>
    {
        protected override async Task<OutputResult> ExecuteCoreAsync(
            ShowImageStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var logger = ctx.Logger;
            logger.LogDebug("ShowImageStepHandler: Window '{Name}'", step.Settings.WindowName);

            var capture   = ctx.Results.GetById<CaptureResult>(step.Settings.SourceCaptureStepId);
            var hasDetectionSource = !string.IsNullOrWhiteSpace(step.Settings.SourceDetectionStepId);
            var detection = !hasDetectionSource
                ? DetectionResult.Default
                : ctx.Results.GetById<DetectionResult>(step.Settings.SourceDetectionStepId);

            var rawImage = capture.Image;

            if (rawImage == null)
            {
                logger.LogInformation("ShowImageStepHandler: Kein Bild verfügbar, Step wird übersprungen");
                return new OutputResult { WasExecuted = true, Success = false };
            }

            using var drawnImage = hasDetectionSource && detection.Found
                ? DetectionResultDrawing.Draw(rawImage, detection, capture.Offset)
                : null;
            var image = drawnImage ?? rawImage;
            var displayType = drawnImage != null ? ImageDisplayType.Processed : ImageDisplayType.Raw;

            ctx.OpenedWindowNames.Add(step.Settings.WindowName);
            ctx.ImageDisplayService.DisplayImage(step.Settings.WindowName, image, displayType);

            logger.LogInformation(
                "ShowImageStepHandler: Bild in '{Window}' angezeigt (Erkennungsquelle={HasDetectionSource}, Ergebnis eingezeichnet={DetectionDrawn})",
                step.Settings.WindowName,
                hasDetectionSource,
                drawnImage != null);
            return new OutputResult { WasExecuted = true, Success = true };
        }

        protected override OutputResult CreateDefault() => OutputResult.Default;
    }
}
