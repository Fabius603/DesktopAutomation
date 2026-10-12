using TaskAutomation.Contracts.Steps;
using TaskAutomation.Steps;

namespace TaskAutomation.Jobs;

/// <summary>
/// Stores compound input bindings as a tree while keeping legacy dotted input keys readable.
/// Every node may have a provider reference as its base value and structured child overrides.
/// </summary>
public static class ValueBindingTree
{
    public static ResultBinding? Find(
        IReadOnlyDictionary<string, ResultBinding>? inputs,
        string path)
    {
        if (inputs is null || string.IsNullOrWhiteSpace(path)) return null;
        if (inputs.TryGetValue(path, out var legacy)) return legacy;
        var segments = Split(path);
        if (segments.Length == 0 || !inputs.TryGetValue(segments[0], out var current)) return null;
        for (var index = 1; index < segments.Length; index++)
        {
            if (int.TryParse(segments[index], out var itemIndex))
            {
                if (current.Items is null || itemIndex < 0 || itemIndex >= current.Items.Count) return null;
                current = current.Items[itemIndex];
            }
            else
            {
                if (current.Members is null || !current.Members.TryGetValue(segments[index], out var member))
                    return null;
                current = member;
            }
        }
        return current;
    }

    public static void Set(
        IDictionary<string, ResultBinding> inputs,
        string path,
        ResultBinding binding)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(binding);
        var segments = Split(path);
        if (segments.Length == 0) throw new ArgumentException("A binding path is required.", nameof(path));
        if (segments.Length == 1)
        {
            inputs[segments[0]] = binding;
            return;
        }

        if (!inputs.TryGetValue(segments[0], out var root))
            inputs[segments[0]] = root = new ResultBinding();
        Set(root, segments, 1, binding);
    }

    public static bool Normalize(
        IDictionary<string, ResultBinding> inputs,
        IReadOnlyList<StepFieldDescriptor> fields)
    {
        var nested = inputs.Where(candidate => candidate.Key.Contains('.')).ToArray();
        if (nested.Length == 0)
        {
            ApplySchemas(inputs, fields);
            return false;
        }
        foreach (var (path, binding) in nested)
        {
            Set(inputs, path, binding);
            inputs.Remove(path);
        }
        ApplySchemas(inputs, fields);
        return true;
    }

    public static void ApplySchemas(
        IDictionary<string, ResultBinding> inputs,
        IReadOnlyList<StepFieldDescriptor> fields)
    {
        foreach (var field in fields)
        {
            if (!inputs.TryGetValue(field.Id, out var root)) continue;
            var schemaId = InputContractRegistry.SchemaForField(field);
            if (schemaId is null) continue;
            ApplySchema(root, schemaId);
        }
    }

    public static IEnumerable<(string Path, ResultBinding Binding)> EnumerateReferences(
        IReadOnlyDictionary<string, ResultBinding> inputs)
    {
        foreach (var (key, binding) in inputs)
            foreach (var item in EnumerateReferences(binding, key))
                yield return item;
    }

    private static IEnumerable<(string Path, ResultBinding Binding)> EnumerateReferences(
        ResultBinding binding,
        string path)
    {
        if (binding.HasProviderReference || binding.TryGetStepResult(out _)) yield return (path, binding);
        if (binding.Members is not null)
            foreach (var (member, child) in binding.Members)
                foreach (var item in EnumerateReferences(child, $"{path}.{member}"))
                    yield return item;
        if (binding.Items is not null)
            for (var index = 0; index < binding.Items.Count; index++)
                foreach (var item in EnumerateReferences(binding.Items[index], $"{path}.{index}"))
                    yield return item;
    }

    private static void Set(ResultBinding current, string[] segments, int index, ResultBinding binding)
    {
        if (int.TryParse(segments[index], out var itemIndex))
        {
            if (itemIndex < 0) throw new ArgumentOutOfRangeException(nameof(segments));
            current.Items ??= [];
            while (current.Items.Count <= itemIndex) current.Items.Add(new ResultBinding());
            if (index == segments.Length - 1) current.Items[itemIndex] = binding;
            else Set(current.Items[itemIndex], segments, index + 1, binding);
            return;
        }

        current.Members ??= new Dictionary<string, ResultBinding>(StringComparer.Ordinal);
        if (index == segments.Length - 1)
        {
            current.Members[segments[index]] = binding;
            return;
        }
        if (!current.Members.TryGetValue(segments[index], out var child))
            current.Members[segments[index]] = child = new ResultBinding();
        Set(child, segments, index + 1, binding);
    }

    private static void ApplySchema(ResultBinding binding, string schemaId)
    {
        if (!string.IsNullOrEmpty(binding.SchemaId) && binding.SchemaId != schemaId) return;
        binding.SchemaId = schemaId;
        if (!InputContractRegistry.TryGetSchema(schemaId, out var schema)) return;
        if (binding.Members is not null)
            foreach (var (memberId, child) in binding.Members)
                if (schema.Members.GetValueOrDefault(memberId)?.SchemaId is { } nested)
                    ApplySchema(child, nested);
        if (binding.Items is not null && schema.ItemSchemaId is { } itemSchema)
            foreach (var item in binding.Items) ApplySchema(item, itemSchema);
    }

    private static string[] Split(string path) =>
        path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

