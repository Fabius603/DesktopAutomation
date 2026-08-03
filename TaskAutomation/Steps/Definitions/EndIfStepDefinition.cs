using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class EndIfStepDefinition : StepDefinition<EndIfStep>
{
    public override StepDescriptor Descriptor { get; } = new(
        TypeId: "end_if",
        CategoryId: "AblaufSteuern",
        DisplayNameKey: "Step.Type.EndIf",
        DescriptionKey: "Step.Description.EndIf",
        IconKey: "source-branch-minus",
        Fields: [],
        Presentation: new StepPresentationDescriptor(
            EditorSections: [],
            SummaryItems: [],
            DetailFieldIds: [],
            EditorDescriptionKey: "Ui.Step.Settings.EndIfDescription"));

    public override EndIfStep CreateDefaultStep() => new();

    protected override StepDraft Read(EndIfStep step) => new(Descriptor.TypeId);

    protected override void Apply(StepDraft draft, EndIfStep step)
    {
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft) => [];
}
