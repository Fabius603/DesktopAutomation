using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Steps
{
    public sealed class VideoCreationStepHandler : JobStepHandler<VideoCreationStep, OutputResult>
    {
        protected override async Task<OutputResult> ExecuteCoreAsync(
            VideoCreationStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var logger = ctx.Logger;
            ct.ThrowIfCancellationRequested();

            if (ctx.VideoRecorder == null)
                throw new InvalidOperationException("VideoRecorder is not initialized");

            var capture   = ctx.Results.GetById<CaptureResult>(step.Settings.SourceCaptureStepId);
            var detection = string.IsNullOrEmpty(step.Settings.SourceDetectionStepId)
                ? DetectionResult.Default
                : ctx.Results.GetById<DetectionResult>(step.Settings.SourceDetectionStepId);

            if (capture.Image == null || capture.Image.Width == 0)
            {
                logger.LogInformation("VideoCreationStepHandler: Kein Bild verfügbar, Frame wird übersprungen");
                return new OutputResult { WasExecuted = true, Success = false };
            }

            Bitmap? frameToAdd = null;
            try
            {
                var drawDetectionResult = !string.IsNullOrWhiteSpace(step.Settings.SourceDetectionStepId)
                    && detection.Found;
                frameToAdd = drawDetectionResult
                    ? DetectionResultDrawing.Draw(capture.Image, detection, capture.Offset)
                    : (Bitmap)capture.Image.Clone();

                ct.ThrowIfCancellationRequested();
                ctx.VideoRecorder.AddFrame(frameToAdd);
            }
            finally
            {
                frameToAdd?.Dispose();
            }
            logger.LogInformation(
                "VideoCreationStepHandler: Frame zum Video hinzugefügt (Detection-Quelle={DetectionSource}, Ergebnis eingezeichnet={DetectionDrawn}).",
                string.IsNullOrWhiteSpace(step.Settings.SourceDetectionStepId)
                    ? "keine"
                    : step.Settings.SourceDetectionStepId,
                !string.IsNullOrWhiteSpace(step.Settings.SourceDetectionStepId) && detection.Found);

            return new OutputResult { WasExecuted = true, Success = true };
        }

        protected override OutputResult CreateDefault() => OutputResult.Default;
    }
}
