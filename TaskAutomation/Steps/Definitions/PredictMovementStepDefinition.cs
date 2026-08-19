using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps.Definitions;

public sealed class PredictMovementStepDefinition : StepDefinition<PredictMovementStep>
{
    public const string PointsSourceFieldId = "points_source";
    public const string PredictionModelFieldId = "prediction_model";
    public const string MinimumConfidenceFieldId = "minimum_confidence";
    public const string MinSamplesFieldId = "min_samples";
    public const string PredictionMsFieldId = "prediction_ms";
    public const string ResetDistanceFieldId = "reset_distance_threshold";
    public const string MaxSampleAgeFieldId = "max_sample_age_ms";
    public const string TimeBasisFieldId = "time_basis";
    public const string MaxPredictionDistanceFieldId = "max_prediction_distance";
    public const string MaxFitErrorFieldId = "max_fit_error";

    private static readonly string[] Models = ["Automatic", "Linear", "Acceleration", "Kalman"];

    public override StepDescriptor Descriptor { get; } = new(
        TypeId: "predict_movement",
        CategoryId: "BildAuswerten",
        DisplayNameKey: "Step.Type.PredictMovement",
        DescriptionKey: "Step.Description.PredictMovement",
        IconKey: "movement-prediction",
        Fields:
        [
            new StepFieldDescriptor(PointsSourceFieldId, "Ui.Step.Settings.DetectionStep", StepValueKind.ResultBinding,
                Required: true, EditorHint: StepEditorHints.ValueReferencePicker, Order: 0, InputContractId: "points"),
            new StepFieldDescriptor(PredictionModelFieldId, "Ui.Step.Settings.PredictionModel", StepValueKind.Enum,
                Required: true, DefaultValue: JsonValue.Create("Automatic"),
                Constraints: new StepFieldConstraints(AllowedValues: Models), Order: 1,
                Options:
                [
                    new("Automatic", "Enum.PredictionModel.Automatic"),
                    new("Linear", "Enum.PredictionModel.Linear"),
                    new("Acceleration", "Enum.PredictionModel.Acceleration"),
                    new("Kalman", "Enum.PredictionModel.Kalman")
                ]),
            new StepFieldDescriptor(MinimumConfidenceFieldId, "Ui.Step.Settings.MinimumConfidencePercent", StepValueKind.Number,
                DefaultValue: JsonValue.Create(0.15d), EditorHint: StepEditorHints.Percentage,
                Constraints: new StepFieldConstraints(Minimum: 0, Maximum: 1), Order: 2),
            new StepFieldDescriptor(MinSamplesFieldId, "Ui.Step.Settings.MinValues", StepValueKind.Integer,
                DefaultValue: JsonValue.Create(3), Constraints: new StepFieldConstraints(Minimum: 2), Advanced: true, Order: 3),
            new StepFieldDescriptor(ResetDistanceFieldId, "Ui.Step.Settings.ResetAtDistance", StepValueKind.Number,
                DefaultValue: JsonValue.Create(250d), Constraints: new StepFieldConstraints(Minimum: 0), Advanced: true, Order: 4),
            new StepFieldDescriptor(MaxSampleAgeFieldId, "Ui.Step.Settings.MaxAgeMs", StepValueKind.Duration,
                DefaultValue: JsonValue.Create(500), Constraints: new StepFieldConstraints(Minimum: 0), Advanced: true, Order: 5),
            new StepFieldDescriptor(MaxPredictionDistanceFieldId, "Ui.Step.Settings.MaxPredictionDistance", StepValueKind.Number,
                DefaultValue: JsonValue.Create(500d), Constraints: new StepFieldConstraints(Minimum: 0), Advanced: true, Order: 6),
            new StepFieldDescriptor(MaxFitErrorFieldId, "Ui.Step.Settings.MaxFitError", StepValueKind.Number,
                DefaultValue: JsonValue.Create(75d), Constraints: new StepFieldConstraints(Minimum: 0), Advanced: true, Order: 7),
            // No longer edited, but retained so old jobs keep their runtime timing semantics.
            new StepFieldDescriptor(PredictionMsFieldId, "Ui.Step.Settings.PredictionMs", StepValueKind.Duration,
                DefaultValue: JsonValue.Create(0), Order: 8),
            new StepFieldDescriptor(TimeBasisFieldId, "Ui.Step.Settings.TimeBasis", StepValueKind.Text,
                DefaultValue: JsonValue.Create("Execution"), Order: 9)
        ],
        Presentation: new StepPresentationDescriptor(
            EditorSections:
            [
                new StepEditorSectionDescriptor("general", null,
                    [PointsSourceFieldId, PredictionModelFieldId, MinimumConfidenceFieldId]),
                new StepEditorSectionDescriptor("advanced", "Ui.Step.Settings.Advanced",
                    [MinSamplesFieldId, ResetDistanceFieldId, MaxSampleAgeFieldId,
                        MaxPredictionDistanceFieldId, MaxFitErrorFieldId],
                    Order: 1, Collapsible: true, InitiallyExpanded: false)
            ],
            SummaryItems:
            [
                new StepSummaryItemDescriptor(PointsSourceFieldId),
                new StepSummaryItemDescriptor(PredictionModelFieldId)
            ],
            DetailFieldIds:
            [PointsSourceFieldId, PredictionModelFieldId, MinimumConfidenceFieldId, MinSamplesFieldId,
                ResetDistanceFieldId, MaxSampleAgeFieldId, MaxPredictionDistanceFieldId, MaxFitErrorFieldId]));

