using System.Collections.Frozen;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskAutomation.Contracts.Steps;

public sealed class InputProviderSetConverter : JsonConverter<IReadOnlySet<string>>
{
    public override IReadOnlySet<string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        JsonSerializer.Deserialize<string[]>(ref reader, options)?.ToFrozenSet(StringComparer.Ordinal);

    public override void Write(Utf8JsonWriter writer, IReadOnlySet<string> value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value.Order(StringComparer.Ordinal).ToArray(), options);
}
