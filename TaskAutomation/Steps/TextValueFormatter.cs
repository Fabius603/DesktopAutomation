using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

/// <summary>Shared source policy and conversion for inputs that consume display text.</summary>
public static class TextValueFormatter
{
    public static IReadOnlyList<AcceptedResultShape> AcceptedShapes { get; } =
        Enum.GetValues<ResultValueKind>().Where(kind => kind != ResultValueKind.Image)
            .OrderBy(kind => kind == ResultValueKind.Text ? 0 : 1)
            .Select(kind => new AcceptedResultShape(kind, ResultCardinality.Single,
                ResultCardinality.OptionalSingle, ResultCardinality.Collection)).ToArray();

    public static bool Accepts(ResultPropertyDescriptor property) => AcceptedShapes.Any(shape => shape.Accepts(property));

    public static string Format(object? value) => value switch
    {
        null => string.Empty,
        string text => text,
        JsonArray array => string.Join(Environment.NewLine, array.Select(item => Format(item))),
        JsonObject obj => obj.ToJsonString(),
        JsonValue scalar => FormatScalar(scalar),
        JsonElement element => Format(JsonNode.Parse(element.GetRawText())),
        IDictionary dictionary => JsonSerializer.Serialize(dictionary, dictionary.GetType()),
        IEnumerable items => string.Join(Environment.NewLine, items.Cast<object?>().Select(Format)),
        _ => ResultDisplayFormatter.Format(value)
    };

    private static string FormatScalar(JsonValue value)
    {
        if (value.TryGetValue<string>(out var text)) return text;
        if (value.TryGetValue<bool>(out var boolean)) return boolean.ToString();
        if (value.TryGetValue<decimal>(out var number)) return number.ToString(CultureInfo.CurrentCulture);
        if (value.TryGetValue<double>(out var real)) return real.ToString(CultureInfo.CurrentCulture);
        if (value.TryGetValue<float>(out var single)) return single.ToString(CultureInfo.CurrentCulture);
        if (value.TryGetValue<DateTime>(out var date)) return date.ToString(CultureInfo.CurrentCulture);
        return value.ToJsonString();
    }
}
