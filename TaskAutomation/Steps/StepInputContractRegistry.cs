using TaskAutomation.Jobs;
using TaskAutomation.Contracts.Steps;

namespace TaskAutomation.Steps;

public sealed record AcceptedResultShape(ResultValueKind ValueKind, params ResultCardinality[] Cardinalities)
{
    public bool Accepts(ResultPropertyDescriptor property) => ValueKind == property.DataType
        && (Cardinalities.Length == 0 || Cardinalities.Contains(property.Cardinality));

    public bool Accepts(ResultValueKind valueKind, ResultCardinality cardinality) =>
        ValueKind == valueKind && (Cardinalities.Length == 0 || Cardinalities.Contains(cardinality));
}

public enum CollectionConsumptionMode { NotApplicable, FirstValue, AllValues }

public sealed record StepInputDescriptor(
    string Key,
    bool Required,
    MissingValuePolicy MissingValuePolicy,
    CollectionConsumptionMode CollectionConsumption,
    params AcceptedResultShape[] AcceptedShapes)
{
    public IReadOnlySet<string>? AllowedProviderIds { get; init; }

    public bool AllowsProvider(string providerId) =>
        AllowedProviderIds is null || AllowedProviderIds.Contains(providerId);

    public bool Accepts(ResultPropertyDescriptor property) => AcceptedShapes.Any(shape => shape.Accepts(property));

    public bool Accepts(JobVariable variable) => AcceptedShapes.Any(shape =>
        shape.Accepts(variable.ValueKind, variable.Cardinality));

    public ResultPropertyDescriptor? FindPreferredProperty(IEnumerable<ResultPropertyDescriptor> properties)
    {
        var candidates = properties.ToArray();
        foreach (var shape in AcceptedShapes)
        {
            var match = candidates.FirstOrDefault(shape.Accepts);
            if (match is not null) return match;
        }
        return null;
    }
}

/// <summary>Backend-owned input contract. UI and validation only show paths accepted here.</summary>
public static class StepInputContractRegistry
{
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
    private static readonly AcceptedResultShape Integer = new(ResultValueKind.Integer,
        ResultCardinality.Single, ResultCardinality.OptionalSingle);
    private static readonly AcceptedResultShape[] DisplayableText =
    [
        new(ResultValueKind.Text, ResultCardinality.Single, ResultCardinality.OptionalSingle),
        new(ResultValueKind.Boolean, ResultCardinality.Single, ResultCardinality.OptionalSingle),
        new(ResultValueKind.Integer, ResultCardinality.Single, ResultCardinality.OptionalSingle),
        new(ResultValueKind.Number, ResultCardinality.Single, ResultCardinality.OptionalSingle),
        new(ResultValueKind.DateTime, ResultCardinality.Single, ResultCardinality.OptionalSingle),
        new(ResultValueKind.Enum, ResultCardinality.Single, ResultCardinality.OptionalSingle),
        new(ResultValueKind.Point, ResultCardinality.Single, ResultCardinality.OptionalSingle, ResultCardinality.Collection),
        new(ResultValueKind.Rectangle, ResultCardinality.Single, ResultCardinality.OptionalSingle, ResultCardinality.Collection),
        new(ResultValueKind.Detection, ResultCardinality.Single, ResultCardinality.OptionalSingle, ResultCardinality.Collection),
        new(ResultValueKind.ProcessReference, ResultCardinality.Single, ResultCardinality.OptionalSingle, ResultCardinality.Collection)
    ];

