using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class ShowImageStepDefinition : StepDefinition<ShowImageStep>
{
    public const string ImageSourceFieldId = "image_source";
    public const string WindowNameFieldId = "window_name";
    public const string OverlayFieldId = "overlay";

    public override StepDescriptor Descriptor { get; } = new(
        "show_image", "AnzeigenSpeichern", "Step.Type.ShowImage", "Step.Description.ShowImage", "image",
        [
            new(ImageSourceFieldId, "Ui.Step.Settings.CaptureStep", StepValueKind.ResultBinding, true,
                EditorHint: StepEditorHints.ResultBindingPicker, InputContractId: "image", Order: 0),
            new(WindowNameFieldId, "Ui.Step.Settings.WindowName", StepValueKind.Text, true,
                DefaultValue: JsonValue.Create("MyWindow"), Order: 1),
            new(OverlayFieldId, "Ui.Step.Settings.Overlays", StepValueKind.Object,
                DefaultValue: JsonSerializer.SerializeToNode(new VisualOverlaySettings()),
                EditorHint: StepEditorHints.VisualOverlay, Advanced: true, Order: 2,
                VisualOverlayOptions: new("detections", "text"))
        ],
        new(
            [new("general", null, [ImageSourceFieldId, WindowNameFieldId]),
             new("overlay", "Ui.Step.Settings.Overlays", [OverlayFieldId], 1, true, true)],
            [new(WindowNameFieldId, StepSummaryValueFormat.ShortText), new(ImageSourceFieldId)],
            [ImageSourceFieldId, WindowNameFieldId, OverlayFieldId]));

    public override ShowImageStep CreateDefaultStep() => new();

    protected override StepDraft Read(ShowImageStep step)
    {
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[ImageSourceFieldId] = JsonSerializer.SerializeToNode(step.Settings.ImageSource);
        draft.Values[WindowNameFieldId] = JsonValue.Create(step.Settings.WindowName);
        draft.Values[OverlayFieldId] = VisualOverlayDefinitionValue.Write(
            step.Settings.Overlay, step.Settings.DetectionsSource);
        return draft;
    }

    protected override void Apply(StepDraft draft, ShowImageStep step)
    {
        step.Settings.ImageSource = DefinitionValueReader.Binding(draft, ImageSourceFieldId);
        step.Settings.WindowName = DefinitionValueReader.String(draft, WindowNameFieldId);
        step.Settings.Overlay = VisualOverlayDefinitionValue.Read(draft, OverlayFieldId);
        step.Settings.DetectionsSource = new ResultBinding();
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft)
    {
        if (!DefinitionValueReader.Binding(draft, ImageSourceFieldId).IsConfigured)
            return [new("StepValidation.Required", ImageSourceFieldId)];
        if (string.IsNullOrWhiteSpace(DefinitionValueReader.String(draft, WindowNameFieldId)))
            return [new("StepValidation.Required", WindowNameFieldId)];
        return VisualOverlayDefinitionValue.IsValid(
            VisualOverlayDefinitionValue.Read(draft, OverlayFieldId), false)
            ? [] : [new("StepValidation.Invalid", OverlayFieldId)];
    }
}
