using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class TerminateProcessStepDefinition : StepDefinition<TerminateProcessStep>
{
    public const string ProcessTargetFieldId = "process_target";
    public const string WindowTitleFieldId = "window_title_contains";

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
                Order: 0),
            new StepFieldDescriptor(
                WindowTitleFieldId,
                "Ui.Step.Settings.WindowTitleContains",
                StepValueKind.Text,
                DefaultValue: JsonValue.Create(string.Empty),
                Order: 1)
        ],
        Presentation: new StepPresentationDescriptor(
            EditorSections:
            [
                new StepEditorSectionDescriptor(
                    "general",
                    null,
                    [ProcessTargetFieldId, WindowTitleFieldId])
            ],
            SummaryItems:
            [
                new StepSummaryItemDescriptor(ProcessTargetFieldId),
                new StepSummaryItemDescriptor(WindowTitleFieldId)
            ],
            DetailFieldIds: [ProcessTargetFieldId, WindowTitleFieldId]));

    public override TerminateProcessStep CreateDefaultStep() => new();

    protected override StepDraft Read(TerminateProcessStep step)
    {
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[ProcessTargetFieldId] = ProcessSelectorDraft.Create(step.Settings.Target);
        draft.Values[WindowTitleFieldId] = JsonValue.Create(step.Settings.Target.WindowTitleContains);
        return draft;
    }

    protected override void Apply(StepDraft draft, TerminateProcessStep step)
    {
        ProcessSelectorDraft.Apply(draft, ProcessTargetFieldId, step.Settings.Target);
        step.Settings.Target.WindowTitleContains = DefinitionValueReader.String(draft, WindowTitleFieldId);
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft) =>
        ProcessSelectorDraft.IsConfigured(draft, ProcessTargetFieldId)
            ? []
            : [new("StepValidation.Required", ProcessTargetFieldId)];
}
