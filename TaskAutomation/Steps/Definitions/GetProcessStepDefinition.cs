using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class GetProcessStepDefinition : StepDefinition<GetProcessStep>
{
    public const string ProcessNameFieldId = "process_name";
    public const string ExecutablePathFieldId = "executable_path";
    public const string WindowTitleFieldId = "window_title_contains";

    public override StepDescriptor Descriptor { get; } = new(
        TypeId: "get_process",
        CategoryId: "ProgrammeFenster",
        DisplayNameKey: "Step.Type.GetProcess",
        DescriptionKey: "Step.Description.GetProcess",
        IconKey: "process-search",
        Fields:
        [
            new StepFieldDescriptor(
                ProcessNameFieldId,
                "Ui.Step.Settings.ProcessName",
                StepValueKind.Text,
                DefaultValue: JsonValue.Create(string.Empty),
                EditorHint: StepEditorHints.ProcessNameSuggestions,
                Order: 0),
            new StepFieldDescriptor(
                ExecutablePathFieldId,
                "Ui.Step.Settings.PathProgram",
                StepValueKind.FilePath,
                DefaultValue: JsonValue.Create(string.Empty),
                EditorHint: StepEditorHints.ExecutablePathSuggestions,
                Order: 1),
            new StepFieldDescriptor(
                WindowTitleFieldId,
                "Ui.Step.Settings.WindowTitleContains",
                StepValueKind.Text,
                DefaultValue: JsonValue.Create(string.Empty),
                Order: 2)
        ],
        Presentation: new StepPresentationDescriptor(
            EditorSections:
            [
                new StepEditorSectionDescriptor(
                    "general",
                    null,
                    [ProcessNameFieldId, ExecutablePathFieldId, WindowTitleFieldId])
            ],
            SummaryItems:
            [
                new StepSummaryItemDescriptor(ProcessNameFieldId),
                new StepSummaryItemDescriptor(ExecutablePathFieldId, StepSummaryValueFormat.FileName)
            ],
            DetailFieldIds: [ProcessNameFieldId, ExecutablePathFieldId, WindowTitleFieldId],
            EditorDescriptionKey: "Ui.Step.Settings.GetProcessHint"));

    public override GetProcessStep CreateDefaultStep() => new();

    protected override StepDraft Read(GetProcessStep step)
    {
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[ProcessNameFieldId] = JsonValue.Create(step.Settings.Query.ProcessName);
        draft.Values[ExecutablePathFieldId] = JsonValue.Create(step.Settings.Query.ExecutablePath);
        draft.Values[WindowTitleFieldId] = JsonValue.Create(step.Settings.Query.WindowTitleContains);
        return draft;
    }

    protected override void Apply(StepDraft draft, GetProcessStep step)
    {
        step.Settings.Query.ProcessSource = new ResultBinding();
        step.Settings.Query.ProcessName = DefinitionValueReader.String(draft, ProcessNameFieldId);
        step.Settings.Query.ExecutablePath = DefinitionValueReader.String(draft, ExecutablePathFieldId);
        step.Settings.Query.WindowTitleContains = DefinitionValueReader.String(draft, WindowTitleFieldId);
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft)
    {
        if (string.IsNullOrWhiteSpace(DefinitionValueReader.String(draft, ProcessNameFieldId))
            && string.IsNullOrWhiteSpace(DefinitionValueReader.String(draft, ExecutablePathFieldId)))
            return [new("StepValidation.Invalid", null)];
        return [];
    }
}
