using System.Collections;
using System.Globalization;
using System.Reflection;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public enum ResultPropertyType { Bool, Double, Integer, String, DateTime }

public sealed record ResultPropertyDescriptor(
    string Name,
    string DisplayName,
    ResultPropertyType PropertyType,
    string? Description = null,
    bool IsNullable = false,
    string? Example = null);

public sealed record ResultTypeDescriptor(
    string TypeName,
    string DisplayName,
    ResultPropertyDescriptor[] Properties)
{
    // ResultTypeDescriptor ist Bestandteil von SourceStepItem (ebenfalls ein Record).
    // Ein nachträglich gesetztes Backing-Field würde deshalb Equality/GetHashCode
    // ändern, während der Eintrag bereits in einer ComboBox verwendet wird.
    private readonly IReadOnlyList<ResultPropertyNode> _propertyTree = ResultPropertyTree.Create(Properties);
    public IReadOnlyList<ResultPropertyNode> PropertyTree => _propertyTree;
}

public sealed class ResultPropertyNode
{
    private readonly List<ResultPropertyNode> _children = [];

    internal ResultPropertyNode(string segment, string displayName)
    {
        Segment = segment;
        DisplayName = displayName;
    }

    public string Segment { get; }
    public string DisplayName { get; internal set; }
    public ResultPropertyDescriptor? Property { get; internal set; }
    public IReadOnlyList<ResultPropertyNode> Children => _children;
    internal List<ResultPropertyNode> MutableChildren => _children;
}

public static class ResultPropertyTree
{
    public static IReadOnlyList<ResultPropertyNode> Create(IEnumerable<ResultPropertyDescriptor> properties)
    {
        var roots = new List<ResultPropertyNode>();
        foreach (var property in properties)
        {
            var children = roots;
            var segments = property.Name.Split('.');
            var displaySegments = property.DisplayName.Split('/', StringSplitOptions.TrimEntries);
            if (displaySegments.Length != segments.Length)
                displaySegments = segments.Select(HumanizeSegment).ToArray();

            for (var index = 0; index < segments.Length; index++)
            {
                var segment = segments[index];
                var node = children.FirstOrDefault(n => n.Segment == segment);
                if (node is null)
                {
                    node = new ResultPropertyNode(segment, displaySegments[index]);
                    children.Add(node);
                }

                if (index == segments.Length - 1)
                {
                    node.DisplayName = displaySegments[index];
                    node.Property = property;
                }
                children = node.MutableChildren;
            }
        }
        return roots;
    }

    private static string HumanizeSegment(string value) =>
        System.Text.RegularExpressions.Regex.Replace(value, "(?<=[a-z0-9])([A-Z])", " $1");
}

/// <summary>
/// Single contract used by the condition editor, validation and execution.
/// Properties are derived from the real result records, including inherited fields.
/// </summary>
public static class StepResultMetadata
{
    private static readonly Type[] ResultClrTypes =
    [
        typeof(CaptureResult), typeof(DetectionResult), typeof(TaskResult), typeof(DynamicRoiResult),
        typeof(OutputResult), typeof(ActiveProcessResult), typeof(ActiveWindowResult), typeof(PointComparisonResult)
    ];

    public static readonly IReadOnlyList<ResultTypeDescriptor> ResultTypes = ResultClrTypes
        .Select(CreateDescriptor).ToArray();

    public static ResultTypeDescriptor? GetResultType(string typeName) =>
        ResultTypes.FirstOrDefault(r => r.TypeName == typeName);

    public static StepResultBase? CreateDefaultResult(string resultTypeName)
    {
        var resultType = ResultClrTypes.FirstOrDefault(type => type.Name == resultTypeName);
        if (resultType is null) return null;
        var defaultField = resultType.GetField("Default", BindingFlags.Public | BindingFlags.Static);
        return defaultField?.GetValue(null) as StepResultBase
               ?? Activator.CreateInstance(resultType) as StepResultBase;
    }

    public static IReadOnlyList<ConditionResultSource> GetConditionSources(IReadOnlyList<JobStep> steps, int consumerIndex) =>
        steps.Take(Math.Clamp(consumerIndex, 0, steps.Count))
            .Select((step, index) => (step, index, output: StepPipelineRegistry.Get(step.GetType())?.Output))
            .Where(x => x.step.IsEnabled && !string.IsNullOrWhiteSpace(x.output) && x.output != "–")
            .Select(x => new { x.step, x.index, ResultType = GetResultType(x.output!) })
            .Where(x => x.ResultType is not null)
            .Select(x => new ConditionResultSource(x.step, x.index, x.ResultType!))
            .ToArray();

