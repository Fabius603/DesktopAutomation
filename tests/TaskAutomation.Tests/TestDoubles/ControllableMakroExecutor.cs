using TaskAutomation.Makros;

namespace TaskAutomation.Tests.TestDoubles;

internal sealed record MakroInvocation(Makro Makro, CancellationToken Token, TaskCompletionSource Completion);

internal sealed class ControllableMakroExecutor : IMakroExecutor
{
    private readonly object _gate = new();
    public List<MakroInvocation> Invocations { get; } = [];
    public Task ExecuteMakro(Makro makro, ImageHelperMethods.DxgiResources dxgi, CancellationToken ct)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_gate) Invocations.Add(new(makro, ct, completion));
        return completion.Task.WaitAsync(ct);
    }
    public MakroInvocation[] Snapshot()
    {
        lock (_gate) return Invocations.ToArray();
    }
}
