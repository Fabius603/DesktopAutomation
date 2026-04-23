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
            var detection = string.IsNullOrEmpty(step.Settings.SourceDetectionStepId)
                ? DetectionResult.Default
                : ctx.Results.GetById<DetectionResult>(step.Settings.SourceDetectionStepId);

            var rawImage       = capture.Image;
            var processedImage = detection.ProcessedImage ?? rawImage;

            if (rawImage == null)
            {
                logger.LogInformation("ShowImageStepHandler: Kein Bild verfügbar, Step wird übersprungen");
                return new OutputResult { WasExecuted = true, Success = false };
            }

            if (step.Settings.ShowRawImage)
            {
                var winName = $"{step.Settings.WindowName} - Raw Image";
                ctx.ImageDisplayService.DisplayImage(winName, rawImage, ImageDisplayType.Raw);
                logger.LogDebug("ShowImageStepHandler: Raw image displayed in '{Win}'", winName);
            }

            if (step.Settings.ShowProcessedImage)
            {
                var winName = $"{step.Settings.WindowName} - Processed Image";
                var img     = (processedImage != null && processedImage.Width >= 10 && processedImage.Height >= 10)
                                  ? processedImage : rawImage;
                ctx.ImageDisplayService.DisplayImage(winName, img, ImageDisplayType.Processed);
                logger.LogDebug("ShowImageStepHandler: Processed image displayed in '{Win}'", winName);
            }

            logger.LogInformation("ShowImageStepHandler: Images displayed successfully");
            return new OutputResult { WasExecuted = true, Success = true };
        }

        protected override OutputResult CreateDefault() => OutputResult.Default;
    }
}