    public static ResultPropertyDescriptor[]? GetProperties(string stepTypeName)
    {
        var stepType = typeof(TaskAutomation.Jobs.JobStep).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name == stepTypeName && typeof(TaskAutomation.Jobs.JobStep).IsAssignableFrom(t));
        var output = stepType is null ? null : StepPipelineRegistry.Get(stepType)?.Output;
        if (stepType == typeof(TaskAutomation.Jobs.DynamicRoiStep)) output = nameof(DynamicRoiResult);
        return output is null ? null : GetResultType(output)?.Properties;
    }

    public static bool HasResult(string stepTypeName) => GetProperties(stepTypeName)?.Length > 0;
    public static string GetFriendlyName(string stepTypeName) => StepPipelineRegistry.GetDisplayName(stepTypeName);

    public static bool TryGetProperty(string resultTypeName, string propertyPath, out ResultPropertyDescriptor descriptor)
    {
        descriptor = GetResultType(resultTypeName)?.Properties.FirstOrDefault(p =>
            string.Equals(p.Name, propertyPath, StringComparison.OrdinalIgnoreCase))!;
        return descriptor is not null;
    }

    public static bool TryReadValue(object result, ResultPropertyDescriptor property, out object? value)
    {
        value = result;
        foreach (var segment in property.Name.Split('.'))
        {
            if (value is null) return true;
            if (segment == "Count" && value is IEnumerable enumerable)
            {
                value = enumerable.Cast<object>().Count();
                continue;
            }
            var type = value.GetType();
            var member = (MemberInfo?)type.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance)
                         ?? type.GetField(segment, BindingFlags.Public | BindingFlags.Instance);
            value = member switch
            {
                PropertyInfo p => p.GetValue(value),
                FieldInfo f => f.GetValue(value),
                _ => null
            };
            if (member is null) return false;
        }
        return true;
    }

    public static bool TryParseComparison(ResultPropertyDescriptor property, string? text, out object? value)
    {
        value = null;
        if (text is null) return false;
        switch (property.PropertyType)
        {
            case ResultPropertyType.Double:
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) { value = d; return true; }
                return false;
            case ResultPropertyType.Integer:
                if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) { value = i; return true; }
                return false;
            case ResultPropertyType.DateTime:
                if (DateTime.TryParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt)) { value = dt; return true; }
                return false;
            case ResultPropertyType.Bool:
                if (bool.TryParse(text, out var b)) { value = b; return true; }
                return false;
            default: value = text; return true;
        }
    }

    private static ResultTypeDescriptor CreateDescriptor(Type type) => new(type.Name, type.Name, BuildProperties(type).ToArray());

    private static IEnumerable<ResultPropertyDescriptor> BuildProperties(Type type)
    {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead))
        {
            var actual = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var nullable = Nullable.GetUnderlyingType(property.PropertyType) is not null || !property.PropertyType.IsValueType;
            if (TryMapType(actual, out var mapped))
            {
                yield return Property(property.Name, mapped, nullable);
                continue;
            }
            if (actual == typeof(System.Drawing.Point) || actual == typeof(System.Drawing.Rectangle))
            {
                foreach (var child in actual.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.CanRead && p.PropertyType == typeof(int)))
                    yield return Property($"{property.Name}.{child.Name}", ResultPropertyType.Integer, nullable);
                continue;
            }
            if (typeof(IEnumerable).IsAssignableFrom(actual) && actual != typeof(string))
                yield return Property($"{property.Name}.Count", ResultPropertyType.Integer, false);
        }
    }

    private static ResultPropertyDescriptor Property(string path, ResultPropertyType type, bool nullable) =>
        new(path, Humanize(path), type, Description(type, nullable), nullable, Example(path));
    private static bool TryMapType(Type type, out ResultPropertyType result)
    {
        if (type == typeof(bool)) { result = ResultPropertyType.Bool; return true; }
        if (type == typeof(string)) { result = ResultPropertyType.String; return true; }
        if (type == typeof(DateTime)) { result = ResultPropertyType.DateTime; return true; }
        if (type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long)) { result = ResultPropertyType.Integer; return true; }
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) { result = ResultPropertyType.Double; return true; }
        result = default; return false;
    }
    private static string Humanize(string path) => string.Join(" / ", path.Split('.').Select(s =>
        System.Text.RegularExpressions.Regex.Replace(s, "(?<=[a-z0-9])([A-Z])", " $1")));
    private static string Description(ResultPropertyType type, bool nullable) =>
        $"{type}{(nullable ? ", kann leer sein" : string.Empty)}";
    private static string? Example(string path) => path switch
    {
        "AppliedRoi" or "GlobalBounds" => "{X=10,Y=20,Width=300,Height=200}",
        "ErrorMessage" => "No detection point available",
        _ => null
    };
}

public sealed record ConditionResultSource(JobStep Step, int StepIndex, ResultTypeDescriptor ResultType);
