using System.Text.Json;
using System.Text.Json.Nodes;
using System.IO;
using OpenCvSharp;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class TemplateMatchingStepDefinition : StepDefinition<TemplateMatchingStep>
{
    public const string TemplatePathFieldId = "template_path";
    public const string ConfidenceFieldId = "confidence_threshold";
    public const string MatchModeFieldId = "template_match_mode";
    public const string MultiplePointsFieldId = "multiple_points";

    private static readonly string[] MatchModes = Enum.GetNames<TemplateMatchModes>();

    public override StepDescriptor Descriptor { get; } = new(
        "template_matching", "BildAuswerten", "Step.Type.TemplateMatching", "Step.Description.TemplateMatching",
        "template-matching",
        [
            ImageDetectionStepDefinitionSupport.ImageSource(),
            new StepFieldDescriptor(TemplatePathFieldId, "Ui.Step.Settings.Template", StepValueKind.FilePath,
                Required: true, EditorHint: StepEditorHints.FilePicker, Order: 1,
                FilePickerOptions: new StepFilePickerOptions(StepFilePickerKind.Image, ShowPreview: true)),
            new StepFieldDescriptor(ConfidenceFieldId, "Ui.Step.Settings.ConfidencePercent", StepValueKind.Number,
                DefaultValue: JsonValue.Create(0.9d), EditorHint: StepEditorHints.Percentage,
                Constraints: new StepFieldConstraints(Minimum: 0, Maximum: 1), Order: 2),
            ImageDetectionStepDefinitionSupport.Roi(3),
            new StepFieldDescriptor(MatchModeFieldId, "Ui.Step.Settings.MatchMode", StepValueKind.Enum,
                Required: true, DefaultValue: JsonValue.Create(nameof(TemplateMatchModes.CCoeffNormed)),
                Constraints: new StepFieldConstraints(AllowedValues: MatchModes), Advanced: true, Order: 4,
                Options: MatchModes.Select(value => new StepFieldOptionDescriptor(value, $"Enum.TemplateMatchMode.{value}")).ToArray()),
            new StepFieldDescriptor(MultiplePointsFieldId, "Ui.Step.Settings.MultiplePoints", StepValueKind.Boolean,
                DefaultValue: JsonValue.Create(false), Order: 5)
        ],
        new StepPresentationDescriptor(
            [
                new StepEditorSectionDescriptor("general", null,
                    [ImageDetectionStepDefinitionSupport.ImageSourceFieldId, TemplatePathFieldId, ConfidenceFieldId]),
                new StepEditorSectionDescriptor("advanced", "Ui.Step.Settings.Advanced",
                    [ImageDetectionStepDefinitionSupport.RoiFieldId, MatchModeFieldId], 1, true, false)
            ],
            [new StepSummaryItemDescriptor(TemplatePathFieldId, StepSummaryValueFormat.FileName),
             new StepSummaryItemDescriptor(ConfidenceFieldId)],
            [ImageDetectionStepDefinitionSupport.ImageSourceFieldId, TemplatePathFieldId, ConfidenceFieldId,
             ImageDetectionStepDefinitionSupport.RoiFieldId, MatchModeFieldId]));

    public override TemplateMatchingStep CreateDefaultStep() => new();

    protected override StepDraft Read(TemplateMatchingStep step)
    {
        var s = step.Settings;
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[ImageDetectionStepDefinitionSupport.ImageSourceFieldId] = JsonSerializer.SerializeToNode(s.ImageSource);
        draft.Values[TemplatePathFieldId] = JsonValue.Create(s.TemplatePath);
        draft.Values[ConfidenceFieldId] = JsonValue.Create(s.ConfidenceThreshold);
        draft.Values[ImageDetectionStepDefinitionSupport.RoiFieldId] =
            ImageDetectionStepDefinitionSupport.WriteRoi(s.EnableROI, s.ROI, s.DynamicRoiSource);
        draft.Values[MatchModeFieldId] = JsonValue.Create(s.TemplateMatchMode.ToString());
        draft.Values[MultiplePointsFieldId] = JsonValue.Create(s.MultiplePoints);
        return draft;
    }

    protected override void Apply(StepDraft draft, TemplateMatchingStep step)
    {
        var s = step.Settings;
        s.ImageSource = DefinitionValueReader.Binding(draft, ImageDetectionStepDefinitionSupport.ImageSourceFieldId);
        s.TemplatePath = DefinitionValueReader.String(draft, TemplatePathFieldId);
        s.ConfidenceThreshold = DefinitionValueReader.Number(draft, ConfidenceFieldId);
        var roi = ImageDetectionStepDefinitionSupport.ReadRoi(draft);
        s.EnableROI = roi.Enabled;
        s.ROI = roi.Roi;
        s.DynamicRoiSource = roi.DynamicSource;
        s.TemplateMatchMode = Enum.TryParse<TemplateMatchModes>(DefinitionValueReader.String(draft, MatchModeFieldId), out var mode)
            ? mode : TemplateMatchModes.CCoeffNormed;
        s.MultiplePoints = DefinitionValueReader.Boolean(draft, MultiplePointsFieldId);
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
