using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Tests.Jobs;

public sealed class NestedInputCompatibilityTests
{
    [Theory]
    [InlineData("#123456", true)]
    [InlineData("not-a-color", false)]
    public void ExistingTextColorBinding_UsesTheSameValidityAtAuthoringAndRuntime(string token, bool valid)
    {
        var text = new JobVariable { ValueKind = ResultValueKind.Text, Value = JsonValue.Create("Status") };
        var color = new JobVariable { ValueKind = ResultValueKind.Text, Value = JsonValue.Create(token) };
        var step = new ShowOnDesktopStep { Settings = new() { Overlay = new() { TextResults = [new() { Result = Reference(text) }] } } };
        ValueBindingTree.Set(step.Inputs, "overlay.text_results.0.font_color", Reference(color));
        ValueBindingTree.ApplySchemas(step.Inputs, new ShowOnDesktopStepDefinition().Descriptor.Fields);
        var validation = JobValidation.ValidateCandidate([], step, variables: [text, color]);
        Assert.Equal(valid, validation.IsValid);
        if (valid)
        {
            var materialized = Assert.IsType<ShowOnDesktopStep>(StepInputMaterializer.Materialize(step, new JobResultStore([text, color])));
            Assert.Equal(token, Assert.Single(materialized.Settings.Overlay.TextResults).FontColor);
        }
        else Assert.Throws<InvalidOperationException>(() => StepInputMaterializer.Materialize(step, new JobResultStore([text, color])));
        Assert.Equal(token, color.Value!.GetValue<string>());
    }

    [Theory]
    [InlineData(24f)]
    [InlineData(1f)]
    [InlineData(0.1f)]
    public void FiniteFloatNumber_ValidatesAndResolvesIdenticallyBeforeAndAfterSerialization(float number)
    {
        var variable = new LocalValue { ValueKind = ResultValueKind.Number, Value = JsonValue.Create(number) };
        Assert.True(JobVariableValueRules.IsValid(variable));
        var restored = JsonSerializer.Deserialize<LocalValue>(JsonSerializer.Serialize(variable))!;
        Assert.True(JobVariableValueRules.IsValid(restored));
        Assert.Equal(JobVariableRuntimeValueReader.Read(variable), JobVariableRuntimeValueReader.Read(restored));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void NonFiniteFloatNumber_RemainsInvalid(float number)
    {
        var variable = new LocalValue { ValueKind = ResultValueKind.Number, Value = JsonValue.Create(number) };
        Assert.False(JobVariableValueRules.IsValid(variable));
        Assert.Throws<InvalidOperationException>(() => JobVariableRuntimeValueReader.Read(variable));
    }

    [Fact]
    public void AvailableOptionalSingleCoordinate_IsValidAndMaterialized()
    {
        var value = new JobVariable
        {
            ValueKind = ResultValueKind.Integer,
            Cardinality = ResultCardinality.OptionalSingle,
            Value = JsonValue.Create(17)
        };
        var step = new UserChoiceStepDefinition().CreateDefaultStep();
        step.Settings.Options[0].Label = "First";
        step.Settings.Options[1].Label = "Second";
        step.Inputs[UserChoiceStepDefinition.DesktopIndexFieldId] = Reference(value);
        Assert.True(JobValidation.ValidateCandidate([], step, variables: [value]).IsValid);
        var materialized = Assert.IsType<UserChoiceStep>(StepInputMaterializer.Materialize(step, new JobResultStore([value])));
        Assert.Equal(17, materialized.Settings.DesktopIndex);
    }

    private static ResultBinding Reference(JobVariable value) => new()
    { ProviderId = ValueProviderIds.JobVariable, SourceId = value.Id.ToString("D") };
}
