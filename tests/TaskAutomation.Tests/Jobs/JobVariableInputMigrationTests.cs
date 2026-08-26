using System.Text.Json.Nodes;
using System.Text.Json;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Tests.Jobs;

public sealed class JobVariableInputMigrationTests
{
    [Fact]
    public void StoredColorAndFilePathVariables_ReturnTheirTypedStringValues()
    {
        var color = new JobVariable
        {
            ValueKind = ResultValueKind.Color,
            Value = JsonValue.Create("#12ABEF")
        };
        var file = new JobVariable
        {
            ValueKind = ResultValueKind.FilePath,
            Value = JsonValue.Create(@"C:\Data\input.txt")
        };
        var store = new JobResultStore([color, file]);

        var colorResult = store.ReadProvider(ValueProviderIds.JobVariable, color.Id.ToString("D"));
        var fileResult = store.ReadProvider(ValueProviderIds.JobVariable, file.Id.ToString("D"));

        Assert.True(colorResult.IsSuccess);
        Assert.Equal("#12ABEF", colorResult.Value);
        Assert.Equal(ResultValueKind.Color, colorResult.Descriptor?.ValueKind);
        Assert.True(fileResult.IsSuccess);
        Assert.Equal(@"C:\Data\input.txt", fileResult.Value);
        Assert.Equal(ResultValueKind.FilePath, fileResult.Descriptor?.ValueKind);
    }

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
                Assert.Equal(ValueProviderIds.LocalValue, reference.ProviderId);
                var variable = Assert.Single(job.LocalValues, candidate =>
                    candidate.Id.ToString("D") == reference.SourceId);
                Assert.Equal(JobVariableScope.StepValue, variable.Scope);
                Assert.Equal(step.Id, variable.OwnerStepId);
                Assert.Equal(field.Id, variable.InputPath);
                if (field.ValueKind != TaskAutomation.Contracts.Steps.StepValueKind.ResultBinding)
                    Assert.Equal(JobVariableInputMigration.MapKind(field), variable.ValueKind);
            });
            var variableCount = job.LocalValues.Count;
            Assert.False(JobVariableInputMigration.Migrate(job));
            Assert.Equal(variableCount, job.LocalValues.Count);
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
    public void Materialize_RejectsInvalidResolvedValue()
    {
        var variable = new JobVariable
        {
            Name = "Ungültige Wartezeit",
            ValueKind = ResultValueKind.Integer,
            Value = JsonValue.Create(-1)
        };
        var step = new TimeoutStep();
        step.Inputs[TimeoutStepDefinition.DelayFieldId] = new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = variable.Id.ToString("D")
        };

        var error = Assert.Throws<InvalidOperationException>(() =>
            StepInputMaterializer.Materialize(step, new JobResultStore([variable])));

        Assert.Contains(TimeoutStepDefinition.DelayFieldId, error.Message, StringComparison.Ordinal);
        Assert.Contains("StepValidation.Minimum", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Materialize_UsesSelectedVariableSubproperty()
    {
        var rectangle = new JobVariable
        {
            ValueKind = ResultValueKind.Rectangle,
            Value = JsonSerializer.SerializeToNode(
                new TaskAutomation.Contracts.Geometry.PixelRegion(275, 10, 20, 30))
        };
        var step = new TimeoutStep();
        step.Inputs[TimeoutStepDefinition.DelayFieldId] = new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = rectangle.Id.ToString("D"),
            ValuePath = "X"
        };

        var materialized = Assert.IsType<TimeoutStep>(
            StepInputMaterializer.Materialize(step, new JobResultStore([rectangle])));

        Assert.Equal(275, materialized.Settings.DelayMs);
    }

    [Fact]
    public void Materialize_UsesPrivateLocalJobReference()
    {
        var jobId = Guid.NewGuid();
        var local = new LocalValue
        {
            OwnerStepId = "step",
            InputPath = JobExecutionStepDefinition.JobFieldId,
            ValueKind = ResultValueKind.JobReference,
            Value = JsonSerializer.SerializeToNode(new TaskAutomation.Contracts.Steps.StepReferenceValue(
                jobId.ToString("D"), "Child"))
        };
        var step = new JobExecutionStep { Id = "step" };
        step.Inputs[JobExecutionStepDefinition.JobFieldId] = new ResultBinding
        {
            ProviderId = ValueProviderIds.LocalValue,
            SourceId = local.Id.ToString("D")
        };
        var results = new JobResultStore(localValues: [local]);

        var materialized = Assert.IsType<JobExecutionStep>(StepInputMaterializer.Materialize(step, results));

        Assert.Equal(jobId, materialized.Settings.JobId);
        Assert.Equal("Child", materialized.Settings.JobName);
    }

    [Fact]
    public void Migrate_MovesLegacyStepValueOutOfVisibleJobVariables()
    {
        var legacy = new JobVariable
        {
            Scope = JobVariableScope.StepValue,
            ValueKind = ResultValueKind.Integer,
            Value = JsonValue.Create(1500)
        };
        var step = new TimeoutStep();
        step.Inputs[TimeoutStepDefinition.DelayFieldId] = new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = legacy.Id.ToString("D")
        };
        var job = new Job { FormatVersion = 2, Variables = [legacy], Steps = [step] };

        Assert.True(JobVariableInputMigration.Migrate(job));

        Assert.Empty(job.Variables);
        var local = Assert.Single(job.LocalValues);
        Assert.Equal(step.Id, local.OwnerStepId);
        Assert.Equal(TimeoutStepDefinition.DelayFieldId, local.InputPath);
        Assert.Equal(ValueProviderIds.LocalValue, step.Inputs[TimeoutStepDefinition.DelayFieldId].ProviderId);
    }

    [Fact]
    public void Materialize_ResolvesStructuredCompositeBinding()
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
        ValueBindingTree.Set(
            step.Inputs,
            $"{ActiveProcessStepDefinition.ProcessTargetFieldId}.window_title_contains",
            new ResultBinding
            {
                ProviderId = ValueProviderIds.JobVariable,
                SourceId = title.Id.ToString("D")
            });
        Assert.True(BuiltInStepDefinitions.Instance.TryGetByType(typeof(ActiveProcessStep), out var definition));
        ValueBindingTree.ApplySchemas(step.Inputs, definition.Descriptor.Fields);
        var results = new JobResultStore([target, title]);

        var materialized = Assert.IsType<ActiveProcessStep>(StepInputMaterializer.Materialize(step, results));

        var root = Assert.Single(step.Inputs).Value;
        Assert.Equal(ValueBindingSchemaRegistry.ProcessTarget, root.SchemaId);
        Assert.Equal(title.Id.ToString("D"), root.Members!["window_title_contains"].SourceId);
        Assert.Equal("notepad", materialized.Settings.Target.ProcessName);
        Assert.Equal("Bericht", materialized.Settings.Target.WindowTitleContains);
    }

    [Fact]
    public void Materialize_PreservesDirectSiblingsForStructuredOnlyBinding()
    {
        var title = new JobVariable
        {
            ValueKind = ResultValueKind.Text,
            Value = JsonValue.Create("Bericht")
        };
        var step = new ActiveProcessStep
        {
            Settings = new() { Target = new() { ProcessName = "notepad" } }
        };
        ValueBindingTree.Set(
            step.Inputs,
            $"{ActiveProcessStepDefinition.ProcessTargetFieldId}.window_title_contains",
            new ResultBinding
            {
                ProviderId = ValueProviderIds.JobVariable,
                SourceId = title.Id.ToString("D")
            });
        Assert.True(BuiltInStepDefinitions.Instance.TryGetByType(typeof(ActiveProcessStep), out var definition));
        ValueBindingTree.ApplySchemas(step.Inputs, definition.Descriptor.Fields);

        var materialized = Assert.IsType<ActiveProcessStep>(
            StepInputMaterializer.Materialize(step, new JobResultStore([title])));

        Assert.Equal("notepad", materialized.Settings.Target.ProcessName);
        Assert.Equal("Bericht", materialized.Settings.Target.WindowTitleContains);
    }

    [Fact]
    public void Materialize_ResolvesVariableBackedVisualOverlaySettings()
    {
        var textResult = new JobVariable
        {
            ValueKind = ResultValueKind.Text,
            Value = JsonValue.Create("Status")
        };
        var overlay = new LocalValue
        {
            ValueKind = ResultValueKind.ResultObject,
            Value = JsonSerializer.SerializeToNode(new VisualOverlaySettings
            {
                TextResults =
                [
                    new TextResultOverlaySettings
                    {
                        Result = new ResultBinding
                        {
                            ProviderId = ValueProviderIds.JobVariable,
                            SourceId = textResult.Id.ToString("D")
                        },
                        FontSize = 24,
                        DurationMs = 5000
                    }
                ]
            })
        };
        var fontSize = new JobVariable
        {
            ValueKind = ResultValueKind.Number,
            Value = JsonValue.Create(36d)
        };
        var duration = new JobVariable
        {
            ValueKind = ResultValueKind.Integer,
            Value = JsonValue.Create(9000)
        };
        var step = new ShowOnDesktopStep();
        step.Inputs[ShowOnDesktopStepDefinition.OverlayFieldId] = new ResultBinding
        {
            ProviderId = ValueProviderIds.LocalValue,
            SourceId = overlay.Id.ToString("D")
        };
        ValueBindingTree.Set(step.Inputs, "overlay.text_results.0.font_size", new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = fontSize.Id.ToString("D")
        });
        ValueBindingTree.Set(step.Inputs, "overlay.text_results.0.duration_ms", new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = duration.Id.ToString("D")
        });
        Assert.True(BuiltInStepDefinitions.Instance.TryGetByType(typeof(ShowOnDesktopStep), out var definition));
        ValueBindingTree.ApplySchemas(step.Inputs, definition.Descriptor.Fields);
        var results = new JobResultStore([textResult, fontSize, duration], localValues: [overlay]);

        var materialized = Assert.IsType<ShowOnDesktopStep>(StepInputMaterializer.Materialize(step, results));

        var text = Assert.Single(materialized.Settings.Overlay.TextResults);
        Assert.Equal(36f, text.FontSize);
        Assert.Equal(9000, text.DurationMs);
        Assert.Equal(ValueBindingSchemaRegistry.VisualOverlay,
            step.Inputs[ShowOnDesktopStepDefinition.OverlayFieldId].SchemaId);
    }

    [Fact]
    public void Migrate_ConvertsLegacyDottedInputsIntoVersionedBindingTree()
    {
        var target = new JobVariable
        {
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.ResultObject,
            Value = JsonSerializer.SerializeToNode(new TaskAutomation.Contracts.Steps.StepProcessSelectorValue(
                null, "notepad", string.Empty, "Editor"))
        };
        var title = new JobVariable
        {
            Scope = JobVariableScope.Shared,
            ValueKind = ResultValueKind.Text,
            Value = JsonValue.Create("Bericht")
        };
        var step = new ActiveProcessStep();
        step.Inputs[ActiveProcessStepDefinition.ProcessTargetFieldId] = new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = target.Id.ToString("D")
        };
        var nestedPath = $"{ActiveProcessStepDefinition.ProcessTargetFieldId}.window_title_contains";
        step.Inputs[nestedPath] = new ResultBinding
        {
            ProviderId = ValueProviderIds.JobVariable,
            SourceId = title.Id.ToString("D")
        };
        var job = new Job { FormatVersion = 3, Variables = [target, title], Steps = [step] };

        Assert.True(JobVariableInputMigration.Migrate(job));

        Assert.Equal(Job.CurrentFormatVersion, job.FormatVersion);
        Assert.DoesNotContain(nestedPath, step.Inputs.Keys);
        var root = Assert.Contains(ActiveProcessStepDefinition.ProcessTargetFieldId, step.Inputs);
        Assert.Equal(ValueBindingSchemaRegistry.ProcessTarget, root.SchemaId);
        Assert.Equal(title.Id.ToString("D"), ValueBindingTree.Find(step.Inputs, nestedPath)!.SourceId);
        var options = new JsonSerializerOptions();
        JobJsonSerialization.Configure(options);
        var json = JsonSerializer.Serialize(job, options);
        Assert.Contains("\"schema_id\":\"desktopautomation.process-target/v1\"", json);
        Assert.Contains("\"members\":", json);
        Assert.DoesNotContain($"\"{nestedPath}\"", json);
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
        Assert.Contains("\"localValues\"", json);
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
        var textVariable = Assert.Single(job.LocalValues, variable =>
            variable.Id.ToString("D") == showText.Inputs[ShowTextStepDefinition.TextResultFieldId].SourceId);
        Assert.Equal("Legacy text", textVariable.Value!.GetValue<string>());
        var paddingVariable = Assert.Single(job.LocalValues, variable =>
            variable.Id.ToString("D") == dynamicRoi.Inputs[DynamicRoiStepDefinition.PaddingSourceFieldId].SourceId);
        Assert.Equal(23, paddingVariable.Value!.GetValue<int>());
    }
}
