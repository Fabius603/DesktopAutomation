using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Steps.Definitions;

public sealed class WindowsStateQueryStepDefinition : StepDefinition<WindowsStateQueryStep>
{
    public const string CapabilityFieldId = "capability";

    public override StepDescriptor Descriptor { get; } = WindowsCapabilityStepDefinitionSupport.CreateDescriptor(
        "windows_state_query", "Step.Type.WindowsStateQuery", "Step.Description.WindowsStateQuery",
        StepWindowsCapabilityPickerMode.StateQuery,
        new StepWindowsCapabilitySelectionValue("network.connectivity", new Dictionary<string, string?>()));

    public override WindowsStateQueryStep CreateDefaultStep() => new();

    protected override StepDraft Read(WindowsStateQueryStep step) => WindowsCapabilityStepDefinitionSupport.Read(
        Descriptor.TypeId, step.Settings.QueryType, step.Settings.Parameters);

    protected override void Apply(StepDraft draft, WindowsStateQueryStep step)
    {
        var value = WindowsCapabilityStepDefinitionSupport.ReadSelection(draft);
        step.Settings.QueryType = value.CapabilityId;
        step.Settings.Parameters = new(value.Parameters, StringComparer.OrdinalIgnoreCase);
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft) =>
        WindowsCapabilityStepDefinitionSupport.Validate(draft, StepWindowsCapabilityPickerMode.StateQuery);
}

internal static class WindowsCapabilityStepDefinitionSupport
{
    public static StepDescriptor CreateDescriptor(
        string typeId,
        string displayNameKey,
        string descriptionKey,
        StepWindowsCapabilityPickerMode mode,
        StepWindowsCapabilitySelectionValue defaultValue) => new(
        TypeId: typeId,
        CategoryId: "WindowsSystem",
        DisplayNameKey: displayNameKey,
        DescriptionKey: descriptionKey,
        IconKey: "windows",
        Fields:
        [
            new StepFieldDescriptor(WindowsStateQueryStepDefinition.CapabilityFieldId, "Ui.Windows.Capability", StepValueKind.Object,
                Required: true,
                DefaultValue: JsonSerializer.SerializeToNode(defaultValue),
                EditorHint: StepEditorHints.WindowsCapabilityPicker,
                Order: 0,
                WindowsCapabilityPickerOptions: new StepWindowsCapabilityPickerOptions(mode))
        ],
        Presentation: new StepPresentationDescriptor(
            EditorSections: [new StepEditorSectionDescriptor("general", null, [WindowsStateQueryStepDefinition.CapabilityFieldId])],
            SummaryItems: [new StepSummaryItemDescriptor(WindowsStateQueryStepDefinition.CapabilityFieldId)],
            DetailFieldIds: [WindowsStateQueryStepDefinition.CapabilityFieldId]));

    public static StepDraft Read(
        string typeId,
        string capabilityId,
        IReadOnlyDictionary<string, string?> parameters)
    {
        var draft = new StepDraft(typeId);
        draft.Values[WindowsStateQueryStepDefinition.CapabilityFieldId] = JsonSerializer.SerializeToNode(
            new StepWindowsCapabilitySelectionValue(capabilityId,
                new Dictionary<string, string?>(parameters, StringComparer.OrdinalIgnoreCase)));
        return draft;
    }

    public static StepWindowsCapabilitySelectionValue ReadSelection(StepDraft draft)
    {
        try
        {
            return draft.Values.GetValueOrDefault(WindowsStateQueryStepDefinition.CapabilityFieldId)
                       ?.Deserialize<StepWindowsCapabilitySelectionValue>()
                   ?? new StepWindowsCapabilitySelectionValue(string.Empty, new Dictionary<string, string?>());
        }
        catch (JsonException)
        {
            return new StepWindowsCapabilitySelectionValue(string.Empty, new Dictionary<string, string?>());
        }
        catch (InvalidOperationException)
        {
            return new StepWindowsCapabilitySelectionValue(string.Empty, new Dictionary<string, string?>());
        }
    }

    public static IReadOnlyList<StepValidationIssue> Validate(
        StepDraft draft,
        StepWindowsCapabilityPickerMode mode)
    {
        var value = ReadSelection(draft);
        var capability = new WindowsCapabilityCatalog().Find(value.CapabilityId);
        var supported = mode == StepWindowsCapabilityPickerMode.StateQuery
            ? capability?.SupportsStateQuery == true
            : capability?.SupportsSettingChange == true;
        if (!supported)
            return [Invalid()];

        foreach (var parameter in capability!.Parameters ?? [])
        {
            var parameterValue = ParameterValue(value, parameter.Name);
            if (parameter.Required && string.IsNullOrWhiteSpace(parameterValue))
                return [Invalid()];
            if (string.IsNullOrWhiteSpace(parameterValue)) continue;
            if (mode == StepWindowsCapabilityPickerMode.SettingChange
                && parameter.Type == WindowsParameterType.Integer
                && !int.TryParse(parameterValue, out _))
                return [Invalid()];
            if (mode == StepWindowsCapabilityPickerMode.SettingChange
                && parameter.AllowedValues is { Count: > 0 }
                && !parameter.AllowedValues.Contains(parameterValue, StringComparer.OrdinalIgnoreCase))
                return [Invalid()];
        }

        if (mode == StepWindowsCapabilityPickerMode.SettingChange && !ValidateSettingSpecific(value))
            return [Invalid()];
        return [];
    }

    private static bool ValidateSettingSpecific(StepWindowsCapabilitySelectionValue value)
    {
        if (value.CapabilityId == "audio.master_volume"
            && (!int.TryParse(ParameterValue(value, "value"), out var volume)
                || volume is < 0 or > 100))
            return false;
        if (value.CapabilityId is "power.display_timeout" or "power.sleep_timeout"
            && (!int.TryParse(ParameterValue(value, "minutes"), out var minutes) || minutes < 0))
            return false;
        if (value.CapabilityId == "network.wifi_connection"
            && string.Equals(ParameterValue(value, "action"), "connect", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(ParameterValue(value, "profile")))
            return false;
        return true;
    }

    private static string? ParameterValue(StepWindowsCapabilitySelectionValue value, string name) =>
        value.Parameters.FirstOrDefault(parameter =>
            parameter.Key.Equals(name, StringComparison.OrdinalIgnoreCase)).Value;

    private static StepValidationIssue Invalid() =>
        new("StepValidation.Invalid", WindowsStateQueryStepDefinition.CapabilityFieldId);
}
