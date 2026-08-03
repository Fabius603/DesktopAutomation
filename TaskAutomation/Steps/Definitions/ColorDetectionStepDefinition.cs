using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class ColorDetectionStepDefinition : StepDefinition<ColorDetectionStep>
{
    public const string ColorFieldId = "color_hex";
    public const string ConfidenceFieldId = "confidence_threshold";
    public const string MinSizeFieldId = "min_size";
    public const string MaxSizeFieldId = "max_size";
    public const string MinWidthFieldId = "min_width";
    public const string MinHeightFieldId = "min_height";
    public const string DownscaleFieldId = "downscale_factor";

    public override StepDescriptor Descriptor { get; } = new(
        "color_detection", "BildAuswerten", "Step.Type.ColorDetection", "Step.Description.ColorDetection",
        "color-detection",
        [
            ImageDetectionStepDefinitionSupport.ImageSource(),
            new StepFieldDescriptor(ColorFieldId, "Ui.Step.Settings.Color", StepValueKind.Color,
                Required: true, DefaultValue: JsonValue.Create("#FF0000"), Order: 1),
            new StepFieldDescriptor(ConfidenceFieldId, "Ui.Step.Settings.ConfidencePercent", StepValueKind.Number,
                DefaultValue: JsonValue.Create(0.9d), EditorHint: StepEditorHints.Percentage,
                Constraints: new StepFieldConstraints(Minimum: 0, Maximum: 1), Order: 2),
            ImageDetectionStepDefinitionSupport.Roi(3),
            PositiveInteger(MinSizeFieldId, "Ui.Step.Settings.MinSize", 25, 4),
            PositiveInteger(MaxSizeFieldId, "Ui.Step.Settings.MaxSize", int.MaxValue, 5),
            PositiveInteger(MinWidthFieldId, "Ui.Step.Settings.MinWidth", 1, 6),
            PositiveInteger(MinHeightFieldId, "Ui.Step.Settings.MinHeight", 1, 7),
            PositiveInteger(DownscaleFieldId, "Ui.Step.Settings.Downscale", 1, 8)
        ],
        new StepPresentationDescriptor(
            [
                new StepEditorSectionDescriptor("general", null,
                    [ImageDetectionStepDefinitionSupport.ImageSourceFieldId, ColorFieldId, ConfidenceFieldId]),
                new StepEditorSectionDescriptor("advanced", "Ui.Step.Settings.Advanced",
                    [ImageDetectionStepDefinitionSupport.RoiFieldId, MinSizeFieldId, MaxSizeFieldId,
                     MinWidthFieldId, MinHeightFieldId, DownscaleFieldId], 1, true, false)
            ],
            [new StepSummaryItemDescriptor(ColorFieldId), new StepSummaryItemDescriptor(ConfidenceFieldId)],
            [ImageDetectionStepDefinitionSupport.ImageSourceFieldId, ColorFieldId, ConfidenceFieldId,
             ImageDetectionStepDefinitionSupport.RoiFieldId, MinSizeFieldId, MaxSizeFieldId,
             MinWidthFieldId, MinHeightFieldId, DownscaleFieldId]));

    public override ColorDetectionStep CreateDefaultStep() => new();

    protected override StepDraft Read(ColorDetectionStep step)
    {
        var s = step.Settings;
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[ImageDetectionStepDefinitionSupport.ImageSourceFieldId] = JsonSerializer.SerializeToNode(s.ImageSource);
        draft.Values[ColorFieldId] = JsonValue.Create(s.ColorHex);
        draft.Values[ConfidenceFieldId] = JsonValue.Create(s.ConfidenceThreshold);
        draft.Values[ImageDetectionStepDefinitionSupport.RoiFieldId] =
            ImageDetectionStepDefinitionSupport.WriteRoi(s.EnableROI, s.ROI, s.DynamicRoiSource);
        draft.Values[MinSizeFieldId] = JsonValue.Create(s.MinSize);
        draft.Values[MaxSizeFieldId] = JsonValue.Create(s.MaxSize);
        draft.Values[MinWidthFieldId] = JsonValue.Create(s.MinWidth);
        draft.Values[MinHeightFieldId] = JsonValue.Create(s.MinHeight);
        draft.Values[DownscaleFieldId] = JsonValue.Create(s.DownscaleFactor);
        return draft;
    }

    protected override void Apply(StepDraft draft, ColorDetectionStep step)
    {
        var s = step.Settings;
        s.ImageSource = DefinitionValueReader.Binding(draft, ImageDetectionStepDefinitionSupport.ImageSourceFieldId);
        s.ColorHex = DefinitionValueReader.String(draft, ColorFieldId);
        s.ConfidenceThreshold = DefinitionValueReader.Number(draft, ConfidenceFieldId);
        var roi = ImageDetectionStepDefinitionSupport.ReadRoi(draft);
        s.EnableROI = roi.Enabled;
        s.ROI = roi.Roi;
        s.DynamicRoiSource = roi.DynamicSource;
        s.MinSize = DefinitionValueReader.Integer(draft, MinSizeFieldId);
        s.MaxSize = DefinitionValueReader.Integer(draft, MaxSizeFieldId);
        s.MinWidth = DefinitionValueReader.Integer(draft, MinWidthFieldId);
        s.MinHeight = DefinitionValueReader.Integer(draft, MinHeightFieldId);
        s.DownscaleFactor = DefinitionValueReader.Integer(draft, DownscaleFieldId);
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft)
    {
        var common = ImageDetectionStepDefinitionSupport.ValidateCommon(draft);
        if (common is not null) return [common];
        var color = DefinitionValueReader.String(draft, ColorFieldId);
        if (color.Length != 7 || color[0] != '#' || !int.TryParse(color[1..], System.Globalization.NumberStyles.HexNumber, null, out _))
            return [new("StepValidation.Invalid", ColorFieldId)];
        return DefinitionValueReader.Integer(draft, MaxSizeFieldId) < DefinitionValueReader.Integer(draft, MinSizeFieldId)
            ? [new("StepValidation.Invalid", MaxSizeFieldId)] : [];
    }

    private static StepFieldDescriptor PositiveInteger(string id, string labelKey, int defaultValue, int order) => new(
        id, labelKey, StepValueKind.Integer, DefaultValue: JsonValue.Create(defaultValue),
        Constraints: new StepFieldConstraints(Minimum: 1), Advanced: true, Order: order);
}
