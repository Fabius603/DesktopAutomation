using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class OcrStepDefinition : StepDefinition<OcrStep>
{
    public const string LanguagesFieldId = "languages";
    public const string LayoutFieldId = "page_layout";

    private static readonly string[] Languages = ["deu", "eng", "deu+eng"];
    private static readonly string[] Layouts = Enum.GetNames<OcrPageLayout>();

    public override StepDescriptor Descriptor { get; } = new(
        "ocr", "BildAuswerten", "Step.Type.Ocr", "Step.Description.Ocr", "text-recognition",
        [
            ImageDetectionStepDefinitionSupport.ImageSource(),
            new StepFieldDescriptor(LanguagesFieldId, "Ui.Step.Settings.OcrLanguages", StepValueKind.Enum,
                Required: true, DefaultValue: JsonValue.Create("deu+eng"), Order: 1,
                Constraints: new StepFieldConstraints(AllowedValues: Languages),
                Options: Languages.Select(value => new StepFieldOptionDescriptor(value, $"Enum.OcrLanguage.{value}")).ToArray()),
            new StepFieldDescriptor(LayoutFieldId, "Ui.Step.Settings.OcrLayout", StepValueKind.Enum,
                Required: true, DefaultValue: JsonValue.Create(nameof(OcrPageLayout.Automatic)), Order: 2,
                Constraints: new StepFieldConstraints(AllowedValues: Layouts),
                Options: Layouts.Select(value => new StepFieldOptionDescriptor(value, $"Enum.OcrPageLayout.{value}")).ToArray()),
            ImageDetectionStepDefinitionSupport.Roi(3)
        ],
        new StepPresentationDescriptor(
            [
                new StepEditorSectionDescriptor("general", null,
                    [ImageDetectionStepDefinitionSupport.ImageSourceFieldId, LanguagesFieldId, LayoutFieldId]),
                new StepEditorSectionDescriptor("advanced", "Ui.Step.Settings.Advanced",
                    [ImageDetectionStepDefinitionSupport.RoiFieldId], 1, true, false)
            ],
            [new StepSummaryItemDescriptor(LanguagesFieldId), new StepSummaryItemDescriptor(LayoutFieldId)],
            [ImageDetectionStepDefinitionSupport.ImageSourceFieldId, LanguagesFieldId, LayoutFieldId,
             ImageDetectionStepDefinitionSupport.RoiFieldId]));

    public override OcrStep CreateDefaultStep() => new();

    protected override StepDraft Read(OcrStep step)
    {
        var settings = step.Settings;
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[ImageDetectionStepDefinitionSupport.ImageSourceFieldId] = JsonSerializer.SerializeToNode(settings.ImageSource);
        draft.Values[LanguagesFieldId] = JsonValue.Create(settings.Languages);
        draft.Values[LayoutFieldId] = JsonValue.Create(settings.PageLayout.ToString());
        draft.Values[ImageDetectionStepDefinitionSupport.RoiFieldId] =
            ImageDetectionStepDefinitionSupport.WriteRoi(settings.EnableROI, settings.ROI, settings.DynamicRoiSource);
        return draft;
    }

    protected override void Apply(StepDraft draft, OcrStep step)
    {
        var settings = step.Settings;
        settings.ImageSource = DefinitionValueReader.Binding(draft, ImageDetectionStepDefinitionSupport.ImageSourceFieldId);
        settings.Languages = DefinitionValueReader.String(draft, LanguagesFieldId);
        settings.PageLayout = Enum.TryParse<OcrPageLayout>(DefinitionValueReader.String(draft, LayoutFieldId), out var layout)
            ? layout : OcrPageLayout.Automatic;
        var roi = ImageDetectionStepDefinitionSupport.ReadRoi(draft);
        settings.EnableROI = roi.Enabled;
        settings.ROI = roi.Roi;
        settings.DynamicRoiSource = roi.DynamicSource;
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft)
    {
        var common = ImageDetectionStepDefinitionSupport.ValidateCommon(draft);
        return common is null ? [] : [common];
    }
}
