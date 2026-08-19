using System.Globalization;
using System.Text.Json.Nodes;
using DesktopAutomationApp.Localization;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace DesktopAutomationApp.Services.Jobs;

public interface IValueReferenceDisplayFormatter
{
    string CompactValue(JobVariable variable);
    string FullValue(JobVariable variable);
    string Type(ResultValueKind kind, ResultCardinality cardinality);
}

public sealed class ValueReferenceDisplayFormatter : IValueReferenceDisplayFormatter
{
    public static ValueReferenceDisplayFormatter Instance { get; } = new();

    public string CompactValue(JobVariable variable) => Truncate(FullValue(variable), 48);

    public string FullValue(JobVariable variable)
    {
        if (variable.Value is null) return Loc.Get("Ui.ValueReference.EmptyValue");
        try
        {
            return variable.ValueKind switch
            {
                ResultValueKind.Text or ResultValueKind.Enum => variable.Value.GetValue<string>(),
                ResultValueKind.Boolean => variable.Value.GetValue<bool>()
                    ? Loc.Get("Ui.Common.Yes")
                    : Loc.Get("Ui.Common.No"),
                ResultValueKind.Integer => variable.Value.GetValue<int>().ToString(CultureInfo.CurrentCulture),
                ResultValueKind.Number => variable.Value.GetValue<double>().ToString(CultureInfo.CurrentCulture),
                ResultValueKind.DateTime => variable.Value.GetValue<DateTime>().ToString("g", CultureInfo.CurrentCulture),
                ResultValueKind.Point => Geometry(variable.Value, "x", "y"),
                ResultValueKind.Rectangle => Geometry(variable.Value, "x", "y", "width", "height"),
                ResultValueKind.Image => variable.Value.GetValue<string>(),
                _ => variable.Value.ToJsonString()
            };
        }
        catch (Exception exception) when (exception is InvalidOperationException or FormatException)
        {
            return variable.Value.ToJsonString();
        }
    }

    public string Type(ResultValueKind kind, ResultCardinality cardinality) =>
        StepLocalization.ResultValueType(kind, cardinality);

    private static string Geometry(JsonNode value, params string[] properties) =>
        string.Join(", ", properties.Select(property =>
            $"{property}: {value[property]?.ToJsonString() ?? Loc.Get("Ui.ValueReference.EmptyValue")}"));

    private static string Truncate(string value, int length) => value.Length <= length
        ? value
        : string.Concat(value.AsSpan(0, length - 1), "…");
}
