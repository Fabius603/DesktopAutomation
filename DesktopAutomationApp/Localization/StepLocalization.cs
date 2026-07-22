using System.Collections;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace DesktopAutomationApp.Localization;

public static class StepLocalization
{
    public static string Type(Type type) => Type(type.Name);

    public static string Type(string typeName)
    {
        var name = typeName.EndsWith("Step", StringComparison.Ordinal)
            ? typeName[..^4]
            : typeName;
        return GetOrFallback($"Step.Type.{name}", StepPipelineRegistry.GetDisplayName(typeName));
    }

    public static string Property(string propertyName, string? fallback = null) =>
        GetOrFallback($"Step.ResultProperty.{propertyName}", fallback ?? propertyName);

    public static string PropertyPath(string propertyPath)
    {
        var fullPathKey = $"Step.ResultProperty.{propertyPath}";
        var fullPath = LocalizationService.Instance[fullPathKey];
        if (fullPath != $"[{fullPathKey}]") return fullPath;

        return string.Join(" / ", propertyPath.Split('.').Select(segment =>
        {
            var cleanSegment = segment.Replace("[]", string.Empty, StringComparison.Ordinal);
            var segmentKey = $"Ui.Job.Steps.ResultProperty.{cleanSegment}";
            var translated = LocalizationService.Instance[segmentKey];
            return translated == $"[{segmentKey}]"
                ? System.Text.RegularExpressions.Regex.Replace(cleanSegment, "(?<=[a-z0-9])([A-Z])", " $1")
                : translated;
        }));
    }

    public static string ResultValueType(ResultPropertyDescriptor property)
    {
        var key = property.ValueKind switch
        {
            ResultValueKind.Boolean => "Boolean",
            ResultValueKind.Integer => "Integer",
            ResultValueKind.Number => "Number",
            ResultValueKind.Text => "Text",
            ResultValueKind.DateTime => "DateTime",
            ResultValueKind.Image => "Image",
            ResultValueKind.Point => "Point",
            ResultValueKind.Rectangle => "Rectangle",
            ResultValueKind.ProcessReference => "Process",
            ResultValueKind.Detection => "Detection",
            ResultValueKind.Enum => "Enum",
            _ => property.PropertyType.ToString()
        };
        var type = GetOrFallback($"Step.ResultValueType.{key}", key);
        return property.Cardinality == ResultCardinality.Collection
            ? GetOrFallback("Step.ResultValueType.Collection", "List of {0}").Replace("{0}", type, StringComparison.Ordinal)
            : type;
    }

    public static ResultTypeDescriptor ResultType(ResultTypeDescriptor descriptor) =>
        new(
            descriptor.TypeName,
            descriptor.DisplayName,
            descriptor.Properties
                .Select(p => new ResultPropertyDescriptor(p.Name, PropertyPath(p.Name), p.PropertyType,
                    p.Description, p.IsNullable, p.Example, p.ValueKind, p.Cardinality, p.EnumTypeName, p.EnumValues))
                .ToArray());

    public static string NumberedName(Type type, int oneBasedIndex) =>
        $"{Type(type)} ({Loc.Format("Step.Number", oneBasedIndex)})";

    public static bool IsNumbered(JobStep step) =>
        step is not (IfStep or ElseIfStep or ElseStep or EndIfStep);

    public static int? DisplayNumber(IEnumerable? steps, JobStep target)
    {
        if (!IsNumbered(target) || steps is null) return null;
        var number = 0;
        foreach (var item in steps)
        {
            if (item is not JobStep step) continue;
            if (IsNumbered(step)) number++;
            if (ReferenceEquals(step, target) || step.Id == target.Id) return number;
        }
        return null;
    }

    public static string NumberedName(JobStep step, IEnumerable? steps)
    {
        var number = DisplayNumber(steps, step);
        return number.HasValue ? NumberedName(step.GetType(), number.Value) : Type(step.GetType());
    }

    public static string ResultStepName(JobStep step, IEnumerable? steps)
    {
        var number = DisplayNumber(steps, step);
        return number.HasValue
            ? Loc.Format("Ui.Step.ResultSource.StepLabel", number.Value, Type(step.GetType()))
            : Type(step.GetType());
    }

    public static string ResultSourceName(
        JobStep step,
        IEnumerable? steps,
        ResultTypeDescriptor resultType,
        string? propertyName = null)
    {
        var resultValue = resultType.TypeName;
        if (!string.IsNullOrWhiteSpace(propertyName))
            resultValue += "." + PropertyPath(propertyName);
        return Loc.Format("Ui.Step.ResultSource.FromStep", resultValue, NumberedName(step, steps));
    }

    private static string GetOrFallback(string key, string fallback)
    {
        var value = LocalizationService.Instance[key];
        return value == $"[{key}]" ? fallback : value;
    }
}
