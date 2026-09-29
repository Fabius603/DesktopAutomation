using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskAutomation.Makros;

public static class MakroSnapshotService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static MakroBefehl CloneCommand(MakroBefehl command)
        => JsonSerializer.Deserialize<MakroBefehl>(
            JsonSerializer.Serialize(command, Options), Options)
           ?? throw new JsonException("The macro command clone could not be materialized.");

    public static MakroGruppe CloneGroup(MakroGruppe group)
        => new() { Id = group.Id, Title = group.Title, IsAutomatic = group.IsAutomatic };

    public static string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, Options);
}
