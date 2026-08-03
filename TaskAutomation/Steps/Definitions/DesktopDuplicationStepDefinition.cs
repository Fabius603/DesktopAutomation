using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class DesktopDuplicationStepDefinition : StepDefinition<DesktopDuplicationStep>
{
    public const string DesktopIndexFieldId = "desktop_idx";
    public const string CaptureCursorFieldId = "capture_cursor";

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
                Order: 0),
            new StepFieldDescriptor(
                Id: CaptureCursorFieldId,
                LabelKey: "Ui.Step.Settings.CaptureMousePointer",
                ValueKind: StepValueKind.Boolean,
                DefaultValue: JsonValue.Create(false),
                Order: 1)
        ],
        Presentation: new StepPresentationDescriptor(
            EditorSections:
            [
                new StepEditorSectionDescriptor(
                    "general",
                    null,
                    [DesktopIndexFieldId, CaptureCursorFieldId])
            ],
            SummaryItems:
            [
                new StepSummaryItemDescriptor(DesktopIndexFieldId),
                new StepSummaryItemDescriptor(CaptureCursorFieldId, StepSummaryValueFormat.BooleanBadge)
            ],
            DetailFieldIds: [DesktopIndexFieldId, CaptureCursorFieldId]));

    public override DesktopDuplicationStep CreateDefaultStep() => new();

    protected override StepDraft Read(DesktopDuplicationStep step)
    {
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[DesktopIndexFieldId] = JsonValue.Create(step.Settings.DesktopIdx);
        draft.Values[CaptureCursorFieldId] = JsonValue.Create(step.Settings.CaptureCursor);
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
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft) => [];

    private static bool TryGetDesktopIndex(StepDraft draft, out int desktopIndex)
    {
        desktopIndex = 0;
        if (!draft.Values.TryGetValue(DesktopIndexFieldId, out var value) || value is null)
            return false;
        try
        {
            desktopIndex = value.GetValue<int>();
            return true;
        }
        catch (InvalidOperationException) { return false; }
        catch (FormatException) { return false; }
    }

    private static bool TryGetCaptureCursor(StepDraft draft, out bool captureCursor)
    {
        captureCursor = false;
        if (!draft.Values.TryGetValue(CaptureCursorFieldId, out var value) || value is null)
            return true;
        try
        {
            captureCursor = value.GetValue<bool>();
            return true;
        }
        catch (InvalidOperationException) { return false; }
        catch (FormatException) { return false; }
    }
}
