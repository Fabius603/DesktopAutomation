using Microsoft.Extensions.Logging.Abstractions;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using TaskAutomation.Orchestration;
using TaskAutomation.Tests.TestDoubles;

namespace TaskAutomation.Tests.Orchestration;

public sealed class JobDispatcherMakroTests
{
    [Fact]
    public void StartMakro_UnknownIdDoesNothing()
    {
        var makros = new ControllableMakroExecutor();
        var executor = new ControllableJobExecutor([], makroExecutor: makros);
        using var dispatcher = new JobDispatcher(executor, NullLogger<JobDispatcher>.Instance);
        dispatcher.StartMakro(Guid.NewGuid());
        Assert.Empty(makros.Snapshot());
        Assert.Empty(dispatcher.RunningMakroIds);
    }

    [Fact]
    public async Task StartMakro_RegistersAndRemovesCompletedInstance()
    {
        var makro = new Makro { Name = "macro" };
        var makros = new ControllableMakroExecutor();
        var executor = new ControllableJobExecutor([], [makro], makros);
        using var dispatcher = new JobDispatcher(executor, NullLogger<JobDispatcher>.Instance);
        dispatcher.StartMakro(makro.Id);
        await WaitUntilAsync(() => makros.Snapshot().Length == 1);
        Assert.Equal([makro.Id], dispatcher.RunningMakroIds);
        makros.Snapshot()[0].Completion.TrySetResult();
        await WaitUntilAsync(() => dispatcher.RunningMakroIds.Count == 0);
    }

    [Fact]
    public async Task StartMakro_AllowsParallelInstancesButExposesDistinctDefinitionIds()
    {
        var makro = new Makro { Name = "parallel" };
        var makros = new ControllableMakroExecutor();
        var executor = new ControllableJobExecutor([], [makro], makros);
        using var dispatcher = new JobDispatcher(executor, NullLogger<JobDispatcher>.Instance);
        dispatcher.StartMakro(makro.Id);
        dispatcher.StartMakro(makro.Id);
        await WaitUntilAsync(() => makros.Snapshot().Length == 2);
        Assert.Equal([makro.Id], dispatcher.RunningMakroIds);
        foreach (var invocation in makros.Snapshot()) invocation.Completion.TrySetResult();
        await WaitUntilAsync(() => dispatcher.RunningMakroIds.Count == 0);
    }

    [Fact]
    public async Task CancelMakro_CancelsEveryInstanceOfDefinition()
    {
        var makro = new Makro { Name = "cancel" };
        var makros = new ControllableMakroExecutor();
        var executor = new ControllableJobExecutor([], [makro], makros);
        using var dispatcher = new JobDispatcher(executor, NullLogger<JobDispatcher>.Instance);
        dispatcher.StartMakro(makro.Id);
        dispatcher.StartMakro(makro.Id);
        await WaitUntilAsync(() => makros.Snapshot().Length == 2);
        dispatcher.CancelMakro(makro.Id);
        await WaitUntilAsync(() => makros.Snapshot().All(invocation => invocation.Token.IsCancellationRequested));
        await WaitUntilAsync(() => dispatcher.RunningMakroIds.Count == 0);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (!condition()) { timeout.Token.ThrowIfCancellationRequested(); await Task.Yield(); }
    }
}
