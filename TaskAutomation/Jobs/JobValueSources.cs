namespace TaskAutomation.Jobs;

/// <summary>Provider-qualified identities for stored values, shared by authoring and execution.</summary>
public static class JobValueSources
{
    public static string Key(string providerId, string sourceId) =>
        providerId + ":" + (Guid.TryParse(sourceId, out var id) ? id.ToString("D") : sourceId);

    public static JobVariable? Find(IEnumerable<JobVariable> values, ValueReference reference) =>
        Guid.TryParse(reference.SourceId, out var id)
            ? values.FirstOrDefault(value => value.Id == id && ProviderFor(value) == reference.ProviderId)
            : null;

    public static string ProviderFor(JobVariable value) => value is LocalValue
        ? ValueProviderIds.LocalValue : ValueProviderIds.JobVariable;

    public static IReadOnlyList<string> ValidateIdentities(Job job)
    {
        var errors = new List<string>();
        var steps = job.EnumerateAllSteps().ToArray();
        if (steps.Any(step => string.IsNullOrWhiteSpace(step.Id))
            || steps.GroupBy(step => step.Id, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            errors.Add("StepValidation.DuplicateStepId");
        foreach (var values in new IEnumerable<JobVariable>[] { job.Variables, job.LocalValues })
            if (values.Any(value => value.Id == Guid.Empty)
                || values.GroupBy(value => value.Id).Any(group => group.Count() > 1))
                errors.Add("StepValidation.DuplicateValueId");
        var usages = ValueReferenceUsageInspector.Find(job);
        foreach (var local in job.LocalValues)
        {
            var used = usages.Where(usage => usage.Reference.ProviderId == ValueProviderIds.LocalValue
                && Guid.TryParse(usage.Reference.SourceId, out var id) && id == local.Id).ToArray();
            if (used.Length == 0) continue;
            if (!string.IsNullOrEmpty(local.OwnerStepId) && used.Any(usage => !string.Equals(usage.Step.Id, local.OwnerStepId, StringComparison.OrdinalIgnoreCase))
                || !string.IsNullOrEmpty(local.InputPath) && !used.Any(usage =>
                    ValueReferenceUsageInspector.NormalizeLogicalPath(usage.Path)
                    == ValueReferenceUsageInspector.NormalizeLogicalPath(local.InputPath)))
                errors.Add("StepValidation.LocalValueOwnership");
        }
        return errors.Distinct(StringComparer.Ordinal).ToArray();
    }
}
