using TaskAutomation.Contracts.Steps;

namespace TaskAutomation.Jobs;

/// <summary>
/// Stores compound input bindings as a tree while keeping legacy dotted input keys readable.
/// Every node may have a provider reference as its base value and structured child overrides.
/// </summary>
public static class ValueBindingTree
{
    public static ResultBinding? Find(
        IReadOnlyDictionary<string, ResultBinding>? inputs,
        string path)
    {
        if (inputs is null || string.IsNullOrWhiteSpace(path)) return null;
        if (inputs.TryGetValue(path, out var legacy)) return legacy;
        var segments = Split(path);
        if (segments.Length == 0 || !inputs.TryGetValue(segments[0], out var current)) return null;
        for (var index = 1; index < segments.Length; index++)
        {
            if (int.TryParse(segments[index], out var itemIndex))
            {
                if (current.Items is null || itemIndex < 0 || itemIndex >= current.Items.Count) return null;
                current = current.Items[itemIndex];
            }
            else
            {
                if (current.Members is null || !current.Members.TryGetValue(segments[index], out var member))
                    return null;
                current = member;
            }
        }
        return current;
    }

    public static void Set(
        IDictionary<string, ResultBinding> inputs,
        string path,
        ResultBinding binding)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(binding);
        var segments = Split(path);
        if (segments.Length == 0) throw new ArgumentException("A binding path is required.", nameof(path));
        if (segments.Length == 1)
        {
            inputs[segments[0]] = binding;
            return;
        }

        if (!inputs.TryGetValue(segments[0], out var root))
            inputs[segments[0]] = root = new ResultBinding();
        Set(root, segments, 1, binding);
    }

    public static bool Normalize(
        IDictionary<string, ResultBinding> inputs,
        IReadOnlyList<StepFieldDescriptor> fields)
    {
        var nested = inputs.Where(candidate => candidate.Key.Contains('.')).ToArray();
        if (nested.Length == 0)
        {
            ApplySchemas(inputs, fields);
            return false;
        }
        foreach (var (path, binding) in nested)
        {
            Set(inputs, path, binding);
            inputs.Remove(path);
        }
        ApplySchemas(inputs, fields);
        return true;
    }

    public static void ApplySchemas(
        IDictionary<string, ResultBinding> inputs,
        IReadOnlyList<StepFieldDescriptor> fields)
    {
        foreach (var field in fields)
        {
            if (!inputs.TryGetValue(field.Id, out var root)) continue;
            var schemaId = ValueBindingSchemaRegistry.ForField(field);
            if (schemaId is null) continue;
            ApplySchema(root, schemaId);
        }
    }

    public static IEnumerable<(string Path, ResultBinding Binding)> EnumerateReferences(
        IReadOnlyDictionary<string, ResultBinding> inputs)
    {
        foreach (var (key, binding) in inputs)
            foreach (var item in EnumerateReferences(binding, key))
                yield return item;
    }

    private static IEnumerable<(string Path, ResultBinding Binding)> EnumerateReferences(
        ResultBinding binding,
        string path)
    {
        if (binding.HasProviderReference || binding.TryGetStepResult(out _)) yield return (path, binding);
        if (binding.Members is not null)
            foreach (var (member, child) in binding.Members)
                foreach (var item in EnumerateReferences(child, $"{path}.{member}"))
                    yield return item;
        if (binding.Items is not null)
            for (var index = 0; index < binding.Items.Count; index++)
                foreach (var item in EnumerateReferences(binding.Items[index], $"{path}.{index}"))
                    yield return item;
    }

    private static void Set(ResultBinding current, string[] segments, int index, ResultBinding binding)
    {
        if (int.TryParse(segments[index], out var itemIndex))
        {
            if (itemIndex < 0) throw new ArgumentOutOfRangeException(nameof(segments));
            current.Items ??= [];
            while (current.Items.Count <= itemIndex) current.Items.Add(new ResultBinding());
            if (index == segments.Length - 1) current.Items[itemIndex] = binding;
            else Set(current.Items[itemIndex], segments, index + 1, binding);
            return;
        }

        current.Members ??= new Dictionary<string, ResultBinding>(StringComparer.Ordinal);
        if (index == segments.Length - 1)
        {
            current.Members[segments[index]] = binding;
            return;
        }
        if (!current.Members.TryGetValue(segments[index], out var child))
            current.Members[segments[index]] = child = new ResultBinding();
        Set(child, segments, index + 1, binding);
    }

    private static void ApplySchema(ResultBinding binding, string schemaId)
    {
        binding.SchemaId = schemaId;
        if (!ValueBindingSchemaRegistry.TryGet(schemaId, out var schema)) return;
        if (binding.Members is not null)
            foreach (var (memberId, child) in binding.Members)
                if (schema.Members.GetValueOrDefault(memberId)?.NestedSchemaId is { } nested)
                    ApplySchema(child, nested);
        if (binding.Items is not null && schema.ItemSchemaId is { } itemSchema)
            foreach (var item in binding.Items) ApplySchema(item, itemSchema);
    }

    private static string[] Split(string path) =>
        path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

public sealed record ValueBindingMemberDescriptor(
    ResultValueKind ValueKind,
    ResultCardinality Cardinality = ResultCardinality.Single,
    string? NestedSchemaId = null,
    IReadOnlySet<string>? AllowedProviderIds = null);

public sealed record ValueBindingSchemaDescriptor(
    string Id,
    IReadOnlyDictionary<string, ValueBindingMemberDescriptor> Members,
    string? ItemSchemaId = null);

