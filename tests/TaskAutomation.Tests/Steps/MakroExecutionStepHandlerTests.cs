using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using TaskAutomation.Steps;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Steps;

public sealed class MakroExecutionStepHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_ResolvesByIdAndStoresSuccess()
    {
        var makro = new Makro { Name = "macro" };
        var executor = new RecordingMakroExecutor();
        var context = Context(makro, executor);
        var step = new MakroExecutionStep { Id = "run", Settings = new() { MakroId = makro.Id, MakroName = "wrong" } };
        var result = Assert.IsType<MakroExecutionResult>(await new MakroExecutionStepHandler().ExecuteAsync(step, context, default));
        Assert.Same(makro, Assert.Single(executor.Makros));
        Assert.True(result.Success);
        Assert.Same(result, context.Results.GetRaw("run"));
    }

    [Fact]
    public async Task ExecuteAsync_ResolvesByNameCaseInsensitively()
    {
        var makro = new Makro { Name = "My Macro" };
        var executor = new RecordingMakroExecutor();
        await new MakroExecutionStepHandler().ExecuteAsync(new MakroExecutionStep
            { Settings = new() { MakroName = "my macro" } }, Context(makro, executor), default);
        Assert.Same(makro, Assert.Single(executor.Makros));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_MissingSelectionOrTargetThrows(bool configuredMissingId)
    {
        var step = new MakroExecutionStep { Settings = configuredMissingId
            ? new() { MakroId = Guid.NewGuid() } : new() };
        await Assert.ThrowsAsync<InvalidOperationException>(() => new MakroExecutionStepHandler()
            .ExecuteAsync(step, new PipelineContextStub(), default));
    }

    [Fact]
    public async Task ExecuteAsync_ExecutorFailurePropagatesAndDoesNotStoreResult()
    {
        var makro = new Makro { Name = "fail" };
        var context = Context(makro, new RecordingMakroExecutor { Error = new InvalidOperationException("boom") });
        var step = new MakroExecutionStep { Id = "run", Settings = new() { MakroId = makro.Id } };
        await Assert.ThrowsAsync<InvalidOperationException>(() => new MakroExecutionStepHandler().ExecuteAsync(step, context, default));
        Assert.Null(context.Results.GetRaw("run"));
    }

    private static PipelineContextStub Context(Makro makro, RecordingMakroExecutor executor) => new()
    { AllMakros = new Dictionary<string, Makro> { [makro.Id.ToString()] = makro }, MakroExecutor = executor };

    private sealed class RecordingMakroExecutor : IMakroExecutor
    {
        public List<Makro> Makros { get; } = [];
        public Exception? Error { get; init; }
        public Task ExecuteMakro(Makro makro, ImageHelperMethods.DxgiResources dxgi, CancellationToken ct)
        { Makros.Add(makro); return Error is null ? Task.CompletedTask : Task.FromException(Error); }
    }
}
