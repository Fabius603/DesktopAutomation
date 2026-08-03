using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class ActiveProcessStepDefinition : StepDefinition<ActiveProcessStep>
{
    public const string ProcessTargetFieldId = "process_target";

    public override StepDescriptor Descriptor { get; } = new(
        TypeId: "active_process",
        CategoryId: "ProgrammeFenster",
        DisplayNameKey: "Step.Type.ActiveProcess",
        DescriptionKey: "Step.Description.ActiveProcess",
        IconKey: "process-check",
        Fields:
        [
            new StepFieldDescriptor(
                ProcessTargetFieldId,
                "Ui.Step.Settings.ProcessSource",
                StepValueKind.Object,
                Required: true,
                EditorHint: StepEditorHints.ProcessTargetPicker,
                InputContractId: "process")
        ],
        Presentation: new StepPresentationDescriptor(
            EditorSections:
            [
                new StepEditorSectionDescriptor("general", null, [ProcessTargetFieldId])
            ],
            SummaryItems: [new StepSummaryItemDescriptor(ProcessTargetFieldId)],
            DetailFieldIds: [ProcessTargetFieldId],
            EditorDescriptionKey: "Ui.Step.Settings.ProcessResultHint"));

    public override ActiveProcessStep CreateDefaultStep() => new();

    protected override StepDraft Read(ActiveProcessStep step)
    {
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[ProcessTargetFieldId] = ProcessSelectorDraft.Create(step.Settings.Target);
        return draft;
    }

    protected override void Apply(StepDraft draft, ActiveProcessStep step) =>
        ProcessSelectorDraft.Apply(draft, ProcessTargetFieldId, step.Settings.Target);

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft) =>
        ProcessSelectorDraft.IsConfigured(draft, ProcessTargetFieldId)
            ? []
            : [new("StepValidation.Required", ProcessTargetFieldId)];
}
