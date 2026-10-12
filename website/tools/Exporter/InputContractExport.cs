using TaskAutomation.Contracts.Steps;

/// <summary>Preserves published reference JSON keys without introducing domain contract adapters.</summary>
internal static class InputContractExport
{
    internal static object Root(InputContractNode node) => new
    {
        node.Key, node.Required, node.MissingValuePolicy, node.CollectionConsumption, node.AcceptedShapes,
        node.AllowedProviderIds, node.LegacyAllowedProviderIds, node.AllowsDirectValue, node.LegacyAcceptedShapes
    };

    internal static object Schema(InputContractSchema schema) => new
    {
        schema.Id,
        Members = schema.Members.ToDictionary(pair => pair.Key, pair => (object)new
        {
            ValueKind = pair.Value.TargetKind,
            Cardinality = pair.Value.TargetCardinality,
            NestedSchemaId = pair.Value.SchemaId,
            pair.Value.AllowedProviderIds,
            pair.Value.LegacyAllowedProviderIds,
            pair.Value.AcceptedShapes,
            pair.Value.LegacyAcceptedShapes
        }, StringComparer.Ordinal),
        schema.ItemSchemaId
    };
}
