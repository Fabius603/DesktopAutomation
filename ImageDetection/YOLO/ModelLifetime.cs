using System.Runtime.CompilerServices;
namespace ImageDetection.YOLO;

public static class ModelLifetime
{
    private sealed class Usage { public readonly Dictionary<string, int> Counts = new(StringComparer.OrdinalIgnoreCase); }
    private static readonly ConditionalWeakTable<IYoloManager, Usage> Owners = new();
    public static bool InUse(IYoloManager manager, string model)
    {
        var usage = Owners.GetOrCreateValue(manager);
        lock (usage) return usage.Counts.GetValueOrDefault(model) > 0;
    }
    public static bool TryUnload(IYoloManager manager, string model, Func<bool> unload)
    {
        var usage = Owners.GetOrCreateValue(manager);
        lock (usage) return usage.Counts.GetValueOrDefault(model) == 0 && unload();
    }
    public static async Task<IDisposable> AcquireAsync(IYoloManager manager, string model, CancellationToken ct)
    {
        var usage = Owners.GetOrCreateValue(manager);
        lock (usage) usage.Counts[model] = usage.Counts.GetValueOrDefault(model) + 1;
        var lease = new Lease(manager, usage, model);
        try
        {
            // One caller cancelling must not cancel another caller's shared initialization.
            await manager.EnsureModelAsync(model, CancellationToken.None).WaitAsync(ct).ConfigureAwait(false);
            return lease;
        }
        catch { lease.Dispose(); throw; }
    }
    private sealed class Lease(IYoloManager manager, Usage usage, string model) : IDisposable
    {
        private int _released;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) != 0) return;
            lock (usage)
            {
                if (--usage.Counts[model] != 0) return;
                usage.Counts.Remove(model);
                manager.UnloadModel(model);
            }
        }
    }
}
