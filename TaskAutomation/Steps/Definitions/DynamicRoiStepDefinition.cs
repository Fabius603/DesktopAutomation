using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class DynamicRoiStepDefinition : StepDefinition<DynamicRoiStep>
{
    private const string LegacyPaddingDraftKey = "__legacy_padding";
    public const string BoundsSourceFieldId = "bounds_source";
    public const string PaddingSourceFieldId = "padding_source";
    public const string MinimumConfidenceFieldId = "minimum_confidence";
    public const string FullSearchIntervalFieldId = "full_search_interval";
    public const string ResetAfterMissesFieldId = "reset_after_misses";

    public override StepDescriptor Descriptor { get; } = new(
        TypeId: "dynamic_roi",
        CategoryId: "BildAuswerten",
        DisplayNameKey: "Step.Type.DynamicRoi",
        DescriptionKey: "Ui.Step.DynamicRoi.Description",
        IconKey: "dynamic-roi",
        Fields:
        [
            new StepFieldDescriptor(BoundsSourceFieldId, "Ui.Step.DynamicRoi.Source", StepValueKind.ResultBinding,
                Required: true, EditorHint: StepEditorHints.ValueReferencePicker, Order: 0, InputContractId: "bounds"),
            new StepFieldDescriptor(PaddingSourceFieldId, "Ui.Step.DynamicRoi.Padding", StepValueKind.ResultBinding,
                Required: false, EditorHint: StepEditorHints.ValueReferencePicker, Order: 1, InputContractId: "padding"),
            new StepFieldDescriptor(MinimumConfidenceFieldId, "Ui.Step.DynamicRoi.MinimumConfidence", StepValueKind.Number,
                DefaultValue: JsonValue.Create(0d), EditorHint: StepEditorHints.Percentage,
                Constraints: new StepFieldConstraints(Minimum: 0, Maximum: 1), Advanced: true, Order: 2),
            new StepFieldDescriptor(FullSearchIntervalFieldId, "Ui.Step.DynamicRoi.FullSearchInterval", StepValueKind.Integer,
                DefaultValue: JsonValue.Create(10), Constraints: new StepFieldConstraints(Minimum: 0), Advanced: true, Order: 3),
            new StepFieldDescriptor(ResetAfterMissesFieldId, "Ui.Step.DynamicRoi.ResetAfterMisses", StepValueKind.Integer,
                DefaultValue: JsonValue.Create(3), Constraints: new StepFieldConstraints(Minimum: 0), Advanced: true, Order: 4)
        ],
        Presentation: new StepPresentationDescriptor(
            EditorSections:
            [
                new StepEditorSectionDescriptor("general", null, [BoundsSourceFieldId, PaddingSourceFieldId]),
                new StepEditorSectionDescriptor("advanced", "Ui.Step.Settings.Advanced",
                    [MinimumConfidenceFieldId, FullSearchIntervalFieldId, ResetAfterMissesFieldId],
                    Order: 1, Collapsible: true, InitiallyExpanded: false)
            ],
            SummaryItems:
            [
                new StepSummaryItemDescriptor(BoundsSourceFieldId),
                new StepSummaryItemDescriptor(PaddingSourceFieldId)
            ],
            DetailFieldIds:
            [BoundsSourceFieldId, PaddingSourceFieldId, MinimumConfidenceFieldId,
                FullSearchIntervalFieldId, ResetAfterMissesFieldId],
            EditorDescriptionKey: "Ui.Step.DynamicRoi.Description"));

    public override DynamicRoiStep CreateDefaultStep() => new()
    {
        Settings = new DynamicRoiSettings { Padding = -1 }
    };

    protected override StepDraft Read(DynamicRoiStep step)
    {
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[BoundsSourceFieldId] = JsonSerializer.SerializeToNode(step.Settings.BoundsSource);
        draft.Values[PaddingSourceFieldId] = JsonSerializer.SerializeToNode(step.Settings.PaddingSource);
        draft.Values[LegacyPaddingDraftKey] = JsonValue.Create(step.Settings.Padding);
        draft.Values[MinimumConfidenceFieldId] = JsonValue.Create(step.Settings.MinimumConfidence);
        draft.Values[FullSearchIntervalFieldId] = JsonValue.Create(step.Settings.FullSearchInterval);
        draft.Values[ResetAfterMissesFieldId] = JsonValue.Create(step.Settings.ResetAfterMisses);
        return draft;
    }

    protected override void Apply(StepDraft draft, DynamicRoiStep step)
    {
        step.Settings.BoundsSource = DefinitionValueReader.Binding(draft, BoundsSourceFieldId);
        step.Settings.PaddingSource = DefinitionValueReader.Binding(draft, PaddingSourceFieldId);
        step.Settings.MinimumConfidence = DefinitionValueReader.Number(draft, MinimumConfidenceFieldId);
        step.Settings.FullSearchInterval = DefinitionValueReader.Integer(draft, FullSearchIntervalFieldId);
        step.Settings.ResetAfterMisses = DefinitionValueReader.Integer(draft, ResetAfterMissesFieldId);
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft)
    {
        var paddingSource = DefinitionValueReader.Binding(draft, PaddingSourceFieldId);
        return paddingSource.IsConfigured || ReadLegacyPadding(draft) >= 0
            ? []
            : [new("StepValidation.Required", PaddingSourceFieldId)];
    }

    private static int ReadLegacyPadding(StepDraft draft) =>
        DefinitionValueReader.Integer(draft, LegacyPaddingDraftKey);
}
