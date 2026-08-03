using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class UnblockInputStepDefinition : StepDefinition<UnblockInputStep>
{
    public override StepDescriptor Descriptor { get; } = new(
        TypeId: "unblock_input",
        CategoryId: "AblaufSteuern",
        DisplayNameKey: "Step.Type.UnblockInput",
        DescriptionKey: "Step.Description.UnblockInput",
        IconKey: "lock-open-outline",
        Fields: [],
        Presentation: new StepPresentationDescriptor(
            EditorSections: [],
            SummaryItems: [],
            DetailFieldIds: [],
            EditorDescriptionKey: "Ui.Step.Settings.UnblockInputDescription"));

    public override UnblockInputStep CreateDefaultStep() => new();

    protected override StepDraft Read(UnblockInputStep step) => new(Descriptor.TypeId);

    protected override void Apply(StepDraft draft, UnblockInputStep step)
    {
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft) => [];
}
