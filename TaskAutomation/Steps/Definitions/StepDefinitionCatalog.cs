using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public interface IStepDefinitionCatalog
{
    IReadOnlyList<IStepDefinition> Definitions { get; }
    bool TryGetByType(Type stepType, out IStepDefinition definition);
    bool TryGetByTypeId(string typeId, out IStepDefinition definition);
    bool TryGetByName(string name, out IStepDefinition definition);
}

public sealed class StepDefinitionCatalog : IStepDefinitionCatalog
{
    private readonly IReadOnlyDictionary<Type, IStepDefinition> _byType;
    private readonly IReadOnlyDictionary<string, IStepDefinition> _byTypeId;
    private readonly IReadOnlyDictionary<string, IStepDefinition> _byName;

    public StepDefinitionCatalog(IEnumerable<IStepDefinition> definitions)
    {
        Definitions = definitions.ToArray();
        _byType = BuildUnique(Definitions, definition => definition.StepType, "step type");
        _byTypeId = BuildUnique(Definitions, definition => definition.Descriptor.TypeId, "type ID", StringComparer.Ordinal);
        _byName = BuildUnique(
            Definitions,
            definition => TrimStepSuffix(definition.StepType.Name),
            "step name",
            StringComparer.Ordinal);

        foreach (var definition in Definitions)
            ValidateDefinition(definition);
    }

    public IReadOnlyList<IStepDefinition> Definitions { get; }

    public bool TryGetByType(Type stepType, out IStepDefinition definition) =>
        _byType.TryGetValue(stepType, out definition!);

    public bool TryGetByTypeId(string typeId, out IStepDefinition definition) =>
        _byTypeId.TryGetValue(typeId, out definition!);

    public bool TryGetByName(string name, out IStepDefinition definition) =>
        _byName.TryGetValue(name, out definition!);

    private static Dictionary<TKey, IStepDefinition> BuildUnique<TKey>(
        IEnumerable<IStepDefinition> definitions,
        Func<IStepDefinition, TKey> keySelector,
        string keyName,
        IEqualityComparer<TKey>? comparer = null) where TKey : notnull
    {
        var result = new Dictionary<TKey, IStepDefinition>(comparer);
        foreach (var definition in definitions)
        {
            var key = keySelector(definition);
            if (!result.TryAdd(key, definition))
                throw new InvalidOperationException($"Duplicate step-definition {keyName}: '{key}'.");
        }
        return result;
    }

