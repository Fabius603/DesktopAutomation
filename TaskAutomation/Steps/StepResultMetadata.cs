using System.Collections;
using System.Globalization;
using System.Reflection;
using TaskAutomation.Jobs;
using TaskAutomation.WindowsIntegration;

namespace TaskAutomation.Steps;

[AttributeUsage(AttributeTargets.Property)]
public sealed class ResultPropertyAttribute(string id) : Attribute
{
    public string Id { get; } = id;
    public ResultValueKind? DataType { get; init; }
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
}

public sealed record ResultPropertyDescriptor(
    string Name,
    string DisplayName,
    ResultValueKind DataType,
    string? Description = null,
    bool IsNullable = false,
    string? Example = null,
    ResultCardinality Cardinality = ResultCardinality.Single,
    string? EnumTypeName = null,
    IReadOnlyList<string>? EnumValues = null,
    string? Id = null)
{
    public string StableId => string.IsNullOrWhiteSpace(Id)
        ? ResultContractIds.FromPropertyPath(Name)
        : Id;
}

public static class ResultContractIds
{
    public static string FromPropertyPath(string path)
    {
        var result = new System.Text.StringBuilder(path.Length + 8);
        foreach (var character in path.Replace("[]", string.Empty, StringComparison.Ordinal))
        {
            if (character == '.')
            {
                if (result.Length > 0 && result[^1] != '.') result.Append('.');
                continue;
            }
            if (char.IsUpper(character) && result.Length > 0 && result[^1] is not '.' and not '_')
                result.Append('_');
            result.Append(char.ToLowerInvariant(character));
        }
        return result.ToString();
    }
}

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
                var segment = segments[index].Replace("[]", string.Empty, StringComparison.Ordinal);
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
    private static readonly Type[] ResultClrTypes = typeof(StepResultBase).Assembly.GetTypes()
        .Where(type => !type.IsAbstract && typeof(StepResultBase).IsAssignableFrom(type))
        .OrderBy(type => type.Name, StringComparer.Ordinal)
        .ToArray();

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
            .Where(step => step.IsEnabled)
            .Select((step, index) => new { step, index, ResultType = GetResultTypeForStep(step) })
            .Where(x => x.ResultType?.Properties.Length > 0)
            .Select(x => new ConditionResultSource(x.step, x.index, x.ResultType!))
            .ToArray();

    public static ResultTypeDescriptor? GetResultTypeForStep(JobStep step)
        => StepResultContractRegistry.Resolve(step);

    public static ResultPropertyDescriptor[]? GetProperties(string stepTypeName)
    {
        var stepType = typeof(TaskAutomation.Jobs.JobStep).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name == stepTypeName && typeof(TaskAutomation.Jobs.JobStep).IsAssignableFrom(t));
        if (stepType is null || Activator.CreateInstance(stepType) is not JobStep step) return null;
        return GetResultTypeForStep(step)?.Properties;
    }

    public static bool HasResult(string stepTypeName) => GetProperties(stepTypeName)?.Length > 0;
    public static string GetFriendlyName(string stepTypeName) => StepPipelineRegistry.GetDisplayName(stepTypeName);

    public static bool TryGetProperty(string resultTypeName, string propertyPath, out ResultPropertyDescriptor descriptor)
    {
        descriptor = GetResultType(resultTypeName)?.Properties.FirstOrDefault(p =>
            string.Equals(p.Name, propertyPath, StringComparison.OrdinalIgnoreCase))!;
        return descriptor is not null;
    }

    public static bool TryGetProperty(
        ResultTypeDescriptor resultType,
        ResultBinding binding,
        out ResultPropertyDescriptor descriptor) =>
        TryGetProperty(resultType, binding.PropertyId, binding.PropertyPath, out descriptor);

    public static bool TryGetProperty(
        ResultTypeDescriptor resultType,
        string? propertyId,
        string? propertyPath,
        out ResultPropertyDescriptor descriptor)
    {
        descriptor = resultType.Properties.FirstOrDefault(property =>
            !string.IsNullOrWhiteSpace(propertyId)
            && string.Equals(property.StableId, propertyId, StringComparison.OrdinalIgnoreCase))
            ?? resultType.Properties.FirstOrDefault(property =>
                string.Equals(property.Name, propertyPath, StringComparison.OrdinalIgnoreCase))!;
        return descriptor is not null;
    }

    public static bool TryGetProperty(
        Type resultType,
        string? propertyId,
        string? propertyPath,
        out ResultPropertyDescriptor descriptor)
    {
        var contract = GetResultType(resultType.Name);
        descriptor = contract?.Properties.FirstOrDefault(property =>
            !string.IsNullOrWhiteSpace(propertyId)
            && string.Equals(property.StableId, propertyId, StringComparison.OrdinalIgnoreCase))
            ?? contract?.Properties.FirstOrDefault(property =>
                string.Equals(property.Name, propertyPath, StringComparison.OrdinalIgnoreCase))!;
        return descriptor is not null;
    }

    public static bool AreComparable(ResultPropertyDescriptor left, ResultPropertyDescriptor right) =>
        left.DataType == right.DataType
        && (left.DataType != ResultValueKind.Enum || left.EnumTypeName == right.EnumTypeName)
        && (left.Cardinality == ResultCardinality.Collection)
            == (right.Cardinality == ResultCardinality.Collection);

    public static bool TryReadValue(object result, ResultPropertyDescriptor property, out object? value)
    {
        if (property.Name.Contains("[]", StringComparison.Ordinal))
            return ResultBindingResolver.TryReadPath(result, property.Name, out value);
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
        switch (property.DataType)
        {
            case ResultValueKind.Number:
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) { value = d; return true; }
                return false;
            case ResultValueKind.Integer:
                if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) { value = i; return true; }
                return false;
            case ResultValueKind.DateTime:
                if (DateTime.TryParseExact(text, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt)) { value = dt; return true; }
                return false;
            case ResultValueKind.Boolean:
                if (bool.TryParse(text, out var b)) { value = b; return true; }
                return false;
            case ResultValueKind.Enum:
                var enumValue = property.EnumValues?.FirstOrDefault(x => string.Equals(x, text, StringComparison.OrdinalIgnoreCase));
                if (enumValue is not null) { value = enumValue; return true; }
                return false;
            default: value = text; return true;
        }
    }

    private static ResultTypeDescriptor CreateDescriptor(Type type) => new(type.Name, type.Name, BuildProperties(type).ToArray());

    private static IEnumerable<ResultPropertyDescriptor> BuildProperties(Type type)
    {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(property => property.CanRead && !IsHidden(type, property)))
        {
            var attribute = GetResultPropertyAttribute(type, property)
                ?? throw new InvalidOperationException(
                    $"Result property {type.Name}.{property.Name} requires an explicit ResultProperty ID.");
            foreach (var descriptor in BuildProperty(property.PropertyType, property.Name,
                         attribute: attribute, stableId: attribute.Id))
                yield return descriptor;
        }
    }

    private static IEnumerable<ResultPropertyDescriptor> BuildProperty(Type declaredType, string path,
        ResultCardinality? forcedCardinality = null,
        ResultPropertyAttribute? attribute = null,
        string? stableId = null)
    {
        var actual = Nullable.GetUnderlyingType(declaredType) ?? declaredType;
        var nullable = Nullable.GetUnderlyingType(declaredType) is not null || !declaredType.IsValueType;
        var cardinality = forcedCardinality
            ?? (nullable ? ResultCardinality.OptionalSingle : ResultCardinality.Single);

        if (TryGetCollectionItemType(actual, out var itemType))
        {
            if (itemType == typeof(DetectionItem))
                yield return Property(path, ResultValueKind.Detection, false, ResultCardinality.Collection,
                    stableId: stableId);
            else if (itemType is not null && TryMapType(itemType, out var itemDataType))
                yield return Property(path, itemDataType, false, ResultCardinality.Collection,
                    stableId: stableId);

            yield return Property($"{path}.Count", ResultValueKind.Integer, false, ResultCardinality.Single,
                stableId: $"{stableId}.count");
            if (itemType == typeof(DetectionItem) || itemType == typeof(RuntimeProcessReference))
                foreach (var child in itemType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(property => property.CanRead && !IsHidden(itemType, property)))
                {
                    var childAttribute = GetResultPropertyAttribute(itemType, child)
                        ?? throw new InvalidOperationException(
                            $"Result property {itemType.Name}.{child.Name} requires an explicit ResultProperty ID.");
                    foreach (var descriptor in BuildProperty(
                                 child.PropertyType,
                                 $"{path}[].{child.Name}",
                                 ResultCardinality.Collection,
                                 childAttribute,
                                 $"{stableId}.{childAttribute.Id}"))
                        yield return descriptor;
                }
            yield break;
        }

        if (TryMapType(actual, out var mapped))
            yield return Property(path, attribute?.DataType ?? mapped, nullable,
                cardinality, actual.IsEnum ? actual : null, attribute, stableId);

        if (actual == typeof(System.Drawing.Point) || actual == typeof(System.Drawing.Rectangle))
            yield return Property(path, attribute?.DataType ?? SemanticKind(actual), nullable,
                cardinality, attribute: attribute, stableId: stableId);

        if (actual is not null && (actual == typeof(System.Drawing.Point)
                                   || actual == typeof(System.Drawing.Rectangle)
                                   || actual == typeof(RuntimeProcessReference)))
                foreach (var child in actual.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                         .Where(property => property.CanRead && !IsHidden(actual, property)))
                foreach (var descriptor in BuildProperty(
                             child.PropertyType,
                             $"{path}.{child.Name}",
                             cardinality,
                             stableId: $"{stableId}.{ResultContractIds.FromPropertyPath(child.Name)}"))
                    yield return descriptor;
    }

    private static bool IsHidden(Type ownerType, PropertyInfo property) =>
        property.GetCustomAttribute<ResultHiddenAttribute>(inherit: true) is not null
        || ownerType.GetInterfaces().Any(interfaceType =>
            interfaceType.GetProperty(property.Name)?.GetCustomAttribute<ResultHiddenAttribute>(inherit: true) is not null);

    private static ResultPropertyAttribute? GetResultPropertyAttribute(Type ownerType, PropertyInfo property) =>
        property.GetCustomAttribute<ResultPropertyAttribute>(inherit: true)
        ?? ownerType.GetInterfaces()
            .Select(interfaceType => interfaceType.GetProperty(property.Name))
            .Where(interfaceProperty => interfaceProperty is not null)
            .Select(interfaceProperty => interfaceProperty!.GetCustomAttribute<ResultPropertyAttribute>(inherit: true))
            .FirstOrDefault(attribute => attribute is not null);

    private static bool TryGetCollectionItemType(Type type, out Type? itemType)
    {
        itemType = type.IsArray ? type.GetElementType()
            : type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type)
                ? type.GetGenericArguments().FirstOrDefault()
                : null;
        return type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type);
    }

    private static ResultPropertyDescriptor Property(
        string path, ResultValueKind dataType, bool nullable,
        ResultCardinality cardinality = ResultCardinality.Single,
        Type? enumType = null,
        ResultPropertyAttribute? attribute = null,
        string? stableId = null) =>
        new(path, attribute?.DisplayName ?? Humanize(path), dataType,
            attribute?.Description ?? Description(dataType, nullable), nullable, Example(path), cardinality,
            enumType?.FullName, enumType is null ? null : Enum.GetNames(enumType),
            stableId ?? attribute?.Id ?? ResultContractIds.FromPropertyPath(path));

    private static ResultValueKind SemanticKind(Type type) => type.IsEnum ? ResultValueKind.Enum
        : type == typeof(bool) ? ResultValueKind.Boolean
        : type == typeof(DateTime) ? ResultValueKind.DateTime
        : type == typeof(string) ? ResultValueKind.Text
        : type == typeof(System.Drawing.Bitmap) ? ResultValueKind.Image
        : type == typeof(System.Drawing.Point) ? ResultValueKind.Point
        : type == typeof(System.Drawing.Rectangle) ? ResultValueKind.Rectangle
        : type == typeof(RuntimeProcessReference) ? ResultValueKind.ProcessReference
        : type == typeof(DetectionItem) ? ResultValueKind.Detection
        : type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long)
            ? ResultValueKind.Integer
            : ResultValueKind.Number;
    private static bool TryMapType(Type type, out ResultValueKind result)
    {
        if (type == typeof(bool)) { result = ResultValueKind.Boolean; return true; }
        if (type == typeof(string)) { result = ResultValueKind.Text; return true; }
        if (type == typeof(DateTime)) { result = ResultValueKind.DateTime; return true; }
        if (type.IsEnum) { result = ResultValueKind.Enum; return true; }
        if (type == typeof(System.Drawing.Bitmap) || type == typeof(RuntimeProcessReference))
        { result = SemanticKind(type); return true; }
        if (type == typeof(byte) || type == typeof(short) || type == typeof(int) || type == typeof(long)) { result = ResultValueKind.Integer; return true; }
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal)) { result = ResultValueKind.Number; return true; }
        result = default; return false;
    }
    private static string Humanize(string path) => string.Join(" / ", path.Split('.').Select(s =>
        System.Text.RegularExpressions.Regex.Replace(s, "(?<=[a-z0-9])([A-Z])", " $1")));
    private static string Description(ResultValueKind type, bool nullable) =>
        $"{type}{(nullable ? ", kann leer sein" : string.Empty)}";
    private static string? Example(string path) => path switch
    {
        "AppliedRoi" or "GlobalBounds" => "{X=10,Y=20,Width=300,Height=200}",
        "ErrorMessage" => "No detection point available",
        _ => null
    };
}

public sealed record ConditionResultSource(JobStep Step, int StepIndex, ResultTypeDescriptor ResultType);
