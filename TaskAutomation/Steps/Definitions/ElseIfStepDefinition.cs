using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class ElseIfStepDefinition : StepDefinition<ElseIfStep>
{
    public override StepDescriptor Descriptor { get; } = ConditionStepDefinitionSupport.CreateDescriptor(
        "else_if", "Step.Type.ElseIf", "Step.Description.ElseIf");

    public override ElseIfStep CreateDefaultStep() => new();

    protected override StepDraft Read(ElseIfStep step) => ConditionStepDefinitionSupport.Read(Descriptor.TypeId, step.Settings);

    protected override void Apply(StepDraft draft, ElseIfStep step) =>
        step.Settings = ConditionStepDefinitionSupport.ReadSettings(draft);

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft) =>
        ConditionStepDefinitionSupport.Validate(draft);
}
