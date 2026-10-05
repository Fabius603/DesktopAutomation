using System.Text.Json;
using System.Text.Json.Nodes;

namespace TaskAutomation.Jobs;

/// <summary>Non-mutating stored-value shape checks and explicit defaults for newly created values.</summary>
public static class JobVariableValueRules
{
    public static JsonNode? CreateDefault(ResultValueKind kind, TimeProvider? clock = null) => kind switch
    {
        ResultValueKind.Text or ResultValueKind.Enum or ResultValueKind.FilePath or ResultValueKind.Image => JsonValue.Create(string.Empty),
        ResultValueKind.Boolean => JsonValue.Create(false),
        ResultValueKind.Integer => JsonValue.Create(0),
        ResultValueKind.Number => JsonValue.Create(0d),
        ResultValueKind.DateTime => JsonValue.Create((clock ?? TimeProvider.System).GetLocalNow().DateTime),
        ResultValueKind.Color => JsonValue.Create("#FFFFFF"),
        ResultValueKind.Point => new JsonObject { ["x"] = 0, ["y"] = 0 },
        ResultValueKind.Rectangle => new JsonObject { ["x"] = 0, ["y"] = 0, ["width"] = 0, ["height"] = 0 },
        ResultValueKind.ResultObject or ResultValueKind.Detection or ResultValueKind.ProcessReference => new JsonObject(),
        _ => null
    };

    public static bool IsValid(JobVariable variable)
    {
        if (!Enum.IsDefined(variable.ValueKind) || !Enum.IsDefined(variable.Cardinality)) return false;
        if (variable.Value is null) return variable.Cardinality == ResultCardinality.OptionalSingle;
        if (variable.Cardinality == ResultCardinality.Collection)
            return variable.Value is JsonArray array && array.All(item => IsScalarValid(variable, item));
        return IsScalarValid(variable, variable.Value);
    }

    private static bool IsScalarValid(JobVariable variable, JsonNode? node)
    {
        if (node is null) return false;
        if (variable.ValueKind is ResultValueKind.ResultObject or ResultValueKind.JobReference or ResultValueKind.MacroReference)
            return variable.ValueKind == ResultValueKind.ResultObject || node is JsonObject;
        if (variable.ValueKind is ResultValueKind.Point or ResultValueKind.Rectangle)
            return node is JsonObject obj && Integer(obj["x"]) && Integer(obj["y"])
                && (variable.ValueKind != ResultValueKind.Rectangle || Integer(obj["width"]) && Integer(obj["height"]));
        if (variable.ValueKind is ResultValueKind.Detection or ResultValueKind.ProcessReference) return node is JsonObject;
        if (node is not JsonValue value) return false;
        try
        {
            return variable.ValueKind switch
            {
                ResultValueKind.Boolean => value.TryGetValue<bool>(out _),
                ResultValueKind.Integer => Integer(value),
                ResultValueKind.Number => value.TryGetValue<double>(out var number) && double.IsFinite(number)
                    || value.TryGetValue<decimal>(out _) || Integer(value),
                ResultValueKind.Color => value.TryGetValue<string>(out var color) && TaskAutomation.Steps.ColorValueRules.TryParse(color, out _),
                ResultValueKind.DateTime => value.TryGetValue<DateTime>(out _),
                ResultValueKind.Enum => value.TryGetValue<string>(out var token)
                    && (variable.EnumValues is not { Count: > 0 } options || options.Contains(token, StringComparer.Ordinal)),
                _ => value.TryGetValue<string>(out _)
            };
        }
        catch (InvalidOperationException) { return false; }
        catch (FormatException) { return false; }
    }

    private static bool Integer(JsonNode? node) => node is JsonValue value
        && (value.TryGetValue<int>(out _) || value.TryGetValue<long>(out var number) && number is >= int.MinValue and <= int.MaxValue);
}
