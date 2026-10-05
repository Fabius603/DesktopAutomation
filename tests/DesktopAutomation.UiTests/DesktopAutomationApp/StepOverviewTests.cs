using System.Text.Json.Nodes;
using DesktopAutomationApp.Services.Jobs;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Tests.DesktopAutomationApp;

public sealed class StepOverviewTests
{
    [Fact]
    public void ValidationMessages_UseTheSameLocalizedFieldLabelsAsTheEditor()
    {
        var step = new IfStep();
        var result = JobValidation.ValidateJob(new Job { Steps = [step, new EndIfStep()] });
        var error = Assert.Single(result.Steps, item => ReferenceEquals(item.Step, step)).Error;
        var localized = global::DesktopAutomationApp.Localization.JobValidationErrorLocalizer.Localize(error, step);
        Assert.Equal(global::DesktopAutomationApp.Localization.Loc.Format("Ui.Step.Generated.Validation.Required",
            global::DesktopAutomationApp.Localization.Loc.Get("Ui.Step.Settings.Evaluation")), localized);
        Assert.DoesNotContain("StepValidation.", localized);
        Assert.DoesNotContain("conditions:", localized);
    }

    [Fact]
    public void StoredTextSummary_DisplaysTextWithoutJsonQuotationMarks()
    {
        var step = new ShowTextStep();
        var local = new LocalValue { ValueKind = ResultValueKind.Text, Value = JsonValue.Create("Fortsetzung bestätigt") };
        step.Inputs[ShowTextStepDefinition.TextResultFieldId] = new ResultBinding
        { ProviderId = ValueProviderIds.LocalValue, SourceId = local.Id.ToString() };
        var summary = new JobStepDetailsProvider().GetSummary(step, new[] { step }, new[] { local });
        Assert.Contains("Fortsetzung bestätigt", summary);
        Assert.DoesNotContain("\"", summary);
    }

    public static IEnumerable<object[]> StepTypes => BuiltInStepDefinitions.Instance.Definitions
        .Select(definition => new object[] { definition.Descriptor.TypeId });

    [Theory]
    [MemberData(nameof(StepTypes))]
    public void StoredSummaryValues_RenderLikeLegacySettings(string typeId)
    {
        Assert.True(BuiltInStepDefinitions.Instance.TryGetByTypeId(typeId, out var definition));
        var step = definition.CreateDefault();
        var draft = definition.CreateDraft(step);
        var provider = new JobStepDetailsProvider();
        var expected = provider.GetSummary(step, new[] { step });
        var locals = new List<LocalValue>();
        foreach (var field in definition.Descriptor.Fields.Where(field =>
                     definition.Descriptor.Presentation.SummaryItems.Any(item => item.FieldId == field.Id)
                     && field.ValueKind != StepValueKind.ResultBinding))
        {
            if (draft.Values.GetValueOrDefault(field.Id) is not { } value) continue;
            var local = new LocalValue
            {
                ValueKind = JobVariableInputMigration.MapKind(field),
                Cardinality = field.ValueKind == StepValueKind.Collection ? ResultCardinality.Collection : ResultCardinality.Single,
                Value = value.DeepClone()
            };
            locals.Add(local);
            step.Inputs[field.Id] = new ResultBinding { ProviderId = ValueProviderIds.LocalValue, SourceId = local.Id.ToString() };
        }
        Assert.Equal(expected, provider.GetSummary(step, new[] { step }, locals));
    }

    [Fact]
    public void RuntimeAndMissingSources_DoNotPretendToBeStoredValues()
    {
        var step = new TimeoutStep();
        step.Inputs[TimeoutStepDefinition.DelayFieldId] = new ResultBinding
        { ProviderId = ValueProviderIds.LocalValue, SourceId = Guid.NewGuid().ToString() };
        var summary = new JobStepDetailsProvider().GetSummary(step, new[] { step }, []);
        Assert.DoesNotContain("1000", summary);
        Assert.NotEmpty(summary);
    }

