using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class FocusProcessStepDefinition : StepDefinition<FocusProcessStep>
{
    public const string ProcessTargetFieldId = "process_target";
    public const string WindowTitleFieldId = "window_title_contains";
    public const string ActionFieldId = "action";
    public const string WindowModeFieldId = "window_mode";

    private static readonly string[] Actions = [nameof(FocusProcessAction.BringToFront), nameof(FocusProcessAction.Minimize)];
    private static readonly string[] WindowModes = [nameof(FocusProcessWindowMode.Normal), nameof(FocusProcessWindowMode.Maximized)];

    public override StepDescriptor Descriptor { get; } = new(
        TypeId: "focus_process",
        CategoryId: "ProgrammeFenster",
        DisplayNameKey: "Step.Type.FocusProcess",
        DescriptionKey: "Step.Description.FocusProcess",
        IconKey: "window-focus",
        Fields:
        [
            new StepFieldDescriptor(
                ProcessTargetFieldId,
                "Ui.Step.Settings.ProcessSource",
                StepValueKind.Object,
                Required: true,
                EditorHint: StepEditorHints.ExecutableProcessTargetPicker,
                InputContractId: "process",
                Order: 0),
            new StepFieldDescriptor(
                WindowTitleFieldId,
                "Ui.Step.Settings.WindowTitleContains",
                StepValueKind.Text,
                DefaultValue: JsonValue.Create(string.Empty),
                Order: 1),
            new StepFieldDescriptor(
                ActionFieldId,
                "Ui.Step.Settings.Action",
                StepValueKind.Enum,
                Required: true,
                DefaultValue: JsonValue.Create(nameof(FocusProcessAction.BringToFront)),
                Constraints: new StepFieldConstraints(AllowedValues: Actions),
                Advanced: true,
                Order: 2,
                Options:
                [
                    new(nameof(FocusProcessAction.BringToFront), "Enum.FocusProcessAction.BringToFront"),
                    new(nameof(FocusProcessAction.Minimize), "Enum.FocusProcessAction.Minimize")
                ]),
            new StepFieldDescriptor(
                WindowModeFieldId,
                "Ui.Step.Settings.WindowMode",
                StepValueKind.Enum,
                Required: true,
                DefaultValue: JsonValue.Create(nameof(FocusProcessWindowMode.Normal)),
                Constraints: new StepFieldConstraints(AllowedValues: WindowModes),
                Advanced: true,
                Order: 3,
                VisibleWhen: new StepVisibilityRule(
                    ActionFieldId,
                    JsonValue.Create(nameof(FocusProcessAction.BringToFront))),
                Options:
                [
                    new(nameof(FocusProcessWindowMode.Normal), "Enum.FocusProcessWindowMode.Normal"),
                    new(nameof(FocusProcessWindowMode.Maximized), "Enum.FocusProcessWindowMode.Maximized")
                ])
        ],
        Presentation: new StepPresentationDescriptor(
            EditorSections:
            [
                new StepEditorSectionDescriptor("general", null, [ProcessTargetFieldId, WindowTitleFieldId]),
                new StepEditorSectionDescriptor(
                    "advanced",
                    "Ui.Step.Settings.Advanced",
                    [ActionFieldId, WindowModeFieldId],
                    Order: 1,
                    Collapsible: true,
                    InitiallyExpanded: false)
            ],
            SummaryItems:
            [
                new StepSummaryItemDescriptor(ProcessTargetFieldId),
                new StepSummaryItemDescriptor(ActionFieldId),
                new StepSummaryItemDescriptor(WindowTitleFieldId)
            ],
            DetailFieldIds: [ProcessTargetFieldId, WindowTitleFieldId, ActionFieldId, WindowModeFieldId]));

    public override FocusProcessStep CreateDefaultStep() => new();

    protected override StepDraft Read(FocusProcessStep step)
    {
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[ProcessTargetFieldId] = ProcessSelectorDraft.Create(step.Settings.Target);
        draft.Values[WindowTitleFieldId] = JsonValue.Create(step.Settings.Target.WindowTitleContains);
        draft.Values[ActionFieldId] = JsonValue.Create(step.Settings.Action.ToString());
        draft.Values[WindowModeFieldId] = JsonValue.Create(
            step.Settings.WindowMode == FocusProcessWindowMode.Fullscreen
                ? nameof(FocusProcessWindowMode.Maximized)
                : step.Settings.WindowMode.ToString());
        return draft;
    }

    protected override void Apply(StepDraft draft, FocusProcessStep step)
    {
        ProcessSelectorDraft.Apply(draft, ProcessTargetFieldId, step.Settings.Target);
        step.Settings.Target.WindowTitleContains = DefinitionValueReader.String(draft, WindowTitleFieldId);
        step.Settings.Action = Enum.Parse<FocusProcessAction>(DefinitionValueReader.String(draft, ActionFieldId));
        step.Settings.WindowMode = Enum.Parse<FocusProcessWindowMode>(DefinitionValueReader.String(draft, WindowModeFieldId));
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft)
    {
        if (!ProcessSelectorDraft.IsConfigured(draft, ProcessTargetFieldId))
            return [new("StepValidation.Required", ProcessTargetFieldId)];
        return [];
    }
}
