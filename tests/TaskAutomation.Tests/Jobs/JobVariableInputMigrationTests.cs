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
            var migratableFields = definition.Descriptor.Fields.Where(field =>
                field.ValueKind != TaskAutomation.Contracts.Steps.StepValueKind.ResultBinding
                || StepInputContractRegistry.Resolve(definition.StepType, field).AllowsDirectValue).ToArray();
            Assert.Equal(migratableFields.Length, step.Inputs.Count);
            Assert.All(migratableFields, field =>
            {
                var reference = Assert.Contains(field.Id, step.Inputs);
                Assert.Equal(ValueProviderIds.JobVariable, reference.ProviderId);
                var variable = Assert.Single(job.Variables, candidate =>
                    candidate.Id.ToString("D") == reference.SourceId);
                Assert.Equal(JobVariableScope.StepValue, variable.Scope);
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
    public void Materialize_OverlaysReferencedCompositeSubValues()
    {
        var target = new JobVariable
        {
            Name = "Prozessziel",
            ValueKind = ResultValueKind.ResultObject,
            Value = JsonSerializer.SerializeToNode(new TaskAutomation.Contracts.Steps.StepProcessSelectorValue(
                null, "notepad", string.Empty, "Editor"))
        };
        var title = new JobVariable
        {
            Name = "Fenstertitel",
            ValueKind = ResultValueKind.Text,
            Value = JsonValue.Create("Bericht")
        };
        var step = new ActiveProcessStep();
        step.Inputs[ActiveProcessStepDefinition.ProcessTargetFieldId] = new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = target.Id.ToString("D")
        };
        step.Inputs[$"{ActiveProcessStepDefinition.ProcessTargetFieldId}.window_title_contains"] = new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = title.Id.ToString("D")
        };
        var results = new JobResultStore([target, title]);

        var materialized = Assert.IsType<ActiveProcessStep>(StepInputMaterializer.Materialize(step, results));

        Assert.Equal("notepad", materialized.Settings.Target.ProcessName);
        Assert.Equal("Bericht", materialized.Settings.Target.WindowTitleContains);
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

    [Fact]
    public void Migrate_MapsLegacyUnifiedFieldsWithoutLosingTheirValues()
    {
        var sourceBinding = ResultBinding.ForStepResult("source", "path");
        var fileSystem = new FileSystemOperationStep
        {
            Settings = new FileSystemOperationSettings
            {
                Operation = FileSystemOperation.Delete,
                SourceMode = FileSystemPathSource.TaskResult,
                SourceResult = sourceBinding
            }
        };
        var showText = new ShowTextStep
        {
            Settings = new ShowTextSettings
            {
                TextSource = ShowTextSource.ExplicitText,
                Text = "Legacy text"
            }
        };
        var dynamicRoi = new DynamicRoiStep
        {
            Settings = new DynamicRoiSettings { Padding = 23 }
        };
        var job = new Job { Steps = [fileSystem, showText, dynamicRoi], FormatVersion = 1 };

        Assert.True(JobVariableInputMigration.Migrate(job));

        Assert.Same(sourceBinding, fileSystem.Inputs[FileSystemOperationStepDefinition.SourcePathFieldId]);
        var textVariable = Assert.Single(job.Variables, variable =>
            variable.Id.ToString("D") == showText.Inputs[ShowTextStepDefinition.TextResultFieldId].SourceId);
        Assert.Equal("Legacy text", textVariable.Value!.GetValue<string>());
        var paddingVariable = Assert.Single(job.Variables, variable =>
            variable.Id.ToString("D") == dynamicRoi.Inputs[DynamicRoiStepDefinition.PaddingSourceFieldId].SourceId);
        Assert.Equal(23, paddingVariable.Value!.GetValue<int>());
    }
}
