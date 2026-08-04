using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class KlickOnPoint3DStepDefinition : StepDefinition<KlickOnPoint3DStep>
{
    public const string PointsSourceFieldId = "points_source";
    public const string OriginFieldId = "origin";
    public const string ClickTypeFieldId = "click_type";
    public const string MovementFactorXFieldId = "movement_factor_x";
    public const string MovementFactorYFieldId = "movement_factor_y";
    public const string OffsetXFieldId = "offset_x";
    public const string OffsetYFieldId = "offset_y";
    public const string TimeoutFieldId = "timeout_ms";
    public const string DoubleClickFieldId = "double_click";

    private static readonly string[] ClickTypes = ["left", "right", "middle", "none"];

    public override StepDescriptor Descriptor { get; } = new(
        "klick_on_point_3d", "MausTastatur", "Step.Type.KlickOnPoint3D", "Step.Description.KlickOnPoint3D", "mouse-click-3d",
        [
            new(PointsSourceFieldId, "Ui.Step.Settings.DetectionStep", StepValueKind.ResultBinding, true,
                EditorHint: StepEditorHints.ResultBindingPicker, Order: 0, InputContractId: "points"),
            new(OriginFieldId, "Ui.Step.Settings.Origin", StepValueKind.Object, true,
                JsonSerializer.SerializeToNode(new StepScreenPointSelectionValue(0, 0, 0, KlickOnPoint3DSettings.MonitorLocalCoordinates)),
                "Ui.Step.Settings.OriginLocalHelp", StepEditorHints.ScreenPointPicker, Order: 1,
                ScreenPointPickerOptions: new(true)),
            new(ClickTypeFieldId, "Ui.Step.Settings.ClickType", StepValueKind.Enum, true, JsonValue.Create("left"),
                Constraints: new(AllowedValues: ClickTypes), Order: 2,
                Options: [new("left", "Enum.MouseClickType.Left"), new("right", "Enum.MouseClickType.Right"),
                    new("middle", "Enum.MouseClickType.Middle"), new("none", "Enum.MouseClickType.None")]),
            new(MovementFactorXFieldId, "Ui.Step.Settings.MovementFactorX", StepValueKind.Number, DefaultValue: JsonValue.Create(1d),
                DescriptionKey: "Ui.Step.Settings.MovementFactorHelp", Constraints: new(Minimum: 0.01m, Maximum: 100), Advanced: true, Order: 3),
            new(MovementFactorYFieldId, "Ui.Step.Settings.MovementFactorY", StepValueKind.Number, DefaultValue: JsonValue.Create(1d),
                DescriptionKey: "Ui.Step.Settings.MovementFactorHelp", Constraints: new(Minimum: 0.01m, Maximum: 100), Advanced: true, Order: 4),
            new(OffsetXFieldId, "Ui.Step.Settings.XOffsetPixels", StepValueKind.Integer, DefaultValue: JsonValue.Create(0), Advanced: true, Order: 5),
            new(OffsetYFieldId, "Ui.Step.Settings.YOffsetPixels", StepValueKind.Integer, DefaultValue: JsonValue.Create(0), Advanced: true, Order: 6),
            new(TimeoutFieldId, "Ui.Step.Settings.TimeoutMs", StepValueKind.Duration, DefaultValue: JsonValue.Create(0),
                Constraints: new(Minimum: 0), Advanced: true, Order: 7),
            new(DoubleClickFieldId, "Ui.Step.Settings.DoubleClick", StepValueKind.Boolean, DefaultValue: JsonValue.Create(false), Advanced: true, Order: 8)
        ],
        new([
                new("general", null, [PointsSourceFieldId, OriginFieldId, ClickTypeFieldId]),
                new("advanced", "Ui.Step.Settings.Advanced", [MovementFactorXFieldId, MovementFactorYFieldId,
                    OffsetXFieldId, OffsetYFieldId, TimeoutFieldId, DoubleClickFieldId], 1, true, false)
            ],
            [new(PointsSourceFieldId), new(ClickTypeFieldId)],
            [PointsSourceFieldId, OriginFieldId, ClickTypeFieldId, MovementFactorXFieldId, MovementFactorYFieldId,
                OffsetXFieldId, OffsetYFieldId, TimeoutFieldId, DoubleClickFieldId]));

    public override KlickOnPoint3DStep CreateDefaultStep() => new()
    {
        Settings = new() { OriginCoordinateSpace = KlickOnPoint3DSettings.MonitorLocalCoordinates, TimeoutMs = 0 }
    };

    protected override StepDraft Read(KlickOnPoint3DStep step)
    {
        var s = step.Settings;
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[PointsSourceFieldId] = JsonSerializer.SerializeToNode(s.PointsSource);
        draft.Values[OriginFieldId] = JsonSerializer.SerializeToNode(new StepScreenPointSelectionValue(
            s.OriginMonitorIndex, s.OriginX, s.OriginY, s.OriginCoordinateSpace));
        draft.Values[ClickTypeFieldId] = JsonValue.Create(s.ClickType);
        draft.Values[MovementFactorXFieldId] = JsonValue.Create(s.EffectiveMovementFactorX);
        draft.Values[MovementFactorYFieldId] = JsonValue.Create(s.EffectiveMovementFactorY);
        draft.Values[OffsetXFieldId] = JsonValue.Create(s.OffsetX);
        draft.Values[OffsetYFieldId] = JsonValue.Create(s.OffsetY);
        draft.Values[TimeoutFieldId] = JsonValue.Create(s.TimeoutMs);
        draft.Values[DoubleClickFieldId] = JsonValue.Create(s.DoubleClick);
        return draft;
    }

    protected override void Apply(StepDraft draft, KlickOnPoint3DStep step)
    {
        var s = step.Settings;
        var origin = ReadOrigin(draft);
        s.PointsSource = DefinitionValueReader.Binding(draft, PointsSourceFieldId);
        s.OriginMonitorIndex = origin.MonitorIndex;
        s.OriginPoint = origin.Position;
        s.OriginCoordinateSpace = origin.CoordinateSpace;
        s.LegacyMovementFactor = null;
        s.MovementFactorX = DefinitionValueReader.Number(draft, MovementFactorXFieldId);
        s.MovementFactorY = DefinitionValueReader.Number(draft, MovementFactorYFieldId);
        s.ClickType = DefinitionValueReader.String(draft, ClickTypeFieldId);
        s.OffsetX = DefinitionValueReader.Integer(draft, OffsetXFieldId);
        s.OffsetY = DefinitionValueReader.Integer(draft, OffsetYFieldId);
        s.TimeoutMs = DefinitionValueReader.Integer(draft, TimeoutFieldId);
        s.DoubleClick = DefinitionValueReader.Boolean(draft, DoubleClickFieldId);
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft)
    {
        var origin = ReadOrigin(draft);
        if (origin.CoordinateSpace == KlickOnPoint3DSettings.MonitorLocalCoordinates && origin.MonitorIndex < 0) return [Invalid(OriginFieldId)];
        return [];
    }

    private static StepScreenPointSelectionValue ReadOrigin(StepDraft draft)
    {
        try { return draft.Values.GetValueOrDefault(OriginFieldId)?.Deserialize<StepScreenPointSelectionValue>() ?? new(0, 0, 0, KlickOnPoint3DSettings.MonitorLocalCoordinates); }
        catch (JsonException) { return new(0, 0, 0, KlickOnPoint3DSettings.MonitorLocalCoordinates); }
    }

    private static StepValidationIssue Invalid(string fieldId) => new("StepValidation.Invalid", fieldId);
}
