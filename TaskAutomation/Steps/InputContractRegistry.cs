using System.Collections.Frozen;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

/// <summary>Canonical semantic path resolution for every frontend, validation and runtime.</summary>
public static class InputContractRegistry
{
    internal static readonly IReadOnlySet<string> DirectOnlyProviders = new HashSet<string>();
    internal static readonly IReadOnlySet<string> LegacyTextProviders = new HashSet<string> { ValueProviderIds.Secret };
    private static readonly IReadOnlySet<string> StepResultProviders = new HashSet<string>
    {
        ValueProviderIds.StepResult
    };
    internal static readonly IReadOnlySet<string> ReusableValueProviders = new HashSet<string>
    {
        ValueProviderIds.JobVariable,
        ValueProviderIds.StepResult
    };

    private static readonly AcceptedResultShape Image = new(ResultValueKind.Image,
        ResultCardinality.Single, ResultCardinality.OptionalSingle);
    private static readonly AcceptedResultShape Points = new(ResultValueKind.Point,
        ResultCardinality.Single, ResultCardinality.OptionalSingle, ResultCardinality.Collection);
    private static readonly AcceptedResultShape Rectangles = new(ResultValueKind.Rectangle,
        ResultCardinality.Single, ResultCardinality.OptionalSingle, ResultCardinality.Collection);
    private static readonly AcceptedResultShape Detections = new(ResultValueKind.Detection, ResultCardinality.Collection);
    private static readonly AcceptedResultShape Process = new(ResultValueKind.ProcessReference,
        ResultCardinality.Single, ResultCardinality.OptionalSingle);
    private static readonly AcceptedResultShape Text = new(ResultValueKind.Text,
        ResultCardinality.Single, ResultCardinality.OptionalSingle);
    private static readonly AcceptedResultShape FilePath = new(ResultValueKind.FilePath,
        ResultCardinality.Single, ResultCardinality.OptionalSingle);
    private static readonly AcceptedResultShape Integer = new(ResultValueKind.Integer,
        ResultCardinality.Single, ResultCardinality.OptionalSingle);
    private static readonly AcceptedResultShape[] DisplayableText = TextValueFormatter.AcceptedShapes.ToArray();

