using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;
using TaskAutomation.Timing;

namespace TaskAutomation.Steps
{
    public sealed class TimeoutStepHandler : JobStepHandler<TimeoutStep, TimeoutResult>
    {
        private readonly IPreciseDelayService _delayService;

        public TimeoutStepHandler(IPreciseDelayService delayService) =>
            _delayService = delayService;

        protected override async Task<TimeoutResult> ExecuteCoreAsync(
            TimeoutStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            await _delayService.DelayAsync(
                TimeSpan.FromMilliseconds(step.Settings.DelayMs),
                ct).ConfigureAwait(false);
            ctx.Logger.LogInformation(
                "TimeoutStepHandler: Wartezeit von {DelayMs} ms abgeschlossen.",
                step.Settings.DelayMs);
            return new TimeoutResult { WasExecuted = true, Success = true };
        }

        protected override TimeoutResult CreateDefault() => TimeoutResult.Default;
    }
}
