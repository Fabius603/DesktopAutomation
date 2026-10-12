using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public sealed record InputBindingContractEntry(string Path, ResultBinding Binding, InputContractNode Contract);

/// <summary>One structural contract traversal for validation and runtime composition.</summary>
public static class InputBindingContractTraversal
{
    public static IEnumerable<InputBindingContractEntry> Walk(ResultBinding binding, InputContractNode contract, string path)
    {
        if (contract.SchemaId is { } expected && binding.SchemaId is { Length: > 0 } actual && actual != expected)
            throw Invalid(path);
        yield return new(path, binding, contract);
        if (!binding.HasStructuredChildren) yield break;
        if (contract.SchemaId is null || !InputContractRegistry.TryGetSchema(contract.SchemaId, out var schema))
            throw Invalid(path);
        foreach (var (name, child) in binding.Members ?? [])
        {
            if (!schema.Members.TryGetValue(name, out var member)) throw Invalid(path + "." + name);
            if (!child.IsConfigured) continue;
            foreach (var entry in Walk(child, member, path + "." + name)) yield return entry;
        }
        if (binding.Items is null) yield break;
        if (schema.ItemSchemaId is null) throw Invalid(path);
        var item = InputContractRegistry.Item(schema.ItemSchemaId);
        for (var index = 0; index < binding.Items.Count; index++)
            if (binding.Items[index].IsConfigured)
                foreach (var entry in Walk(binding.Items[index], item, $"{path}.{index}")) yield return entry;
    }

    private static InvalidOperationException Invalid(string path) => new($"{path}: StepValidation.Invalid");
}
