using System.Collections;
using System.Reflection;

namespace TaskAutomation.Jobs;

public sealed record ValueReferenceUsage(JobStep Step, ValueReference Reference, string Path);

public static class ValueReferenceUsageInspector
{
    public static IReadOnlyList<ValueReferenceUsage> Find(Job job) =>
        job.EnumerateAllSteps()
            .SelectMany(step => Find(step).Select(item => new ValueReferenceUsage(step, item.Reference, item.Path)))
            .ToArray();

    public static IReadOnlyList<ValueReferenceUsage> Find(
        Job job,
        string providerId,
        string sourceId) => Find(job)
        .Where(usage => string.Equals(usage.Reference.ProviderId, providerId, StringComparison.Ordinal)
                        && string.Equals(usage.Reference.SourceId, sourceId, StringComparison.OrdinalIgnoreCase))
        .ToArray();

    public static int Count(
        IEnumerable<JobStep> steps,
        string providerId,
        string sourceId) => steps.Sum(step => Find(step).Count(item =>
            string.Equals(item.Reference.ProviderId, providerId, StringComparison.Ordinal)
            && string.Equals(item.Reference.SourceId, sourceId, StringComparison.OrdinalIgnoreCase)));

    private static IReadOnlyList<(ValueReference Reference, string Path)> Find(JobStep step)
    {
        var result = new List<(ValueReference, string)>();
        var visited = new HashSet<object>(ReferenceEqualityComparer.Instance);
        Visit(step, step.GetType().Name, result, visited);
        return result;
    }

    private static void Visit(
        object? value,
        string path,
        ICollection<(ValueReference Reference, string Path)> result,
        ISet<object> visited)
    {
        if (value is null || value is string || value.GetType().IsPrimitive || value.GetType().IsEnum)
            return;
        if (!value.GetType().IsValueType && !visited.Add(value)) return;

        if (value is ValueReference reference && reference.HasProviderReference)
            result.Add((reference, path));

        if (value is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
                Visit(entry.Value, $"{path}[{entry.Key}]", result, visited);
            return;
        }

        if (value is IEnumerable items)
        {
            var index = 0;
            foreach (var item in items) Visit(item, $"{path}[{index++}]", result, visited);
            return;
        }

        var type = value.GetType();
        if (type.Namespace?.StartsWith("System", StringComparison.Ordinal) == true) return;
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                     .Where(property => property.CanRead && property.GetIndexParameters().Length == 0))
        {
            object? child;
            try { child = property.GetValue(value); }
            catch (TargetInvocationException) { continue; }
            Visit(child, $"{path}.{property.Name}", result, visited);
        }
    }
}
