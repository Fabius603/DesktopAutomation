using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class PointComparisonStepDefinition : StepDefinition<PointComparisonStep>
{
    public const string ModeFieldId = "mode";
    public const string MatchRequirementFieldId = "match_requirement";
    public const string PointsFieldId = "points";
    public const string ReferenceSourceFieldId = "reference_source";
    public const string ReferenceXFieldId = "reference_x";
    public const string ReferenceYFieldId = "reference_y";
    public const string ReferencePointsFieldId = "reference_points_source";
    public const string OffsetXFieldId = "offset_x";
    public const string OffsetYFieldId = "offset_y";
    public const string CombineModeFieldId = "combine_mode";
    public const string ExpressionsFieldId = "expressions";

    private static readonly StepVisibilityRule OffsetMode = new(ModeFieldId, JsonValue.Create("Offset"));
    private static readonly StepVisibilityRule ExpressionMode = new(ModeFieldId, JsonValue.Create("Expression"));
    private static readonly StepVisibilityRule ManualReference = new(ReferenceSourceFieldId, JsonValue.Create("Manual"));
    private static readonly StepVisibilityRule ResultReference = new(ReferenceSourceFieldId, JsonValue.Create("JobResult"));

    public override StepDescriptor Descriptor { get; } = new(
        "point_comparison", "BildAuswerten", "Step.Type.PointComparison", "Step.Description.PointComparison", "point-comparison",
        [
            EnumField(ModeFieldId, "Ui.Step.Settings.Mode", "Offset", ["Offset", "Expression"],
                [new("Offset", "Ui.Step.Settings.OffsetTolerance"), new("Expression", "Ui.Step.Settings.Expression")], 0),
            EnumField(MatchRequirementFieldId, "Ui.Step.Settings.Evaluation", "All", ["All", "Any"],
                [new("All", "Ui.Step.Settings.AllAND"), new("Any", "Ui.Step.Settings.AtLeastOneOR")], 1),
            new(PointsFieldId, "Ui.Step.Settings.PointsToCheck", StepValueKind.Collection, true,
                JsonSerializer.SerializeToNode(new[] { new StepPointEntryValue("Manual", 0, 0, null) }),
                EditorHint: StepEditorHints.PointEntryList, Order: 2, InputContractId: "points"),
            EnumField(ReferenceSourceFieldId, "Ui.Step.Settings.ReferenceSource", "Manual", ["Manual", "JobResult"],
                [new("Manual", "Ui.Step.Settings.EnterManually"), new("JobResult", "Ui.Step.Settings.FromDetectionResult")], 3, OffsetMode),
            new(ReferenceXFieldId, "Ui.Step.Settings.X", StepValueKind.Integer, DefaultValue: JsonValue.Create(0), Order: 4,
                VisibleWhenAll: [OffsetMode, ManualReference]),
            new(ReferenceYFieldId, "Ui.Step.Settings.Y", StepValueKind.Integer, DefaultValue: JsonValue.Create(0), Order: 5,
                VisibleWhenAll: [OffsetMode, ManualReference]),
            new(ReferencePointsFieldId, "Ui.Step.Settings.DetectionStep", StepValueKind.ResultBinding, Required: true,
                EditorHint: StepEditorHints.ResultBindingPicker, Order: 6, InputContractId: "points",
                VisibleWhenAll: [OffsetMode, ResultReference]),
            new(OffsetXFieldId, "Ui.Step.Settings.XOffsetPixels", StepValueKind.Integer, DefaultValue: JsonValue.Create(10),
                Constraints: new(Minimum: 0), Order: 7, VisibleWhen: OffsetMode),
            new(OffsetYFieldId, "Ui.Step.Settings.YOffsetPixels", StepValueKind.Integer, DefaultValue: JsonValue.Create(10),
                Constraints: new(Minimum: 0), Order: 8, VisibleWhen: OffsetMode),
            EnumField(CombineModeFieldId, "Ui.Step.Settings.CombineWith", "And", ["And", "Or"],
                [new("And", "Ui.Step.Settings.ANDAll"), new("Or", "Ui.Step.Settings.ORAny")], 9, ExpressionMode),
            new(ExpressionsFieldId, "Ui.Step.Settings.AxisExpressions", StepValueKind.Collection, true,
                JsonSerializer.SerializeToNode(new[] { new StepAxisExpressionValue("X", "LessThan", 0) }),
                EditorHint: StepEditorHints.AxisExpressionList, Order: 10, VisibleWhen: ExpressionMode)
        ],
        new([
                new("general", "Ui.Step.Settings.BasicSettings", [ModeFieldId, MatchRequirementFieldId, PointsFieldId]),
                new("offset", "Ui.Step.Settings.ReferencePointTolerance", [ReferenceSourceFieldId, ReferenceXFieldId,
                    ReferenceYFieldId, ReferencePointsFieldId, OffsetXFieldId, OffsetYFieldId], 1),
                new("expression", "Ui.Step.Settings.AxisExpressions", [CombineModeFieldId, ExpressionsFieldId], 2)
            ],
            [new(ModeFieldId), new(MatchRequirementFieldId)],
            [ModeFieldId, MatchRequirementFieldId, PointsFieldId, ReferenceSourceFieldId, ReferenceXFieldId,
                ReferenceYFieldId, ReferencePointsFieldId, OffsetXFieldId, OffsetYFieldId, CombineModeFieldId, ExpressionsFieldId]));

    public override PointComparisonStep CreateDefaultStep() => new()
    {
        Settings = new PointComparisonSettings
        {
            Points = [new PointEntry()],
            ExpressionSettings = new ExpressionComparisonSettings { Expressions = [new AxisExpression()] }
        }
    };

    protected override StepDraft Read(PointComparisonStep step)
    {
        var s = step.Settings;
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[ModeFieldId] = JsonValue.Create(s.Mode.ToString());
        draft.Values[MatchRequirementFieldId] = JsonValue.Create(s.MatchRequirement.ToString());
        draft.Values[PointsFieldId] = JsonSerializer.SerializeToNode(s.Points.Select(ToValue).ToArray());
        draft.Values[ReferenceSourceFieldId] = JsonValue.Create(s.OffsetSettings.ReferenceSource.ToString());
        draft.Values[ReferenceXFieldId] = JsonValue.Create(s.OffsetSettings.ReferenceX);
        draft.Values[ReferenceYFieldId] = JsonValue.Create(s.OffsetSettings.ReferenceY);
        draft.Values[ReferencePointsFieldId] = JsonSerializer.SerializeToNode(s.OffsetSettings.ReferencePointsSource);
        draft.Values[OffsetXFieldId] = JsonValue.Create(s.OffsetSettings.OffsetX);
        draft.Values[OffsetYFieldId] = JsonValue.Create(s.OffsetSettings.OffsetY);
        draft.Values[CombineModeFieldId] = JsonValue.Create(s.ExpressionSettings.CombineMode.ToString());
        draft.Values[ExpressionsFieldId] = JsonSerializer.SerializeToNode(s.ExpressionSettings.Expressions.Select(e =>
            new StepAxisExpressionValue(e.Axis, e.Operator.ToString(), e.Value)).ToArray());
        return draft;
    }

    protected override void Apply(StepDraft draft, PointComparisonStep step)
    {
        Enum.TryParse(DefinitionValueReader.String(draft, ModeFieldId), out PointComparisonMode mode);
        Enum.TryParse(DefinitionValueReader.String(draft, MatchRequirementFieldId), out PointMatchRequirement requirement);
        Enum.TryParse(DefinitionValueReader.String(draft, ReferenceSourceFieldId), out PointEntrySource referenceSource);
        Enum.TryParse(DefinitionValueReader.String(draft, CombineModeFieldId), out ExpressionCombineMode combineMode);
        step.Settings.Mode = mode;
        step.Settings.MatchRequirement = requirement;
        step.Settings.Points = ReadPoints(draft).Select(FromValue).ToList();
        step.Settings.OffsetSettings = new OffsetComparisonSettings
        {
            ReferenceSource = referenceSource,
            ReferenceX = DefinitionValueReader.Integer(draft, ReferenceXFieldId),
            ReferenceY = DefinitionValueReader.Integer(draft, ReferenceYFieldId),
            ReferencePointsSource = DefinitionValueReader.Binding(draft, ReferencePointsFieldId),
            OffsetX = DefinitionValueReader.Integer(draft, OffsetXFieldId),
            OffsetY = DefinitionValueReader.Integer(draft, OffsetYFieldId)
        };
        step.Settings.ExpressionSettings = new ExpressionComparisonSettings
        {
            CombineMode = combineMode,
            Expressions = ReadExpressions(draft).Select(value => new AxisExpression
            {
                Axis = value.Axis,
                Operator = Enum.TryParse(value.Operator, out PointAxisOperator op) ? op : PointAxisOperator.LessThan,
                Value = value.Value
            }).ToList()
        };
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft)
    {
        var mode = Enum.Parse<PointComparisonMode>(DefinitionValueReader.String(draft, ModeFieldId));
        var points = ReadPoints(draft);
        if (points.Count == 0 || points.Any(point => point.Source != "Manual" && !ReadBinding(point.PointsSource).IsConfigured)) return [Invalid(PointsFieldId)];
        if (mode != PointComparisonMode.Offset)
        {
            var expressions = ReadExpressions(draft);
            if (expressions.Count == 0 || expressions.Any(expression => expression.Axis is not ("X" or "Y")
                    || !Enum.TryParse<PointAxisOperator>(expression.Operator, out _))) return [Invalid(ExpressionsFieldId)];
        }
        return [];
    }

    private static StepFieldDescriptor EnumField(string id, string label, string defaultValue, string[] values,
        StepFieldOptionDescriptor[] options, int order, StepVisibilityRule? visible = null) =>
        new(id, label, StepValueKind.Enum, true, JsonValue.Create(defaultValue), Constraints: new(AllowedValues: values),
            Order: order, VisibleWhen: visible, Options: options);
    private static StepPointEntryValue ToValue(PointEntry entry) => new(entry.Source.ToString(), entry.ManualX, entry.ManualY,
        JsonSerializer.SerializeToNode(entry.PointsSource));
    private static PointEntry FromValue(StepPointEntryValue value) => new()
    {
        Source = Enum.TryParse(value.Source, out PointEntrySource source) ? source : PointEntrySource.Manual,
        ManualX = value.ManualX, ManualY = value.ManualY, PointsSource = ReadBinding(value.PointsSource)
    };
    private static ResultBinding ReadBinding(JsonNode? value)
    {
        try { return value?.Deserialize<ResultBinding>() ?? new(); } catch (JsonException) { return new(); }
    }
    private static IReadOnlyList<StepPointEntryValue> ReadPoints(StepDraft draft)
    {
        try { return draft.Values.GetValueOrDefault(PointsFieldId)?.Deserialize<List<StepPointEntryValue>>() ?? []; } catch (JsonException) { return []; }
    }
    private static IReadOnlyList<StepAxisExpressionValue> ReadExpressions(StepDraft draft)
    {
        try { return draft.Values.GetValueOrDefault(ExpressionsFieldId)?.Deserialize<List<StepAxisExpressionValue>>() ?? []; } catch (JsonException) { return []; }
    }
    private static StepValidationIssue Invalid(string fieldId) => new("StepValidation.Invalid", fieldId);
}
