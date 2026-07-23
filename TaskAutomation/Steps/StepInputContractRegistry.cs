using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public sealed record AcceptedResultShape(ResultValueKind ValueKind, params ResultCardinality[] Cardinalities)
{
    public bool Accepts(ResultPropertyDescriptor property) => ValueKind == property.DataType
        && (Cardinalities.Length == 0 || Cardinalities.Contains(property.Cardinality));
}

public enum CollectionConsumptionMode { NotApplicable, FirstValue, AllValues }

public sealed record StepInputDescriptor(
    string Key,
    bool Required,
    MissingValuePolicy MissingValuePolicy,
    CollectionConsumptionMode CollectionConsumption,
    params AcceptedResultShape[] AcceptedShapes)
{
    public bool Accepts(ResultPropertyDescriptor property) => AcceptedShapes.Any(shape => shape.Accepts(property));

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
        [typeof(DynamicRoiStep)] = [Required("bounds", CollectionConsumptionMode.FirstValue, Rectangles)],
        [typeof(ShowOnDesktopStep)] = [Required("detections", CollectionConsumptionMode.AllValues, Detections, Rectangles, Points)],
        [typeof(ShowImageStep)] = [Required("image", CollectionConsumptionMode.NotApplicable, Image), Optional("detections", CollectionConsumptionMode.AllValues, Detections, Rectangles, Points)],
        [typeof(VideoCreationStep)] = [Required("image", CollectionConsumptionMode.NotApplicable, Image), Optional("detections", CollectionConsumptionMode.AllValues, Detections, Rectangles, Points)],
        [typeof(ActiveProcessStep)] = [Optional("process", CollectionConsumptionMode.NotApplicable, Process)],
        [typeof(TerminateProcessStep)] = [Optional("process", CollectionConsumptionMode.NotApplicable, Process)],
        [typeof(FocusProcessStep)] = [Optional("process", CollectionConsumptionMode.NotApplicable, Process)],
        [typeof(ActiveWindowStep)] = [Optional("process", CollectionConsumptionMode.NotApplicable, Process)],
        [typeof(PointComparisonStep)] = [Optional("points", CollectionConsumptionMode.AllValues, Points)],
        [typeof(ShowTextStep)] = [Required("text", CollectionConsumptionMode.FirstValue, DisplayableText)]
    };

    public static IReadOnlyList<StepInputDescriptor> Get(Type stepType) =>
        Contracts.TryGetValue(stepType, out var descriptors) ? descriptors : [];

    public static StepInputDescriptor? Get(Type stepType, string key) =>
        Get(stepType).FirstOrDefault(input => input.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

    private static StepInputDescriptor Required(string key, CollectionConsumptionMode collection, params AcceptedResultShape[] shapes) =>
        new(key, true, MissingValuePolicy.FailStep, collection, shapes);
    private static StepInputDescriptor Optional(string key, CollectionConsumptionMode collection, params AcceptedResultShape[] shapes) =>
        new(key, false, MissingValuePolicy.SkipStep, collection, shapes);
}
