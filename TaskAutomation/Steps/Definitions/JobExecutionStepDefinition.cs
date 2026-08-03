using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class JobExecutionStepDefinition : StepDefinition<JobExecutionStep>
{
    public const string JobFieldId = "job";
    public const string WaitForCompletionFieldId = "wait_for_completion";

    public override StepDescriptor Descriptor { get; } = new(
        TypeId: "job_execution",
        CategoryId: "AblaufSteuern",
        DisplayNameKey: "Step.Type.JobExecution",
        DescriptionKey: "Step.Description.JobExecution",
        IconKey: "job-run",
        Fields:
        [
            new StepFieldDescriptor(
                JobFieldId,
                "Ui.Step.Settings.Job",
                StepValueKind.Object,
                Required: true,
                EditorHint: StepEditorHints.JobPicker,
                Order: 0),
            new StepFieldDescriptor(
                WaitForCompletionFieldId,
                "Ui.Step.Settings.WaitForCompletion",
                StepValueKind.Boolean,
                DefaultValue: JsonValue.Create(true),
                Order: 1)
        ],
        Presentation: new StepPresentationDescriptor(
            EditorSections:
            [
                new StepEditorSectionDescriptor(
                    "general",
                    null,
                    [JobFieldId, WaitForCompletionFieldId])
            ],
            SummaryItems:
            [
                new StepSummaryItemDescriptor(JobFieldId),
                new StepSummaryItemDescriptor(WaitForCompletionFieldId, StepSummaryValueFormat.BooleanBadge)
            ],
            DetailFieldIds: [JobFieldId, WaitForCompletionFieldId]));

    public override JobExecutionStep CreateDefaultStep() => new();

    protected override StepDraft Read(JobExecutionStep step)
    {
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[JobFieldId] = StepReferenceDraft.Create(
            step.Settings.JobId,
            step.Settings.JobName);
        draft.Values[WaitForCompletionFieldId] = JsonValue.Create(step.Settings.WaitForCompletion);
        return draft;
    }

    protected override void Apply(StepDraft draft, JobExecutionStep step)
    {
        if (!StepReferenceDraft.TryGetId(draft, JobFieldId, out var id, out var reference))
            throw new InvalidOperationException("The job draft does not contain a valid reference.");
        if (!TryGetBoolean(draft, WaitForCompletionFieldId, out var waitForCompletion))
            throw new InvalidOperationException("The job draft does not contain a valid wait option.");
        step.Settings.JobId = id;
        step.Settings.JobName = reference.Name;
        step.Settings.WaitForCompletion = waitForCompletion;
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft)
    {
        if (!StepReferenceDraft.TryGetId(draft, JobFieldId, out _, out _))
            return [new("StepValidation.Required", JobFieldId)];
        return [];
    }

    private static bool TryGetBoolean(StepDraft draft, string fieldId, out bool result)
    {
        result = true;
        if (!draft.Values.TryGetValue(fieldId, out var value) || value is null)
            return true;
        try
        {
            result = value.GetValue<bool>();
            return true;
        }
        catch (InvalidOperationException) { return false; }
        catch (FormatException) { return false; }
    }
}