    private static void ValidateDefinition(IStepDefinition definition)
    {
        var descriptor = definition.Descriptor;
        if (string.IsNullOrWhiteSpace(descriptor.TypeId))
            throw new InvalidOperationException($"{definition.StepType.Name} has no stable type ID.");

        var fieldIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in descriptor.Fields)
        {
            if (string.IsNullOrWhiteSpace(field.Id) || !fieldIds.Add(field.Id))
                throw new InvalidOperationException(
                    $"{definition.StepType.Name} contains an empty or duplicate field ID '{field.Id}'.");
            if (field.VisibleWhen is { } rule && !descriptor.Fields.Any(candidate => candidate.Id == rule.FieldId))
                throw new InvalidOperationException(
                    $"{definition.StepType.Name} field '{field.Id}' references unknown visibility field '{rule.FieldId}'.");
            foreach (var additionalRule in field.VisibleWhenAll ?? [])
            {
                if (!descriptor.Fields.Any(candidate => candidate.Id == additionalRule.FieldId))
                    throw new InvalidOperationException(
                        $"{definition.StepType.Name} field '{field.Id}' references unknown visibility field '{additionalRule.FieldId}'.");
            }
            if (field.EditorHint is { } editorHint && !TaskAutomation.Contracts.Steps.StepEditorHints.IsKnown(editorHint))
                throw new InvalidOperationException(
                    $"{definition.StepType.Name} field '{field.Id}' uses unknown editor hint '{editorHint}'.");
            if (field.Options is { Count: > 0 }
                && field.Options.Select(option => option.Value).Distinct(StringComparer.Ordinal).Count() != field.Options.Count)
                throw new InvalidOperationException(
                    $"{definition.StepType.Name} field '{field.Id}' contains duplicate option values.");
            if (field.Constraints is { Minimum: { } minimum, Maximum: { } maximum } && minimum > maximum)
                throw new InvalidOperationException(
                    $"{definition.StepType.Name} field '{field.Id}' has inconsistent numeric constraints.");
            if (field.Constraints is { MinimumLength: { } minimumLength, MaximumLength: { } maximumLength }
                && minimumLength > maximumLength)
                throw new InvalidOperationException(
                    $"{definition.StepType.Name} field '{field.Id}' has inconsistent length constraints.");
            if (field.EditorHint is TaskAutomation.Contracts.Steps.StepEditorHints.ResultBindingPicker
                    or TaskAutomation.Contracts.Steps.StepEditorHints.ProcessTargetPicker
                    or TaskAutomation.Contracts.Steps.StepEditorHints.ExecutableProcessTargetPicker
                    or TaskAutomation.Contracts.Steps.StepEditorHints.PointEntryList
                && string.IsNullOrWhiteSpace(field.InputContractId))
                throw new InvalidOperationException(
                    $"{definition.StepType.Name} field '{field.Id}' has no input-contract ID.");
            if (field.InputContractId is { } inputContractId
                && StepInputContractRegistry.Get(definition.StepType, inputContractId) is null)
                throw new InvalidOperationException(
                    $"{definition.StepType.Name} field '{field.Id}' references unknown input contract '{inputContractId}'.");
            if (string.Equals(field.EditorHint, TaskAutomation.Contracts.Steps.StepEditorHints.VisualOverlay,
                    StringComparison.Ordinal)
                && field.VisualOverlayOptions is null)
                throw new InvalidOperationException(
                    $"{definition.StepType.Name} field '{field.Id}' has no visual-overlay options.");
            if (field.VisualOverlayOptions is { } overlayOptions
                && (StepInputContractRegistry.Get(definition.StepType, overlayOptions.DetectionInputContractId) is null
                    || StepInputContractRegistry.Get(definition.StepType, overlayOptions.TextInputContractId) is null))
                throw new InvalidOperationException(
                    $"{definition.StepType.Name} field '{field.Id}' references an unknown visual-overlay input contract.");
            if (string.Equals(field.EditorHint, TaskAutomation.Contracts.Steps.StepEditorHints.RoiPicker,
                    StringComparison.Ordinal)
                && field.RoiPickerOptions is null)
                throw new InvalidOperationException(
                    $"{definition.StepType.Name} field '{field.Id}' has no ROI-picker options.");
            if (field.RoiPickerOptions is { } roiOptions
                && StepInputContractRegistry.Get(definition.StepType, roiOptions.DynamicInputContractId) is null)
                throw new InvalidOperationException(
                    $"{definition.StepType.Name} field '{field.Id}' references an unknown dynamic-ROI input contract.");
            if (string.Equals(field.EditorHint, TaskAutomation.Contracts.Steps.StepEditorHints.YoloPicker,
                    StringComparison.Ordinal)
                && field.YoloPickerOptions is null)
                throw new InvalidOperationException(
                    $"{definition.StepType.Name} field '{field.Id}' has no YOLO-picker options.");
            if (field.YoloPickerOptions is { } yoloOptions
                && !descriptor.Fields.Any(candidate => candidate.Id == yoloOptions.RecommendedConfidenceTargetFieldId))
                throw new InvalidOperationException(
                    $"{definition.StepType.Name} field '{field.Id}' references an unknown confidence field.");
            if (string.Equals(field.EditorHint, TaskAutomation.Contracts.Steps.StepEditorHints.WindowsCapabilityPicker,
                    StringComparison.Ordinal)
                && field.WindowsCapabilityPickerOptions is null)
                throw new InvalidOperationException(
                    $"{definition.StepType.Name} field '{field.Id}' has no Windows-capability picker options.");
            if (string.Equals(field.EditorHint, TaskAutomation.Contracts.Steps.StepEditorHints.ScreenPointPicker,
                    StringComparison.Ordinal)
                && field.ScreenPointPickerOptions is null)
                throw new InvalidOperationException(
                    $"{definition.StepType.Name} field '{field.Id}' has no screen-point picker options.");
        }

