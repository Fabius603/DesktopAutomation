using ImageCapture.DesktopDuplication;
using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using ImageHelperMethods;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Steps
{
    public sealed class DesktopDuplicationStepHandler : JobStepHandler<DesktopDuplicationStep, CaptureResult>
    {
        protected override async Task<CaptureResult> ExecuteCoreAsync(
            DesktopDuplicationStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            var logger = ctx.Logger;
            // Hinweis: Bitmaps der vorherigen Runde werden durch ResetResults() am Anfang jeder Runde disposed.
            logger.LogDebug("DesktopDuplicationStepHandler: Capturing monitor {MonitorIndex}", step.Settings.DesktopIdx);

            if (ctx.DesktopDuplicator == null)
            {
                logger.LogDebug("DesktopDuplicationStepHandler: Creating new DesktopDuplicator for monitor {MonitorIndex}", step.Settings.DesktopIdx);
                ctx.DesktopDuplicator = new DesktopDuplicator(step.Settings.DesktopIdx);
                // 16 ms ≈ 60 fps: DXGI blockiert intern bis ein neuer Frame verfügbar ist,
                // statt sofort zurückzukehren (aquireFrameTimeout=0) und im Handler zu pollen.
                ctx.DesktopDuplicator.SetFrameTimeout(16);
                await Task.Delay(100, ct);
            }

            ct.ThrowIfCancellationRequested();

            DesktopFrame? frame = null;
            int retryCount = 0;
            const int maxRetries = 3;

            while (frame?.DesktopImage == null && retryCount < maxRetries)
            {
                try
                {
                    frame?.Dispose();
                    // GetLatestFrame() blockiert jetzt bis zu 16 ms (SetFrameTimeout),
                    // daher reichen kurze Retries ohne extra Task.Delay.
                    frame = ctx.DesktopDuplicator.GetLatestFrame();
                    if (frame?.DesktopImage == null)
                    {
                        retryCount++;
                        logger.LogWarning("DesktopDuplicationStepHandler: No image on attempt {Attempt}/{Max}, retrying...", retryCount, maxRetries);
                    }
                }
                catch (Exception ex) when (retryCount < maxRetries - 1)
                {
                    retryCount++;
                    frame?.Dispose();
                    frame = null;
                    logger.LogWarning(ex, "DesktopDuplicationStepHandler: Capture failed on attempt {Attempt}/{Max}, retrying...", retryCount, maxRetries);
                    await Task.Delay(50, ct);
                }
            }

            if (frame?.DesktopImage == null)
            {
                frame?.Dispose();
                throw new InvalidOperationException("No desktop image captured after retries");
            }

            using (frame)
            {
                // Eigentumsübertragung: frame.DesktopImage wird direkt übernommen,
                // kein extra new Bitmap(...)-Klon nötig (spart ~8 MB Memcopy bei 1080p).
                var bitmap       = frame.DesktopImage;
                frame.DesktopImage = null; // verhindert Dispose in using(frame)

                var screenBounds = ScreenHelper.GetDesktopBounds(step.Settings.DesktopIdx);
                var offset       = new System.Drawing.Point(screenBounds.Left, screenBounds.Top);

                logger.LogInformation(
                    "DesktopDuplicationStepHandler: Captured {W}x{H} at offset ({X},{Y})",
                    bitmap.Width, bitmap.Height, offset.X, offset.Y);

                return new CaptureResult
                {
                    WasExecuted = true,
                    Image       = bitmap,
                    Bounds      = screenBounds,
                    Offset      = offset
                };
            }
        }

        protected override CaptureResult CreateDefault() => CaptureResult.Default;
    }
}


