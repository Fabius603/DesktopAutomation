using System.Globalization;
using System.Collections;
using System.Text.Json.Nodes;
using DesktopAutomationApp.Localization;
using TaskAutomation.Contracts.Geometry;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace DesktopAutomationApp.Services.Jobs;

public interface IValueReferenceDisplayFormatter
{
    string CompactValue(JobVariable variable);
    string FullValue(JobVariable variable);
    string CompactValue(object? value, ResultValueKind kind, ResultCardinality cardinality);
    string FullValue(object? value, ResultValueKind kind, ResultCardinality cardinality);
    string Type(ResultValueKind kind, ResultCardinality cardinality);
}

public sealed class ValueReferenceDisplayFormatter : IValueReferenceDisplayFormatter
{
    public static ValueReferenceDisplayFormatter Instance { get; } = new();

    public string CompactValue(JobVariable variable) => Truncate(FullValue(variable), 48);

    public string CompactValue(object? value, ResultValueKind kind, ResultCardinality cardinality) =>
        Truncate(FullValue(value, kind, cardinality), 48);

    public string FullValue(JobVariable variable)
    {
        if (variable.Value is null) return Loc.Get("Ui.ValueReference.EmptyValue");
        try
        {
            return variable.ValueKind switch
            {
                ResultValueKind.Text => Quote(variable.Value.GetValue<string>()),
                ResultValueKind.Enum or ResultValueKind.Color or ResultValueKind.FilePath => variable.Value.GetValue<string>(),
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

    public string FullValue(object? value, ResultValueKind kind, ResultCardinality cardinality)
    {
        if (value is null) return Loc.Get("Ui.ValueReference.EmptyValue");
        if (cardinality == ResultCardinality.Collection && value is IEnumerable values and not string)
        {
            var formatted = values.Cast<object?>()
                .Select(item => FullValue(item, kind, ResultCardinality.Single))
                .Take(4)
                .ToArray();
            return formatted.Length == 0
                ? Loc.Get("Ui.ValueReference.EmptyValue")
                : string.Join(", ", formatted);
        }

        if (kind == ResultValueKind.Text && value is string text) return Quote(text);

        return value switch
        {
            bool boolean => boolean ? Loc.Get("Ui.Common.Yes") : Loc.Get("Ui.Common.No"),
            DateTime dateTime => dateTime.ToString("g", CultureInfo.CurrentCulture),
            PixelPoint point => $"x: {point.X.ToString(CultureInfo.CurrentCulture)}, y: {point.Y.ToString(CultureInfo.CurrentCulture)}",
            PixelRegion rectangle =>
                $"x: {rectangle.X.ToString(CultureInfo.CurrentCulture)}, y: {rectangle.Y.ToString(CultureInfo.CurrentCulture)}, " +
                $"width: {rectangle.Width.ToString(CultureInfo.CurrentCulture)}, height: {rectangle.Height.ToString(CultureInfo.CurrentCulture)}",
            JsonNode node => node.ToJsonString(),
            IFormattable formattable => formattable.ToString(null, CultureInfo.CurrentCulture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string Geometry(JsonNode value, params string[] properties) =>
        string.Join(", ", properties.Select(property =>
            $"{property}: {value[property]?.ToJsonString() ?? Loc.Get("Ui.ValueReference.EmptyValue")}"));

    private static string Quote(string value) => $"“{value}”";

    private static string Truncate(string value, int length) => value.Length <= length
        ? value
        : string.Concat(value.AsSpan(0, length - 1), "…");
}
