using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Steps
{
    public sealed class DesktopDuplicationStepHandler : JobStepHandler<DesktopDuplicationStep, DesktopDuplicationResult>
    {
        protected override async Task<DesktopDuplicationResult> ExecuteCoreAsync(
            DesktopDuplicationStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            ctx.Logger.LogDebug(
                "DesktopDuplicationStepHandler: Capturing monitor {MonitorIndex}", step.Settings.DesktopIdx);

            var settings = step.Settings;
            var result = await ctx.DesktopCaptureService.CaptureAsync(
                                new DesktopCaptureRequest(settings.DesktopIdx, settings.MonitorDeviceName,
                                    settings.CaptureCursor, settings.WaitForNewFrame, settings.TimeoutMilliseconds,
                                    settings.AllowCachedFallback), ct)
                            .ConfigureAwait(false);

            if (result.HasImage)
            {
                ctx.Logger.LogInformation(
                    "DesktopDuplicationStepHandler: Monitor {MonitorIndex} aufgenommen, Bounds={Bounds}, Frisch={IsFresh}, Cursor={CaptureCursor}.",
                    step.Settings.DesktopIdx, result.Bounds, result.IsFresh, step.Settings.CaptureCursor);
            }
            else
            {
                ctx.Logger.LogWarning(
                    "DesktopDuplicationStepHandler: Monitor {MonitorIndex} lieferte kein Bild.",
                    step.Settings.DesktopIdx);
            }

            return new DesktopDuplicationResult
            {
                WasExecuted = true,
                Image = result.Image,
                Bounds = result.Bounds,
                Offset = result.Offset,
                IsFresh = result.IsFresh,
                CaptureTimestampUtc = result.CaptureTimestampUtc,
                FrameVersion = result.FrameVersion,
                FrameTimestamp = result.FrameTimestamp
            };
        }

        protected override DesktopDuplicationResult CreateDefault() => DesktopDuplicationResult.Default;
    }
}

