using ImageCapture.Video;
using TaskAutomation.Logging;
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

            var imageInput = ResultBindingResolver.ResolveCapture(ctx.Results, step.Settings.ImageSource);
            var capture = imageInput.Capture;
            if (imageInput.Image == null || imageInput.Image.Width == 0)
            {
                logger.LogInformation("VideoCreationStepHandler: Kein Bild verfügbar, Frame wird übersprungen");
                return new VideoCreationResult { WasExecuted = true, Success = false };
            }

            if (ctx.VideoRecorder is null)
            {
                var recorder = ctx.CreateVideoRecorder(imageInput.Image.Width, imageInput.Image.Height, 60);
                try
                {
                    recorder.OutputDirectory = step.Settings.SavePath;
                    recorder.FileName = step.Settings.FileName;
                    await recorder.StartAsync(ct).ConfigureAwait(false);
                    if (ctx.ExecutionLogSession is { } session)
                        ctx.ExecutionLogService.Write(session, ExecutionLogLevel.Information,
                        "Videoaufnahme gestartet.",
                        $"Datei={recorder.OutputFilePath}, Größe={imageInput.Image.Width}x{imageInput.Image.Height}, FPS=60",
                        stepId: step.Id, stepType: step.GetType().Name);
                    ctx.VideoRecorder = recorder;
                }
                catch { recorder.Dispose(); throw; }
            }

            Bitmap? frameToAdd = null;
            try
            {
                var overlay = VisualOverlayResolver.Resolve(
                    ctx.Results, step.Settings.Overlay, step.Settings.DetectionsSource, logger);
                frameToAdd = overlay.HasContent
                    ? VisualOverlayRenderer.Draw(imageInput.Image, capture.Offset, overlay)
                    : (Bitmap)imageInput.Image.Clone();

                ct.ThrowIfCancellationRequested();
                ctx.VideoRecorder.AddFrame(frameToAdd);
            }
            finally
            {
                frameToAdd?.Dispose();
            }
            logger.LogInformation(
                "VideoCreationStepHandler: Frame mit konfigurierten Overlays zum Video hinzugefügt.");

            return new VideoCreationResult { WasExecuted = true, Success = true };
        }

        protected override VideoCreationResult CreateDefault() => VideoCreationResult.Default;

    }
}
