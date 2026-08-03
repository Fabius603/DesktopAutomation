using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class ElseStepDefinition : StepDefinition<ElseStep>
{
    public override StepDescriptor Descriptor { get; } = new(
        TypeId: "else",
        CategoryId: "AblaufSteuern",
        DisplayNameKey: "Step.Type.Else",
        DescriptionKey: "Step.Description.Else",
        IconKey: "source-branch",
        Fields: [],
        Presentation: new StepPresentationDescriptor(
            EditorSections: [],
            SummaryItems: [],
            DetailFieldIds: [],
            EditorDescriptionKey: "Ui.Step.Settings.ElseDescription"));

    public override ElseStep CreateDefaultStep() => new();

    protected override StepDraft Read(ElseStep step) => new(Descriptor.TypeId);

    protected override void Apply(StepDraft draft, ElseStep step)
    {
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft) => [];
}
