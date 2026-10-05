using TaskAutomation.Logging;
using TaskAutomation.Steps;

namespace TaskAutomation.Tests.Logging;

public sealed class StepLogResultPrivacyTests
{
    [Fact]
    public void Summaries_PreserveUsefulStatusButExcludeRawTextNamesAndInputs()
    {
        const string privateValue = "private-result-text";
        var results = new object[] {
            new OcrResult { Text = privateValue, Words = [new() { Text = privateValue }], Found = true, WordCount = 1 },
            new ClipboardContentQueryResult { Text = privateValue, Name = privateValue, ErrorMessage = privateValue, Exists = true },
            new ForegroundWindowQueryResult { Text = privateValue, Name = privateValue, Exists = true },
            new WindowsSettingChangeResult { PreviousValue = privateValue, AppliedValue = privateValue, ErrorMessage = privateValue },
            new UserChoiceResult { SelectedLabel = privateValue, SelectedValue = privateValue, SelectedOptionId = privateValue, SelectedIndex = 2 },
            new StartProcessResult { Success = false, ErrorMessage = privateValue,
                Process = new() { ProcessId = 123, ProcessName = privateValue, ExecutablePath = privateValue } }
        };
        foreach (var result in results)
            Assert.DoesNotContain(privateValue, StepLogResults.Summarize(result).Values);
        Assert.Equal("1", StepLogResults.Summarize(results[0])["WordCount"]);
        Assert.Equal("True", StepLogResults.Summarize(results[1])["Exists"]);
        Assert.Equal("2", StepLogResults.Summarize(results[4])["SelectedIndex"]);
        Assert.Equal("123", StepLogResults.Summarize(results[5])["ProcessId"]);
    }

    [Fact]
    public void ExceptionDetails_KeepTypeAndInnerTypeWithoutInputValues()
    {
        var details = LogDiagnostics.ExceptionDetails(new InvalidOperationException("private-input", new IOException("private-output")));
        Assert.Contains(nameof(InvalidOperationException), details);
        Assert.Contains(nameof(IOException), details);
        Assert.DoesNotContain("private-input", details);
        Assert.DoesNotContain("private-output", details);
    }

    [Fact]
    public void LateScriptOutput_DoesNotReplaceTheStepLifecycle()
    {
        var id = Guid.NewGuid();
        var run = new LogRun { EndedAt = DateTimeOffset.UtcNow, Steps = [new("script", "script", 1, "Main", true)] };
        var entries = new[] {
            new LogEvent { Code = LogCodes.StepStarted, Sequence = 1, Context = new(StepId: "script", StepExecutionId: id) },
            new LogEvent { Code = LogCodes.StepCompleted, Sequence = 2, Context = new(StepId: "script", StepExecutionId: id), DurationMs = 5 },
            new LogEvent { Code = LogCodes.StepOutput, Sequence = 3, Context = new(StepId: "script", StepExecutionId: id) }
        };
        var step = Assert.Single(LogTimeline.Build(run, entries));
        Assert.Equal(StepLogOutcome.Successful, step.Outcome);
        Assert.Equal(5, step.DurationMs);
        Assert.Equal(3, step.Events.Count);
    }
}
