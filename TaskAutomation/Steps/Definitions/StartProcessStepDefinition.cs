using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace TaskAutomation.Steps.Definitions;

public sealed class StartProcessStepDefinition : StepDefinition<StartProcessStep>
{
    public const string ActionFieldId = "action";
    public const string ProcessTargetFieldId = "process_target";
    public const string ExecutablePathFieldId = "executable_path";
    public const string ArgumentsFieldId = "arguments";
    public const string WorkingDirectoryFieldId = "working_directory";
    public const string WaitForExitFieldId = "wait_for_exit";
    public const string MonitorIndexFieldId = "monitor_index";
    public const string PlacementModeFieldId = "placement_mode";
    public const string OffsetXFieldId = "offset_x";
    public const string OffsetYFieldId = "offset_y";
    public const string WindowModeFieldId = "window_mode";

    private static readonly string[] Actions = [nameof(StartProcessAction.Start), nameof(StartProcessAction.Terminate)];
    private static readonly string[] PlacementModes = [nameof(StartProcessPlacementMode.Centered), nameof(StartProcessPlacementMode.Custom)];
    private static readonly string[] WindowModes =
        [nameof(StartProcessWindowMode.ApplicationDefault), nameof(StartProcessWindowMode.Normal), nameof(StartProcessWindowMode.Maximized)];

    public override StepDescriptor Descriptor { get; } = new(
        TypeId: "start_process",
        CategoryId: "ProgrammeFenster",
        DisplayNameKey: "Step.Type.StartProcess",
        DescriptionKey: "Step.Description.StartProcess",
        IconKey: "process-start",
        Fields:
        [
            // Action and target keep legacy StartProcess/Terminate jobs valid. They are
            // intentionally not rendered; the UI exposes termination as its own step.
            new StepFieldDescriptor(ActionFieldId, "Ui.Step.Settings.Action", StepValueKind.Enum,
                Required: true, DefaultValue: JsonValue.Create(nameof(StartProcessAction.Start)),
                Constraints: new StepFieldConstraints(AllowedValues: Actions), Order: -2),
            new StepFieldDescriptor(ProcessTargetFieldId, "Ui.Step.Settings.ProcessSource", StepValueKind.Object,
                Required: true, EditorHint: StepEditorHints.ProcessTargetPicker, InputContractId: "process", Order: -1,
                VisibleWhen: new StepVisibilityRule(ActionFieldId, JsonValue.Create(nameof(StartProcessAction.Terminate)))),
            new StepFieldDescriptor(ExecutablePathFieldId, "Ui.Step.Settings.PathProgram", StepValueKind.FilePath,
                Required: true, DefaultValue: JsonValue.Create(string.Empty),
                EditorHint: StepEditorHints.StartProgramPicker, Order: 0,
                VisibleWhen: new StepVisibilityRule(ActionFieldId, JsonValue.Create(nameof(StartProcessAction.Start)))),
            new StepFieldDescriptor(WaitForExitFieldId, "Ui.Step.Settings.WaitForCompletion", StepValueKind.Boolean,
                DefaultValue: JsonValue.Create(false), Order: 1),
            new StepFieldDescriptor(ArgumentsFieldId, "Ui.Step.Settings.Arguments", StepValueKind.Text,
                DefaultValue: JsonValue.Create(string.Empty), Advanced: true, Order: 2),
            new StepFieldDescriptor(WorkingDirectoryFieldId, "Ui.Step.Settings.WorkingDirectory", StepValueKind.DirectoryPath,
                DefaultValue: JsonValue.Create(string.Empty), Advanced: true, Order: 3),
            new StepFieldDescriptor(MonitorIndexFieldId, "Ui.Step.Settings.Monitor", StepValueKind.Integer,
                DefaultValue: JsonValue.Create(0), EditorHint: StepEditorHints.MonitorPicker,
                Constraints: new StepFieldConstraints(Minimum: 0), Advanced: true, Order: 4),
            new StepFieldDescriptor(WindowModeFieldId, "Ui.Step.Settings.WindowMode", StepValueKind.Enum,
                Required: true, DefaultValue: JsonValue.Create(nameof(StartProcessWindowMode.ApplicationDefault)),
                Constraints: new StepFieldConstraints(AllowedValues: WindowModes), Advanced: true, Order: 5,
                Options:
                [
                    new(nameof(StartProcessWindowMode.ApplicationDefault), "Enum.StartProcessWindowMode.ApplicationDefault"),
                    new(nameof(StartProcessWindowMode.Normal), "Enum.StartProcessWindowMode.Normal"),
                    new(nameof(StartProcessWindowMode.Maximized), "Enum.StartProcessWindowMode.Maximized")
                ]),
            new StepFieldDescriptor(PlacementModeFieldId, "Ui.Step.Settings.Position", StepValueKind.Enum,
                Required: true, DefaultValue: JsonValue.Create(nameof(StartProcessPlacementMode.Centered)),
                Constraints: new StepFieldConstraints(AllowedValues: PlacementModes), Advanced: true, Order: 6,
                Options:
                [
                    new(nameof(StartProcessPlacementMode.Centered), "Enum.StartProcessPlacementMode.Centered"),
                    new(nameof(StartProcessPlacementMode.Custom), "Enum.StartProcessPlacementMode.Custom")
                ]),
            new StepFieldDescriptor(OffsetXFieldId, "Ui.Step.Settings.XOffsetPixels", StepValueKind.Integer,
                DefaultValue: JsonValue.Create(0), Advanced: true, Order: 7,
                VisibleWhen: new StepVisibilityRule(PlacementModeFieldId, JsonValue.Create(nameof(StartProcessPlacementMode.Custom)))),
            new StepFieldDescriptor(OffsetYFieldId, "Ui.Step.Settings.YOffsetPixels", StepValueKind.Integer,
                DefaultValue: JsonValue.Create(0), Advanced: true, Order: 8,
                VisibleWhen: new StepVisibilityRule(PlacementModeFieldId, JsonValue.Create(nameof(StartProcessPlacementMode.Custom))))
        ],
        Presentation: new StepPresentationDescriptor(
            EditorSections:
            [
                new StepEditorSectionDescriptor("general", null, [ExecutablePathFieldId, WaitForExitFieldId]),
                new StepEditorSectionDescriptor("advanced", "Ui.Step.Settings.Advanced",
                    [ArgumentsFieldId, WorkingDirectoryFieldId, MonitorIndexFieldId, WindowModeFieldId,
                        PlacementModeFieldId, OffsetXFieldId, OffsetYFieldId],
                    Order: 1, Collapsible: true, InitiallyExpanded: false)
            ],
            SummaryItems:
            [
                new StepSummaryItemDescriptor(ExecutablePathFieldId, StepSummaryValueFormat.FileName),
                new StepSummaryItemDescriptor(WaitForExitFieldId, StepSummaryValueFormat.BooleanBadge)
            ],
            DetailFieldIds:
            [ExecutablePathFieldId, ArgumentsFieldId, WorkingDirectoryFieldId, WaitForExitFieldId,
                MonitorIndexFieldId, WindowModeFieldId, PlacementModeFieldId, OffsetXFieldId, OffsetYFieldId]));

