using System.Text.Json.Nodes;
using System.Text.Json;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Tests.Jobs;

public sealed class JobVariableInputMigrationTests
{
    [Fact]
    public void Migrate_CreatesTypedReferenceForEveryFieldAndIsIdempotent()
    {
        foreach (var definition in BuiltInStepDefinitions.Instance.Definitions)
        {
            var step = definition.CreateDefault();
            var job = new Job { Name = definition.Descriptor.TypeId, Steps = [step], FormatVersion = 1 };

            Assert.True(JobVariableInputMigration.Migrate(job));

            Assert.Equal(Job.CurrentFormatVersion, job.FormatVersion);
            Assert.Equal(definition.Descriptor.Fields.Count, step.Inputs.Count);
            Assert.All(definition.Descriptor.Fields, field =>
            {
                var reference = Assert.Contains(field.Id, step.Inputs);
                Assert.Equal(ValueProviderIds.JobVariable, reference.ProviderId);
                var variable = Assert.Single(job.Variables, candidate =>
                    candidate.Id.ToString("D") == reference.SourceId);
                if (field.ValueKind != TaskAutomation.Contracts.Steps.StepValueKind.ResultBinding)
                    Assert.Equal(JobVariableInputMigration.MapKind(field.ValueKind), variable.ValueKind);
            });
            var variableCount = job.Variables.Count;
            Assert.False(JobVariableInputMigration.Migrate(job));
            Assert.Equal(variableCount, job.Variables.Count);
        }
    }

    [Fact]
    public void Materialize_UsesReferencedVariableInsteadOfStoredLegacyValue()
    {
        var variable = new JobVariable
        {
            Name = "Wartezeit",
            ValueKind = ResultValueKind.Integer,
            Value = JsonValue.Create(2750)
        };
        var step = new TimeoutStep { Settings = new TimeoutSettings { DelayMs = 1000 } };
        step.Inputs[TimeoutStepDefinition.DelayFieldId] = new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = variable.Id.ToString("D")
        };
        var results = new JobResultStore([variable]);

        var materialized = Assert.IsType<TimeoutStep>(StepInputMaterializer.Materialize(step, results));

        Assert.Equal(2750, materialized.Settings.DelayMs);
        Assert.Equal(1000, step.Settings.DelayMs);
    }

    [Fact]
    public void ReferenceBackedStep_SerializesWithoutLiteralSettings()
    {
        var step = new TimeoutStep { Settings = new TimeoutSettings { DelayMs = 9876 } };
        var job = new Job { Name = "Nur Referenzen", Steps = [step] };
        JobVariableInputMigration.Migrate(job);
        var options = new JsonSerializerOptions();
        JobJsonSerialization.Configure(options);

        var json = JsonSerializer.Serialize(job, options);

        Assert.Contains("\"inputs\"", json);
        Assert.DoesNotContain("\"settings\"", json);
    }
}
