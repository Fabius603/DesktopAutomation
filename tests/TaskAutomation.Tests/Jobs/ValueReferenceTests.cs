using System.Text.Json;
using System.Text.Json.Nodes;
using TaskAutomation.Jobs;

namespace TaskAutomation.Tests.Jobs;

public sealed class ValueReferenceTests
{
    [Fact]
    public void StepResultReference_RoundTripsOnlyProviderAndSourceIds()
    {
        var reference = ResultBinding.ForStepResult("step/one", "response.body");

        var json = JsonSerializer.Serialize(reference);
        var restored = JsonSerializer.Deserialize<ResultBinding>(json)!;

        Assert.Contains("\"provider_id\":\"step_result\"", json);
        Assert.Contains("\"source_id\":", json);
        Assert.DoesNotContain("source_step_id", json);
        Assert.DoesNotContain("property_id", json);
        Assert.Equal("step/one", restored.SourceStepId);
        Assert.Equal("response.body", restored.PropertyId);
    }

    [Fact]
    public void LegacyResultBinding_RemainsReadable()
    {
        const string json = """
            {"source_step_id":"step-2","property_id":"response_body","property_path":"ResponseBody"}
            """;

        var restored = JsonSerializer.Deserialize<ResultBinding>(json)!;

        Assert.True(restored.IsConfigured);
        Assert.Equal("step-2", restored.SourceStepId);
        Assert.Equal("response_body", restored.PropertyId);
        Assert.Equal("ResponseBody", restored.PropertyPath);
    }

    [Fact]
    public void ExistingJobWithoutVariables_LoadsWithEmptyStore()
    {
        var job = JsonSerializer.Deserialize<Job>("{\"id\":\"00000000-0000-0000-0000-000000000001\",\"name\":\"Legacy\"}")!;

        Assert.NotNull(job.Variables);
        Assert.Empty(job.Variables);
    }

    [Fact]
    public void JobVariable_RoundTripsTypedValue()
    {
        var job = new Job
        {
            Variables =
            [
                new JobVariable
                {
                    Name = "Origin",
                    Description = "Click origin",
                    ValueKind = ResultValueKind.Point,
                    Value = new JsonObject { ["x"] = 12, ["y"] = 34 }
                }
            ]
        };

        var restored = JsonSerializer.Deserialize<Job>(JsonSerializer.Serialize(job))!;

        var variable = Assert.Single(restored.Variables);
        Assert.Equal(ResultValueKind.Point, variable.ValueKind);
        Assert.Equal(12, variable.Value!["x"]!.GetValue<int>());
    }

    [Fact]
    public void ConditionReference_RoundTripsWithoutLegacyStepFields()
    {
        var condition = new StepCondition
        {
            ProviderId = ValueProviderIds.StepResult,
            SourceId = StepResultSourceIdCodec.Create("step-1", "is_ready"),
            Operator = ConditionOperator.IsTrue
        };

        var json = JsonSerializer.Serialize(condition);
        var restored = JsonSerializer.Deserialize<StepCondition>(json)!;

        Assert.DoesNotContain("source_step_id", json);
        Assert.DoesNotContain("property_id", json);
        Assert.Equal("step-1", restored.SourceStepId);
        Assert.Equal("is_ready", restored.PropertyId);
    }

    [Fact]
    public void UsageInspector_FindsNestedProviderReferences()
    {
        var variableId = Guid.NewGuid();
        var job = new Job
        {
            Steps =
            [
                new IfStep
                {
                    Settings = new IfConditionSettings
                    {
                        Conditions =
                        [
                            new StepCondition
                            {
                                ProviderId = ValueProviderIds.JobVariable,
                                SourceId = variableId.ToString("D"),
                                Operator = ConditionOperator.IsTrue
                            }
                        ]
                    }
                }
            ]
        };

        var usage = Assert.Single(ValueReferenceUsageInspector.Find(
            job, ValueProviderIds.JobVariable, variableId.ToString("D")));

        Assert.IsType<IfStep>(usage.Step);
        Assert.Contains(nameof(IfConditionSettings.Conditions), usage.Path);
    }
}
