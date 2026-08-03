using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class KeyPointMatchingStepDefinition : StepDefinition<KeyPointMatchingStep>
{
    public const string TemplatePathFieldId = "template_path";
    public const string MinimumMatchesFieldId = "min_match_count";
    public const string RatioFieldId = "lowes_ratio_threshold";

    public override StepDescriptor Descriptor { get; } = new(
        "keypoint_matching", "BildAuswerten", "Step.Type.KeyPointMatching", "Step.Description.KeyPointMatching",
        "keypoint-matching",
        [
            ImageDetectionStepDefinitionSupport.ImageSource(),
            new StepFieldDescriptor(TemplatePathFieldId, "Ui.Step.Settings.Template", StepValueKind.FilePath,
                Required: true, EditorHint: StepEditorHints.FilePicker, Order: 1,
                FilePickerOptions: new StepFilePickerOptions(StepFilePickerKind.Image, ShowPreview: true)),
            ImageDetectionStepDefinitionSupport.Roi(2),
            new StepFieldDescriptor(MinimumMatchesFieldId, "Ui.Step.Settings.MinMatches", StepValueKind.Integer,
                DefaultValue: JsonValue.Create(10), Constraints: new StepFieldConstraints(Minimum: 1),
                Advanced: true, Order: 3),
            new StepFieldDescriptor(RatioFieldId, "Ui.Step.Settings.LoweSRatio01", StepValueKind.Number,
                DefaultValue: JsonValue.Create(0.75d), Constraints: new StepFieldConstraints(Minimum: 0.000001m, Maximum: 1),
                Advanced: true, Order: 4)
        ],
        new StepPresentationDescriptor(
            [
                new StepEditorSectionDescriptor("general", null,
                    [ImageDetectionStepDefinitionSupport.ImageSourceFieldId, TemplatePathFieldId]),
                new StepEditorSectionDescriptor("advanced", "Ui.Step.Settings.Advanced",
                    [ImageDetectionStepDefinitionSupport.RoiFieldId, MinimumMatchesFieldId, RatioFieldId], 1, true, false)
            ],
            [new StepSummaryItemDescriptor(TemplatePathFieldId, StepSummaryValueFormat.FileName),
             new StepSummaryItemDescriptor(MinimumMatchesFieldId)],
            [ImageDetectionStepDefinitionSupport.ImageSourceFieldId, TemplatePathFieldId,
             ImageDetectionStepDefinitionSupport.RoiFieldId, MinimumMatchesFieldId, RatioFieldId]));

    public override KeyPointMatchingStep CreateDefaultStep() => new();

    protected override StepDraft Read(KeyPointMatchingStep step)
    {
        var s = step.Settings;
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[ImageDetectionStepDefinitionSupport.ImageSourceFieldId] = JsonSerializer.SerializeToNode(s.ImageSource);
        draft.Values[TemplatePathFieldId] = JsonValue.Create(s.TemplatePath);
        draft.Values[ImageDetectionStepDefinitionSupport.RoiFieldId] =
            ImageDetectionStepDefinitionSupport.WriteRoi(s.EnableROI, s.ROI, s.DynamicRoiSource);
        draft.Values[MinimumMatchesFieldId] = JsonValue.Create(s.MinMatchCount);
        draft.Values[RatioFieldId] = JsonValue.Create(s.LowesRatioThreshold);
        return draft;
    }

    protected override void Apply(StepDraft draft, KeyPointMatchingStep step)
    {
        var s = step.Settings;
        s.ImageSource = DefinitionValueReader.Binding(draft, ImageDetectionStepDefinitionSupport.ImageSourceFieldId);
        s.TemplatePath = DefinitionValueReader.String(draft, TemplatePathFieldId);
        var roi = ImageDetectionStepDefinitionSupport.ReadRoi(draft);
        s.EnableROI = roi.Enabled;
        s.ROI = roi.Roi;
        s.DynamicRoiSource = roi.DynamicSource;
        s.MinMatchCount = DefinitionValueReader.Integer(draft, MinimumMatchesFieldId);
        s.LowesRatioThreshold = DefinitionValueReader.Number(draft, RatioFieldId);
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft)
    {
        var common = ImageDetectionStepDefinitionSupport.ValidateCommon(draft);
        if (common is not null) return [common];
        var path = DefinitionValueReader.String(draft, TemplatePathFieldId);
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return [new("StepValidation.FileNotFound", TemplatePathFieldId)];
        return [];
    }
}
