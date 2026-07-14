using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using TaskAutomation.Timing;

namespace TaskAutomation.Steps
{
    public sealed class TimeoutStepHandler : JobStepHandler<TimeoutStep, TaskResult>
    {
        private readonly IPreciseDelayService _delayService;

        public TimeoutStepHandler(IPreciseDelayService delayService) =>
            _delayService = delayService;

        protected override async Task<TaskResult> ExecuteCoreAsync(
            TimeoutStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            await _delayService.DelayAsync(
                TimeSpan.FromMilliseconds(step.Settings.DelayMs),
                ct).ConfigureAwait(false);
            return new TaskResult { WasExecuted = true, Success = true };
        }

        protected override TaskResult CreateDefault() => TaskResult.Default;
    }
}