public static class ValueBindingSchemaRegistry
{
    public const string Roi = "desktopautomation.roi/v1";
    public const string ScreenPoint = "desktopautomation.screen-point/v1";
    public const string ProcessTarget = "desktopautomation.process-target/v1";
    public const string YoloSelection = "desktopautomation.yolo-selection/v1";
    public const string UserChoiceOptions = "desktopautomation.user-choice-options/v1";
    public const string UserChoiceOption = "desktopautomation.user-choice-option/v1";
    public const string PointEntries = "desktopautomation.point-entries/v1";
    public const string PointEntry = "desktopautomation.point-entry/v1";
    public const string VisualOverlay = "desktopautomation.visual-overlay/v1";
    public const string VisualOverlayTextEntries = "desktopautomation.visual-overlay-text-entries/v1";
    public const string VisualOverlayTextEntry = "desktopautomation.visual-overlay-text-entry/v1";

    private static readonly IReadOnlySet<string> ReusableProviders = new HashSet<string>(StringComparer.Ordinal)
    {
        ValueProviderIds.LocalValue, ValueProviderIds.JobVariable, ValueProviderIds.StepResult
    };

    private static readonly IReadOnlySet<string> DirectProviders = new HashSet<string>(StringComparer.Ordinal)
    {
        ValueProviderIds.LocalValue
    };

    private static readonly IReadOnlyDictionary<string, ValueBindingSchemaDescriptor> Schemas =
        new Dictionary<string, ValueBindingSchemaDescriptor>(StringComparer.Ordinal)
        {
            [Roi] = Object(Roi,
                ("enabled", ResultValueKind.Boolean), ("x", ResultValueKind.Integer),
                ("y", ResultValueKind.Integer), ("width", ResultValueKind.Integer),
                ("height", ResultValueKind.Integer)),
            [ScreenPoint] = Object(ScreenPoint,
                ("monitor_index", ResultValueKind.Integer), ("x", ResultValueKind.Integer),
                ("y", ResultValueKind.Integer)),
            [ProcessTarget] = Object(ProcessTarget,
                ("process_name", ResultValueKind.Text), ("executable_path", ResultValueKind.Text),
                ("window_title_contains", ResultValueKind.Text)),
            [YoloSelection] = DirectObject(YoloSelection,
                ("model", ResultValueKind.Text), ("class_name", ResultValueKind.Text)),
            [UserChoiceOptions] = new(UserChoiceOptions,
                new Dictionary<string, ValueBindingMemberDescriptor>(), UserChoiceOption),
            [UserChoiceOption] = Object(UserChoiceOption,
                ("label", ResultValueKind.Text), ("value", ResultValueKind.Text)),
            [PointEntries] = new(PointEntries,
                new Dictionary<string, ValueBindingMemberDescriptor>(), PointEntry),
            [PointEntry] = Object(PointEntry,
                ("manual_x", ResultValueKind.Integer), ("manual_y", ResultValueKind.Integer)),
            [VisualOverlay] = new(VisualOverlay,
                new Dictionary<string, ValueBindingMemberDescriptor>(StringComparer.Ordinal)
                {
                    ["text_results"] = new(ResultValueKind.ResultObject,
                        NestedSchemaId: VisualOverlayTextEntries,
                        AllowedProviderIds: ReusableProviders)
                }),
            [VisualOverlayTextEntries] = new(VisualOverlayTextEntries,
                new Dictionary<string, ValueBindingMemberDescriptor>(), VisualOverlayTextEntry),
            [VisualOverlayTextEntry] = Object(VisualOverlayTextEntry,
                ("font_size", ResultValueKind.Number), ("font_color", ResultValueKind.Color),
                ("opacity", ResultValueKind.Number), ("desktop_index", ResultValueKind.Integer),
                ("offset_x", ResultValueKind.Integer), ("offset_y", ResultValueKind.Integer),
                ("duration_ms", ResultValueKind.Integer), ("clear_on_job_end", ResultValueKind.Boolean))
        };

    public static bool TryGet(string schemaId, out ValueBindingSchemaDescriptor schema) =>
        Schemas.TryGetValue(schemaId, out schema!);

    public static string? ForField(StepFieldDescriptor field) => field.EditorHint switch
    {
        StepEditorHints.RoiPicker => Roi,
        StepEditorHints.ScreenPointPicker => ScreenPoint,
        StepEditorHints.ProcessTargetPicker or StepEditorHints.ExecutableProcessTargetPicker => ProcessTarget,
        StepEditorHints.YoloPicker => YoloSelection,
        StepEditorHints.UserChoiceOptions => UserChoiceOptions,
        StepEditorHints.PointEntryList => PointEntries,
        StepEditorHints.VisualOverlay => VisualOverlay,
        _ => null
    };

    private static ValueBindingSchemaDescriptor Object(
        string id,
        params (string Id, ResultValueKind Kind)[] members) => new(
        id,
        members.ToDictionary(member => member.Id,
            member => new ValueBindingMemberDescriptor(member.Kind, AllowedProviderIds: ReusableProviders),
            StringComparer.Ordinal));

    private static ValueBindingSchemaDescriptor DirectObject(
        string id,
        params (string Id, ResultValueKind Kind)[] members) => new(
        id,
        members.ToDictionary(member => member.Id,
            member => new ValueBindingMemberDescriptor(member.Kind, AllowedProviderIds: DirectProviders),
            StringComparer.Ordinal));

}