    private static readonly Dictionary<Type, InputContractNode[]> RootDefinitions = new()
    {
        [typeof(TemplateMatchingStep)] = [Required("image", CollectionConsumptionMode.NotApplicable, Image), OptionalReusable("dynamicRoi", CollectionConsumptionMode.FirstValue, Rectangles)],
        [typeof(OcrStep)] = [Required("image", CollectionConsumptionMode.NotApplicable, Image), OptionalReusable("dynamicRoi", CollectionConsumptionMode.FirstValue, Rectangles)],
        [typeof(ColorDetectionStep)] = [Required("image", CollectionConsumptionMode.NotApplicable, Image), OptionalReusable("dynamicRoi", CollectionConsumptionMode.FirstValue, Rectangles)],
        [typeof(YOLODetectionStep)] = [Required("image", CollectionConsumptionMode.NotApplicable, Image), OptionalReusable("dynamicRoi", CollectionConsumptionMode.FirstValue, Rectangles)],
        [typeof(KeyPointMatchingStep)] = [Required("image", CollectionConsumptionMode.NotApplicable, Image), OptionalReusable("dynamicRoi", CollectionConsumptionMode.FirstValue, Rectangles)],
        [typeof(PredictMovementStep)] = [Required("points", CollectionConsumptionMode.AllValues, Points)],
        [typeof(KlickOnPointStep)] = [Required("points", CollectionConsumptionMode.FirstValue, Points) with
        {
            AllowedProviderIds = ReusableValueProviders,
            LegacyAllowedProviderIds = new HashSet<string> { ValueProviderIds.LocalValue }
        }],
        [typeof(KlickOnPoint3DStep)] = [Required("points", CollectionConsumptionMode.FirstValue, Points) with
        {
            AllowedProviderIds = ReusableValueProviders,
            LegacyAllowedProviderIds = new HashSet<string> { ValueProviderIds.LocalValue }
        }, OptionalReusable("origin", CollectionConsumptionMode.FirstValue, Points)],
        [typeof(DynamicRoiStep)] = [
            Required("bounds", CollectionConsumptionMode.FirstValue, Rectangles),
            Required("padding", CollectionConsumptionMode.FirstValue, Integer) with
            {
                AllowedProviderIds = ReusableValueProviders,
                AllowsDirectValue = true
            }],
        [typeof(ShowOnDesktopStep)] = [
            OptionalReusable("detections", CollectionConsumptionMode.AllValues, Detections, Rectangles, Points),
            OptionalOverlayText()],
        [typeof(ShowImageStep)] = [
            Required("image", CollectionConsumptionMode.NotApplicable, Image),
            OptionalReusable("detections", CollectionConsumptionMode.AllValues, Detections, Rectangles, Points),
            OptionalOverlayText()],
        [typeof(VideoCreationStep)] = [
            Required("image", CollectionConsumptionMode.NotApplicable, Image),
            OptionalReusable("detections", CollectionConsumptionMode.AllValues, Detections, Rectangles, Points),
            OptionalOverlayText()],
        [typeof(SaveImageStep)] = [
            Required("image", CollectionConsumptionMode.NotApplicable, Image),
            OptionalReusable("detections", CollectionConsumptionMode.AllValues, Detections, Rectangles, Points),
            OptionalOverlayText()],
        [typeof(ActiveProcessStep)] = [OptionalReusable("process", CollectionConsumptionMode.NotApplicable, Process)],
        [typeof(StartProcessStep)] = [OptionalReusable("process", CollectionConsumptionMode.NotApplicable, Process)],
        [typeof(TerminateProcessStep)] = [OptionalReusable("process", CollectionConsumptionMode.NotApplicable, Process)],
        [typeof(FocusProcessStep)] = [OptionalReusable("process", CollectionConsumptionMode.NotApplicable, Process)],
        [typeof(ActiveWindowStep)] = [OptionalReusable("process", CollectionConsumptionMode.NotApplicable, Process)],
        [typeof(PointComparisonStep)] = [OptionalReusable("points", CollectionConsumptionMode.AllValues, Points)],
        [typeof(ShowTextStep)] = [Required("text", CollectionConsumptionMode.AllValues, DisplayableText) with
        {
            AllowedProviderIds = ReusableValueProviders,
            LegacyAllowedProviderIds = LegacyTextProviders,
            TargetKind = ResultValueKind.Text,
            Conversion = InputConversion.DisplayText,
            AllowsDirectValue = true
        }],
        [typeof(FileSystemOperationStep)] =
        [
            Optional("source", CollectionConsumptionMode.NotApplicable, Text, FilePath),
            Optional("target", CollectionConsumptionMode.NotApplicable, Text, FilePath)
        ]
    };

    private static readonly IReadOnlySet<string> ReusableProviders = new HashSet<string>(StringComparer.Ordinal)
    {
        ValueProviderIds.LocalValue, ValueProviderIds.JobVariable, ValueProviderIds.StepResult
    };

    private static readonly IReadOnlySet<string> DirectProviders = new HashSet<string>(StringComparer.Ordinal)
    {
        ValueProviderIds.LocalValue
    };

