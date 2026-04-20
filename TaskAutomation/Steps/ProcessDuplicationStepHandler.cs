using ImageCapture.ProcessDuplication;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Steps
{
    public sealed class ProcessDuplicationStepHandler : JobStepHandler<ProcessDuplicationStep, CaptureResult>
    {
        protected override async Task<CaptureResult> ExecuteCoreAsync(
            ProcessDuplicationStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var logger = ctx.Logger;
            logger.LogDebug("ProcessDuplicationStepHandler: Capturing process '{ProcessName}'", step.Settings.ProcessName);

            if (string.IsNullOrWhiteSpace(step.Settings.ProcessName))
                throw new InvalidOperationException("No process name specified");

            if (ctx.ProcessDuplicator == null)
                ctx.ProcessDuplicator = new ProcessDuplicator(step.Settings.ProcessName);

            using var captureResult = ctx.ProcessDuplicator.CaptureProcess();

            if (!captureResult.ProcessFound)
            {
                logger.LogWarning("ProcessDuplicationStepHandler: Process '{ProcessName}' not found", step.Settings.ProcessName);
                return new CaptureResult { WasExecuted = true };
            }

            var bitmap = captureResult.ProcessImage.Clone() as Bitmap;
            var offset = captureResult.WindowOffsetOnDesktop;
            var bounds = new System.Drawing.Rectangle(
                offset.X, offset.Y, bitmap!.Width, bitmap.Height);

            logger.LogInformation(
                "ProcessDuplicationStepHandler: Captured '{ProcessName}' at offset ({X},{Y})",
                step.Settings.ProcessName, offset.X, offset.Y);

            return new CaptureResult
            {
                WasExecuted = true,
                Image       = bitmap,
                Bounds      = bounds,
                Offset      = new System.Drawing.Point(offset.X, offset.Y)
            };
        }

        protected override CaptureResult CreateDefault() => CaptureResult.Default;
    }
}

