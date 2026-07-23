using TaskAutomation.Jobs;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Steps;

public sealed class WindowsStateQueryStepHandler : DynamicJobStepHandler<WindowsStateQueryStep>
{
    private readonly IWindowsSystemStateService _states;
    public WindowsStateQueryStepHandler(IWindowsSystemStateService states) => _states = states;

    protected override async Task<StepResultBase> ExecuteCoreAsync(
        WindowsStateQueryStep step, IStepPipelineContext ctx, CancellationToken ct)
    {
        var result = await _states.QueryAsync(new WindowsStateQuery
        {
            QueryType = step.Settings.QueryType,
            Parameters = new Dictionary<string, string?>(step.Settings.Parameters, StringComparer.OrdinalIgnoreCase)
        }, ct).ConfigureAwait(false);
        return result with { WasExecuted = true };
    }
}
