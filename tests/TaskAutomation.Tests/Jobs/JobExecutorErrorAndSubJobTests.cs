using TaskAutomation.Jobs;
using TaskAutomation.Orchestration;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Jobs;

public sealed class JobExecutorErrorAndSubJobTests
{
    [Fact]
    public async Task ExecuteJob_StepFailureRaisesOnlyStepErrorAndStillRunsEndPhase()
    {
        var scriptPath = Path.GetTempFileName();
        try
        {
            var job = new Job { Name = "failure", Steps = [new ScriptExecutionStep { Settings = new()
                { ScriptPath = scriptPath, WaitForExit = true } }], EndSteps = [Text("cleanup")] };
            var builder = new JobExecutorTestBuilder().WithJobs(job);
            builder.Scripts.Execute = (_, _, _) => throw new InvalidOperationException("boom");
            using var executor = await builder.BuildAsync();
            var stepErrors = 0;
            var jobErrors = 0;
            executor.JobStepErrorOccurred += (_, _) => stepErrors++;
            executor.JobErrorOccurred += (_, _) => jobErrors++;
            await executor.ExecuteJob(job.Id);
            Assert.Equal(1, stepErrors);
            Assert.Equal(0, jobErrors);
            Assert.Equal(["cleanup"], builder.Overlay.TextCalls.Select(call => call.Text));
            Assert.False(Assert.Single(builder.Logs.Completions).Success);
        }
        finally { File.Delete(scriptPath); }
    }

    [Fact]
    public async Task ExecuteJob_WaitingSubJobCompletesBeforeParentContinues()
    {
        var child = new Job { Name = "child", Steps = [Text("child")] };
        var parent = new Job { Name = "parent", Steps = [new JobExecutionStep { Settings = new()
            { JobId = child.Id, WaitForCompletion = true } }, Text("parent")] };
        var builder = new JobExecutorTestBuilder().WithJobs(parent, child);
        using var executor = await builder.BuildAsync();
        await executor.ExecuteJob(parent.Id);
        Assert.Equal(["child", "parent"], builder.Overlay.TextCalls.Select(call => call.Text));
        Assert.Equal(2, builder.Logs.Completions.Count);
    }

    [Fact]
    public async Task ExecuteJob_DirectSelfReferenceIsRejectedByStepHandler()
    {
        var job = new Job { Name = "self" };
        job.Steps.Add(new JobExecutionStep { Settings = new() { JobId = job.Id, WaitForCompletion = true } });
        var builder = new JobExecutorTestBuilder().WithJobs(job);
        using var executor = await builder.BuildAsync();
        var stepErrors = 0;
        executor.JobStepErrorOccurred += (_, _) => stepErrors++;
        await executor.ExecuteJob(job.Id);
        Assert.Equal(1, stepErrors);
        Assert.False(Assert.Single(builder.Logs.Completions).Success);
    }

    [Fact]
    public async Task ExecuteJob_IndirectCycleRaisesJobErrorAndTerminates()
    {
        var first = new Job { Name = "first" };
        var second = new Job { Name = "second" };
        first.Steps.Add(new JobExecutionStep { Settings = new() { JobId = second.Id, WaitForCompletion = true } });
        second.Steps.Add(new JobExecutionStep { Settings = new() { JobId = first.Id, WaitForCompletion = true } });
        var builder = new JobExecutorTestBuilder().WithJobs(first, second);
        using var executor = await builder.BuildAsync();
        var errors = new List<JobErrorEventArgs>();
        executor.JobErrorOccurred += (_, error) => errors.Add(error);
        await executor.ExecuteJob(first.Id);
        Assert.Contains(errors, error => error.Exception.Message.Contains("Zirkuläre Abhängigkeit"));
        Assert.Null(executor.CurrentJob);
    }

    private static ShowTextStep Text(string text) => new() { Settings = new() { Text = text, ClearOnJobEnd = false } };
}
