using System.Text.Json;
using System.Text.Json.Nodes;
using System.IO;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class FileSystemOperationStepDefinition : StepDefinition<FileSystemOperationStep>
{
    public const string OperationFieldId = "operation";
    public const string SourceModeFieldId = "source_mode";
    public const string SourcePathFieldId = "source_path";
    public const string SourceResultFieldId = "source_result";
    public const string TargetModeFieldId = "target_mode";
    public const string TargetPathFieldId = "target_path";
    public const string TargetResultFieldId = "target_result";
    public const string NewNameFieldId = "new_name";
    public const string FilterFieldId = "filter";
    public const string CreateParentsFieldId = "create_parent_directories";
    public const string RetryLockedFieldId = "retry_locked_files";
    public const string RetryCountFieldId = "retry_count";
    public const string RetryDelayFieldId = "retry_delay_ms";

    private static readonly string[] Operations = ["Copy", "Move", "Rename", "Delete"];
    private static readonly string[] PathSources = ["ExplicitPath", "TaskResult"];

    public override StepDescriptor Descriptor { get; } = new(
        "file_system_operation", "DateienOrdner", "Step.Type.FileSystemOperation",
        "Step.Description.FileSystemOperation", "folder",
        [
            EnumField(OperationFieldId, "Ui.Step.FileSystem.Operation", "Copy", Operations,
                [new("Copy", "Ui.Step.FileSystem.Copy"), new("Move", "Ui.Step.FileSystem.Move"),
                 new("Rename", "Ui.Step.FileSystem.Rename"), new("Delete", "Ui.Step.FileSystem.Delete")], 0),
            EnumField(SourceModeFieldId, "Ui.Step.FileSystem.Source", "ExplicitPath", PathSources,
                [new("ExplicitPath", "Ui.Step.FileSystem.ExplicitPath"), new("TaskResult", "Ui.Step.FileSystem.StepResult")], 1),
            new(SourcePathFieldId, "Ui.Step.FileSystem.Source", StepValueKind.Text, Required: true, EditorHint: StepEditorHints.FileOrFolderPicker,
                Order: 2, VisibleWhen: Is(SourceModeFieldId, "ExplicitPath")),
            new(SourceResultFieldId, "Ui.Step.FileSystem.Source", StepValueKind.ResultBinding, Required: true,
                EditorHint: StepEditorHints.ResultBindingPicker, Order: 3, VisibleWhen: Is(SourceModeFieldId, "TaskResult"),
                InputContractId: "source"),
            EnumField(TargetModeFieldId, "Ui.Step.FileSystem.Target", "ExplicitPath", PathSources,
                [new("ExplicitPath", "Ui.Step.FileSystem.ExplicitPath"), new("TaskResult", "Ui.Step.FileSystem.StepResult")], 4,
                [AnyOf(OperationFieldId, "Copy", "Move")]),
            new(TargetPathFieldId, "Ui.Step.FileSystem.Target", StepValueKind.Text, Required: true, EditorHint: StepEditorHints.FileOrFolderPicker,
                Order: 5, VisibleWhenAll: [AnyOf(OperationFieldId, "Copy", "Move"), Is(TargetModeFieldId, "ExplicitPath")]),
            new(TargetResultFieldId, "Ui.Step.FileSystem.Target", StepValueKind.ResultBinding, Required: true,
                EditorHint: StepEditorHints.ResultBindingPicker, Order: 6, InputContractId: "target",
                VisibleWhenAll: [AnyOf(OperationFieldId, "Copy", "Move"), Is(TargetModeFieldId, "TaskResult")]),
            new(NewNameFieldId, "Ui.Step.FileSystem.NewName", StepValueKind.Text, Order: 7,
                VisibleWhen: Is(OperationFieldId, "Rename")),
            new(FilterFieldId, "Ui.Step.FileSystem.Filter", StepValueKind.Text, DescriptionKey: "Ui.Step.FileSystem.FilterHint",
                Order: 8, VisibleWhen: Is(OperationFieldId, "Delete")),
            new(CreateParentsFieldId, "Ui.Step.FileSystem.CreateParents", StepValueKind.Boolean,
                DefaultValue: JsonValue.Create(true), Order: 9,
                VisibleWhen: AnyOf(OperationFieldId, "Copy", "Move")),
            new(RetryLockedFieldId, "Ui.Step.FileSystem.RetryLocked", StepValueKind.Boolean,
                DefaultValue: JsonValue.Create(true), Advanced: true, Order: 10),
            new(RetryCountFieldId, "Ui.Step.FileSystem.RetryCount", StepValueKind.Integer,
                DefaultValue: JsonValue.Create(3), Constraints: new(Minimum: 0), Advanced: true, Order: 11,
                VisibleWhen: Is(RetryLockedFieldId, true)),
            new(RetryDelayFieldId, "Ui.Step.FileSystem.RetryDelay", StepValueKind.Duration,
                DefaultValue: JsonValue.Create(100), Constraints: new(Minimum: 0), Advanced: true, Order: 12,
                VisibleWhen: Is(RetryLockedFieldId, true))
        ],
        new(
            [new("general", null, [OperationFieldId, SourceModeFieldId, SourcePathFieldId, SourceResultFieldId,
                TargetModeFieldId, TargetPathFieldId, TargetResultFieldId, NewNameFieldId, FilterFieldId, CreateParentsFieldId]),
             new("advanced", "Ui.Step.Settings.Advanced", [RetryLockedFieldId, RetryCountFieldId, RetryDelayFieldId],
                 1, true, false)],
            [new(OperationFieldId), new(SourcePathFieldId, StepSummaryValueFormat.FileName)],
            [OperationFieldId, SourceModeFieldId, SourcePathFieldId, SourceResultFieldId, TargetModeFieldId,
                TargetPathFieldId, TargetResultFieldId, NewNameFieldId, FilterFieldId, CreateParentsFieldId,
                RetryLockedFieldId, RetryCountFieldId, RetryDelayFieldId]));

    public override FileSystemOperationStep CreateDefaultStep() => new();

    protected override StepDraft Read(FileSystemOperationStep step)
    {
        var s = step.Settings;
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[OperationFieldId] = JsonValue.Create(s.Operation.ToString());
        draft.Values[SourceModeFieldId] = JsonValue.Create(s.SourceMode.ToString());
        draft.Values[SourcePathFieldId] = JsonValue.Create(s.SourcePath);
        draft.Values[SourceResultFieldId] = JsonSerializer.SerializeToNode(s.SourceResult);
        draft.Values[TargetModeFieldId] = JsonValue.Create(s.TargetMode.ToString());
        draft.Values[TargetPathFieldId] = JsonValue.Create(s.TargetPath);
        draft.Values[TargetResultFieldId] = JsonSerializer.SerializeToNode(s.TargetResult);
        draft.Values[NewNameFieldId] = JsonValue.Create(s.NewName);
        draft.Values[FilterFieldId] = JsonValue.Create(s.Filter);
        draft.Values[CreateParentsFieldId] = JsonValue.Create(s.CreateParentDirectories);
        draft.Values[RetryLockedFieldId] = JsonValue.Create(s.RetryLockedFiles);
        draft.Values[RetryCountFieldId] = JsonValue.Create(s.RetryCount);
        draft.Values[RetryDelayFieldId] = JsonValue.Create(s.RetryDelayMs);
        return draft;
    }

    protected override void Apply(StepDraft draft, FileSystemOperationStep step)
    {
        var s = step.Settings;
        s.Operation = EnumValue<FileSystemOperation>(draft, OperationFieldId);
        s.SourceMode = EnumValue<FileSystemPathSource>(draft, SourceModeFieldId);
        s.SourcePath = DefinitionValueReader.String(draft, SourcePathFieldId);
        s.SourceResult = DefinitionValueReader.Binding(draft, SourceResultFieldId);
        s.TargetMode = EnumValue<FileSystemPathSource>(draft, TargetModeFieldId);
        s.TargetPath = DefinitionValueReader.String(draft, TargetPathFieldId);
        s.TargetResult = DefinitionValueReader.Binding(draft, TargetResultFieldId);
        s.NewName = DefinitionValueReader.String(draft, NewNameFieldId);
        s.Filter = DefinitionValueReader.String(draft, FilterFieldId);
        s.CreateParentDirectories = DefinitionValueReader.Boolean(draft, CreateParentsFieldId);
        s.RetryLockedFiles = DefinitionValueReader.Boolean(draft, RetryLockedFieldId);
        s.RetryCount = DefinitionValueReader.Integer(draft, RetryCountFieldId);
        s.RetryDelayMs = DefinitionValueReader.Integer(draft, RetryDelayFieldId);
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft)
    {
        var operation = Enum.Parse<FileSystemOperation>(DefinitionValueReader.String(draft, OperationFieldId));
        if (operation == FileSystemOperation.Rename)
        {
            var name = DefinitionValueReader.String(draft, NewNameFieldId);
            if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || !string.Equals(Path.GetFileName(name), name, StringComparison.Ordinal))
                return [new("StepValidation.Invalid", NewNameFieldId)];
        }
        if (operation == FileSystemOperation.Delete)
        {
            var filter = DefinitionValueReader.String(draft, FilterFieldId);
            if (filter.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(value => value.IndexOfAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]) >= 0))
                return [new("StepValidation.Invalid", FilterFieldId)];
        }
        return [];
    }
    private static StepVisibilityRule Is(string field, string value) => new(field, JsonValue.Create(value));
    private static StepVisibilityRule Is(string field, bool value) => new(field, JsonValue.Create(value));
    private static StepVisibilityRule AnyOf(string field, params string[] values) =>
        new(field, AnyOfValues: values.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray());
    private static StepFieldDescriptor EnumField(string id, string label, string defaultValue, string[] values,
        IReadOnlyList<StepFieldOptionDescriptor> options, int order,
        IReadOnlyList<StepVisibilityRule>? visibleWhenAll = null) =>
        new(id, label, StepValueKind.Enum, true, JsonValue.Create(defaultValue),
            Constraints: new(AllowedValues: values), Order: order, Options: options, VisibleWhenAll: visibleWhenAll);
    private static T EnumValue<T>(StepDraft draft, string id) where T : struct, Enum =>
        Enum.TryParse<T>(DefinitionValueReader.String(draft, id), out var value) ? value : default;
}