        var knownFields = fieldIds;
        var fieldsById = descriptor.Fields.ToDictionary(field => field.Id, StringComparer.Ordinal);
        foreach (var section in descriptor.Presentation.EditorSections.Where(section => section.EditorNodes is not null))
            ValidateEditorNodes(definition, section, fieldsById);
        foreach (var fieldId in descriptor.Presentation.EditorSections.SelectMany(section => section.FieldIds)
                     .Concat(descriptor.Presentation.SummaryItems.Select(item => item.FieldId))
                     .Concat(descriptor.Presentation.DetailFieldIds))
        {
            if (!knownFields.Contains(fieldId))
                throw new InvalidOperationException(
                    $"{definition.StepType.Name} presentation references unknown field '{fieldId}'.");
        }
    }

    private static void ValidateEditorNodes(
        IStepDefinition definition,
        TaskAutomation.Contracts.Steps.StepEditorSectionDescriptor section,
        IReadOnlyDictionary<string, TaskAutomation.Contracts.Steps.StepFieldDescriptor> fieldsById)
    {
        var sectionFields = section.FieldIds.ToHashSet(StringComparer.Ordinal);
        var ownedFields = new HashSet<string>(StringComparer.Ordinal);
        var activeNodes = new HashSet<TaskAutomation.Contracts.Steps.StepEditorNodeDescriptor>(ReferenceEqualityComparer.Instance);

        foreach (var node in section.EditorNodes ?? [])
            Visit(node);
        if (!ownedFields.SetEquals(sectionFields))
            throw Invalid("does not represent every section field exactly once");
        return;

        void Visit(TaskAutomation.Contracts.Steps.StepEditorNodeDescriptor node)
        {
            if (!activeNodes.Add(node))
                throw Invalid("contains a cycle");
            switch (node)
            {
                case TaskAutomation.Contracts.Steps.StepFieldNodeDescriptor fieldNode:
                    Own(fieldNode.FieldId);
                    break;
                case TaskAutomation.Contracts.Steps.StepChoiceGroupDescriptor group:
                    Own(group.SelectionFieldId);
                    if (!fieldsById.TryGetValue(group.SelectionFieldId, out var selectionField)
                        || selectionField.ValueKind != TaskAutomation.Contracts.Steps.StepValueKind.Enum
                        || group.Branches.Count < 2)
                        throw Invalid($"uses invalid selection field '{group.SelectionFieldId}'");
                    var values = group.Branches.Select(branch => branch.Value).ToArray();
                    if (values.Distinct(StringComparer.Ordinal).Count() != values.Length
                        || values.Any(value => selectionField.Options?.Any(option => option.Value == value) != true))
                        throw Invalid($"contains duplicate or unknown values for '{group.SelectionFieldId}'");
                    if (selectionField.DefaultValue is not null
                        && (!TryReadString(selectionField.DefaultValue, out var defaultValue)
                            || !values.Contains(defaultValue, StringComparer.Ordinal)))
                        throw Invalid($"does not contain the default value for '{group.SelectionFieldId}'");
                    foreach (var branch in group.Branches)
                        foreach (var child in branch.Children)
                            Visit(child);
                    break;
                default:
                    throw Invalid($"contains unsupported node '{node.GetType().Name}'");
            }
            activeNodes.Remove(node);
        }

        void Own(string fieldId)
        {
            if (!fieldsById.ContainsKey(fieldId) || !sectionFields.Contains(fieldId) || !ownedFields.Add(fieldId))
                throw Invalid($"references unknown, cross-section, or repeated field '{fieldId}'");
        }

        InvalidOperationException Invalid(string reason) => new(
            $"{definition.StepType.Name} section '{section.Id}' choice group structure {reason}.");

        static bool TryReadString(System.Text.Json.Nodes.JsonNode value, out string text)
        {
            try
            {
                text = value.GetValue<string>();
                return true;
            }
            catch (InvalidOperationException)
            {
                text = string.Empty;
                return false;
            }
        }
    }

    private static string TrimStepSuffix(string value) =>
        value.EndsWith("Step", StringComparison.Ordinal) ? value[..^4] : value;
}

public static class BuiltInStepDefinitions
{
    public static IStepDefinitionCatalog Instance { get; } = new StepDefinitionCatalog(
    [
        new TimeoutStepDefinition(),
        new BlockInputStepDefinition(),
        new UnblockInputStepDefinition(),
        new EndJobStepDefinition(),
        new ContinueJobStepDefinition(),
        new DesktopDuplicationStepDefinition(),
        new ScriptExecutionStepDefinition(),
        new GetProcessStepDefinition(),
        new MakroExecutionStepDefinition(),
        new JobExecutionStepDefinition(),
        new ActiveProcessStepDefinition(),
        new ActiveWindowStepDefinition(),
        new TerminateProcessStepDefinition(),
        new FocusProcessStepDefinition(),
        new StartProcessStepDefinition(),
        new DynamicRoiStepDefinition(),
        new PredictMovementStepDefinition(),
        new KlickOnPointStepDefinition(),
        new KlickOnPoint3DStepDefinition(),
        new FileSystemOperationStepDefinition(),
        new ShowTextStepDefinition(),
        new UserChoiceStepDefinition(),
        new PointComparisonStepDefinition(),
        new IfStepDefinition(),
        new ElseIfStepDefinition(),
        new WindowsStateQueryStepDefinition(),
        new WindowsSettingChangeStepDefinition(),
        new ElseStepDefinition(),
        new EndIfStepDefinition(),
        new CameraCaptureStepDefinition(),
        new ShowImageStepDefinition(),
        new ShowOnDesktopStepDefinition(),
        new VideoCreationStepDefinition(),
        new SaveImageStepDefinition(),
        new TemplateMatchingStepDefinition(),
        new ColorDetectionStepDefinition(),
        new YoloDetectionStepDefinition(),
        new KeyPointMatchingStepDefinition()
    ]);
}
