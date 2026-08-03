using System.IO;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class ScriptExecutionStepDefinition : StepDefinition<ScriptExecutionStep>
{
    public const string ScriptPathFieldId = "script_path";
    public const string WaitForExitFieldId = "wait_for_exit";
    public const string ArgumentsFieldId = "arguments";

    public override StepDescriptor Descriptor { get; } = new(
        TypeId: "script_execution",
        CategoryId: "ProgrammeFenster",
        DisplayNameKey: "Step.Type.ScriptExecution",
        DescriptionKey: "Step.Description.ScriptExecution",
        IconKey: "script-run",
        Fields:
        [
            new StepFieldDescriptor(
                ScriptPathFieldId,
                "Ui.Step.Settings.ScriptPath",
                StepValueKind.FilePath,
                Required: true,
                DefaultValue: JsonValue.Create(string.Empty),
                EditorHint: StepEditorHints.FilePicker,
                Order: 0),
            new StepFieldDescriptor(
                WaitForExitFieldId,
                "Ui.Step.Settings.WaitForCompletion",
                StepValueKind.Boolean,
                DefaultValue: JsonValue.Create(false),
                Order: 1),
            new StepFieldDescriptor(
                ArgumentsFieldId,
                "Ui.Step.Settings.Arguments",
                StepValueKind.Text,
                DefaultValue: JsonValue.Create(string.Empty),
                Advanced: true,
                Order: 2)
        ],
        Presentation: new StepPresentationDescriptor(
            EditorSections:
            [
                new StepEditorSectionDescriptor(
                    "general",
                    null,
                    [ScriptPathFieldId, WaitForExitFieldId]),
                new StepEditorSectionDescriptor(
                    "advanced",
                    "Ui.Step.Settings.Advanced",
                    [ArgumentsFieldId],
                    Order: 1,
                    Collapsible: true,
                    InitiallyExpanded: false)
            ],
            SummaryItems:
            [
                new StepSummaryItemDescriptor(ScriptPathFieldId, StepSummaryValueFormat.FileName),
                new StepSummaryItemDescriptor(WaitForExitFieldId, StepSummaryValueFormat.BooleanBadge)
            ],
            DetailFieldIds: [ScriptPathFieldId, ArgumentsFieldId, WaitForExitFieldId]));

    public override ScriptExecutionStep CreateDefaultStep() => new();

    protected override StepDraft Read(ScriptExecutionStep step)
    {
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[ScriptPathFieldId] = JsonValue.Create(step.Settings.ScriptPath);
        draft.Values[WaitForExitFieldId] = JsonValue.Create(step.Settings.WaitForExit);
        draft.Values[ArgumentsFieldId] = JsonValue.Create(step.Settings.Arguments);
        return draft;
    }

    protected override void Apply(StepDraft draft, ScriptExecutionStep step)
    {
        step.Settings.ScriptPath = DefinitionValueReader.String(draft, ScriptPathFieldId);
        step.Settings.WaitForExit = DefinitionValueReader.Boolean(draft, WaitForExitFieldId);
        step.Settings.Arguments = DefinitionValueReader.String(draft, ArgumentsFieldId);
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft)
    {
        var path = DefinitionValueReader.String(draft, ScriptPathFieldId);
        if (!File.Exists(path))
            return [new("StepValidation.Invalid", ScriptPathFieldId)];
        return [];
    }
}