    private static readonly Dictionary<Type, StepInputDescriptor[]> Contracts = new()
    {
        [typeof(TemplateMatchingStep)] = [Required("image", CollectionConsumptionMode.NotApplicable, Image), Optional("dynamicRoi", CollectionConsumptionMode.FirstValue, Rectangles)],
        [typeof(ColorDetectionStep)] = [Required("image", CollectionConsumptionMode.NotApplicable, Image), Optional("dynamicRoi", CollectionConsumptionMode.FirstValue, Rectangles)],
        [typeof(YOLODetectionStep)] = [Required("image", CollectionConsumptionMode.NotApplicable, Image), Optional("dynamicRoi", CollectionConsumptionMode.FirstValue, Rectangles)],
        [typeof(KeyPointMatchingStep)] = [Required("image", CollectionConsumptionMode.NotApplicable, Image), Optional("dynamicRoi", CollectionConsumptionMode.FirstValue, Rectangles)],
        [typeof(PredictMovementStep)] = [Required("points", CollectionConsumptionMode.AllValues, Points)],
        [typeof(KlickOnPointStep)] = [Required("points", CollectionConsumptionMode.FirstValue, Points)],
        [typeof(KlickOnPoint3DStep)] = [Required("points", CollectionConsumptionMode.FirstValue, Points)],
        [typeof(DynamicRoiStep)] = [
            Required("bounds", CollectionConsumptionMode.FirstValue, Rectangles),
            Required("padding", CollectionConsumptionMode.FirstValue, Integer)],
        [typeof(ShowOnDesktopStep)] = [
            Optional("detections", CollectionConsumptionMode.AllValues, Detections, Rectangles, Points),
            Optional("text", CollectionConsumptionMode.AllValues, DisplayableText)],
        [typeof(ShowImageStep)] = [
            Required("image", CollectionConsumptionMode.NotApplicable, Image),
            Optional("detections", CollectionConsumptionMode.AllValues, Detections, Rectangles, Points),
            Optional("text", CollectionConsumptionMode.AllValues, DisplayableText)],
        [typeof(VideoCreationStep)] = [
            Required("image", CollectionConsumptionMode.NotApplicable, Image),
            Optional("detections", CollectionConsumptionMode.AllValues, Detections, Rectangles, Points),
            Optional("text", CollectionConsumptionMode.AllValues, DisplayableText)],
        [typeof(SaveImageStep)] = [
            Required("image", CollectionConsumptionMode.NotApplicable, Image),
            Optional("detections", CollectionConsumptionMode.AllValues, Detections, Rectangles, Points),
            Optional("text", CollectionConsumptionMode.AllValues, DisplayableText)],
        [typeof(ActiveProcessStep)] = [Optional("process", CollectionConsumptionMode.NotApplicable, Process)],
        [typeof(StartProcessStep)] = [Optional("process", CollectionConsumptionMode.NotApplicable, Process)],
        [typeof(TerminateProcessStep)] = [Optional("process", CollectionConsumptionMode.NotApplicable, Process)],
        [typeof(FocusProcessStep)] = [Optional("process", CollectionConsumptionMode.NotApplicable, Process)],
        [typeof(ActiveWindowStep)] = [Optional("process", CollectionConsumptionMode.NotApplicable, Process)],
        [typeof(PointComparisonStep)] = [Optional("points", CollectionConsumptionMode.AllValues, Points)],
        [typeof(ShowTextStep)] = [Required("text", CollectionConsumptionMode.FirstValue, DisplayableText)],
        [typeof(FileSystemOperationStep)] =
        [
            Optional("source", CollectionConsumptionMode.NotApplicable, Text),
            Optional("target", CollectionConsumptionMode.NotApplicable, Text)
        ]
    };

    public static IReadOnlyList<StepInputDescriptor> Get(Type stepType) =>
        Contracts.TryGetValue(stepType, out var descriptors) ? descriptors : [];

    public static StepInputDescriptor? Get(Type stepType, string key) =>
        Get(stepType).FirstOrDefault(input => input.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

    public static StepInputDescriptor ForField(StepFieldDescriptor field)
    {
        var cardinality = field.ValueKind == StepValueKind.Collection
            ? ResultCardinality.Collection
            : ResultCardinality.Single;
        var kind = JobVariableInputMigration.MapKind(field.ValueKind);
        return new StepInputDescriptor(
            field.Id,
            true,
            MissingValuePolicy.FailStep,
            cardinality == ResultCardinality.Collection
                ? CollectionConsumptionMode.AllValues
                : CollectionConsumptionMode.NotApplicable,
            new AcceptedResultShape(kind, cardinality));
    }

    private static StepInputDescriptor Required(string key, CollectionConsumptionMode collection, params AcceptedResultShape[] shapes) =>
        new(key, true, MissingValuePolicy.FailStep, collection, shapes);
    private static StepInputDescriptor Optional(string key, CollectionConsumptionMode collection, params AcceptedResultShape[] shapes) =>
        new(key, false, MissingValuePolicy.SkipStep, collection, shapes);
}
