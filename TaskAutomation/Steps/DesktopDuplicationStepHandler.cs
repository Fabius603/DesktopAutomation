using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Steps
{
    public sealed class DesktopDuplicationStepHandler : JobStepHandler<DesktopDuplicationStep, CaptureResult>
    {
        protected override async Task<CaptureResult> ExecuteCoreAsync(
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

            return result;
        }

        protected override CaptureResult CreateDefault() => CaptureResult.Default;
    }
}

