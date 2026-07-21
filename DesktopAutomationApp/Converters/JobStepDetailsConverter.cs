using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Data;
using DesktopAutomationApp.Localization;
using DesktopAutomationApp.Services.Jobs;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace DesktopAutomationApp.Converters;

public sealed record StepDetailItem(string Name, string Value);
public sealed record StepDetailGroup(string Title, IReadOnlyList<StepDetailItem> Items);
public sealed record StepResultDetails(string TypeName, IReadOnlyList<StepDetailItem> Properties);
public sealed record JobStepDetails(IReadOnlyList<StepDetailGroup> Groups, StepResultDetails? Result);

/// <summary>Creates a complete, read-only description directly from a step's settings and result types.</summary>
public sealed class JobStepDetailsConverter : IMultiValueConverter
{
    private static readonly JobStepDetailsProvider Provider = new();

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.FirstOrDefault() is not JobStep step)
            return new JobStepDetails([], null);

        var steps = values.Skip(1).FirstOrDefault() as IEnumerable;
        return Provider.GetDetails(step, steps);
    }

    private static void AddConditions(IfConditionSettings settings,
        List<(string Group, StepDetailItem Item)> target, IEnumerable? steps)
    {
        target.Add(("general", new StepDetailItem(Loc.Get("Ui.Step.Settings.ConditionMatchMode"),
            settings.MatchMode == ConditionMatchMode.All ? Loc.Get("Ui.Step.Settings.AllAND") : Loc.Get("Ui.Step.Settings.OneOR"))));
        var index = 1;
        foreach (var condition in settings.Conditions)
        {
            target.Add(("conditions", new StepDetailItem(
                $"{index}. {Loc.Get("Ui.Step.IfEditor.Condition")}",
                ConditionDisplayFormatter.Format(condition, steps as IList))));
            index++;
        }
    }

    private static string OperatorText(ConditionOperator op) => op switch
    {
        ConditionOperator.IsTrue => Loc.Get("Condition.IsTrue"), ConditionOperator.IsFalse => Loc.Get("Condition.IsFalse"),
        ConditionOperator.Equals => "=", ConditionOperator.NotEquals => "≠", ConditionOperator.GreaterThan => ">",
        ConditionOperator.LessThan => "<", ConditionOperator.GreaterThanOrEqual => "≥", ConditionOperator.LessThanOrEqual => "≤",
        ConditionOperator.Contains => Loc.Get("Condition.Contains"), ConditionOperator.StartsWith => Loc.Get("Condition.StartsWith"),
        ConditionOperator.IsEmpty => Loc.Get("Condition.IsEmpty"), ConditionOperator.IsNotEmpty => Loc.Get("Condition.IsNotEmpty"), _ => op.ToString()
    };

    private static void AddProperties(object value, string prefix,
        List<(string Group, StepDetailItem Item)> target, IEnumerable? steps, int depth)
    {
        if (depth > 2) return;
        foreach (var property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     .Where(p => p.CanRead && p.GetIndexParameters().Length == 0))
        {
            var propertyValue = property.GetValue(value);
            if (IsNested(property.PropertyType))
            {
                if (propertyValue is not null)
                    AddProperties(propertyValue, prefix + Humanize(property.Name) + " / ", target, steps, depth + 1);
                continue;
            }

            var name = property.Name;
            var displayValue = FormatValue(propertyValue);
            if (name.EndsWith("StepId", StringComparison.Ordinal) && propertyValue is string id && id.Length > 0)
                displayValue = ResolveStep(id, steps) ?? id;
            target.Add((GetGroup(name), new StepDetailItem(prefix + Humanize(name), displayValue)));
        }
    }

    private static StepResultDetails? CreateResultDetails(JobStep step)
    {
        var output = StepPipelineRegistry.Get(step.GetType())?.Output;
        if (string.IsNullOrWhiteSpace(output) || output == "–") return new StepResultDetails("–", []);
        if (step is DynamicRoiStep) output = nameof(DynamicRoiResult);

        var type = typeof(StepResultBase).Assembly.GetType($"TaskAutomation.Steps.{output}");
        if (type is null) return new StepResultDetails(output, []);
        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .Select(p => new StepDetailItem(Humanize(p.Name), FriendlyType(p.PropertyType)))
            .ToArray();
        return new StepResultDetails(output, properties);
    }

    private static string GetGroup(string name)
    {
        if (name.Contains("Source", StringComparison.OrdinalIgnoreCase) || name.EndsWith("StepId", StringComparison.Ordinal)) return "source";
        if (name.Contains("Roi", StringComparison.OrdinalIgnoreCase)) return "roi";
        if (name.Contains("Threshold", StringComparison.OrdinalIgnoreCase) || name.Contains("Confidence", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Template", StringComparison.OrdinalIgnoreCase) || name.Contains("Color", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Model", StringComparison.OrdinalIgnoreCase)) return "detection";
        if (name.Contains("Interval", StringComparison.OrdinalIgnoreCase) || name.Contains("Reset", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Timeout", StringComparison.OrdinalIgnoreCase) || name.Contains("Downscale", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Delay", StringComparison.OrdinalIgnoreCase)) return "advanced";
        return "general";
    }

    private static string GroupTitle(string group) => group switch
    {
        "source" => Loc.Get("Ui.Job.Steps.DetailsSources"),
        "detection" => Loc.Get("Ui.Job.Steps.DetailsDetection"),
        "roi" => Loc.Get("Ui.Job.Steps.DetailsRoi"),
        "advanced" => Loc.Get("Ui.Job.Steps.DetailsAdvanced"),
        "conditions" => Loc.Get("Ui.Job.Steps.DetailsConditions"),
        _ => Loc.Get("Ui.Job.Steps.DetailsGeneral")
    };

    private static bool IsNested(Type type) => !type.IsPrimitive && !type.IsEnum && type != typeof(string) &&
        !type.IsValueType && !typeof(IEnumerable).IsAssignableFrom(type);
    private static string Humanize(string value) => Regex.Replace(value, "(?<=[a-z0-9])([A-Z])", " $1");
    private static string FriendlyType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
            return $"Liste<{FriendlyType(type.GetGenericArguments()[0])}>";
        return type.Name switch { "Boolean" => "Bool", "String" => "Text", "Double" => "Zahl", "Int32" => "Ganzzahl", _ => type.Name };
    }
    private static string FormatValue(object? value) => value switch
    {
        null => "–", bool b => b ? Loc.Get("Ui.Common.Yes") : Loc.Get("Ui.Common.No"),
        OpenCvSharp.Rect r => $"X {r.X}  ·  Y {r.Y}  ·  {r.Width} × {r.Height} px",
        double d => d.ToString("0.###", CultureInfo.CurrentCulture), float f => f.ToString("0.###", CultureInfo.CurrentCulture),
        IEnumerable sequence when value is not string => string.Join(", ", sequence.Cast<object>()),
        _ => value.ToString() ?? "–"
    };
    private static string? ResolveStep(string id, IEnumerable? steps)
    {
        var list = steps?.Cast<object>().OfType<JobStep>().ToList();
        var index = list?.FindIndex(s => s.Id == id) ?? -1;
        if (index < 0) return null;
        var step = list![index];
        var number = StepLocalization.DisplayNumber(list, step);
        return number.HasValue ? $"{number.Value}. {StepLocalization.Type(step.GetType())}" : StepLocalization.Type(step.GetType());
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class StepHasDetailsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is EndIfStep ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class StepDetailsEnabledConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is not EndIfStep;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class StepRegularSummaryVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is IfStep or ElseIfStep or ElseStep or EndIfStep ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class StepBranchSummaryConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var step = values.FirstOrDefault();
        if (step is ElseStep) return Loc.Get("Ui.Step.Settings.ElseDescription");
        if (step is not IfStep and not ElseIfStep) return string.Empty;
        var settings = step is IfStep i ? i.Settings : ((ElseIfStep)step).Settings;
        var steps = values.Skip(1).FirstOrDefault() as IList;
        var join = settings.MatchMode == ConditionMatchMode.All ? " AND " : " OR ";
        return string.Join(join, settings.Conditions.Select(condition =>
            ConditionDisplayFormatter.Format(condition, steps)));
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
}

public sealed class StepCanAddBranchesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is IfStep or ElseIfStep ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
