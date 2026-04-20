using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    public sealed class TimeoutStepHandler : JobStepHandler<TimeoutStep, TaskResult>
    {
        protected override async Task<TaskResult> ExecuteCoreAsync(
            TimeoutStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            await Task.Delay(step.Settings.DelayMs, ct).ConfigureAwait(false);
            return new TaskResult { WasExecuted = true, Success = true };
        }

        protected override TaskResult CreateDefault() => TaskResult.Default;
    }
}
