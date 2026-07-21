using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using Microsoft.Extensions.Logging;
using TaskAutomation.Events;

namespace TaskAutomation.Steps
{
    public sealed class ShowImageStepHandler : JobStepHandler<ShowImageStep, ShowImageResult>
    {
        protected override async Task<ShowImageResult> ExecuteCoreAsync(
            ShowImageStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var logger = ctx.Logger;
            logger.LogDebug("ShowImageStepHandler: Window '{Name}'", step.Settings.WindowName);

            var imageInput = ResultBindingResolver.ResolveCapture(ctx.Results, step.Settings.ImageSource);
            var capture = imageInput.Capture;
            var detections = ResultBindingResolver.ResolveDetections(ctx.Results, step.Settings.DetectionsSource);
            var hasDetectionSource = step.Settings.DetectionsSource.IsConfigured;
            var rawImage = imageInput.Image;

            if (rawImage == null)
            {
                logger.LogInformation("ShowImageStepHandler: Kein Bild verfügbar, Step wird übersprungen");
                return new ShowImageResult { WasExecuted = true, Success = false };
            }

            using var drawnImage = detections.IsSuccess
                ? DetectionResultDrawing.Draw(rawImage, detections.Values, capture.Offset)
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
            return new ShowImageResult { WasExecuted = true, Success = true };
        }

        protected override ShowImageResult CreateDefault() => ShowImageResult.Default;

    }
}
