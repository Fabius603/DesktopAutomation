using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Steps
{
    public sealed class VideoCreationStepHandler : JobStepHandler<VideoCreationStep, VideoCreationResult>
    {
        protected override async Task<VideoCreationResult> ExecuteCoreAsync(
            VideoCreationStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var logger = ctx.Logger;
            ct.ThrowIfCancellationRequested();

            if (ctx.VideoRecorder == null)
                throw new InvalidOperationException("VideoRecorder is not initialized");

            var imageInput = ResultBindingResolver.ResolveCapture(ctx.Results, step.Settings.ImageSource);
            var capture = imageInput.Capture;
            var detections = ResultBindingResolver.ResolveDetections(ctx.Results, step.Settings.DetectionsSource);
            if (imageInput.Image == null || imageInput.Image.Width == 0)
            {
                logger.LogInformation("VideoCreationStepHandler: Kein Bild verfügbar, Frame wird übersprungen");
                return new VideoCreationResult { WasExecuted = true, Success = false };
            }

            Bitmap? frameToAdd = null;
            try
            {
                var drawDetectionResult = detections.IsSuccess;
                frameToAdd = drawDetectionResult
                    ? DetectionResultDrawing.Draw(imageInput.Image, detections.Values, capture.Offset)
                    : (Bitmap)imageInput.Image.Clone();

                ct.ThrowIfCancellationRequested();
                ctx.VideoRecorder.AddFrame(frameToAdd);
            }
            finally
            {
                frameToAdd?.Dispose();
            }
            logger.LogInformation(
                "VideoCreationStepHandler: Frame zum Video hinzugefügt (Detection-Quelle={DetectionSource}, Ergebnis eingezeichnet={DetectionDrawn}).",
                !step.Settings.DetectionsSource.IsConfigured
                    ? "keine"
                    : step.Settings.DetectionsSource.SourceStepId,
                step.Settings.DetectionsSource.IsConfigured && detections.Values.Count > 0);

            return new VideoCreationResult { WasExecuted = true, Success = true };
        }

        protected override VideoCreationResult CreateDefault() => VideoCreationResult.Default;

    }
}
