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

            var result = await ctx.DesktopCaptureService.CaptureAsync(
                                step.Settings.DesktopIdx, ct,
                                captureCursor: step.Settings.CaptureCursor)
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
                WasExecuted = true, Image = result.Image, Bounds = result.Bounds, Offset = result.Offset,
                IsFresh = result.IsFresh, CaptureTimestampUtc = result.CaptureTimestampUtc
            };
        }

        protected override DesktopDuplicationResult CreateDefault() => DesktopDuplicationResult.Default;
    }
}

