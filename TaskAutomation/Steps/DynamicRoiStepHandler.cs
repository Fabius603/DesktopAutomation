using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public sealed class DynamicRoiStepHandler : JobStepHandler<DynamicRoiStep, DynamicRoiResult>
{
    protected override Task<DynamicRoiResult> ExecuteCoreAsync(DynamicRoiStep step, IStepPipelineContext ctx, CancellationToken ct)
    {
        var resolved = ResultBindingResolver.Resolve<Rectangle>(ctx.Results, step.Settings.BoundsSource);
        var detection = resolved.SourceResult as IDetectionStepResult;
        if (!ctx.DynamicRoiStates.TryGetValue(step.Id, out var state))
            ctx.DynamicRoiStates[step.Id] = state = new DynamicRoiState();

        state.FullSearchInterval = step.Settings.FullSearchInterval;
        var updated = false;
        var reset = false;
        var confidence = detection?.Confidence ?? 1.0;
        if (resolved.IsSuccess && confidence >= step.Settings.MinimumConfidence)
        {
            var box = resolved.FirstOrDefault;
            var padding = System.Math.Max(0, step.Settings.Padding);
            box.Inflate(padding, padding);
            state.GlobalBounds = box;
            state.ConsecutiveMisses = 0;
            updated = true;
            ctx.Logger.LogInformation(
                "Dynamic ROI {StepId}: ROI aktualisiert auf global X={X}, Y={Y}, B={Width}, H={Height}; Confidence={Confidence:F3}, Padding={Padding}.",
                step.Id, box.X, box.Y, box.Width, box.Height, confidence, padding);
        }
        else
        {
            state.ConsecutiveMisses++;
            if (step.Settings.ResetAfterMisses > 0
                && state.ConsecutiveMisses >= step.Settings.ResetAfterMisses)
            {
                state.GlobalBounds = null;
                state.RoiUsesSinceFullSearch = 0;
                state.ConsecutiveMisses = 0;
                reset = true;
                ctx.Logger.LogInformation(
                    "Dynamic ROI {StepId}: ROI nach {Misses} erfolglosen Versuchen entfernt; nächste Suche nutzt den Basis-Suchbereich.",
                    step.Id, step.Settings.ResetAfterMisses);
            }
            else
                ctx.Logger.LogDebug("Dynamic ROI {StepId}: kein gültiger Treffer; Fehlversuche={Misses}/{Limit}.",
                    step.Id, state.ConsecutiveMisses, step.Settings.ResetAfterMisses);
        }

        return Task.FromResult(new DynamicRoiResult
        {
            WasExecuted = true,
            RoiUpdated = updated,
            RoiReset = reset,
            GlobalBounds = state.GlobalBounds,
            ConsecutiveMisses = state.ConsecutiveMisses,
            FullSearchInterval = state.FullSearchInterval
        });
    }

    protected override DynamicRoiResult CreateDefault() => DynamicRoiResult.Default;
}
