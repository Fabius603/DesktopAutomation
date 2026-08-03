using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class UserChoiceStepDefinition : StepDefinition<UserChoiceStep>
{
    public const string TitleFieldId = "title";
    public const string QuestionFieldId = "question";
    public const string DescriptionFieldId = "description";
    public const string DesktopIndexFieldId = "desktop_index";
    public const string OptionsFieldId = "options";

    public override StepDescriptor Descriptor { get; } = new(
        "user_choice", "AblaufSteuern", "Step.Type.UserChoice", "Step.Description.UserChoice", "user-choice",
        [
            new(TitleFieldId, "Ui.UserChoice.Title", StepValueKind.Text, DefaultValue: JsonValue.Create(""), Order: 0),
            new(QuestionFieldId, "Ui.UserChoice.Question", StepValueKind.MultilineText, DefaultValue: JsonValue.Create(""), EditorHint: StepEditorHints.EmojiText, Order: 1),
            new(DescriptionFieldId, "Ui.UserChoice.Description", StepValueKind.MultilineText, DefaultValue: JsonValue.Create(""), EditorHint: StepEditorHints.EmojiText, Order: 2),
            new(DesktopIndexFieldId, "Ui.Step.Settings.DesktopIndex", StepValueKind.Integer, DefaultValue: JsonValue.Create(0),
                EditorHint: StepEditorHints.MonitorPicker, Constraints: new(Minimum: 0), Order: 3),
            new(OptionsFieldId, "Ui.UserChoice.Answers", StepValueKind.Collection, true,
                EditorHint: StepEditorHints.UserChoiceOptions, Constraints: new(MinimumLength: 2, MaximumLength: 18), Order: 4)
        ],
        new([new("general", null, [TitleFieldId, QuestionFieldId, DescriptionFieldId, DesktopIndexFieldId, OptionsFieldId])],
            [new(QuestionFieldId, StepSummaryValueFormat.ShortText), new(OptionsFieldId)],
            [TitleFieldId, QuestionFieldId, DescriptionFieldId, DesktopIndexFieldId, OptionsFieldId]));

    public override UserChoiceStep CreateDefaultStep() => new()
    {
        Settings = new UserChoiceSettings
        {
            Options = [new UserChoiceOption(), new UserChoiceOption()]
        }
    };

    protected override StepDraft Read(UserChoiceStep step)
    {
        var s = step.Settings;
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[TitleFieldId] = JsonValue.Create(s.Title);
        draft.Values[QuestionFieldId] = JsonValue.Create(s.Question);
        draft.Values[DescriptionFieldId] = JsonValue.Create(s.Description);
        draft.Values[DesktopIndexFieldId] = JsonValue.Create(s.DesktopIndex);
        draft.Values[OptionsFieldId] = JsonSerializer.SerializeToNode(s.Options.Select(option =>
            new StepUserChoiceOptionValue(option.Id, option.Label, option.Value)).ToArray());
        return draft;
    }

    protected override void Apply(StepDraft draft, UserChoiceStep step)
    {
        step.Settings.Title = DefinitionValueReader.String(draft, TitleFieldId);
        step.Settings.Question = DefinitionValueReader.String(draft, QuestionFieldId);
        step.Settings.Description = DefinitionValueReader.String(draft, DescriptionFieldId);
        step.Settings.DesktopIndex = DefinitionValueReader.Integer(draft, DesktopIndexFieldId);
        step.Settings.Options = ReadOptions(draft).Select(option => new UserChoiceOption
        {
            Id = option.Id,
            Label = option.Label,
            Value = option.Value
        }).ToList();
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft)
    {
        var options = ReadOptions(draft);
        if (options.Any(option => string.IsNullOrWhiteSpace(option.Id) || string.IsNullOrWhiteSpace(option.Label))
            || options.Select(option => option.Id).Distinct(StringComparer.Ordinal).Count() != options.Count
            || options.Select(option => option.Label.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != options.Count)
            return [Invalid(OptionsFieldId)];
        return [];
    }

    private static IReadOnlyList<StepUserChoiceOptionValue> ReadOptions(StepDraft draft)
    {
        try { return draft.Values.GetValueOrDefault(OptionsFieldId)?.Deserialize<List<StepUserChoiceOptionValue>>() ?? []; }
        catch (JsonException) { return []; }
    }

    private static StepValidationIssue Invalid(string fieldId) => new("StepValidation.Invalid", fieldId);
}
