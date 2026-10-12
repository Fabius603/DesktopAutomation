using System.Text.Json;
using TaskAutomation.Contracts.Steps;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Tests.Steps;

public sealed class CanonicalInputContractTests
{
    [Fact]
    public void EveryBuiltInInput_HasAResolvableSemanticContractIndependentOfPresentation()
    {
        Assert.Empty(InputContractRegistry.ValidateSchemas());
        foreach (var definition in BuiltInStepDefinitions.Instance.Definitions)
        {
            foreach (var field in definition.Descriptor.Fields)
            {
                var contract = InputContractRegistry.Resolve(definition.StepType, field);
                var otherEditor = InputContractRegistry.Resolve(definition.StepType, field with { EditorHint = "other" });
                Assert.Equal(contract.TargetKind, otherEditor.TargetKind);
                Assert.Equal(contract.SchemaId, otherEditor.SchemaId);
                Assert.Equal(contract.Conversion, otherEditor.Conversion);
                foreach (var kind in Enum.GetValues<ResultValueKind>())
                    foreach (var cardinality in Enum.GetValues<ResultCardinality>())
                        Assert.Equal(InputCompatibilityEvaluator.EvaluateShape(contract, kind, cardinality),
                            InputCompatibilityEvaluator.EvaluateShape(otherEditor, kind, cardinality));
            }
        }
    }

    [Fact]
    public void AllSchemas_KeepMemberPoliciesAcrossTransportRoundTrip()
    {
        foreach (var id in InputContractRegistry.SchemaIds)
        {
            Assert.True(InputContractRegistry.TryGetSchema(id, out var schema));
            var restored = JsonSerializer.Deserialize<InputContractSchema>(JsonSerializer.Serialize(schema))!;
            Assert.Equal(schema.Id, restored.Id);
            Assert.Equal(schema.ItemSchemaId, restored.ItemSchemaId);
            foreach (var (name, node) in schema.Members)
            {
                Assert.Equal(node.TargetKind, restored.Members[name].TargetKind);
                foreach (var kind in Enum.GetValues<ResultValueKind>())
                    foreach (var cardinality in Enum.GetValues<ResultCardinality>())
                        foreach (var profile in Enum.GetValues<InputCompatibilityProfile>())
                            Assert.Equal(InputCompatibilityEvaluator.EvaluateShape(node, kind, cardinality, profile),
                                InputCompatibilityEvaluator.EvaluateShape(restored.Members[name], kind, cardinality, profile));
            }
        }
    }

    [Fact]
    public void OverlayResultReferences_UseTheDeclaredRuntimeTextContract()
    {
        var definition = new ShowImageStepDefinition();
        var field = definition.Descriptor.Fields.Single(field => field.InputSchemaId == InputSchemaIds.VisualOverlay);
        var reference = InputContractRegistry.ResolvePath(definition.StepType, field, "overlay.text_results.0.result");
        var runtime = Assert.Single(InputContractRegistry.Roots(definition.StepType), node => node.Key == "text");
        Assert.Equal(runtime.Conversion, reference.Conversion);
        foreach (var kind in Enum.GetValues<ResultValueKind>())
            foreach (var cardinality in Enum.GetValues<ResultCardinality>())
                Assert.Equal(InputCompatibilityEvaluator.EvaluateShape(runtime, kind, cardinality),
                    InputCompatibilityEvaluator.EvaluateShape(reference, kind, cardinality));
    }
}
