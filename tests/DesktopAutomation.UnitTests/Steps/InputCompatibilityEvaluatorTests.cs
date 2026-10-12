using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Tests.Steps;

public sealed class InputCompatibilityEvaluatorTests
{
    [Theory]
    [InlineData(ResultValueKind.Number, ResultCardinality.Single)]
    [InlineData(ResultValueKind.Boolean, ResultCardinality.OptionalSingle)]
    [InlineData(ResultValueKind.ResultObject, ResultCardinality.Single)]
    [InlineData(ResultValueKind.Point, ResultCardinality.Collection)]
    [InlineData(ResultValueKind.Text, ResultCardinality.Collection)]
    public void DisplayText_ProvidesAnExecutableConversionForDisplayableSources(ResultValueKind kind, ResultCardinality cardinality)
    {
        var contract = InputContractRegistry.ForField(new("text", "", StepValueKind.Text));
        var result = InputCompatibilityEvaluator.Evaluate(contract, ValueProviderIds.JobVariable,
            new("value", "value", kind, Cardinality: cardinality));
        Assert.True(result.IsAccepted);
        Assert.Equal(InputConversion.DisplayText, result.Plan!.Conversion);
        var value = JsonNode.Parse("[1,2]");
        Assert.Equal(TextValueFormatter.Format(value), InputValueConverter.Convert(value, result.Plan)!.GetValue<string>());
        Assert.Equal("[1,2]", value!.ToJsonString());
    }

    [Theory]
    [InlineData(StepValueKind.Integer, ResultValueKind.Text)]
    [InlineData(StepValueKind.DirectoryPath, ResultValueKind.Number)]
    [InlineData(StepValueKind.Text, ResultValueKind.Image)]
    public void TypedInputs_RejectUnapprovedConversions(StepValueKind target, ResultValueKind source)
    {
        var result = InputCompatibilityEvaluator.Evaluate(InputContractRegistry.ForField(new("field", "", target)),
            ValueProviderIds.JobVariable, new("value", "value", source));
        Assert.False(result.IsAccepted);
        Assert.Null(result.Plan);
        Assert.Equal("InputContract.ShapeNotAllowed", result.DiagnosticCode);
    }

    [Fact]
    public void LegacyColor_IsReadableButNeverOfferedForNewSelection()
    {
        var contract = InputContractRegistry.ResolveMember(InputSchemaIds.VisualOverlay, "text_results.0.font_color");
        var source = new ResultPropertyDescriptor("value", "value", ResultValueKind.Text);
        Assert.False(InputCompatibilityEvaluator.Evaluate(contract, ValueProviderIds.JobVariable, source).IsAccepted);
        var existing = InputCompatibilityEvaluator.Evaluate(contract, ValueProviderIds.JobVariable, source,
            InputCompatibilityProfile.ExistingBinding);
        Assert.Equal(InputCompatibilityStatus.AcceptedLegacy, existing.Status);
        Assert.Equal(InputConversion.LegacyColor, existing.Plan!.Conversion);
        Assert.Equal("#123456", InputValueConverter.Convert(JsonValue.Create("#123456"), existing.Plan)!.GetValue<string>());
        Assert.Throws<InvalidOperationException>(() => InputValueConverter.Convert(JsonValue.Create("invalid"), existing.Plan));
    }

    [Fact]
    public void OptionalSingle_CompatibilityDoesNotInventAMissingValue()
    {
        var contract = InputContractRegistry.ForField(new("count", "", StepValueKind.Integer, Required: true));
        var result = InputCompatibilityEvaluator.Evaluate(contract, ValueProviderIds.JobVariable,
            new("count", "count", ResultValueKind.Integer, Cardinality: ResultCardinality.OptionalSingle));
        Assert.True(result.IsAccepted);
        Assert.True(contract.Required);
        Assert.Equal(MissingValuePolicy.FailStep, contract.MissingValuePolicy);
        Assert.Null(InputValueConverter.Convert(null, result.Plan!));
    }

    [Fact]
    public void ProviderLegacyProfile_DoesNotRelaxSourceTypeAndDoesNotApplyToNewSelections()
    {
        var node = new InputContractNode("count", ResultValueKind.Integer,
            [new(ResultValueKind.Integer, ResultCardinality.Single)], new HashSet<string>(), true);
        var integer = new ResultPropertyDescriptor("count", "count", ResultValueKind.Integer);
        Assert.False(InputCompatibilityEvaluator.Evaluate(node, ValueProviderIds.JobVariable, integer,
            InputCompatibilityProfile.NewSelection, isLegacyDirectValue: true).IsAccepted);
        Assert.Equal(InputCompatibilityStatus.AcceptedLegacy,
            InputCompatibilityEvaluator.Evaluate(node, ValueProviderIds.JobVariable, integer,
                InputCompatibilityProfile.ExistingBinding, isLegacyDirectValue: true).Status);
        Assert.False(InputCompatibilityEvaluator.Evaluate(node, ValueProviderIds.JobVariable,
            integer with { DataType = ResultValueKind.Text }, InputCompatibilityProfile.ExistingBinding,
            isLegacyDirectValue: true).IsAccepted);
    }

