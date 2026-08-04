using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class ActiveWindowStepDefinition : StepDefinition<ActiveWindowStep>
{
    public const string ProcessTargetFieldId = "process_target";
    public const string CacheFieldId = "cache_ms";

    public override StepDescriptor Descriptor { get; } = new(
        TypeId: "active_window",
        CategoryId: "ProgrammeFenster",
        DisplayNameKey: "Step.Type.ActiveWindow",
        DescriptionKey: "Step.Description.ActiveWindow",
        IconKey: "window-check",
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
                CacheFieldId,
                "Ui.Step.Settings.CacheMs",
                StepValueKind.Duration,
                DefaultValue: JsonValue.Create(0),
                DescriptionKey: "Ui.Step.Settings.ActiveWindowResultHint",
                Constraints: new StepFieldConstraints(Minimum: 0),
                Advanced: true,
                Order: 2)
        ],
        Presentation: new StepPresentationDescriptor(
            EditorSections:
            [
                new StepEditorSectionDescriptor(
                    "general",
                    null,
                    [ProcessTargetFieldId]),
                new StepEditorSectionDescriptor(
                    "advanced",
                    "Ui.Step.Settings.Advanced",
                    [CacheFieldId],
                    Order: 1,
                    Collapsible: true,
                    InitiallyExpanded: false)
            ],
            SummaryItems: [new StepSummaryItemDescriptor(ProcessTargetFieldId)],
            DetailFieldIds: [ProcessTargetFieldId, CacheFieldId]));

    public override ActiveWindowStep CreateDefaultStep() => new();

    protected override StepDraft Read(ActiveWindowStep step)
    {
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[ProcessTargetFieldId] = ProcessSelectorDraft.Create(step.Settings.Target);
        draft.Values[CacheFieldId] = JsonValue.Create(step.Settings.CacheMs);
        return draft;
    }

    protected override void Apply(StepDraft draft, ActiveWindowStep step)
    {
        ProcessSelectorDraft.Apply(draft, ProcessTargetFieldId, step.Settings.Target);
        step.Settings.CacheMs = DefinitionValueReader.Integer(draft, CacheFieldId);
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft)
    {
        if (!ProcessSelectorDraft.IsConfigured(draft, ProcessTargetFieldId))
            return [new("StepValidation.Required", ProcessTargetFieldId)];
        return [];
    }
}