    public override PredictMovementStep CreateDefaultStep() => new()
    {
        Settings = new PredictMovementSettings
        {
            MinSamples = 3,
            PredictionMs = 0,
            ResetDistanceThreshold = 250,
            MaxSampleAgeMs = 500,
            PredictionModel = "Automatic",
            TimeBasis = "Execution",
            MaxPredictionDistance = 500,
            MaxFitError = 75,
            MinimumConfidence = 0.15
        }
    };

    protected override StepDraft Read(PredictMovementStep step)
    {
        var settings = step.Settings;
        var draft = new StepDraft(Descriptor.TypeId);
        draft.Values[PointsSourceFieldId] = JsonSerializer.SerializeToNode(settings.PointsSource);
        draft.Values[PredictionModelFieldId] = JsonValue.Create(settings.PredictionModel);
        draft.Values[MinimumConfidenceFieldId] = JsonValue.Create(settings.MinimumConfidence);
        draft.Values[MinSamplesFieldId] = JsonValue.Create(settings.MinSamples);
        draft.Values[PredictionMsFieldId] = JsonValue.Create(settings.PredictionMs);
        draft.Values[ResetDistanceFieldId] = JsonValue.Create(settings.ResetDistanceThreshold);
        draft.Values[MaxSampleAgeFieldId] = JsonValue.Create(settings.MaxSampleAgeMs);
        draft.Values[TimeBasisFieldId] = JsonValue.Create(settings.TimeBasis);
        draft.Values[MaxPredictionDistanceFieldId] = JsonValue.Create(settings.MaxPredictionDistance);
        draft.Values[MaxFitErrorFieldId] = JsonValue.Create(settings.MaxFitError);
        return draft;
    }

    protected override void Apply(StepDraft draft, PredictMovementStep step)
    {
        var settings = step.Settings;
        settings.PointsSource = DefinitionValueReader.Binding(draft, PointsSourceFieldId);
        settings.PredictionModel = DefinitionValueReader.String(draft, PredictionModelFieldId);
        settings.MinimumConfidence = DefinitionValueReader.Number(draft, MinimumConfidenceFieldId);
        settings.MinSamples = DefinitionValueReader.Integer(draft, MinSamplesFieldId);
        settings.PredictionMs = DefinitionValueReader.Integer(draft, PredictionMsFieldId);
        settings.ResetDistanceThreshold = DefinitionValueReader.Number(draft, ResetDistanceFieldId);
        settings.MaxSampleAgeMs = DefinitionValueReader.Integer(draft, MaxSampleAgeFieldId);
        settings.TimeBasis = DefinitionValueReader.String(draft, TimeBasisFieldId);
        settings.MaxPredictionDistance = DefinitionValueReader.Number(draft, MaxPredictionDistanceFieldId);
        settings.MaxFitError = DefinitionValueReader.Number(draft, MaxFitErrorFieldId);
    }

    protected override IReadOnlyList<StepValidationIssue> ValidateCustomDraft(StepDraft draft)
    {
        return [];
    }
}
