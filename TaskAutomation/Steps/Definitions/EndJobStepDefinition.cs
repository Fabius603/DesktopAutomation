using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class EndJobStepDefinition : StepDefinition<EndJobStep>
{
    public const string SkipEndStepsFieldId = "skip_end_steps";

    public override StepDescriptor Descriptor { get; } = new(
        TypeId: "end_job",
        CategoryId: "AblaufSteuern",
        DisplayNameKey: "Step.Type.EndJob",
        DescriptionKey: "Step.Description.EndJob",
        IconKey: "stop-circle-outline",
        Fields:
        [
            new StepFieldDescriptor(
                Id: SkipEndStepsFieldId,
                LabelKey: "Ui.Step.Settings.SkipEndSteps",
                ValueKind: StepValueKind.Boolean,
                DefaultValue: JsonValue.Create(false),
                DescriptionKey: "Ui.Step.Settings.SkipEndStepsHint")
        ],
        Presentation: new StepPresentationDescriptor(
            EditorSections:
            [
                new StepEditorSectionDescriptor("general", null, [SkipEndStepsFieldId])
            ],
            SummaryItems:
            [
                new StepSummaryItemDescriptor(
                    SkipEndStepsFieldId,
                    StepSummaryValueFormat.BooleanBadge)
            ],
            DetailFieldIds: [SkipEndStepsFieldId],
            EditorDescriptionKey: "Ui.Step.Settings.EndJobDescription"));

    public override EndJobStep CreateDefaultStep() => new();

    protected override StepDraft Read(EndJobStep step)
    {
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[SkipEndStepsFieldId] = JsonValue.Create(step.Settings.SkipEndSteps);
        return draft;
    }

    protected override void Apply(StepDraft draft, EndJobStep step)
    {
        if (!TryGetSkipEndSteps(draft, out var skipEndSteps))
            throw new InvalidOperationException("The end-job draft does not contain a valid skip-end-steps value.");
        step.Settings.SkipEndSteps = skipEndSteps;
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft) => [];

    private static bool TryGetSkipEndSteps(StepDraft draft, out bool skipEndSteps)
    {
        skipEndSteps = false;
        if (!draft.Values.TryGetValue(SkipEndStepsFieldId, out var value) || value is null)
            return true;
        try
        {
            skipEndSteps = value.GetValue<bool>();
            return true;
        }
        catch (InvalidOperationException) { return false; }
        catch (FormatException) { return false; }
    }
}
