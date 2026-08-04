using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class ShowTextStepDefinition : StepDefinition<ShowTextStep>
{
    public const string TextSourceFieldId = "text_source";
    public const string TextFieldId = "text";
    public const string TextResultFieldId = "text_result";
    public const string DesktopFieldId = "desktop_index";
    public const string FontSizeFieldId = "font_size";
    public const string FontColorFieldId = "font_color";
    public const string OpacityFieldId = "opacity";
    public const string DurationFieldId = "duration_ms";
    public const string ClearOnEndFieldId = "clear_on_job_end";
    public const string OffsetXFieldId = "offset_x";
    public const string OffsetYFieldId = "offset_y";

    public override StepDescriptor Descriptor { get; } = new(
        "show_text", "AnzeigenSpeichern", "Step.Type.ShowText", "Step.Description.ShowText", "text",
        [
            new(TextSourceFieldId, "Ui.Step.Settings.TextSource", StepValueKind.Enum, true,
                JsonValue.Create("ExplicitText"), Constraints: new(AllowedValues: ["ExplicitText", "TaskResult"]), Order: 0,
                Options: [new("ExplicitText", "Ui.Step.IfEditor.LiteralValue"), new("TaskResult", "Ui.Step.IfEditor.JobResultValue")]),
            new(TextFieldId, "Ui.Step.Settings.DisplayText", StepValueKind.MultilineText, Required: true, Order: 1),
            new(TextResultFieldId, "Ui.Step.Settings.TaskResult", StepValueKind.ResultBinding, Required: true,
                EditorHint: StepEditorHints.ResultBindingPicker, Order: 2,
                InputContractId: "text"),
            new(DesktopFieldId, "Ui.Step.Settings.DesktopIndex", StepValueKind.Integer,
                DefaultValue: JsonValue.Create(0), EditorHint: StepEditorHints.MonitorPicker,
                Constraints: new(Minimum: 0), Order: 3),
            new(FontSizeFieldId, "Ui.Step.Settings.FontSizePt", StepValueKind.Number,
                DefaultValue: JsonValue.Create(24d), Constraints: new(Minimum: 0.01m), Advanced: true, Order: 4),
            new(FontColorFieldId, "Ui.Step.Settings.FontColor", StepValueKind.Color,
                DefaultValue: JsonValue.Create("#FFFFFF"), Advanced: true, Order: 5),
            new(OpacityFieldId, "Ui.Step.Settings.OpacityPercent", StepValueKind.Number,
                DefaultValue: JsonValue.Create(1d), EditorHint: StepEditorHints.Percentage,
                Constraints: new(Minimum: 0, Maximum: 1), Advanced: true, Order: 6),
            new(DurationFieldId, "Ui.Step.Settings.DisplayDurationMs", StepValueKind.Duration,
                DefaultValue: JsonValue.Create(5000), Constraints: new(Minimum: 0), Advanced: true, Order: 7),
            new(ClearOnEndFieldId, "Ui.Step.Settings.RemoveWhenJobEnds", StepValueKind.Boolean,
                DefaultValue: JsonValue.Create(false), Advanced: true, Order: 8),
            new(OffsetXFieldId, "Ui.Step.Settings.XOffsetPixels", StepValueKind.Integer,
                DefaultValue: JsonValue.Create(100), Advanced: true, Order: 9),
            new(OffsetYFieldId, "Ui.Step.Settings.YOffsetPixels", StepValueKind.Integer,
                DefaultValue: JsonValue.Create(100), Advanced: true, Order: 10)
        ],
        new(
            [new("general", null, [TextSourceFieldId, TextFieldId, TextResultFieldId, DesktopFieldId], EditorNodes:
                [new StepChoiceGroupDescriptor(TextSourceFieldId,
                    [new("ExplicitText", "Ui.Step.IfEditor.LiteralValue", [new StepFieldNodeDescriptor(TextFieldId)]),
                     new("TaskResult", "Ui.Step.IfEditor.JobResultValue", [new StepFieldNodeDescriptor(TextResultFieldId)])]),
                 new StepFieldNodeDescriptor(DesktopFieldId)]),
             new("advanced", "Ui.Step.Settings.Advanced",
                 [FontSizeFieldId, FontColorFieldId, OpacityFieldId, DurationFieldId, ClearOnEndFieldId, OffsetXFieldId, OffsetYFieldId],
                 1, true, false, EditorNodes:
                 [new StepFieldNodeDescriptor(FontSizeFieldId),
                  new StepFieldNodeDescriptor(FontColorFieldId),
                  new StepFieldNodeDescriptor(OpacityFieldId),
                  new StepFieldNodeDescriptor(DurationFieldId),
                  new StepFieldNodeDescriptor(ClearOnEndFieldId),
                  new StepPointFieldPairDescriptor(
                      OffsetXFieldId, OffsetYFieldId, "Ui.Step.Settings.Position")])],
            [new(TextFieldId, StepSummaryValueFormat.ShortText), new(DesktopFieldId)],
            [TextSourceFieldId, TextFieldId, TextResultFieldId, DesktopFieldId, FontSizeFieldId, FontColorFieldId,
                OpacityFieldId, DurationFieldId, ClearOnEndFieldId, OffsetXFieldId, OffsetYFieldId]));

    public override ShowTextStep CreateDefaultStep() => new();

    protected override StepDraft Read(ShowTextStep step)
    {
        var s = step.Settings;
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[TextSourceFieldId] = JsonValue.Create(s.TextSource.ToString());
        draft.Values[TextFieldId] = JsonValue.Create(s.Text);
        draft.Values[TextResultFieldId] = JsonSerializer.SerializeToNode(s.TextResult);
        draft.Values[DesktopFieldId] = JsonValue.Create(s.DesktopIndex);
        draft.Values[FontSizeFieldId] = JsonValue.Create((double)s.FontSize);
        draft.Values[FontColorFieldId] = JsonValue.Create(s.FontColor);
        draft.Values[OpacityFieldId] = JsonValue.Create((double)s.Opacity);
        draft.Values[DurationFieldId] = JsonValue.Create(s.DurationMs);
        draft.Values[ClearOnEndFieldId] = JsonValue.Create(s.ClearOnJobEnd);
        draft.Values[OffsetXFieldId] = JsonValue.Create(s.OffsetX);
        draft.Values[OffsetYFieldId] = JsonValue.Create(s.OffsetY);
        return draft;
    }

    protected override void Apply(StepDraft draft, ShowTextStep step)
    {
        var s = step.Settings;
        s.TextSource = Enum.TryParse<ShowTextSource>(DefinitionValueReader.String(draft, TextSourceFieldId), out var source)
            ? source : ShowTextSource.ExplicitText;
        s.Text = DefinitionValueReader.String(draft, TextFieldId);
        s.TextResult = DefinitionValueReader.Binding(draft, TextResultFieldId);
        s.DesktopIndex = DefinitionValueReader.Integer(draft, DesktopFieldId);
        s.FontSize = (float)DefinitionValueReader.Number(draft, FontSizeFieldId);
        s.FontColor = DefinitionValueReader.String(draft, FontColorFieldId);
        s.Opacity = (float)DefinitionValueReader.Number(draft, OpacityFieldId);
        s.DurationMs = DefinitionValueReader.Integer(draft, DurationFieldId);
        s.ClearOnJobEnd = DefinitionValueReader.Boolean(draft, ClearOnEndFieldId);
        s.OffsetX = DefinitionValueReader.Integer(draft, OffsetXFieldId);
        s.OffsetY = DefinitionValueReader.Integer(draft, OffsetYFieldId);
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft)
    {
        return [];
    }
}
