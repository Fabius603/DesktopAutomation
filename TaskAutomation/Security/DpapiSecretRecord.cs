using System.Text.Json.Serialization;

namespace TaskAutomation.Security;

internal sealed class DpapiSecretRecord
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; set; } = DpapiSecretStore.CurrentSchemaVersion;

    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public SecretKind Kind { get; set; }

    [JsonPropertyName("created_at_utc")]
    public DateTime CreatedAtUtc { get; set; }

    [JsonPropertyName("updated_at_utc")]
    public DateTime UpdatedAtUtc { get; set; }

    [JsonPropertyName("protection")]
    public string Protection { get; set; } = DpapiSecretStore.CurrentProtection;

    [JsonPropertyName("protected_value")]
    public string ProtectedValue { get; set; } = string.Empty;

    public SecretDescriptor ToDescriptor() => new(Id, Name, Description, Kind, CreatedAtUtc, UpdatedAtUtc);
}
