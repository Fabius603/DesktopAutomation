using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class ContinueJobStepDefinition : StepDefinition<ContinueJobStep>
{
    public override StepDescriptor Descriptor { get; } = new(
        TypeId: "continue_job",
        CategoryId: "AblaufSteuern",
        DisplayNameKey: "Step.Type.ContinueJob",
        DescriptionKey: "Step.Description.ContinueJob",
        IconKey: "restart",
        Fields: [],
        Presentation: new StepPresentationDescriptor(
            EditorSections: [],
            SummaryItems: [],
            DetailFieldIds: [],
            EditorDescriptionKey: "Ui.Step.Settings.ContinueJobDescription"));

    public override ContinueJobStep CreateDefaultStep() => new();

    protected override StepDraft Read(ContinueJobStep step) => new(Descriptor.TypeId);

    protected override void Apply(StepDraft draft, ContinueJobStep step)
    {
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft) => [];
}
