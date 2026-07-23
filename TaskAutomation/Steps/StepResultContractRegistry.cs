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

/// <summary>
/// Backend-owned resolver for result contracts. Consumers never infer contracts
/// from UI state; they resolve the fully configured step through this registry.
/// </summary>
public static class StepResultContractRegistry
{
    private static readonly IReadOnlyDictionary<Type, IStepResultContractProvider> DynamicProviders =
        new Dictionary<Type, IStepResultContractProvider>
        {
            [typeof(WindowsStateQueryStep)] = new WindowsQueryStepResultContractProvider()
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