    [Fact]
    public void SecretSummary_DoesNotDiscloseItsIdentifier()
    {
        var step = new TimeoutStep();
        var id = Guid.NewGuid().ToString();
        step.Inputs[TimeoutStepDefinition.DelayFieldId] = new ResultBinding
        { ProviderId = ValueProviderIds.Secret, SourceId = id };
        Assert.DoesNotContain(id, new JobStepDetailsProvider().GetSummary(step, new[] { step }));
    }
    [Fact]
    public void ConditionSummary_ShowsAnswerLabelAndVisibleSourcePositionAfterNestedClosures()
    {
        var choice = new UserChoiceStep();
        choice.Settings.Options = [new() { Label = "Fortsetzen" }];
        var conditional = new IfStep();
        conditional.Settings.Conditions = [new StepCondition { ProviderId = ValueProviderIds.StepResult,
            SourceStepId = choice.Id.ToUpperInvariant(), PropertyPath = "SelectedOptionId", Operator = ConditionOperator.Equals,
            ComparisonValue = choice.Settings.Options[0].Id }];
        JobStep[] steps = [new IfStep(), new TimeoutStep(), new EndIfStep(), choice, conditional, new EndIfStep()];
        var summary = new JobStepDetailsProvider().GetSummary(conditional, steps);
        Assert.Contains("3", summary);
        Assert.Contains("Fortsetzen", summary);
        Assert.DoesNotContain(choice.Id, summary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(choice.Settings.Options[0].Id, summary);
        Assert.DoesNotContain("→", summary);
    }

    [Fact]
    public void MissingConditionSource_DoesNotExposeAnInternalIdentifier()
    {
        var id = Guid.NewGuid().ToString();
        var conditional = new IfStep();
        conditional.Settings.Conditions = [new StepCondition { SourceStepId = id, PropertyPath = "SelectedOptionId",
            ComparisonValue = "value" }];
        var summary = new JobStepDetailsProvider().GetSummary(conditional, new[] { conditional });
        Assert.DoesNotContain(id, summary);
        Assert.NotEmpty(summary);
    }

    [Fact]
    public void ChoiceSummary_ShowsOptionCountInsteadOfSerializedOptions()
    {
        var choice = new UserChoiceStep();
        choice.Settings.Question = "Fortsetzen oder Abbrechen";
        choice.Settings.Options = [new() { Label = "Fortsetzen" }, new() { Label = "Abbrechen" }];
        var summary = new JobStepDetailsProvider().GetSummary(choice, new[] { choice });
        Assert.Contains(choice.Settings.Question, summary);
        Assert.Contains("2", summary);
        Assert.DoesNotContain(choice.Settings.Options[0].Id, summary);
        Assert.DoesNotContain("[", summary);
    }

    [Fact]
    public void TextSummary_IsSingleLineAndClipsLongText()
    {
        var step = new ShowTextStep();
        var local = new LocalValue { ValueKind = ResultValueKind.Text, Value = JsonValue.Create("First line\n" + new string('x', 200)) };
        step.Inputs[ShowTextStepDefinition.TextResultFieldId] = new ResultBinding
        { ProviderId = ValueProviderIds.LocalValue, SourceId = local.Id.ToString() };
        var summary = new JobStepDetailsProvider().GetSummary(step, new[] { step }, new[] { local });
        Assert.DoesNotContain("\n", summary);
        Assert.Contains("…", summary);
        Assert.True(summary.Length < 180);
    }

    [Fact]
    public void CollapsedSummary_CountsVisibleStepPositionsAndOmitsEmptyDiagnostics()
    {
        var outer = new IfStep();
        JobStep[] steps = [outer, new IfStep(), new TimeoutStep(), new ElseStep(), new TimeoutStep(), new EndIfStep(), new EndIfStep()];
        var converter = new global::DesktopAutomationApp.Converters.StepOverviewConverter();
        object[] values = [outer, steps, 0, Array.Empty<JobVariable>(), Array.Empty<LocalValue>(), new[] { outer.Id }, steps];
        var summary = Assert.IsType<string>(converter.Convert(values, typeof(string), "summary", System.Globalization.CultureInfo.InvariantCulture));
        Assert.Contains(global::DesktopAutomationApp.Localization.Loc.Format("Ui.Job.Steps.Summary.Children", 4), summary);
        Assert.DoesNotContain(global::DesktopAutomationApp.Localization.Loc.Format("Ui.Job.Steps.Summary.Problems", 0), summary);
        Assert.DoesNotContain(global::DesktopAutomationApp.Localization.Loc.Format("Ui.Job.Steps.Summary.Breakpoints", 0), summary);
    }

    [Fact]
    public void ResultSourceNumber_MatchesGutterAcrossConditionAndBranchMarkers()
    {
        var source = new UserChoiceStep();
        JobStep[] steps = [new IfStep(), new TimeoutStep(), new ElseStep(), new TimeoutStep(), new EndIfStep(), source];
        var converter = new global::DesktopAutomationApp.Converters.StepNumberConverter();
        object[] values = [source, steps, 0];
        Assert.Equal("5", converter.Convert(values, typeof(string), "position", System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(5, global::DesktopAutomationApp.Localization.StepLocalization.DisplayNumber(steps, source));
        Assert.Equal("5.\u00A0", converter.Convert(values, typeof(string), "number", System.Globalization.CultureInfo.InvariantCulture));
        Assert.Contains("5", global::DesktopAutomationApp.Localization.StepLocalization.ResultStepName(source, steps));
        var copy = new UserChoiceStep { Id = source.Id.ToUpperInvariant() };
        Assert.Equal(5, global::DesktopAutomationApp.Localization.StepLocalization.DisplayNumber(steps, copy));
        Assert.Null(global::DesktopAutomationApp.Localization.StepLocalization.DisplayNumber(steps, steps[0]));
    }

}
