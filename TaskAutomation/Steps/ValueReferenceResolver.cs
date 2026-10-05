using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public sealed record ResolvedValueReference(
    RuntimeValueReadStatus Status,
    ValueProviderSourceDescriptor? Descriptor = null,
    object? Value = null,
    string? Error = null)
{
    public bool IsSuccess => Status == RuntimeValueReadStatus.Success;
}

/// <summary>Canonical value-reference resolution shared by validation and execution.</summary>
public static class ValueReferenceResolver
{
    public static ResolvedValueReference Resolve(IJobResultStore results, ValueReference reference,
        ResultTypeDescriptor? resultContract = null)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(reference);
        RuntimeValueReadResult read;
        if (reference is ResultBinding stepBinding && stepBinding.TryGetStepResult(out var stepSource))
        {
            read = ReadStepResult(results.GetRaw, id => resultContract ?? results.GetResultContract(id),
                stepSource, stepBinding.PropertyPath);
        }
        else if (reference.HasProviderReference)
        {
            read = results.ReadProvider(reference.ProviderId, reference.SourceId);
        }
        else
        {
            return Failure("Die Step-Eingabe enthält keine gültige Referenz.");
        }

        if (!read.IsSuccess)
            return new(read.Status, read.Descriptor, Error: read.Error);
        return ApplyValuePath(read.Value, read.Descriptor, reference.ValuePath);
    }

    internal static RuntimeValueReadResult ReadStepResult(Func<string, StepResultBase?> readResult,
        Func<string, ResultTypeDescriptor?> readContract, StepResultSourceId source, string? propertyPath = null)
    {
        var result = readResult(source.StepId);
        if (result?.WasExecuted != true)
            return new(RuntimeValueReadStatus.SourceUnavailable, Error: $"Der Quell-Step '{source.StepId}' wurde noch nicht ausgeführt.");
        var contract = readContract(source.StepId);
        ResultPropertyDescriptor property;
        var found = contract is not null
            ? StepResultMetadata.TryGetProperty(contract, source.PropertyId, propertyPath ?? source.PropertyId, out property!)
            : StepResultMetadata.TryGetProperty(result.GetType(), source.PropertyId, propertyPath ?? source.PropertyId, out property!);
        if (!found && contract is null && ResultBindingResolver.TryReadPath(
                result, propertyPath ?? source.PropertyId, out var legacyValue))
            return new(RuntimeValueReadStatus.Success, new ValueProviderSourceDescriptor(ValueProviderIds.StepResult,
                StepResultSourceIdCodec.Create(source.StepId, source.PropertyId), source.PropertyId, null,
                ResultValueShape.Kind(legacyValue), ResultValueShape.Cardinality(legacyValue)), legacyValue);
        if (!found || !StepResultMetadata.TryReadValue(result, property, out var value))
            return new(RuntimeValueReadStatus.PropertyUnavailable, Error: $"Die Ergebnis-Eigenschaft '{source.PropertyId}' ist nicht verfügbar.");
        return new(RuntimeValueReadStatus.Success, new ValueProviderSourceDescriptor(ValueProviderIds.StepResult,
            StepResultSourceIdCodec.Create(source.StepId, source.PropertyId), property.DisplayName, property.Description,
            property.DataType, property.Cardinality, EnumTypeName: property.EnumTypeName,
            EnumValues: property.EnumValues, EnumDisplayNames: property.EnumDisplayNames), value);
    }

    public static bool TryReadVariable(
        JobVariable variable,
        string? valuePath,
        out JsonNode? value,
        out string? error)
    {
        value = variable.Value?.DeepClone();
        error = null;
        if (string.IsNullOrWhiteSpace(valuePath)) return true;
        if (value is null || !ResultBindingResolver.TryReadPath(value, valuePath, out var selected))
        {
            value = null;
            error = $"Die Untereigenschaft '{valuePath}' ist in der Wertquelle nicht verfügbar.";
            return false;
        }
        value = ToJsonNode(selected);
        return true;
    }

    public static JsonNode? ToJsonNode(object? value) => value switch
    {
        null => null,
        JsonNode node => node.DeepClone(),
        _ => JsonSerializer.SerializeToNode(value, value.GetType())
    };

    private static ResolvedValueReference ApplyValuePath(
        object? value,
        ValueProviderSourceDescriptor? descriptor,
        string? valuePath)
    {
        if (string.IsNullOrWhiteSpace(valuePath))
            return new(RuntimeValueReadStatus.Success, descriptor, value);
        if (value is null || !ResultBindingResolver.TryReadPath(value, valuePath, out var selected))
            return new(RuntimeValueReadStatus.PropertyUnavailable, descriptor, Error: $"Die Untereigenschaft '{valuePath}' ist in der ausgewählten Wertquelle nicht verfügbar.");

        var property = descriptor is null
            ? null
            : new ResultPropertyDescriptor(
                valuePath,
                valuePath,
                ResultValueShape.Kind(selected),
                Cardinality: ResultValueShape.Cardinality(selected),
                Id: ResultContractIds.FromPropertyPath(valuePath));
        var selectedDescriptor = property is null || descriptor is null
            ? descriptor
            : new ValueProviderSourceDescriptor(
                descriptor.ProviderId,
                descriptor.SourceId,
                $"{descriptor.Name} › {property.DisplayName}",
                descriptor.Description,
                property.DataType,
                property.Cardinality,
                descriptor.IsSensitive);
        return new(RuntimeValueReadStatus.Success, selectedDescriptor, selected);
    }

    private static ResolvedValueReference Failure(
        string error,
        ValueProviderSourceDescriptor? descriptor = null) =>
        new(RuntimeValueReadStatus.SourceUnavailable, descriptor, Error: error);
}
