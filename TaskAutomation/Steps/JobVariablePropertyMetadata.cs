using System.Text.Json.Nodes;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

/// <summary>Describes selectable members below a compound job-variable value.</summary>
public static class JobVariablePropertyMetadata
{
    public static IReadOnlyList<ResultPropertyDescriptor> GetProperties(JobVariable variable)
    {
        var cardinality = variable.Cardinality == ResultCardinality.Collection
            ? ResultCardinality.Collection
            : ResultCardinality.Single;
        var properties = variable.ValueKind switch
        {
            ResultValueKind.Point => Point(string.Empty, cardinality),
            ResultValueKind.Rectangle => Rectangle(string.Empty, cardinality),
            ResultValueKind.Detection => Detection(string.Empty, cardinality),
            ResultValueKind.ProcessReference => Process(string.Empty, cardinality),
            ResultValueKind.ResultObject => FromJson(variable.Value, string.Empty, cardinality),
            _ => []
        };
        return properties.DistinctBy(property => property.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static IEnumerable<ResultPropertyDescriptor> Point(string prefix, ResultCardinality cardinality)
    {
        yield return Property(prefix + "X", ResultValueKind.Integer, cardinality);
        yield return Property(prefix + "Y", ResultValueKind.Integer, cardinality);
    }

    private static IEnumerable<ResultPropertyDescriptor> Rectangle(string prefix, ResultCardinality cardinality)
    {
        yield return Property(prefix + "X", ResultValueKind.Integer, cardinality);
        yield return Property(prefix + "Y", ResultValueKind.Integer, cardinality);
        yield return Property(prefix + "Width", ResultValueKind.Integer, cardinality);
        yield return Property(prefix + "Height", ResultValueKind.Integer, cardinality);
        yield return Property(prefix + "Left", ResultValueKind.Integer, cardinality);
        yield return Property(prefix + "Top", ResultValueKind.Integer, cardinality);
        yield return Property(prefix + "Right", ResultValueKind.Integer, cardinality);
        yield return Property(prefix + "Bottom", ResultValueKind.Integer, cardinality);
        yield return Property(prefix + "IsEmpty", ResultValueKind.Boolean, cardinality);
        yield return Property(prefix + "Location", ResultValueKind.Point, cardinality);
        foreach (var property in Point(prefix + "Location.", cardinality)) yield return property;
        yield return Property(prefix + "Center", ResultValueKind.Point, cardinality);
        foreach (var property in Point(prefix + "Center.", cardinality)) yield return property;
    }

    private static IEnumerable<ResultPropertyDescriptor> Detection(string prefix, ResultCardinality cardinality)
    {
        yield return Property(prefix + "Center", ResultValueKind.Point, cardinality);
        foreach (var property in Point(prefix + "Center.", cardinality)) yield return property;
        yield return Property(prefix + "BoundingBox", ResultValueKind.Rectangle, cardinality);
        foreach (var property in Rectangle(prefix + "BoundingBox.", cardinality)) yield return property;
        yield return Property(prefix + "Confidence", ResultValueKind.Number, cardinality);
    }

    private static IEnumerable<ResultPropertyDescriptor> Process(string prefix, ResultCardinality cardinality)
    {
        yield return Property(prefix + "ProcessId", ResultValueKind.Integer, cardinality);
        yield return Property(prefix + "StartTimeUtc", ResultValueKind.DateTime, cardinality);
        yield return Property(prefix + "ProcessName", ResultValueKind.Text, cardinality);
        yield return Property(prefix + "ExecutablePath", ResultValueKind.Text, cardinality);
        yield return Property(prefix + "WindowHandle", ResultValueKind.Integer, cardinality);
    }

    private static IReadOnlyList<ResultPropertyDescriptor> FromJson(
        JsonNode? value,
        string prefix,
        ResultCardinality cardinality)
    {
        if (value is JsonArray array)
            return FromJson(array.FirstOrDefault(item => item is not null), prefix, ResultCardinality.Collection);
        if (value is not JsonObject objectValue) return [];

        var properties = new List<ResultPropertyDescriptor>();
        foreach (var (name, child) in objectValue)
        {
            var path = string.IsNullOrEmpty(prefix) ? name : $"{prefix}.{name}";
            var childKind = Kind(child);
            if (childKind is not null)
                properties.Add(Property(path, childKind.Value, cardinality));
            properties.AddRange(FromJson(child, path, cardinality));
        }
        return properties;
    }

    private static ResultValueKind? Kind(JsonNode? value)
    {
        if (value is JsonObject) return ResultValueKind.ResultObject;
        if (value is JsonArray array) return Kind(array.FirstOrDefault(item => item is not null));
        if (value is not JsonValue json) return null;
        if (json.TryGetValue<bool>(out _)) return ResultValueKind.Boolean;
        if (json.TryGetValue<int>(out _) || json.TryGetValue<long>(out _)) return ResultValueKind.Integer;
        if (json.TryGetValue<double>(out _) || json.TryGetValue<decimal>(out _)) return ResultValueKind.Number;
        if (json.TryGetValue<DateTime>(out _)) return ResultValueKind.DateTime;
        if (json.TryGetValue<string>(out _)) return ResultValueKind.Text;
        return null;
    }

    private static ResultPropertyDescriptor Property(
        string path,
        ResultValueKind kind,
        ResultCardinality cardinality) => new(
        path,
        string.Join(" / ", path.Split('.').Select(Humanize)),
        kind,
        Cardinality: cardinality,
        Id: ResultContractIds.FromPropertyPath(path));

    private static string Humanize(string value) =>
        System.Text.RegularExpressions.Regex.Replace(value, "(?<=[a-z0-9])([A-Z])", " $1");
}
