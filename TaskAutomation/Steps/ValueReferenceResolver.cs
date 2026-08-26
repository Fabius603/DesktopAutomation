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
    public static ResolvedValueReference Resolve(IJobResultStore results, ValueReference reference)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(reference);
        RuntimeValueReadResult read;
        if (reference.HasProviderReference)
        {
            read = results.ReadProvider(reference.ProviderId, reference.SourceId);
        }
        else if (reference is ResultBinding binding && binding.TryGetStepResult(out var source))
        {
            var result = results.GetRaw(source.StepId);
            if (result is null || !result.WasExecuted)
                return Failure($"Der Quell-Step '{source.StepId}' wurde noch nicht ausgeführt.");
            if (!StepResultMetadata.TryGetProperty(result.GetType(), source.PropertyId, source.PropertyId, out var property)
                || !StepResultMetadata.TryReadValue(result, property, out var value))
                return Failure($"Die Ergebnis-Eigenschaft '{source.PropertyId}' ist nicht verfügbar.");
            read = new RuntimeValueReadResult(
                RuntimeValueReadStatus.Success,
                new ValueProviderSourceDescriptor(
                    ValueProviderIds.StepResult,
                    StepResultSourceIdCodec.Create(source.StepId, source.PropertyId),
                    property.DisplayName,
                    property.Description,
                    property.DataType,
                    property.Cardinality),
                value);
        }
        else
        {
            return Failure("Die Step-Eingabe enthält keine gültige Referenz.");
        }

        if (!read.IsSuccess)
            return new(read.Status, read.Descriptor, Error: read.Error);
        return ApplyValuePath(read.Value, read.Descriptor, reference.ValuePath);
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
            return Failure($"Die Untereigenschaft '{valuePath}' ist in der ausgewählten Wertquelle nicht verfügbar.", descriptor);

        var property = descriptor is null
            ? null
            : new ResultPropertyDescriptor(
                valuePath,
                valuePath,
                InferKind(selected),
                Cardinality: selected is System.Collections.IEnumerable and not string
                    ? ResultCardinality.Collection
                    : ResultCardinality.Single,
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

    private static ResultValueKind InferKind(object? value) => value switch
    {
        bool => ResultValueKind.Boolean,
        byte or short or int or long => ResultValueKind.Integer,
        float or double or decimal => ResultValueKind.Number,
        DateTime => ResultValueKind.DateTime,
        string => ResultValueKind.Text,
        TaskAutomation.Contracts.Geometry.PixelPoint => ResultValueKind.Point,
        TaskAutomation.Contracts.Geometry.PixelRegion => ResultValueKind.Rectangle,
        DetectionItem => ResultValueKind.Detection,
        RuntimeProcessReference => ResultValueKind.ProcessReference,
        _ => ResultValueKind.ResultObject
    };

    private static ResolvedValueReference Failure(
        string error,
        ValueProviderSourceDescriptor? descriptor = null) =>
        new(RuntimeValueReadStatus.SourceUnavailable, descriptor, Error: error);
}