    public override StartProcessStep CreateDefaultStep() => new();

    protected override StepDraft Read(StartProcessStep step)
    {
        var settings = step.Settings;
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[ActionFieldId] = JsonValue.Create(settings.Action.ToString());
        draft.Values[ProcessTargetFieldId] = ProcessSelectorDraft.Create(settings.Target);
        draft.Values[ExecutablePathFieldId] = JsonValue.Create(settings.ExecutablePath);
        draft.Values[ArgumentsFieldId] = JsonValue.Create(settings.Arguments);
        draft.Values[WorkingDirectoryFieldId] = JsonValue.Create(settings.WorkingDirectory);
        draft.Values[WaitForExitFieldId] = JsonValue.Create(settings.WaitForExit);
        draft.Values[MonitorIndexFieldId] = JsonValue.Create(settings.MonitorIndex);
        draft.Values[PlacementModeFieldId] = JsonValue.Create(settings.PlacementMode.ToString());
        draft.Values[OffsetXFieldId] = JsonValue.Create(settings.OffsetX);
        draft.Values[OffsetYFieldId] = JsonValue.Create(settings.OffsetY);
        draft.Values[WindowModeFieldId] = JsonValue.Create(settings.WindowMode.ToString());
        return draft;
    }

    protected override void Apply(StepDraft draft, StartProcessStep step)
    {
        var settings = step.Settings;
        settings.Action = Enum.Parse<StartProcessAction>(DefinitionValueReader.String(draft, ActionFieldId));
        if (settings.Action == StartProcessAction.Terminate)
        {
            ProcessSelectorDraft.Apply(draft, ProcessTargetFieldId, settings.Target);
            return;
        }
        settings.Target = new ProcessTargetSettings();
        settings.ExecutablePath = DefinitionValueReader.String(draft, ExecutablePathFieldId);
        settings.Arguments = DefinitionValueReader.String(draft, ArgumentsFieldId);
        settings.WorkingDirectory = DefinitionValueReader.String(draft, WorkingDirectoryFieldId);
        settings.WaitForExit = DefinitionValueReader.Boolean(draft, WaitForExitFieldId);
        settings.MonitorIndex = DefinitionValueReader.Integer(draft, MonitorIndexFieldId);
        settings.PlacementMode = Enum.Parse<StartProcessPlacementMode>(DefinitionValueReader.String(draft, PlacementModeFieldId));
        settings.OffsetX = DefinitionValueReader.Integer(draft, OffsetXFieldId);
        settings.OffsetY = DefinitionValueReader.Integer(draft, OffsetYFieldId);
        settings.WindowMode = Enum.Parse<StartProcessWindowMode>(DefinitionValueReader.String(draft, WindowModeFieldId));
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft)
    {
        var action = DefinitionValueReader.String(draft, ActionFieldId);
        if (string.Equals(action, nameof(StartProcessAction.Terminate), StringComparison.OrdinalIgnoreCase))
            return ProcessSelectorDraft.IsConfigured(draft, ProcessTargetFieldId)
                ? []
                : [new("StepValidation.Required", ProcessTargetFieldId)];
        if (!ExecutablePathResolver.CanResolve(DefinitionValueReader.String(draft, ExecutablePathFieldId)))
            return [new("StepValidation.Required", ExecutablePathFieldId)];
        return [];
    }
}
