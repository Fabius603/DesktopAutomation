using TaskAutomation.Jobs;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class ScriptExecutionStepHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_WaitingExecutionForwardsPathArgumentsAndToken()
    {
        var path = Path.GetTempFileName();
        try
        {
            string? receivedPath = null, receivedArguments = null;
            CancellationToken receivedToken = default;
            var scripts = new DelegateScriptExecutor { Execute = (scriptPath, arguments, token) =>
            { receivedPath = scriptPath; receivedArguments = arguments; receivedToken = token; return Task.CompletedTask; } };
            var context = new PipelineContextStub { ScriptExecutor = scripts };
            using var cts = new CancellationTokenSource();
            var step = new ScriptExecutionStep { Id = "script", Settings = new()
                { ScriptPath = path, Arguments = "--value 42", WaitForExit = true } };
            var result = Assert.IsType<ScriptExecutionResult>(await new ScriptExecutionStepHandler().ExecuteAsync(step, context, cts.Token));
            Assert.Equal(path, receivedPath);
            Assert.Equal("--value 42", receivedArguments);
            Assert.Equal(cts.Token, receivedToken);
            Assert.True(result.Success);
            Assert.Same(result, context.Results.GetRaw("script"));
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("")]
    [InlineData("Z:\\definitely-missing-script.ps1")]
    public async Task ExecuteAsync_MissingPathOrFileThrows(string path)
    {
        await Assert.ThrowsAnyAsync<Exception>(() => new ScriptExecutionStepHandler().ExecuteAsync(
            new ScriptExecutionStep { Settings = new() { ScriptPath = path } }, new PipelineContextStub(), default));
    }

    [Fact]
    public async Task ExecuteAsync_ExecutorFailurePropagatesWithoutStoredResult()
    {
        var path = Path.GetTempFileName();
        try
        {
            var context = new PipelineContextStub { ScriptExecutor = new DelegateScriptExecutor
                { Execute = (_, _, _) => Task.FromException(new InvalidOperationException("boom")) } };
            var step = new ScriptExecutionStep { Id = "script", Settings = new() { ScriptPath = path, WaitForExit = true } };
            await Assert.ThrowsAsync<InvalidOperationException>(() => new ScriptExecutionStepHandler().ExecuteAsync(step, context, default));
            Assert.Null(context.Results.GetRaw("script"));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public async Task ExecuteAsync_FireAndForgetReturnsBeforeScriptCompletes()
    {
        var path = Path.GetTempFileName();
        try
        {
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var context = new PipelineContextStub { ScriptExecutor = new DelegateScriptExecutor { Execute = async (_, _, _) =>
            { started.TrySetResult(); await release.Task; } } };
            var result = Assert.IsType<ScriptExecutionResult>(await new ScriptExecutionStepHandler().ExecuteAsync(
                new ScriptExecutionStep { Settings = new() { ScriptPath = path, WaitForExit = false } }, context, default));
            Assert.True(result.Success);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            release.TrySetResult();
        }
        finally { File.Delete(path); }
    }
}