    private static readonly IReadOnlyDictionary<string, InputContractSchema> SchemaDefinitions =
        new Dictionary<string, InputContractSchema>(StringComparer.Ordinal)
        {
            [InputSchemaIds.Roi] = Object(InputSchemaIds.Roi,
                ("enabled", ResultValueKind.Boolean), ("x", ResultValueKind.Integer),
                ("y", ResultValueKind.Integer), ("width", ResultValueKind.Integer),
                ("height", ResultValueKind.Integer)),
            [InputSchemaIds.ScreenPoint] = Object(InputSchemaIds.ScreenPoint,
                ("monitor_index", ResultValueKind.Integer), ("x", ResultValueKind.Integer),
                ("y", ResultValueKind.Integer)),
            [InputSchemaIds.ProcessTarget] = Object(InputSchemaIds.ProcessTarget,
                ("process_name", ResultValueKind.Text), ("executable_path", ResultValueKind.Text),
                ("window_title_contains", ResultValueKind.Text)),
            [InputSchemaIds.YoloSelection] = DirectObject(InputSchemaIds.YoloSelection,
                ("model", ResultValueKind.Text), ("class_name", ResultValueKind.Text)),
            [InputSchemaIds.UserChoiceOptions] = new(InputSchemaIds.UserChoiceOptions,
                new Dictionary<string, InputContractNode>(), InputSchemaIds.UserChoiceOption),
            [InputSchemaIds.UserChoiceOption] = Object(InputSchemaIds.UserChoiceOption,
                ("label", ResultValueKind.Text), ("value", ResultValueKind.Text)),
            [InputSchemaIds.PointEntries] = new(InputSchemaIds.PointEntries,
                new Dictionary<string, InputContractNode>(), InputSchemaIds.PointEntry),
            [InputSchemaIds.PointEntry] = Object(InputSchemaIds.PointEntry,
                ("manual_x", ResultValueKind.Integer), ("manual_y", ResultValueKind.Integer)),
            [InputSchemaIds.VisualOverlay] = new(InputSchemaIds.VisualOverlay,
                new Dictionary<string, InputContractNode>(StringComparer.Ordinal)
                {
                    ["text_results"] = Member("text_results", ResultValueKind.ResultObject, ResultCardinality.Collection, InputSchemaIds.VisualOverlayTextEntries)
                }),
            [InputSchemaIds.VisualOverlayTextEntries] = new(InputSchemaIds.VisualOverlayTextEntries,
                new Dictionary<string, InputContractNode>(), InputSchemaIds.VisualOverlayTextEntry),
            [InputSchemaIds.VisualOverlayTextEntry] = Object(InputSchemaIds.VisualOverlayTextEntry,
                ("font_size", ResultValueKind.Number), ("font_color", ResultValueKind.Color),
                ("opacity", ResultValueKind.Number), ("desktop_index", ResultValueKind.Integer),
                ("offset_x", ResultValueKind.Integer), ("offset_y", ResultValueKind.Integer),
                ("duration_ms", ResultValueKind.Integer), ("clear_on_job_end", ResultValueKind.Boolean))
        };

    private static InputContractNode Required(string key, CollectionConsumptionMode collection, params AcceptedResultShape[] shapes) =>
        new(key, PreferredKind(shapes), shapes, StepResultProviders, required: true,
            missingValuePolicy: MissingValuePolicy.FailStep, collectionConsumption: collection);
    private static InputContractNode Optional(string key, CollectionConsumptionMode collection, params AcceptedResultShape[] shapes) =>
        new(key, PreferredKind(shapes), shapes, StepResultProviders,
            legacyAllowedProviderIds: shapes.Any(shape => shape.ValueKind == ResultValueKind.Text) ? LegacyTextProviders : DirectOnlyProviders,
            missingValuePolicy: MissingValuePolicy.SkipStep, collectionConsumption: collection);
    private static InputContractNode OptionalReusable(string key, CollectionConsumptionMode collection, params AcceptedResultShape[] shapes) =>
        Optional(key, collection, shapes) with { AllowedProviderIds = ReusableValueProviders };
    private static InputContractNode OptionalOverlayText() =>
        OptionalReusable("text", CollectionConsumptionMode.AllValues, DisplayableText) with
        { TargetKind = ResultValueKind.Text, AllowsDirectValue = true, Conversion = InputConversion.DisplayText };

    private static ResultValueKind? PreferredKind(IReadOnlyList<AcceptedResultShape> shapes) =>
        shapes.Select(shape => shape.ValueKind).Distinct().Take(2).ToArray() is [var kind] ? kind : null;

