using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class TimeoutStepDefinition : StepDefinition<TimeoutStep>
{
    public const string DelayFieldId = "delay_ms";

    public override StepDescriptor Descriptor { get; } = new(
        TypeId: "timeout",
        CategoryId: "AblaufSteuern",
        DisplayNameKey: "Step.Type.Timeout",
        DescriptionKey: "Step.Description.Timeout",
        IconKey: "timer-outline",
        Fields:
        [
            new StepFieldDescriptor(
                Id: DelayFieldId,
                LabelKey: "Ui.Step.Settings.WaitTimeMs",
                ValueKind: StepValueKind.Duration,
                Required: true,
                DefaultValue: JsonValue.Create(1000),
                Constraints: new StepFieldConstraints(Minimum: 0))
        ],
        Presentation: new StepPresentationDescriptor(
            EditorSections:
            [
                new StepEditorSectionDescriptor("general", null, [DelayFieldId])
            ],
            SummaryItems:
            [
                new StepSummaryItemDescriptor(DelayFieldId, StepSummaryValueFormat.DurationMilliseconds)
            ],
            DetailFieldIds: [DelayFieldId]));

    public override TimeoutStep CreateDefaultStep() => new();

    protected override StepDraft Read(TimeoutStep step)
    {
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[DelayFieldId] = JsonValue.Create(step.Settings.DelayMs);
        return draft;
    }

    protected override void Apply(StepDraft draft, TimeoutStep step)
    {
        if (!TryGetDelay(draft, out var delay))
            throw new InvalidOperationException("The timeout draft does not contain a valid delay.");
        step.Settings.DelayMs = delay;
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft) => [];

    private static bool TryGetDelay(StepDraft draft, out int delay)
    {
        delay = 0;
        if (!draft.Values.TryGetValue(DelayFieldId, out var value) || value is null)
            return false;
        try
        {
            delay = value.GetValue<int>();
            return true;
        }
        catch (InvalidOperationException) { return false; }
        catch (FormatException) { return false; }
    }
}