    [Fact]
    public void CanonicalPolicy_IsIndependentOfCallerMutationAndSupportsTransportRoundTrips()
    {
        var providers = new HashSet<string> { ValueProviderIds.JobVariable };
        var cardinalities = new[] { ResultCardinality.Single };
        var shapes = new[] { new AcceptedResultShape(ResultValueKind.Integer, cardinalities) };
        var node = new InputContractNode("count", ResultValueKind.Integer, shapes, providers);
        providers.Add(ValueProviderIds.Secret);
        cardinalities[0] = ResultCardinality.Collection;
        shapes[0] = new(ResultValueKind.Image);
        var restored = JsonSerializer.Deserialize<InputContractNode>(JsonSerializer.Serialize(node))!;
        Assert.False(InputCompatibilityEvaluator.AllowsProvider(restored, ValueProviderIds.Secret));
        Assert.True(InputCompatibilityEvaluator.Evaluate(restored, ValueProviderIds.JobVariable,
            new("count", "count", ResultValueKind.Integer)).IsAccepted);
        Assert.False(InputCompatibilityEvaluator.Evaluate(restored, ValueProviderIds.JobVariable,
            new("count", "count", ResultValueKind.Integer, Cardinality: ResultCardinality.Collection)).IsAccepted);
    }

    [Theory]
    [InlineData("text_results.-1.font_color")]
    [InlineData("text_results.0.unknown")]
    [InlineData("text_results.font_color")]
    public void SchemaPaths_RejectUnknownMembersAndInvalidIndices(string path) =>
        Assert.Throws<InvalidOperationException>(() => InputContractRegistry.ResolveMember(InputSchemaIds.VisualOverlay, path));

    [Fact]
    public void SchemaResolution_DoesNotDependOnTheEditorHint()
    {
        var field = new ShowImageStepDefinition().Descriptor.Fields.Single(field => field.InputSchemaId is not null);
        var original = InputContractRegistry.ResolvePath(typeof(ShowImageStep), field, "overlay.text_results.0.font_color");
        var otherEditor = InputContractRegistry.ResolvePath(typeof(ShowImageStep), field with { EditorHint = "other-editor" },
            "overlay.text_results.0.font_color");
        Assert.Same(original, otherEditor);
        Assert.Empty(InputContractRegistry.ValidateSchemas());
    }

    [Fact]
    public void ComparisonEnums_RequireTheDeclaredIdentity()
    {
        var left = new ResultPropertyDescriptor("value", "value", ResultValueKind.Enum, EnumTypeName: "choice-A");
        Assert.True(ConditionRules.CanCompare(left, left with { Name = "other" }));
        Assert.False(ConditionRules.CanCompare(left, left with { EnumTypeName = "choice-B" }));
        Assert.False(ConditionRules.CanCompare(left, left with { EnumTypeName = null }));
    }

    [Fact]
    public void DerivedPolicy_FreezeCollectionsAndConstraintsSuppliedByTheCaller()
    {
        var node = InputContractRegistry.ForField(new("text", "", StepValueKind.Text));
        var providers = new HashSet<string> { ValueProviderIds.JobVariable };
        var shapes = new List<AcceptedResultShape> { new(ResultValueKind.Integer, ResultCardinality.Single) };
        var options = new List<string> { "approved" };
        var derived = node with
        {
            AllowedProviderIds = providers,
            AcceptedShapes = shapes,
            Constraints = new(AllowedValues: options)
        };
        providers.Add(ValueProviderIds.Secret);
        shapes.Clear();
        options.Add("unapproved");
        Assert.False(InputCompatibilityEvaluator.AllowsProvider(derived, ValueProviderIds.Secret));
        Assert.Single(derived.AcceptedShapes);
        Assert.Equal(["approved"], derived.Constraints!.AllowedValues);
        Assert.True(node.AcceptedShapes.Count > 1);
    }

    [Theory]
    [InlineData(long.MaxValue)]
    [InlineData(long.MinValue)]
    [InlineData(2147483648L)]
    public void NumberValues_UseTheSameReaderBeforeAndAfterStorageWithoutRelaxingIntegerBounds(long value)
    {
        var number = new JobVariable { ValueKind = ResultValueKind.Number, Value = JsonValue.Create(value) };
        var restored = JsonSerializer.Deserialize<JobVariable>(JsonSerializer.Serialize(number))!;
        Assert.True(JobVariableValueRules.IsValid(number));
        Assert.True(JobVariableValueRules.IsValid(restored));
        Assert.Equal(JobVariableRuntimeValueReader.Read(number), JobVariableRuntimeValueReader.Read(restored));
        number.ValueKind = ResultValueKind.Integer;
        restored.ValueKind = ResultValueKind.Integer;
        Assert.False(JobVariableValueRules.IsValid(number));
        Assert.False(JobVariableValueRules.IsValid(restored));
    }
}
