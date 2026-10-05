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
    IReadOnlyList<StepValidationIssue> ValidateDraft(StepDraft draft, StepValidationContext context);
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

    public IReadOnlyList<StepValidationIssue> ValidateDraft(StepDraft draft) =>
        ValidateDraft(draft, StepValidationContext.FullyResolved());

    public IReadOnlyList<StepValidationIssue> ValidateDraft(StepDraft draft, StepValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var descriptorIssues = StepDescriptorDraftValidator.Validate(Descriptor, draft, context);
        var fieldsWithDescriptorErrors = descriptorIssues
            .Where(issue => issue.Severity == StepValidationSeverity.Error && issue.FieldId is not null)
            .Select(issue => issue.FieldId!)
            .ToHashSet(StringComparer.Ordinal);
        var customIssues = ValidateCustomDraft(draft)
            .Where(context.CanEvaluate)
            .Where(issue => issue.FieldId is null || !fieldsWithDescriptorErrors.Contains(issue.FieldId));
        return descriptorIssues.Concat(customIssues).ToArray();
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
        var effective = draft.Clone();
        var active = StepActiveFieldResolver.GetActiveFieldIds(this, effective);
        var defaults = Read(CreateDefaultStep());
        foreach (var field in Descriptor.Fields.Where(field => !active.Contains(field.Id)))
        {
            var alwaysVisible = field with { VisibleWhen = null, VisibleWhenAll = null };
            var descriptor = Descriptor with { Fields = [alwaysVisible], Presentation = new TaskAutomation.Contracts.Steps.StepPresentationDescriptor([], [], []) };
            if (StepDescriptorDraftValidator.Validate(descriptor, effective,
                    StepValidationContext.FullyResolved(StepValidationPhase.Runtime))
                .Any(issue => issue.Severity == StepValidationSeverity.Error))
                effective.Values[field.Id] = defaults.Values.GetValueOrDefault(field.Id)?.DeepClone();
        }
        Apply(effective, step);
        return step;
    }
}
