using System.Collections;
using TaskAutomation.Jobs;
using TaskAutomation.Jobs.ControlFlow;
using TaskAutomation.Steps;

namespace DesktopAutomationApp.Localization;

public static class StepLocalization
{
    public static string Description(TaskAutomation.Contracts.Steps.StepDescriptor descriptor)
    {
        var description = Loc.Get(descriptor.DescriptionKey);
        if (string.IsNullOrWhiteSpace(descriptor.Presentation.EditorDescriptionKey)) return description;
        var editorDescription = Loc.Get(descriptor.Presentation.EditorDescriptionKey);
        return editorDescription == description ? description : $"{description}\n{editorDescription}";
    }

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
        => PropertyPath(null, propertyPath);

    public static string ResultPath(string sourceName, string propertyName) =>
        $"{sourceName} \u2192 {propertyName}";

    public static string PropertyPath(string? resultTypeName, string propertyPath)
        => ResultPropertyLocalization.Name(resultTypeName, propertyPath, LookupResultText);

    private static string? LookupResultText(string key)
    {
        var text = LocalizationService.Instance[key];
        return text == $"[{key}]" ? null : text;
    }

    public static string PropertyDescription(string resultTypeName, ResultPropertyDescriptor property)
        => ResultPropertyLocalization.Description(resultTypeName, property, LookupResultText);

    public static string PropertyGroupDescription(string resultTypeName, string path) =>
        ResultPropertyLocalization.Description(resultTypeName, path, LookupResultText) ?? string.Empty;

    public static string? PropertyHelp(ResultPropertyDescriptor? localizedProperty) => localizedProperty is null ? null
        : string.IsNullOrWhiteSpace(localizedProperty.Example) ? localizedProperty.Description
        : $"{localizedProperty.Description}\n{Loc.Format("Ui.ValueReference.ResultExample", localizedProperty.Example)}";

    public static string ResultValueType(ResultPropertyDescriptor property)
        => ResultValueType(property.DataType, property.Cardinality);

    public static string ResultValueType(ResultValueKind valueKind, ResultCardinality cardinality)
    {
        var key = valueKind switch
        {
            ResultValueKind.Boolean => "Boolean",
            ResultValueKind.Integer => "Integer",
            ResultValueKind.Number => "Number",
            ResultValueKind.Text => "Text",
            ResultValueKind.DateTime => "DateTime",
            ResultValueKind.Image => "Image",
            ResultValueKind.Point => "Point",
            ResultValueKind.Rectangle => "Rectangle",
            ResultValueKind.Color => "Color",
            ResultValueKind.FilePath => "FilePath",
            ResultValueKind.ProcessReference => "Process",
            ResultValueKind.Detection => "Detection",
            ResultValueKind.Enum => "Enum",
            _ => valueKind.ToString()
        };
        var type = GetOrFallback($"Step.ResultValueType.{key}", key);
        return cardinality == ResultCardinality.Collection
            ? GetOrFallback("Step.ResultValueType.Collection", "List of {0}").Replace("{0}", type, StringComparison.Ordinal)
            : type;
    }

    public static string DebugValueType(string typeName) =>
        DebugValueTypeLocalization.Localize(typeName, GetOrFallback);

    public static ResultTypeDescriptor ResultType(ResultTypeDescriptor descriptor) =>
        new(
            descriptor.TypeName,
            descriptor.DisplayName,
            descriptor.Properties
                .Select(p => p with
                {
                    DisplayName = PropertyPath(descriptor.TypeName, p.Name),
                    Description = PropertyDescription(descriptor.TypeName, p)
                })
                .ToArray());

    public static string NumberedName(Type type, int oneBasedIndex) =>
        $"{Type(type)} ({Loc.Format("Step.Number", oneBasedIndex)})";

    public static bool IsNumbered(JobStep step) =>
        step is not IControlFlowMarker;

    public static bool HasListPosition(JobStep step) => step is not EndIfStep;

    public static int? ListPosition(IEnumerable? steps, JobStep target)
    {
        if (!HasListPosition(target) || steps is null) return null;
        var position = 0;
        foreach (var item in steps)
        {
            if (item is not JobStep step) continue;
            if (HasListPosition(step)) position++;
            if (ReferenceEquals(step, target) || string.Equals(step.Id, target.Id, StringComparison.OrdinalIgnoreCase)) return position;
        }
        return null;
    }

    public static int? DisplayNumber(IEnumerable? steps, JobStep target)
        => IsNumbered(target) ? ListPosition(steps, target) : null;

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
