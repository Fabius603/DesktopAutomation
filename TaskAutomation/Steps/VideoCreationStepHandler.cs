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

            var capture   = TemplateMatchingStepHandler.GetCapture(ctx.Results);
            var detection = KlickOnPointStepHandler.GetDetection(ctx.Results);

            if (capture.Image == null || capture.Image.Width == 0)
                throw new InvalidOperationException("No valid image available for video recording");

            Bitmap? frameToAdd;
            if (step.Settings.UseRawImage)
            {
                frameToAdd = (Bitmap)capture.Image.Clone();
            }
            else
            {
                var processed = detection.ProcessedImage;
                frameToAdd = (processed != null && processed.Width > 0)
                    ? (Bitmap)processed.Clone()
                    : (Bitmap)capture.Image.Clone();
            }

            ct.ThrowIfCancellationRequested();
            ctx.VideoRecorder.AddFrame(frameToAdd);
            logger.LogDebug("VideoCreationStepHandler: Frame added to video recorder");

            return new OutputResult { WasExecuted = true, Success = true };
        }

        protected override OutputResult CreateDefault() => OutputResult.Default;
    }
}
