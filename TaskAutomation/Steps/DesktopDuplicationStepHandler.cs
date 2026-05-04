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

            return await ctx.DesktopCaptureService.CaptureAsync(
                                step.Settings.DesktopIdx, ct,
                                captureCursor: step.Settings.CaptureCursor)
                            .ConfigureAwait(false);
        }

        protected override CaptureResult CreateDefault() => CaptureResult.Default;
    }
}

