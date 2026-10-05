using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TaskAutomation.Jobs;

public sealed record ValueReferenceUsage(JobStep Step, ValueReference Reference, string Path)
{
    internal Action? WriteBack { get; init; }

    public void UpdateReference(Action<ValueReference> update)
    {
        update(Reference);
        WriteBack?.Invoke();
    }
}

public static class ValueReferenceUsageInspector
{
    public static IReadOnlyList<ValueReferenceUsage> Find(Job job)
    {
        var result = new List<ValueReferenceUsage>();
        foreach (var step in job.EnumerateAllSteps())
        {
            var direct = Find(step).Select(item => new ValueReferenceUsage(step, item.Reference, item.Path)).ToArray();
            result.AddRange(direct);
            foreach (var usage in direct)
                FollowStoredValue(usage, new HashSet<(string Provider, Guid Id)>());
        }
        return result;

        void FollowStoredValue(ValueReferenceUsage usage, HashSet<(string Provider, Guid Id)> ancestors)
        {
            if (!Guid.TryParse(usage.Reference.SourceId, out var id)
                || usage.Reference.ProviderId is not (ValueProviderIds.LocalValue or ValueProviderIds.JobVariable)
                || !ancestors.Add((usage.Reference.ProviderId, id))) return;
            var value = JobValueSources.Find(job.Variables.Cast<JobVariable>().Concat(job.LocalValues), usage.Reference);
            VisitJson(value?.Value, usage.Step, usage.Path, ancestors, true);
            ancestors.Remove((usage.Reference.ProviderId, id));
        }

        void VisitJson(JsonNode? node, JobStep step, string path,
            HashSet<(string Provider, Guid Id)> ancestors, bool root = false)
        {
            if (node is JsonArray array)
            {
                for (var index = 0; index < array.Count; index++)
                    VisitJson(array[index], step, $"{path}[{index}]", ancestors);
                return;
            }
            if (node is not JsonObject obj) return;
            if (obj.ContainsKey("provider_id") && obj.ContainsKey("source_id")
                || obj.ContainsKey("source_step_id"))
            {
                ResultBinding? binding;
                try { binding = obj.Deserialize<ResultBinding>(); }
                catch (JsonException) { binding = null; }
                if (binding is not null && (binding.HasProviderReference || binding.TryGetStepResult(out _)))
                {
                    var usage = new ValueReferenceUsage(step, binding, path)
                    {
                        WriteBack = () =>
                        {
                            var serialized = JsonSerializer.SerializeToNode(binding)!.AsObject();
                            foreach (var key in new[] { "provider_id", "source_id", "value_path", "source_step_id", "property_id", "property_path" })
                                if (serialized.TryGetPropertyValue(key, out var value)) obj[key] = value?.DeepClone();
                                else obj.Remove(key);
                        }
                    };
                    result.Add(usage);
                    FollowStoredValue(usage, ancestors);
                }
            }
            foreach (var (key, child) in obj)
            {
                var childPath = root && NormalizeLogicalPath(path).EndsWith(
                    NormalizeLogicalPath(key), StringComparison.Ordinal)
                    ? path : $"{path}.{key}";
                VisitJson(child, step, childPath, ancestors);
            }
        }
    }

    public static IReadOnlyList<ValueReferenceUsage> Find(
        Job job,
        string providerId,
        string sourceId) => Find(job)
        .Where(usage => string.Equals(usage.Reference.ProviderId, providerId, StringComparison.Ordinal)
                        && string.Equals(usage.Reference.SourceId, sourceId, StringComparison.OrdinalIgnoreCase))
        .ToArray();

    public static IReadOnlyList<ValueReferenceUsage> FindLogical(
        Job job,
        string providerId,
        string sourceId) => FindLogical(job, [providerId], sourceId);

    public static IReadOnlyList<ValueReferenceUsage> FindLogical(
        Job job,
        IReadOnlyCollection<string> providerIds,
        string sourceId)
    {
        var acceptedProviders = providerIds.ToHashSet(StringComparer.Ordinal);
        return Find(job)
            .Where(usage => acceptedProviders.Contains(usage.Reference.ProviderId)
                            && string.Equals(
                                usage.Reference.SourceId, sourceId, StringComparison.OrdinalIgnoreCase))
            .GroupBy(usage => (usage.Step.Id, Path: NormalizeLogicalPath(usage.Path)))
            .Select(group => group
                .OrderByDescending(usage => usage.Path.Contains(".Inputs[", StringComparison.Ordinal))
                .First())
            .ToArray();
    }

    public static IReadOnlyDictionary<string, int> CountLogicalByIdentity(Job job) => Find(job)
        .GroupBy(usage => (Key: JobValueSources.Key(usage.Reference.ProviderId, usage.Reference.SourceId),
            usage.Step.Id, Path: NormalizeLogicalPath(usage.Path)))
        .GroupBy(group => group.Key.Key, StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, int> CountLogicalBySource(
        Job job,
        IReadOnlyCollection<string> providerIds)
    {
        var acceptedProviders = providerIds.ToHashSet(StringComparer.Ordinal);
        return Find(job)
            .Where(usage => acceptedProviders.Contains(usage.Reference.ProviderId)
                            && !string.IsNullOrWhiteSpace(usage.Reference.SourceId))
            .GroupBy(usage => (
                SourceId: usage.Reference.SourceId,
                usage.Step.Id,
                Path: NormalizeLogicalPath(usage.Path)),
                new LogicalUsageKeyComparer())
            .Select(group => group.Key.SourceId)
            .GroupBy(sourceId => sourceId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
    }

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

    public static string NormalizeLogicalPath(string path)
    {
        var inputIndex = path.IndexOf(".Inputs[", StringComparison.Ordinal);
        var settingsIndex = path.IndexOf(".Settings.", StringComparison.Ordinal);
        var logicalPath = inputIndex >= 0
            ? path[(inputIndex + ".Inputs[".Length)..]
            : settingsIndex >= 0
                ? path[(settingsIndex + ".Settings.".Length)..]
                : path;
        logicalPath = logicalPath.Replace(".Items[", "[", StringComparison.Ordinal)
            .Replace(".Members[", "[", StringComparison.Ordinal);
        return new string(logicalPath
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    private sealed class LogicalUsageKeyComparer : IEqualityComparer<(string SourceId, string Id, string Path)>
    {
        public bool Equals(
            (string SourceId, string Id, string Path) left,
            (string SourceId, string Id, string Path) right) =>
            string.Equals(left.SourceId, right.SourceId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.Id, right.Id, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.Path, right.Path, StringComparison.Ordinal);

        public int GetHashCode((string SourceId, string Id, string Path) value) => HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(value.SourceId),
            StringComparer.OrdinalIgnoreCase.GetHashCode(value.Id),
            StringComparer.Ordinal.GetHashCode(value.Path));
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

        if (value is ValueReference reference && (reference.HasProviderReference
            || reference is ResultBinding binding && binding.TryGetStepResult(out _)))
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
