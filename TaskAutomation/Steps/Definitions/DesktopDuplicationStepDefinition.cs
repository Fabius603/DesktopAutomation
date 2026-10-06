using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class DesktopDuplicationStepDefinition : StepDefinition<DesktopDuplicationStep>
{
    public const string DesktopIndexFieldId = "desktop_idx";
    public const string CaptureCursorFieldId = "capture_cursor";
    public const string MonitorDeviceNameFieldId = "monitor_device_name";
    public const string WaitForNewFrameFieldId = "wait_for_new_frame";
    public const string TimeoutFieldId = "timeout_ms";
    public const string AllowCachedFallbackFieldId = "allow_cached_fallback";

    public override StepDescriptor Descriptor { get; } = new(
        TypeId: "desktop_duplication",
        CategoryId: "BildAufnehmen",
        DisplayNameKey: "Step.Type.DesktopDuplication",
        DescriptionKey: "Step.Description.DesktopDuplication",
        IconKey: "monitor-screenshot",
        Fields:
        [
            new StepFieldDescriptor(
                Id: DesktopIndexFieldId,
                LabelKey: "Ui.Step.Settings.DesktopIndex",
                ValueKind: StepValueKind.Integer,
                Required: true,
                DefaultValue: JsonValue.Create(0),
                EditorHint: StepEditorHints.MonitorPicker,
                Constraints: new StepFieldConstraints(Minimum: 0),
                Order: 0,
                MonitorDeviceNameFieldId: MonitorDeviceNameFieldId),
            new StepFieldDescriptor(
                Id: CaptureCursorFieldId,
                LabelKey: "Ui.Step.Settings.CaptureMousePointer",
                ValueKind: StepValueKind.Boolean,
                DefaultValue: JsonValue.Create(false),
                Order: 1),
            new StepFieldDescriptor(WaitForNewFrameFieldId, "Ui.Step.Capture.WaitForNewFrame", StepValueKind.Boolean,
                DefaultValue: JsonValue.Create(true), Order: 2),
            new StepFieldDescriptor(TimeoutFieldId, "Ui.Step.Capture.Timeout", StepValueKind.Integer,
                Required: true, DefaultValue: JsonValue.Create(DesktopDuplicationSettings.DefaultTimeoutMilliseconds),
                DescriptionKey: "Ui.Step.Capture.TimeoutHelp",
                Constraints: new StepFieldConstraints(Minimum: 1, Maximum: DesktopDuplicationSettings.MaximumTimeoutMilliseconds), Order: 3),
            new StepFieldDescriptor(AllowCachedFallbackFieldId, "Ui.Step.Capture.AllowCachedFallback", StepValueKind.Boolean,
                DefaultValue: JsonValue.Create(true), Advanced: true, Order: 4),
            new StepFieldDescriptor(MonitorDeviceNameFieldId, "Ui.Step.Capture.MonitorIdentity", StepValueKind.Text,
                DefaultValue: JsonValue.Create(string.Empty), DescriptionKey: "Ui.Step.Capture.MonitorIdentityHelp",
                Advanced: true, Order: 5)
        ],
        Presentation: new StepPresentationDescriptor(
            EditorSections:
            [
                new StepEditorSectionDescriptor(
                    "general",
                    null,
                    [DesktopIndexFieldId, CaptureCursorFieldId, WaitForNewFrameFieldId, TimeoutFieldId]),
                new StepEditorSectionDescriptor("capture_advanced", "Ui.Step.Capture.Advanced",
                    [AllowCachedFallbackFieldId, MonitorDeviceNameFieldId], Order: 1, Collapsible: true, InitiallyExpanded: false)
            ],
            SummaryItems:
            [
                new StepSummaryItemDescriptor(DesktopIndexFieldId),
                new StepSummaryItemDescriptor(CaptureCursorFieldId, StepSummaryValueFormat.BooleanBadge)
            ],
            DetailFieldIds: [DesktopIndexFieldId, CaptureCursorFieldId, WaitForNewFrameFieldId, TimeoutFieldId,
                AllowCachedFallbackFieldId, MonitorDeviceNameFieldId]));

    public override DesktopDuplicationStep CreateDefaultStep() => new();

    protected override StepDraft Read(DesktopDuplicationStep step)
    {
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[DesktopIndexFieldId] = JsonValue.Create(step.Settings.DesktopIdx);
        draft.Values[CaptureCursorFieldId] = JsonValue.Create(step.Settings.CaptureCursor);
        draft.Values[MonitorDeviceNameFieldId] = JsonValue.Create(step.Settings.MonitorDeviceName);
        draft.Values[WaitForNewFrameFieldId] = JsonValue.Create(step.Settings.WaitForNewFrame);
        draft.Values[TimeoutFieldId] = JsonValue.Create(step.Settings.TimeoutMilliseconds);
        draft.Values[AllowCachedFallbackFieldId] = JsonValue.Create(step.Settings.AllowCachedFallback);
        return draft;
    }

    protected override void Apply(StepDraft draft, DesktopDuplicationStep step)
    {
        if (!TryGetDesktopIndex(draft, out var desktopIndex))
            throw new InvalidOperationException("The desktop-capture draft does not contain a valid monitor index.");
        if (!TryGetCaptureCursor(draft, out var captureCursor))
            throw new InvalidOperationException("The desktop-capture draft does not contain a valid cursor option.");

        step.Settings.DesktopIdx = desktopIndex;
        step.Settings.CaptureCursor = captureCursor;
        step.Settings.MonitorDeviceName = DefinitionValueReader.String(draft, MonitorDeviceNameFieldId);
        step.Settings.WaitForNewFrame = ReadBoolean(draft, WaitForNewFrameFieldId, true);
        step.Settings.TimeoutMilliseconds = draft.Values.ContainsKey(TimeoutFieldId)
            ? DefinitionValueReader.Integer(draft, TimeoutFieldId) : DesktopDuplicationSettings.DefaultTimeoutMilliseconds;
        step.Settings.AllowCachedFallback = ReadBoolean(draft, AllowCachedFallbackFieldId, true);
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft) => [];

    private static bool ReadBoolean(StepDraft draft, string id, bool fallback)
        => draft.Values.ContainsKey(id) ? DefinitionValueReader.Boolean(draft, id) : fallback;

    private static bool TryGetDesktopIndex(StepDraft draft, out int desktopIndex)
    {
        desktopIndex = 0;
        if (!draft.Values.TryGetValue(DesktopIndexFieldId, out var value) || value is null)
            return false;
        return DefinitionValueReader.TryInteger(value, out desktopIndex);
    }

    private static bool TryGetCaptureCursor(StepDraft draft, out bool captureCursor)
    {
        captureCursor = false;
        if (!draft.Values.TryGetValue(CaptureCursorFieldId, out var value) || value is null)
            return true;
        return DefinitionValueReader.TryBoolean(value, out captureCursor);
    }
}
