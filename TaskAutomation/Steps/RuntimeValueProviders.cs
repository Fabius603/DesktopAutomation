using System.Text.Json;
using TaskAutomation.Contracts.Geometry;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public enum RuntimeValueReadStatus
{
    Success,
    ProviderUnavailable,
    SourceUnavailable,
    InvalidValue
}

public sealed record RuntimeValueReadResult(
    RuntimeValueReadStatus Status,
    ValueProviderSourceDescriptor? Descriptor = null,
    object? Value = null,
    string? Error = null)
{
    public bool IsSuccess => Status == RuntimeValueReadStatus.Success;
}

public interface IRuntimeValueProvider
{
    string ProviderId { get; }
    RuntimeValueReadResult Read(string sourceId);
}

internal sealed class RuntimeValueProviderRegistry : IDisposable
{
    private readonly IReadOnlyDictionary<string, IRuntimeValueProvider> _providers;

    public RuntimeValueProviderRegistry(IEnumerable<IRuntimeValueProvider> providers)
    {
        _providers = providers.GroupBy(provider => provider.ProviderId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
    }

    public RuntimeValueReadResult Read(string providerId, string sourceId) =>
        _providers.TryGetValue(providerId, out var provider)
            ? provider.Read(sourceId)
            : new(RuntimeValueReadStatus.ProviderUnavailable,
                Error: $"Der Value-Provider '{providerId}' ist nicht verfügbar.");

    public void Dispose()
    {
        foreach (var provider in _providers.Values.OfType<IDisposable>())
            provider.Dispose();
    }
}

internal sealed class JobVariableRuntimeValueProvider : IRuntimeValueProvider, IDisposable
{
    private readonly IReadOnlyDictionary<Guid, JobVariable> _variables;
    private readonly Dictionary<Guid, object?> _values = [];

    public JobVariableRuntimeValueProvider(IEnumerable<JobVariable> variables)
    {
        _variables = variables.Where(variable => variable.Id != Guid.Empty)
            .GroupBy(variable => variable.Id)
            .ToDictionary(group => group.Key, group => group.Last());
    }

    public string ProviderId => ValueProviderIds.JobVariable;

    public RuntimeValueReadResult Read(string sourceId)
    {
        if (!Guid.TryParse(sourceId, out var id) || !_variables.TryGetValue(id, out var variable))
            return new(RuntimeValueReadStatus.SourceUnavailable,
                Error: "Die ausgewählte Jobvariable ist nicht verfügbar.");
        try
        {
            return new(
                RuntimeValueReadStatus.Success,
                ValueProviderSourceDescriptor.FromVariable(variable),
                ReadValueCached(id, variable));
        }
        catch (Exception exception) when (exception is JsonException
            or InvalidOperationException
            or FormatException
            or ArgumentException
            or System.IO.IOException
            or UnauthorizedAccessException
            or System.Runtime.InteropServices.ExternalException)
        {
            return new(RuntimeValueReadStatus.InvalidValue,
                ValueProviderSourceDescriptor.FromVariable(variable),
                Error: $"Die Jobvariable '{variable.Name}' enthält einen ungültigen Wert.");
        }
    }

    public void Dispose()
    {
        foreach (var value in _values.Values.OfType<IDisposable>())
            value.Dispose();
        _values.Clear();
    }

    private object? ReadValueCached(Guid id, JobVariable variable)
    {
        if (_values.TryGetValue(id, out var value))
            return value;

        value = ReadValue(variable);
        _values[id] = value;
        return value;
    }

    private static object? ReadValue(JobVariable variable) => variable.ValueKind switch
    {
        ResultValueKind.Boolean => variable.Value?.GetValue<bool>(),
        ResultValueKind.Integer => variable.Value?.GetValue<int>(),
        ResultValueKind.Number => variable.Value?.GetValue<double>(),
        ResultValueKind.Text or ResultValueKind.Enum => variable.Value?.GetValue<string>(),
        ResultValueKind.DateTime => variable.Value?.GetValue<DateTime>(),
        ResultValueKind.Point => variable.Cardinality == ResultCardinality.Collection
            ? variable.Value?.Deserialize<PixelPoint[]>()
            : variable.Value?.Deserialize<PixelPoint>(),
        ResultValueKind.Rectangle => variable.Cardinality == ResultCardinality.Collection
            ? variable.Value?.Deserialize<PixelRegion[]>()
            : variable.Value?.Deserialize<PixelRegion>(),
        ResultValueKind.Image => string.IsNullOrWhiteSpace(variable.Value?.GetValue<string>())
            ? null
            : new System.Drawing.Bitmap(variable.Value!.GetValue<string>()),
        ResultValueKind.Detection => variable.Cardinality == ResultCardinality.Collection
            ? variable.Value?.Deserialize<DetectionItem[]>()
            : variable.Value?.Deserialize<DetectionItem>(),
        ResultValueKind.ProcessReference => variable.Cardinality == ResultCardinality.Collection
            ? variable.Value?.Deserialize<RuntimeProcessReference[]>()
            : variable.Value?.Deserialize<RuntimeProcessReference>(),
        ResultValueKind.ResultObject => variable.Value?.DeepClone(),
        _ => variable.Value?.Deserialize<object>()
    };
}

internal sealed class SecretRuntimeValueProvider : IRuntimeValueProvider
{
    private readonly IReadOnlyDictionary<Guid, (ValueProviderSourceDescriptor Descriptor, string Value)> _secrets;

    public SecretRuntimeValueProvider(
        IReadOnlyDictionary<Guid, (ValueProviderSourceDescriptor Descriptor, string Value)> secrets) =>
        _secrets = secrets;

    public string ProviderId => ValueProviderIds.Secret;

    public RuntimeValueReadResult Read(string sourceId)
    {
        if (!Guid.TryParse(sourceId, out var id) || !_secrets.TryGetValue(id, out var secret))
            return new(RuntimeValueReadStatus.SourceUnavailable,
                Error: "Das ausgewählte Secret ist nicht verfügbar.");
        return new(RuntimeValueReadStatus.Success, secret.Descriptor, secret.Value);
    }
}
