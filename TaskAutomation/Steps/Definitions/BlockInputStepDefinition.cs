using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class BlockInputStepDefinition : StepDefinition<BlockInputStep>
{
    public const string SafetyTimeoutFieldId = "safety_timeout_seconds";

    public override StepDescriptor Descriptor { get; } = new(
        TypeId: "block_input",
        CategoryId: "AblaufSteuern",
        DisplayNameKey: "Step.Type.BlockInput",
        DescriptionKey: "Step.Description.BlockInput",
        IconKey: "lock-outline",
        Fields:
        [
            new StepFieldDescriptor(
                Id: SafetyTimeoutFieldId,
                LabelKey: "Ui.Step.Settings.SafetyTimeoutSeconds",
                ValueKind: StepValueKind.Integer,
                Required: true,
                DefaultValue: JsonValue.Create(30),
                Constraints: new StepFieldConstraints(Minimum: 1, Maximum: 3600))
        ],
        Presentation: new StepPresentationDescriptor(
            EditorSections:
            [
                new StepEditorSectionDescriptor("general", null, [SafetyTimeoutFieldId])
            ],
            SummaryItems:
            [
                new StepSummaryItemDescriptor(SafetyTimeoutFieldId)
            ],
            DetailFieldIds: [SafetyTimeoutFieldId]));

    public override BlockInputStep CreateDefaultStep() => new();

    protected override StepDraft Read(BlockInputStep step)
    {
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[SafetyTimeoutFieldId] = JsonValue.Create(step.Settings.SafetyTimeoutSeconds);
        return draft;
    }

    protected override void Apply(StepDraft draft, BlockInputStep step)
    {
        if (!TryGetTimeout(draft, out var timeout))
            throw new InvalidOperationException("The block-input draft does not contain a valid safety timeout.");
        step.Settings.SafetyTimeoutSeconds = timeout;
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft) => [];

    private static bool TryGetTimeout(StepDraft draft, out int timeout)
    {
        timeout = 0;
        if (!draft.Values.TryGetValue(SafetyTimeoutFieldId, out var value) || value is null)
            return false;
        try
        {
            timeout = value.GetValue<int>();
            return true;
        }
        catch (InvalidOperationException) { return false; }
        catch (FormatException) { return false; }
    }
}
