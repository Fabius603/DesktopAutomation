using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Steps;

public sealed class BlockInputStepHandler : JobStepHandler<BlockInputStep, InputControlResult>
{
    protected override Task<InputControlResult> ExecuteCoreAsync(BlockInputStep step, IStepPipelineContext ctx, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        WindowsInputBlockController.Block(TimeSpan.FromSeconds(step.Settings.SafetyTimeoutSeconds));
        ctx.Logger.LogInformation("Maus- und Tastatureingaben für maximal {Seconds} Sekunden blockiert.", step.Settings.SafetyTimeoutSeconds);
        return Task.FromResult(new InputControlResult { WasExecuted = true, Success = true });
    }

    protected override InputControlResult CreateDefault() => InputControlResult.Default;
}

public sealed class UnblockInputStepHandler : JobStepHandler<UnblockInputStep, InputControlResult>
{
    protected override Task<InputControlResult> ExecuteCoreAsync(UnblockInputStep step, IStepPipelineContext ctx, CancellationToken ct)
    {
        WindowsInputBlockController.Unblock();
        ctx.Logger.LogInformation("Maus- und Tastatureingaben freigegeben.");
        return Task.FromResult(new InputControlResult { WasExecuted = true, Success = true });
    }

    protected override InputControlResult CreateDefault() => InputControlResult.Default;
}
