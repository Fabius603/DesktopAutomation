using TaskAutomation.Jobs;
using TaskAutomation.Orchestration;
using TaskAutomation.Tests.TestDoubles;
using DesktopAutomationApp.Localization;

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
            var stepErrors = new List<JobStepErrorEventArgs>();
            var jobErrors = 0;
            executor.JobStepErrorOccurred += (_, error) => stepErrors.Add(error);
            executor.JobErrorOccurred += (_, _) => jobErrors++;
            await executor.ExecuteJob(job.Id);
            var stepError = Assert.Single(stepErrors);
            Assert.Equal(StepErrorKind.Unexpected, stepError.ErrorKind);
            Assert.Equal("STEP_UNEXPECTED_ERROR", stepError.ErrorCode);
            Assert.Equal(0, jobErrors);
            Assert.Equal(["cleanup"], builder.Overlay.TextCalls.Select(call => call.Text));
            Assert.False(Assert.Single(builder.Logs.Completions).Success);
        }
        finally { File.Delete(scriptPath); }
    }

    [Fact]
    public void StepErrorPresentation_ReplacesTechnicalExceptionWithLocalizedGuidance()
    {
        var previousCulture = LocalizationService.Instance.CurrentCulture.Name;
        try
        {
            LocalizationService.Instance.SetCulture("de-DE");
            var error = new JobStepErrorEventArgs(
                "job",
                nameof(ScriptExecutionStep),
                new FileNotFoundException("sensitive technical path"));

            var presentation = StepErrorPresentation.Create(error);

            Assert.Equal("STEP_FILE_NOT_FOUND", presentation.ErrorCode);
            Assert.Contains("Datei", presentation.Message);
            Assert.DoesNotContain("sensitive technical path", presentation.Message);
            Assert.NotEqual(nameof(ScriptExecutionStep), presentation.StepName);
        }
        finally
        {
            LocalizationService.Instance.SetCulture(previousCulture);
        }
    }

    [Theory]
    [InlineData(typeof(DirectoryNotFoundException), StepErrorKind.DirectoryNotFound, "STEP_DIRECTORY_NOT_FOUND")]
    [InlineData(typeof(UnauthorizedAccessException), StepErrorKind.AccessDenied, "STEP_ACCESS_DENIED")]
    [InlineData(typeof(TimeoutException), StepErrorKind.TimedOut, "STEP_TIMED_OUT")]
    [InlineData(typeof(ArgumentException), StepErrorKind.InvalidConfiguration, "STEP_INVALID_CONFIGURATION")]
    [InlineData(typeof(IOException), StepErrorKind.InputOutput, "STEP_IO_ERROR")]
    public void JobStepErrorEventArgs_ClassifiesExpectedFailures(
        Type exceptionType,
        StepErrorKind expectedKind,
        string expectedCode)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "technical details")!;

        var error = new JobStepErrorEventArgs("job", "step", exception);

        Assert.Equal(expectedKind, error.ErrorKind);
        Assert.Equal(expectedCode, error.ErrorCode);
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
