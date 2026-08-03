using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class YoloDetectionStepDefinition : StepDefinition<YOLODetectionStep>
{
    public const string SelectionFieldId = "yolo_selection";
    public const string ConfidenceFieldId = "confidence_threshold";

    public override StepDescriptor Descriptor { get; } = new(
        "yolo_detection", "BildAuswerten", "Step.Type.YoloDetection", "Step.Description.YoloDetection",
        "yolo-detection",
        [
            ImageDetectionStepDefinitionSupport.ImageSource(),
            new StepFieldDescriptor(SelectionFieldId, "Ui.Step.Settings.Model", StepValueKind.Object,
                Required: true,
                DefaultValue: JsonSerializer.SerializeToNode(new StepYoloSelectionValue(string.Empty, string.Empty)),
                EditorHint: StepEditorHints.YoloPicker, Order: 1,
                YoloPickerOptions: new StepYoloPickerOptions(ConfidenceFieldId)),
            ImageDetectionStepDefinitionSupport.Roi(2),
            new StepFieldDescriptor(ConfidenceFieldId, "Ui.Step.Settings.ConfidencePercent", StepValueKind.Number,
                DefaultValue: JsonValue.Create(0.5d), EditorHint: StepEditorHints.Percentage,
                Constraints: new StepFieldConstraints(Minimum: 0, Maximum: 1), Advanced: true, Order: 3)
        ],
        new StepPresentationDescriptor(
            [
                new StepEditorSectionDescriptor("general", null,
                    [ImageDetectionStepDefinitionSupport.ImageSourceFieldId, SelectionFieldId]),
                new StepEditorSectionDescriptor("advanced", "Ui.Step.Settings.Advanced",
                    [ImageDetectionStepDefinitionSupport.RoiFieldId, ConfidenceFieldId], 1, true, false)
            ],
            [new StepSummaryItemDescriptor(SelectionFieldId), new StepSummaryItemDescriptor(ConfidenceFieldId)],
            [ImageDetectionStepDefinitionSupport.ImageSourceFieldId, SelectionFieldId,
             ImageDetectionStepDefinitionSupport.RoiFieldId, ConfidenceFieldId]));

    public override YOLODetectionStep CreateDefaultStep() => new();

    protected override StepDraft Read(YOLODetectionStep step)
    {
        var s = step.Settings;
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[ImageDetectionStepDefinitionSupport.ImageSourceFieldId] = JsonSerializer.SerializeToNode(s.ImageSource);
        draft.Values[SelectionFieldId] = JsonSerializer.SerializeToNode(new StepYoloSelectionValue(s.Model, s.ClassName));
        draft.Values[ImageDetectionStepDefinitionSupport.RoiFieldId] =
            ImageDetectionStepDefinitionSupport.WriteRoi(s.EnableROI, s.ROI, s.DynamicRoiSource);
        draft.Values[ConfidenceFieldId] = JsonValue.Create(s.ConfidenceThreshold);
        return draft;
    }

    protected override void Apply(StepDraft draft, YOLODetectionStep step)
    {
        var s = step.Settings;
        s.ImageSource = DefinitionValueReader.Binding(draft, ImageDetectionStepDefinitionSupport.ImageSourceFieldId);
        var selection = ReadSelection(draft);
        s.Model = selection.Model;
        s.ClassName = selection.ClassName;
        var roi = ImageDetectionStepDefinitionSupport.ReadRoi(draft);
        s.EnableROI = roi.Enabled;
        s.ROI = roi.Roi;
        s.DynamicRoiSource = roi.DynamicSource;
        s.ConfidenceThreshold = (float)DefinitionValueReader.Number(draft, ConfidenceFieldId);
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft)
    {
        var common = ImageDetectionStepDefinitionSupport.ValidateCommon(draft);
        if (common is not null) return [common];
        var selection = ReadSelection(draft);
        if (string.IsNullOrWhiteSpace(selection.Model) || string.IsNullOrWhiteSpace(selection.ClassName))
            return [new("StepValidation.Required", SelectionFieldId)];
        return [];
    }

    private static StepYoloSelectionValue ReadSelection(StepDraft draft)
    {
        try
        {
            return draft.Values.GetValueOrDefault(SelectionFieldId)?.Deserialize<StepYoloSelectionValue>()
                ?? new(string.Empty, string.Empty);
        }
        catch (JsonException) { return new(string.Empty, string.Empty); }
        catch (InvalidOperationException) { return new(string.Empty, string.Empty); }
    }
}
