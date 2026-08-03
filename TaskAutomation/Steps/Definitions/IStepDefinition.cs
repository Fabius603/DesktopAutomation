using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public interface IStepDefinition
{
    Type StepType { get; }
    StepDescriptor Descriptor { get; }

    JobStep CreateDefault();
    StepDraft CreateDraft(JobStep? step = null);
    JobStep ApplyDraft(StepDraft draft, JobStep? existingStep = null);
    IReadOnlyList<StepValidationIssue> ValidateDraft(StepDraft draft);
    IReadOnlyList<StepInputBinding> GetInputBindings(JobStep step);
}

public abstract class StepDefinition<TStep> : IStepDefinition where TStep : JobStep
{
    public Type StepType => typeof(TStep);
    public abstract StepDescriptor Descriptor { get; }

    public abstract TStep CreateDefaultStep();
    protected abstract StepDraft Read(TStep step);
    protected abstract void Apply(StepDraft draft, TStep step);
    protected abstract IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft);

    public IReadOnlyList<StepValidationIssue> ValidateDraft(StepDraft draft)
    {
        var descriptorIssues = StepDescriptorDraftValidator.Validate(Descriptor, draft);
        return descriptorIssues.Count > 0 ? descriptorIssues : ValidateCustomDraft(draft);
    }

    public IReadOnlyList<StepInputBinding> GetInputBindings(JobStep step) =>
        step is TStep typed
            ? StepInputBindingReader.Read(Descriptor, Read(typed))
            : throw new ArgumentException(
                $"Expected {typeof(TStep).Name}, got {step.GetType().Name}.", nameof(step));

    public JobStep CreateDefault() => CreateDefaultStep();

    public StepDraft CreateDraft(JobStep? step = null) => step switch
    {
        null => Read(CreateDefaultStep()),
        TStep typed => Read(typed),
        _ => throw new ArgumentException(
            $"Expected {typeof(TStep).Name}, got {step.GetType().Name}.", nameof(step))
    };

    public JobStep ApplyDraft(StepDraft draft, JobStep? existingStep = null)
    {
        if (!string.Equals(draft.TypeId, Descriptor.TypeId, StringComparison.Ordinal))
            throw new ArgumentException(
                $"Expected draft type '{Descriptor.TypeId}', got '{draft.TypeId}'.", nameof(draft));

        var step = existingStep switch
        {
            null => CreateDefaultStep(),
            TStep typed => typed,
            _ => throw new ArgumentException(
                $"Expected {typeof(TStep).Name}, got {existingStep.GetType().Name}.", nameof(existingStep))
        };
        Apply(draft, step);
        return step;
    }
}
