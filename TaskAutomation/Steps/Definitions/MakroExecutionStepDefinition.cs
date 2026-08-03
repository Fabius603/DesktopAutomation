using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class MakroExecutionStepDefinition : StepDefinition<MakroExecutionStep>
{
    public const string MacroFieldId = "macro";

    public override StepDescriptor Descriptor { get; } = new(
        TypeId: "makro_execution",
        CategoryId: "MausTastatur",
        DisplayNameKey: "Step.Type.MakroExecution",
        DescriptionKey: "Step.Description.MakroExecution",
        IconKey: "macro-run",
        Fields:
        [
            new StepFieldDescriptor(
                MacroFieldId,
                "Ui.Step.Settings.Macro",
                StepValueKind.Object,
                Required: true,
                EditorHint: StepEditorHints.MacroPicker)
        ],
        Presentation: new StepPresentationDescriptor(
            EditorSections:
            [
                new StepEditorSectionDescriptor("general", null, [MacroFieldId])
            ],
            SummaryItems: [new StepSummaryItemDescriptor(MacroFieldId)],
            DetailFieldIds: [MacroFieldId]));

    public override MakroExecutionStep CreateDefaultStep() => new();

    protected override StepDraft Read(MakroExecutionStep step)
    {
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[MacroFieldId] = StepReferenceDraft.Create(
            step.Settings.MakroId,
            step.Settings.MakroName);
        return draft;
    }

    protected override void Apply(StepDraft draft, MakroExecutionStep step)
    {
        if (!StepReferenceDraft.TryGetId(draft, MacroFieldId, out var id, out var reference))
            throw new InvalidOperationException("The macro draft does not contain a valid reference.");
        step.Settings.MakroId = id;
        step.Settings.MakroName = reference.Name;
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft) =>
        StepReferenceDraft.TryGetId(draft, MacroFieldId, out _, out _)
            ? []
            : [new("StepValidation.Required", MacroFieldId)];
}
