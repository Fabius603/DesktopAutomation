using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Jobs;

public sealed class TextInputValueTests
{
    [Theory]
    [InlineData("title", ResultValueKind.Integer, ResultCardinality.Single, "42", "42")]
    [InlineData("question", ResultValueKind.Boolean, ResultCardinality.Single, "true", "True")]
    [InlineData("description", ResultValueKind.ResultObject, ResultCardinality.Single, "{\"count\":2}", "{\"count\":2}")]
    [InlineData("question", ResultValueKind.Text, ResultCardinality.Collection, "[\"first\",\"second\"]", "first\nsecond")]
    public void TextFields_ValidateAndMaterializeOtherValueKindsWithoutMutatingStoredValues(
        string field, ResultValueKind kind, ResultCardinality cardinality, string json, string expected)
    {
        var step = Choice();
        var variable = new JobVariable { ValueKind = kind, Cardinality = cardinality, Value = JsonNode.Parse(json) };
        step.Inputs[field] = Reference(variable);
        var validation = JobValidation.ValidateCandidate([], step, variables: [variable]);
        Assert.True(validation.IsValid, validation.Error);
        var materialized = Assert.IsType<UserChoiceStep>(StepInputMaterializer.Materialize(step, new JobResultStore([variable])));
        var draft = new UserChoiceStepDefinition().CreateDraft(materialized);
        Assert.Equal(expected.Replace("\n", Environment.NewLine), draft.Values[field]!.GetValue<string>());
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(json), variable.Value));
        Assert.Equal(variable.Id.ToString("D"), step.Inputs[field].SourceId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedAnswerText_ConvertsValuesInCurrentAndLegacyBindingTrees(bool legacy)
    {
        var step = Choice();
        var value = new JobVariable { ValueKind = ResultValueKind.Integer, Value = JsonValue.Create(7) };
        const string path = "options.0.label";
        if (legacy) step.Inputs[path] = Reference(value);
        else ValueBindingTree.Set(step.Inputs, path, Reference(value));
        ValueBindingTree.ApplySchemas(step.Inputs, new UserChoiceStepDefinition().Descriptor.Fields);
        var validation = JobValidation.ValidateCandidate([], step, variables: [value]);
        Assert.True(validation.IsValid, validation.Error);
        var materialized = Assert.IsType<UserChoiceStep>(StepInputMaterializer.Materialize(step, new JobResultStore([value])));
        Assert.Equal("7", materialized.Settings.Options[0].Label);
    }

    [Fact]
    public async Task TextDisplayAndOverlay_ShowTheEntireCollectionUsingTheSameConversion()
    {
        var value = new JobVariable
        {
            ValueKind = ResultValueKind.Boolean,
            Cardinality = ResultCardinality.Collection,
            Value = JsonNode.Parse("[true,false]")
        };
        var binding = Reference(value);
        var store = new JobResultStore([value]);
        var overlay = new RecordingDesktopResultOverlay();
        var step = new ShowTextStep { Settings = new() { TextSource = ShowTextSource.TaskResult, TextResult = binding } };
        Assert.True(JobValidation.ValidateCandidate([], step, variables: [value]).IsValid);
        await new ShowTextStepHandler().ExecuteAsync(step,
            new PipelineContextStub([value]) { DesktopResultOverlay = overlay }, default);
        var resolved = VisualOverlayResolver.Resolve(store,
            new VisualOverlaySettings { TextResults = [new() { Result = binding }] }, null, NullLogger.Instance);
        var expected = "True" + Environment.NewLine + "False";
        Assert.Equal(expected, Assert.Single(overlay.TextCalls).Text);
        Assert.Equal(expected, Assert.Single(resolved.Texts).Text);
    }

    [Theory]
    [InlineData("de-DE", "12,5")]
    [InlineData("en-US", "12.5")]
    public void Numbers_UseTheSelectedCultureForTypedAndJsonValues(string culture, string expected)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            Assert.Equal(expected, TextValueFormatter.Format(12.5));
            Assert.Equal(expected, TextValueFormatter.Format(JsonNode.Parse("12.5")));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public void TextComparisons_AcceptDisplayableValuesWhileNumericComparisonsStayTyped()
    {
        var text = new ResultPropertyDescriptor("text", "Text", ResultValueKind.Text);
        var number = new ResultPropertyDescriptor("number", "Number", ResultValueKind.Integer);
        var collection = new ResultPropertyDescriptor("list", "List", ResultValueKind.Integer, Cardinality: ResultCardinality.Collection);
        Assert.True(ConditionRules.AreComparisonSourcesCompatible(text, number, false, null));
        Assert.True(ConditionRules.AreComparisonSourcesCompatible(text, collection, false, null));
        Assert.False(ConditionRules.AreComparisonSourcesCompatible(number, text, false, null));
        Assert.Equal("42", ConditionRules.FormatComparisonValue(text, 42));
    }

    private static ResultBinding Reference(JobVariable value) => new()
    { ProviderId = ValueProviderIds.JobVariable, SourceId = value.Id.ToString("D") };

    private static UserChoiceStep Choice()
    {
        var step = new UserChoiceStepDefinition().CreateDefaultStep();
        step.Settings.Options[0].Label = "First";
        step.Settings.Options[1].Label = "Second";
        return step;
    }
}
