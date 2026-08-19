using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class IfStepDefinition : StepDefinition<IfStep>
{
    public const string ConditionsFieldId = "conditions";

    public override StepDescriptor Descriptor { get; } = ConditionStepDefinitionSupport.CreateDescriptor(
        "if", "Step.Type.If", "Step.Description.If");

    public override IfStep CreateDefaultStep() => new();

    protected override StepDraft Read(IfStep step) => ConditionStepDefinitionSupport.Read(Descriptor.TypeId, step.Settings);

    protected override void Apply(StepDraft draft, IfStep step) =>
        step.Settings = ConditionStepDefinitionSupport.ReadSettings(draft);

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft) =>
        ConditionStepDefinitionSupport.Validate(draft);
}

internal static class ConditionStepDefinitionSupport
{
    public static StepDescriptor CreateDescriptor(string typeId, string displayNameKey, string descriptionKey) => new(
        TypeId: typeId,
        CategoryId: "AblaufSteuern",
        DisplayNameKey: displayNameKey,
        DescriptionKey: descriptionKey,
        IconKey: "condition",
        Fields:
        [
            new StepFieldDescriptor(IfStepDefinition.ConditionsFieldId, "Ui.Step.Settings.Evaluation", StepValueKind.Object,
                Required: true,
                DefaultValue: JsonSerializer.SerializeToNode(new IfConditionSettings()),
                EditorHint: StepEditorHints.ConditionEditor,
                Order: 0)
        ],
        Presentation: new StepPresentationDescriptor(
            EditorSections: [new StepEditorSectionDescriptor("general", null, [IfStepDefinition.ConditionsFieldId])],
            SummaryItems: [new StepSummaryItemDescriptor(IfStepDefinition.ConditionsFieldId)],
            DetailFieldIds: [IfStepDefinition.ConditionsFieldId]));

    public static StepDraft Read(string typeId, IfConditionSettings settings)
    {
        var draft = new StepDraft(typeId);
        draft.Values[IfStepDefinition.ConditionsFieldId] = JsonSerializer.SerializeToNode(settings);
        return draft;
    }

    public static IfConditionSettings ReadSettings(StepDraft draft)
    {
        try
        {
            return draft.Values.GetValueOrDefault(IfStepDefinition.ConditionsFieldId)
                       ?.Deserialize<IfConditionSettings>() ?? new IfConditionSettings();
        }
        catch (JsonException) { return new IfConditionSettings(); }
        catch (InvalidOperationException) { return new IfConditionSettings(); }
    }

    public static IReadOnlyList<StepValidationIssue> Validate(StepDraft draft)
    {
        var settings = ReadSettings(draft);
        if (!Enum.IsDefined(settings.MatchMode) || settings.Conditions.Count == 0)
            return [new("StepValidation.Required", IfStepDefinition.ConditionsFieldId)];
        if (settings.Conditions.Any(condition =>
                !condition.IsConfigured
                || !Enum.IsDefined(condition.Operator)))
            return [new("StepValidation.Invalid", IfStepDefinition.ConditionsFieldId)];
        return [];
    }
}
