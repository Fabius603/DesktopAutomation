using System.Text.Json;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class ShowOnDesktopStepDefinition : StepDefinition<ShowOnDesktopStep>
{
    public const string OverlayFieldId = "overlay";

    public override StepDescriptor Descriptor { get; } = new(
        "show_on_desktop", "AnzeigenSpeichern", "Step.Type.ShowOnDesktop",
        "Step.Description.ShowOnDesktop", "desktop-overlay",
        [new(OverlayFieldId, "Ui.Step.Settings.Overlays", StepValueKind.Object, true,
            JsonSerializer.SerializeToNode(new VisualOverlaySettings()),
            EditorHint: StepEditorHints.VisualOverlay,
            VisualOverlayOptions: new("detections", "text", SupportsDesktopPlacement: true))],
        new(
            [new("overlay", null, [OverlayFieldId])],
            [new(OverlayFieldId)],
            [OverlayFieldId]));

    public override ShowOnDesktopStep CreateDefaultStep() => new();

    protected override StepDraft Read(ShowOnDesktopStep step)
    {
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[OverlayFieldId] = VisualOverlayDefinitionValue.Write(
            step.Settings.Overlay, step.Settings.DetectionsSource);
        return draft;
    }

    protected override void Apply(StepDraft draft, ShowOnDesktopStep step)
    {
        step.Settings.Overlay = VisualOverlayDefinitionValue.Read(draft, OverlayFieldId);
        step.Settings.DetectionsSource = new ResultBinding();
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft) =>
        VisualOverlayDefinitionValue.IsValid(
            VisualOverlayDefinitionValue.Read(draft, OverlayFieldId), true)
            ? [] : [new("StepValidation.Invalid", OverlayFieldId)];
}
