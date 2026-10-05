using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Steps.Definitions;

namespace DesktopAutomation.ContractTests.Jobs;

public sealed class StepValueGraphTests
{
    [Fact]
    public void RemovingAnAbsentSourceUpdatesEmbeddedConditionJsonAndKeepsExistingDisabledSources()
    {
        var job = CreateChoiceJob();
        var choice = job.Steps[0]; choice.IsEnabled = false;
        JobValidation.RemoveInvalidSourceSelections(job);
        Assert.Contains(ValueReferenceUsageInspector.Find(job), usage => usage.Reference is ResultBinding binding
            && binding.TryGetStepResult(out var source) && source.StepId == choice.Id);
        job.Steps.Remove(choice);
        JobValidation.RemoveInvalidSourceSelections(job);
        Assert.DoesNotContain(ValueReferenceUsageInspector.Find(job), usage => usage.Reference is ResultBinding binding
            && binding.TryGetStepResult(out var source) && source.StepId == choice.Id);
        var reopened = Roundtrip(job);
        Assert.DoesNotContain(ValueReferenceUsageInspector.Find(reopened), usage => usage.Reference is ResultBinding binding
            && binding.TryGetStepResult(out var source) && source.StepId == choice.Id);
    }

    [Fact]
    public void PersistedCondition_ReportsAndUpdatesEmbeddedReferences()
    {
        var job = CreateChoiceJob();
        var choice = Assert.IsType<UserChoiceStep>(job.Steps[0]);
        var condition = Assert.IsType<IfStep>(job.Steps[1]);
        var comparison = job.LocalValues.Single(value => value.EnumTypeName == UserChoiceEnumContract.ForStep(choice.Id));
        var usage = Assert.Single(ValueReferenceUsageInspector.FindLogical(job, ValueProviderIds.LocalValue, comparison.Id.ToString("D")));
        var replacement = Guid.NewGuid();
        usage.UpdateReference(reference => reference.SourceId = replacement.ToString("D"));
        var stored = job.LocalValues.Single(value => value.Id.ToString("D") == condition.Inputs["conditions"].SourceId);
        Assert.Equal(replacement.ToString("D"), stored.Value!["conditions"]![0]!["comparison"]!["source_id"]!.GetValue<string>());
    }

    [Fact]
    public async Task CloneGraph_RemapsInternalStepsLocalValuesAndEnumIdentity()
    {
        var job = CreateChoiceJob();
        var captured = await JobStepsSnapshotService.CaptureGraphAsync(job, job.Steps);
        var clone = await JobStepsSnapshotService.CloneGraphAsync(captured);
        var copiedJob = new Job { Steps = clone.Steps.ToList(), LocalValues = clone.LocalValues.ToList() };
        var choice = Assert.IsType<UserChoiceStep>(clone.Steps[0]);
        var materialized = Assert.IsType<IfStep>(StepInputMaterializer.Materialize(clone.Steps[1], new JobResultStore(localValues: copiedJob.LocalValues)));
        var condition = Assert.Single(materialized.Settings.Conditions);
        Assert.Equal(choice.Id, condition.SourceStepId);
        Assert.DoesNotContain(choice.Id, job.Steps.Select(step => step.Id));
        var local = copiedJob.LocalValues.Single(value => value.Id.ToString("D") == condition.Comparison!.SourceId);
        Assert.Equal(UserChoiceEnumContract.ForStep(choice.Id), local.EnumTypeName);
        Assert.Equal(clone.Steps[1].Id, local.OwnerStepId);
        Assert.Empty(copiedJob.LocalValues.Select(value => value.Id).Intersect(job.LocalValues.Select(value => value.Id)));
        Assert.True(JobValidation.ValidateJob(copiedJob).IsValid);
    }

    [Fact]
    public async Task CloneGraph_KeepsExternalJobVariablesAndIndependentStoredValues()
    {
        var variable = new JobVariable { Scope = JobVariableScope.Shared, ValueKind = ResultValueKind.Boolean, Value = JsonValue.Create(true) };
        var step = new IfStep { Settings = new() { Conditions = [new() { ProviderId = ValueProviderIds.JobVariable, SourceId = variable.Id.ToString("D"), Operator = ConditionOperator.IsTrue }] } };
        var job = new Job { Variables = [variable], Steps = [step, new EndIfStep()] };
        JobVariableInputMigration.Migrate(job);
        job = Roundtrip(job);
        var graph = await JobStepsSnapshotService.CloneGraphAsync(await JobStepsSnapshotService.CaptureGraphAsync(job, job.Steps));
        var clonedJob = new Job { Steps = graph.Steps.ToList(), LocalValues = graph.LocalValues.ToList(), Variables = [variable] };
        Assert.Single(ValueReferenceUsageInspector.FindLogical(clonedJob, ValueProviderIds.JobVariable, variable.Id.ToString("D")));
        graph.LocalValues[0].Value = new JsonObject();
        Assert.NotEqual(graph.LocalValues[0].Value!.ToJsonString(), job.LocalValues[0].Value!.ToJsonString());
    }

    [Fact]
    public void StoredReferenceCycles_AreBoundedAndKeepBothReferencedValues()
    {
        var first = new LocalValue();
        var second = new LocalValue();
        first.Value = JsonSerializer.SerializeToNode(new ResultBinding { ProviderId = ValueProviderIds.LocalValue, SourceId = second.Id.ToString("D") });
        second.Value = JsonSerializer.SerializeToNode(new ResultBinding { ProviderId = ValueProviderIds.LocalValue, SourceId = first.Id.ToString("D") });
        var step = new TimeoutStep();
        step.Inputs["delay_ms"] = new() { ProviderId = ValueProviderIds.LocalValue, SourceId = first.Id.ToString("D") };
        var references = ValueReferenceUsageInspector.Find(new Job { Steps = [step], LocalValues = [first, second] });
        Assert.Equal(3, references.Count);
    }

    private static Job CreateChoiceJob()
    {
        var choice = new UserChoiceStepDefinition().CreateDefaultStep();
        choice.Settings.Options[0].Label = "A";
        choice.Settings.Options[1].Label = "B";
        var condition = new IfStep();
        var comparison = new LocalValue
        {
            OwnerStepId = condition.Id,
            InputPath = "conditions.0.comparison",
            ValueKind = ResultValueKind.Enum,
            Value = JsonValue.Create(choice.Settings.Options[0].Id),
            EnumTypeName = UserChoiceEnumContract.ForStep(choice.Id),
            EnumValues = choice.Settings.Options.Select(option => option.Id).ToList()
        };
        condition.Settings.Conditions = [new() { ProviderId = ValueProviderIds.StepResult,
            SourceId = StepResultSourceIdCodec.Create(choice.Id, "selected_option_id"), Operator = ConditionOperator.Equals,
            Comparison = new() { Kind = ComparisonOperandKind.JobResult, ProviderId = ValueProviderIds.LocalValue, SourceId = comparison.Id.ToString("D") } }];
        var job = new Job { Steps = [choice, condition, new EndIfStep()], LocalValues = [comparison] };
        JobVariableInputMigration.Migrate(job);
        return Roundtrip(job);
    }

    private static Job Roundtrip(Job job)
    {
        var options = new JsonSerializerOptions();
        JobJsonSerialization.Configure(options);
        return JsonSerializer.Deserialize<Job>(JsonSerializer.Serialize(job, options), options)!;
    }
}
