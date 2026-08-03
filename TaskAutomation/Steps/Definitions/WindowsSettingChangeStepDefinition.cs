using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class WindowsSettingChangeStepDefinition : StepDefinition<WindowsSettingChangeStep>
{
    public override StepDescriptor Descriptor { get; } = WindowsCapabilityStepDefinitionSupport.CreateDescriptor(
        "windows_setting_change", "Step.Type.WindowsSettingChange", "Step.Description.WindowsSettingChange",
        StepWindowsCapabilityPickerMode.SettingChange,
        new StepWindowsCapabilitySelectionValue("audio.master_volume",
            new Dictionary<string, string?> { ["value"] = "50" }));

    public override WindowsSettingChangeStep CreateDefaultStep() => new();

    protected override StepDraft Read(WindowsSettingChangeStep step) => WindowsCapabilityStepDefinitionSupport.Read(
        Descriptor.TypeId, step.Settings.SettingId, step.Settings.Parameters);

    protected override void Apply(StepDraft draft, WindowsSettingChangeStep step)
    {
        var value = WindowsCapabilityStepDefinitionSupport.ReadSelection(draft);
        step.Settings.SettingId = value.CapabilityId;
        step.Settings.Parameters = new(value.Parameters, StringComparer.OrdinalIgnoreCase);
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft) =>
        WindowsCapabilityStepDefinitionSupport.Validate(draft, StepWindowsCapabilityPickerMode.SettingChange);
}