    public static InputContractNode Member(string key, ResultValueKind kind,
        ResultCardinality cardinality = ResultCardinality.Single, string? schemaId = null,
        IReadOnlySet<string>? providers = null, bool allowLegacyProviders = true) => new(key, kind,
        AcceptedSourceShapes(kind, cardinality), providers ?? ReusableProviders, allowsDirectValue: true,
        legacyAcceptedShapes: LegacySourceShapes(kind, cardinality),
        legacyAllowedProviderIds: allowLegacyProviders ? LegacyProvidersFor(kind) : DirectOnlyProviders,
        collectionConsumption: kind == ResultValueKind.Text || cardinality == ResultCardinality.Collection
            ? CollectionConsumptionMode.AllValues : CollectionConsumptionMode.NotApplicable,
        conversion: kind == ResultValueKind.Text ? InputConversion.DisplayText : InputConversion.Exact,
        schemaId: schemaId, targetCardinality: cardinality);

    private static InputContractSchema Object(string id, params (string Id, ResultValueKind Kind)[] members) =>
        new(id, members.ToDictionary(member => member.Id, member => Member(member.Id, member.Kind), StringComparer.Ordinal));
    private static InputContractSchema DirectObject(string id, params (string Id, ResultValueKind Kind)[] members) =>
        new(id, members.ToDictionary(member => member.Id,
            member => Member(member.Id, member.Kind, providers: DirectProviders, allowLegacyProviders: false), StringComparer.Ordinal));

    private static readonly IReadOnlyDictionary<Type, IReadOnlyList<InputContractNode>> RootNodes =
        RootDefinitions.ToFrozenDictionary(pair => pair.Key,
            pair => (IReadOnlyList<InputContractNode>)Array.AsReadOnly(pair.Value.ToArray()));
    private static readonly IReadOnlyDictionary<string, InputContractSchema> SchemaNodes =
        SchemaDefinitions.ToFrozenDictionary(StringComparer.Ordinal);
    public static InputContractNode? Find(Type stepType, string key) =>
        Roots(stepType).FirstOrDefault(node => node.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    public static IReadOnlyList<InputContractNode> Find(Type stepType) => Roots(stepType);
    public static string? SchemaForField(StepFieldDescriptor field) => field.InputSchemaId;
    public static InputContractNode Item(string schemaId) => Member(string.Empty, ResultValueKind.ResultObject, schemaId: schemaId);
    public static InputContractNode? FindMember(string? schemaId, string path) =>
        TryResolveMember(schemaId, path, out var node) ? node : null;

    public static IEnumerable<string> SchemaIds => SchemaNodes.Keys;
    public static IReadOnlyList<InputContractNode> Roots(Type stepType) => RootNodes.GetValueOrDefault(stepType) ?? [];
    public static bool TryGetSchema(string id, out InputContractSchema schema) => SchemaNodes.TryGetValue(id, out schema!);
    public static bool TryResolveMember(string? schemaId, string path, out InputContractNode node)
    {
        node = null!;
        var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) return false;
        foreach (var segment in segments)
        {
            if (schemaId is null || !TryGetSchema(schemaId, out var schema)) return false;
            if (int.TryParse(segment, out var index))
            {
                if (index < 0 || schema.ItemSchemaId is null) return false;
                node = Item(schema.ItemSchemaId);
            }
            else if (!schema.Members.TryGetValue(segment, out node!)) return false;
            schemaId = node.SchemaId;
        }
        return true;
    }

    public static InputContractNode ForField(StepFieldDescriptor field)
    {
        var cardinality = field.ValueKind == StepValueKind.Collection ? ResultCardinality.Collection : ResultCardinality.Single;
        var kind = JobVariableInputMigration.MapKind(field);
        var isText = field.ValueKind is StepValueKind.Text or StepValueKind.MultilineText;
        return new(field.Id, kind, AcceptedSourceShapes(kind, cardinality, isText),
            IsDirectOnly(field) ? DirectOnlyProviders : ReusableValueProviders, allowsDirectValue: true,
            legacyAcceptedShapes: LegacySourceShapes(kind, cardinality), legacyAllowedProviderIds: LegacyProvidersFor(kind),
            required: field.Required, missingValuePolicy: field.Required ? MissingValuePolicy.FailStep : MissingValuePolicy.SkipStep,
            collectionConsumption: isText || cardinality == ResultCardinality.Collection
                ? CollectionConsumptionMode.AllValues : CollectionConsumptionMode.NotApplicable,
            conversion: isText ? InputConversion.DisplayText : InputConversion.Exact,
            schemaId: field.InputSchemaId, constraints: field.Constraints, targetCardinality: cardinality);
    }

