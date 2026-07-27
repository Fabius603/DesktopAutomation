using TaskAutomation.Jobs;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Steps;

public sealed class WindowsSettingChangeStepHandler
    : JobStepHandler<WindowsSettingChangeStep, WindowsSettingChangeResult>
{
    private readonly IWindowsSystemSettingService _settings;

    public WindowsSettingChangeStepHandler(IWindowsSystemSettingService settings) => _settings = settings;

    protected override async Task<WindowsSettingChangeResult> ExecuteCoreAsync(
        WindowsSettingChangeStep step,
        IStepPipelineContext context,
        CancellationToken cancellationToken)
    {
        var result = await _settings.ChangeAsync(new WindowsSettingChange
        {
            SettingId = step.Settings.SettingId,
            Parameters = new Dictionary<string, string?>(
                step.Settings.Parameters, StringComparer.OrdinalIgnoreCase)
        }, cancellationToken).ConfigureAwait(false);
        return result with { WasExecuted = true };
    }

    protected override WindowsSettingChangeResult CreateDefault() =>
        WindowsSettingChangeResult.Default;
}
