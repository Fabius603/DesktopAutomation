using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class KlickOnPointStepDefinition : StepDefinition<KlickOnPointStep>
{
    public const string PointsSourceFieldId = "points_source";
    public const string ClickTypeFieldId = "click_type";
    public const string OffsetXFieldId = "offset_x";
    public const string OffsetYFieldId = "offset_y";
    public const string TimeoutFieldId = "timeout_ms";
    public const string DoubleClickFieldId = "double_click";

    private static readonly string[] ClickTypes = ["left", "right", "middle", "none"];

    public override StepDescriptor Descriptor { get; } = new(
        TypeId: "klick_on_point",
        CategoryId: "MausTastatur",
        DisplayNameKey: "Step.Type.KlickOnPoint",
        DescriptionKey: "Step.Description.KlickOnPoint",
        IconKey: "mouse-click",
        Fields:
        [
            new StepFieldDescriptor(PointsSourceFieldId, "Ui.Step.Settings.PointSource", StepValueKind.ResultBinding,
                Required: true,
                DefaultValue: new JsonObject { ["x"] = 0, ["y"] = 0 },
                EditorHint: StepEditorHints.ValueReferencePicker, Order: 0, InputContractId: "points",
                AllowsDirectValue: true),
            new StepFieldDescriptor(ClickTypeFieldId, "Ui.Step.Settings.ClickType", StepValueKind.Enum,
                Required: true, DefaultValue: JsonValue.Create("left"),
                Constraints: new StepFieldConstraints(AllowedValues: ClickTypes), Order: 1,
                Options:
                [
                    new("left", "Enum.MouseClickType.Left"),
                    new("right", "Enum.MouseClickType.Right"),
                    new("middle", "Enum.MouseClickType.Middle"),
                    new("none", "Enum.MouseClickType.None")
                ]),
            new StepFieldDescriptor(OffsetXFieldId, "Ui.Step.Settings.XOffsetPixels", StepValueKind.Integer,
                DefaultValue: JsonValue.Create(0), Advanced: true, Order: 2),
            new StepFieldDescriptor(OffsetYFieldId, "Ui.Step.Settings.YOffsetPixels", StepValueKind.Integer,
                DefaultValue: JsonValue.Create(0), Advanced: true, Order: 3),
            new StepFieldDescriptor(TimeoutFieldId, "Ui.Step.Settings.TimeoutMs", StepValueKind.Duration,
                DefaultValue: JsonValue.Create(0), Constraints: new StepFieldConstraints(Minimum: 0), Advanced: true, Order: 4),
            new StepFieldDescriptor(DoubleClickFieldId, "Ui.Step.Settings.DoubleClick", StepValueKind.Boolean,
                DefaultValue: JsonValue.Create(false), Advanced: true, Order: 5)
        ],
        Presentation: new StepPresentationDescriptor(
            EditorSections:
            [
                new StepEditorSectionDescriptor("general", null, [PointsSourceFieldId, ClickTypeFieldId]),
                new StepEditorSectionDescriptor("advanced", "Ui.Step.Settings.Advanced",
                    [OffsetXFieldId, OffsetYFieldId, TimeoutFieldId, DoubleClickFieldId],
                    Order: 1, Collapsible: true, InitiallyExpanded: false)
            ],
            SummaryItems:
            [
                new StepSummaryItemDescriptor(PointsSourceFieldId),
                new StepSummaryItemDescriptor(ClickTypeFieldId)
            ],
            DetailFieldIds:
            [PointsSourceFieldId, ClickTypeFieldId, OffsetXFieldId, OffsetYFieldId, TimeoutFieldId, DoubleClickFieldId]));

    public override KlickOnPointStep CreateDefaultStep() => new()
    {
        Settings = new KlickOnPointSettings { TimeoutMs = 0 }
    };

    protected override StepDraft Read(KlickOnPointStep step)
    {
        var settings = step.Settings;
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[PointsSourceFieldId] = JsonSerializer.SerializeToNode(settings.PointsSource);
        draft.Values[ClickTypeFieldId] = JsonValue.Create(settings.ClickType);
        draft.Values[OffsetXFieldId] = JsonValue.Create(settings.OffsetX);
        draft.Values[OffsetYFieldId] = JsonValue.Create(settings.OffsetY);
        draft.Values[TimeoutFieldId] = JsonValue.Create(settings.TimeoutMs);
        draft.Values[DoubleClickFieldId] = JsonValue.Create(settings.DoubleClick);
        return draft;
    }

    protected override void Apply(StepDraft draft, KlickOnPointStep step)
    {
        step.Settings.PointsSource = DefinitionValueReader.Binding(draft, PointsSourceFieldId);
        step.Settings.ClickType = DefinitionValueReader.String(draft, ClickTypeFieldId);
        step.Settings.OffsetX = DefinitionValueReader.Integer(draft, OffsetXFieldId);
        step.Settings.OffsetY = DefinitionValueReader.Integer(draft, OffsetYFieldId);
        step.Settings.TimeoutMs = DefinitionValueReader.Integer(draft, TimeoutFieldId);
        step.Settings.DoubleClick = DefinitionValueReader.Boolean(draft, DoubleClickFieldId);
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft) => [];
}