    internal static IReadOnlySet<string> LegacyProvidersFor(ResultValueKind kind) =>
        kind is ResultValueKind.Text or ResultValueKind.Color or ResultValueKind.FilePath
            ? LegacyTextProviders : DirectOnlyProviders;

    public static IReadOnlyList<AcceptedResultShape> AcceptedSourceShapes(ResultValueKind kind, ResultCardinality cardinality,
        bool allowsTextConversion = true) =>
        kind == ResultValueKind.Text && allowsTextConversion ? TextValueFormatter.AcceptedShapes
        : cardinality == ResultCardinality.Single
            ? [new(kind, ResultCardinality.Single, ResultCardinality.OptionalSingle)]
            : [new(kind, cardinality)];

    public static IReadOnlyList<AcceptedResultShape> LegacySourceShapes(ResultValueKind kind, ResultCardinality cardinality) =>
        kind is ResultValueKind.Color or ResultValueKind.FilePath
            ? cardinality == ResultCardinality.Single
                ? [new(ResultValueKind.Text, ResultCardinality.Single, ResultCardinality.OptionalSingle)]
                : [new(ResultValueKind.Text, cardinality)]
            : [];

    private static bool IsDirectOnly(StepFieldDescriptor field) =>
        field.ValueKind is StepValueKind.Object or StepValueKind.Collection
        || field.ValueKind == StepValueKind.Enum
           && (field.Constraints?.AllowedValues is { Count: > 0 }
               || field.Options is { Count: > 0 });


    public static InputContractNode Resolve(Type stepType, StepFieldDescriptor field) =>
        string.IsNullOrWhiteSpace(field.InputContractId) || field.OwnsStructuredInputContract
            ? ForField(field)
            : Roots(stepType).FirstOrDefault(node => node.Key.Equals(field.InputContractId, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"Input contract '{field.InputContractId}' for {stepType.Name} is missing.");

    public static InputContractNode ResolvePath(Type stepType, StepFieldDescriptor owner, string path)
    {
        if (path == owner.Id) return Resolve(stepType, owner);
        if (!path.StartsWith(owner.Id + ".", StringComparison.Ordinal)) throw Invalid(path);
        var relative = path[(owner.Id.Length + 1)..];
        // This member stores a reference in step settings, not a child value override.
        // Its runtime contract is the overlay text contract declared by the definition.
        if (owner.InputSchemaId == InputSchemaIds.VisualOverlay
            && relative.Split('.') is ["text_results", var index, "result"]
            && int.TryParse(index, out var itemIndex) && itemIndex >= 0)
            return (InputContractRegistry.Find(stepType, owner.VisualOverlayOptions?.TextInputContractId ?? "text")
                ?? throw Invalid(path));
        return ResolveMember(owner.InputSchemaId, relative);
    }

    public static InputContractNode ResolveMember(string? schemaId, string path) =>
        TryResolveMember(schemaId, path, out var node) ? node : throw Invalid(path);

    public static IReadOnlyList<string> ValidateSchemas()
    {
        var errors = new List<string>();
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in SchemaIds) Visit(id);
        return errors;

        void Visit(string id)
        {
            if (visiting.Contains(id)) { errors.Add($"Cyclic input schema: {id}"); return; }
            if (visited.Contains(id)) return;
            if (!TryGetSchema(id, out var schema))
            { errors.Add($"Unknown input schema: {id}"); return; }
            visiting.Add(id);
            foreach (var (key, member) in schema.Members)
            {
                if (string.IsNullOrWhiteSpace(key) || key.Contains('.')) errors.Add($"Invalid member: {id}.{key}");
                if (member.SchemaId is { } child) Visit(child);
                if (!Enum.IsDefined(member.Conversion)) errors.Add($"Unknown conversion: {id}.{key}");
            }
            if (schema.ItemSchemaId is { } item) Visit(item);
            visiting.Remove(id);
            visited.Add(id);
        }
    }

    private static InvalidOperationException Invalid(string path) => new($"{path}: StepValidation.Invalid");
}
