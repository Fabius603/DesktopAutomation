using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public interface IStepResultContractProvider
{
    ResultTypeDescriptor? Resolve(JobStep step);
}

public sealed class FixedStepResultContractProvider(Type resultType) : IStepResultContractProvider
{
    public ResultTypeDescriptor? Resolve(JobStep step) =>
        StepResultMetadata.GetResultType(resultType.Name);
}

public sealed class WindowsQueryStepResultContractProvider : IStepResultContractProvider
{
    public ResultTypeDescriptor? Resolve(JobStep step) =>
        step is WindowsStateQueryStep windows
            ? WindowsQueryResultRegistry.GetContract(windows.Settings.QueryType)
            : null;
}

public sealed class UserChoiceStepResultContractProvider : IStepResultContractProvider
{
    public ResultTypeDescriptor? Resolve(JobStep step)
    {
        if (step is not UserChoiceStep choice) return null;
        var contract = StepResultMetadata.GetResultType(nameof(UserChoiceResult));
        if (contract is null) return null;

        var displayNames = choice.Settings.Options
            .Where(option => !string.IsNullOrWhiteSpace(option.Id))
            .GroupBy(option => option.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Label, StringComparer.Ordinal);
        var ids = displayNames.Keys.ToArray();
        var properties = contract.Properties.Select(property =>
            property.StableId == "selected_option_id"
                ? property with
                {
                    DataType = ResultValueKind.Enum,
                    EnumTypeName = UserChoiceEnumContract.ForStep(choice.Id),
                    EnumValues = ids,
                    EnumDisplayNames = displayNames
                }
                : property).ToArray();
        return new ResultTypeDescriptor(contract.TypeName, contract.DisplayName, properties);
    }
}

internal static class UserChoiceEnumContract
{
    public const string LegacyTypeName = nameof(UserChoiceResult);

    public static string ForStep(string stepId) => $"{LegacyTypeName}:{stepId}";

    public static bool IsStepSpecific(string? typeName) =>
        typeName?.StartsWith($"{LegacyTypeName}:", StringComparison.Ordinal) == true;
}

/// <summary>
/// Backend-owned resolver for result contracts. Consumers never infer contracts
/// from UI state; they resolve the fully configured step through this registry.
/// </summary>
public static class StepResultContractRegistry
{
    /// <summary>Rebuilds derived settings used by dynamic contracts from persisted values.</summary>
    internal static void RestoreResultContractSettings(Job job)
    {
        var values = (job.Variables ?? []).Cast<JobVariable>().Concat(job.LocalValues ?? []).ToArray();
        foreach (var step in job.EnumerateAllSteps())
        {
            if (!DynamicProviders.ContainsKey(step.GetType()) || step.Inputs is not { Count: > 0 }
                || !Definitions.BuiltInStepDefinitions.Instance.TryGetByType(step.GetType(), out var definition))
                continue;
            var draft = definition.CreateDraft(step);
            var contractFields = new HashSet<string>(StringComparer.Ordinal)
            {
                step is UserChoiceStep
                    ? Definitions.UserChoiceStepDefinition.OptionsFieldId
                    : Definitions.WindowsStateQueryStepDefinition.CapabilityFieldId
            };
            var overlay = JobValidation.OverlayKnownInputValues(step, definition, draft, values, contractFields);
            if (overlay.Error is null)
            {
                try { definition.ApplyDraft(draft, step); }
                catch (InvalidOperationException) { /* Preserve malformed persisted inputs for explicit repair. */ }
                catch (System.Text.Json.JsonException) { /* Draft validation reports the malformed contract value. */ }
            }
        }
    }

    private static readonly IReadOnlyDictionary<Type, IStepResultContractProvider> DynamicProviders =
        new Dictionary<Type, IStepResultContractProvider>
        {
            [typeof(WindowsStateQueryStep)] = new WindowsQueryStepResultContractProvider(),
            [typeof(UserChoiceStep)] = new UserChoiceStepResultContractProvider()
        };

    static StepResultContractRegistry()
    {
        foreach (var contract in StepResultMetadata.ResultTypes)
        {
            var duplicate = contract.Properties
                .GroupBy(property => property.StableId, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate is not null)
                throw new InvalidOperationException(
                    $"Result contract {contract.TypeName} contains duplicate property ID '{duplicate.Key}'.");
        }
    }

    public static ResultTypeDescriptor? Resolve(JobStep step)
    {
        if (DynamicProviders.TryGetValue(step.GetType(), out var provider))
            return provider.Resolve(step);

        var resultType = StepPipelineRegistry.Get(step.GetType())?.ResultType;
        return resultType is null ? null : StepResultMetadata.GetResultType(resultType.Name);
    }
}
