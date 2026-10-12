using System.Collections.Frozen;

namespace TaskAutomation.Contracts.Steps;

/// <summary>A shared schema defines member and item nodes once, independently of any editor.</summary>
public sealed record InputContractSchema
{
    public InputContractSchema(string id, IReadOnlyDictionary<string, InputContractNode> members,
        string? itemSchemaId = null)
    {
        Id = id;
        Members = members.ToFrozenDictionary(StringComparer.Ordinal);
        ItemSchemaId = itemSchemaId;
    }
    public string Id { get; }
    public IReadOnlyDictionary<string, InputContractNode> Members { get; }
    public string? ItemSchemaId { get; }
}
