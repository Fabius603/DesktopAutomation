using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class TerminateProcessStepDefinition : StepDefinition<TerminateProcessStep>
{
    public const string ProcessTargetFieldId = "process_target";

    public override StepDescriptor Descriptor { get; } = new(
        TypeId: "terminate_process",
        CategoryId: "ProgrammeFenster",
        DisplayNameKey: "Step.Type.TerminateProcess",
        DescriptionKey: "Step.Description.TerminateProcess",
        IconKey: "process-stop",
        Fields:
        [
            new StepFieldDescriptor(
                ProcessTargetFieldId,
                "Ui.Step.Settings.ProcessSource",
                StepValueKind.Object,
                Required: true,
                EditorHint: StepEditorHints.ProcessTargetPicker,
                InputContractId: "process",
                Order: 0)
        ],
        Presentation: new StepPresentationDescriptor(
            EditorSections:
            [
                new StepEditorSectionDescriptor(
                    "general",
                    null,
                    [ProcessTargetFieldId])
            ],
            SummaryItems: [new StepSummaryItemDescriptor(ProcessTargetFieldId)],
            DetailFieldIds: [ProcessTargetFieldId]));

    public override TerminateProcessStep CreateDefaultStep() => new();

    protected override StepDraft Read(TerminateProcessStep step)
    {
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[ProcessTargetFieldId] = ProcessSelectorDraft.Create(step.Settings.Target);
        return draft;
    }

    protected override void Apply(StepDraft draft, TerminateProcessStep step) =>
        ProcessSelectorDraft.Apply(draft, ProcessTargetFieldId, step.Settings.Target);

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft) =>
        ProcessSelectorDraft.IsConfigured(draft, ProcessTargetFieldId)
            ? []
            : [new("StepValidation.Required", ProcessTargetFieldId)];
}
