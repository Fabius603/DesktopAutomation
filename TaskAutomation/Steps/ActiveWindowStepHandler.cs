using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public sealed class ActiveWindowStepHandler : JobStepHandler<ActiveWindowStep, ActiveWindowResult>
{
    protected override Task<ActiveWindowResult> ExecuteCoreAsync(
        ActiveWindowStep step, IStepPipelineContext ctx, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var target = step.Settings.Target;
        var cacheKey = target.ProcessSource.IsConfigured
            ? $"source:{target.ProcessSource.SourceStepId}:{target.ProcessSource.PropertyPath}"
            : $"{target.ProcessName}|{target.ExecutablePath}|{target.WindowTitleContains}";
        var cacheMs = !target.ProcessSource.IsConfigured
            ? Math.Max(0, step.Settings.CacheMs)
            : 0;

        if (cacheMs > 0
            && ctx.ActiveWindowCache.TryGetValue(step.Id, out var cached)
            && string.Equals(cached.ProcessName, cacheKey, StringComparison.OrdinalIgnoreCase)
            && (DateTime.Now - cached.Timestamp).TotalMilliseconds < cacheMs)
            return Task.FromResult(new ActiveWindowResult { WasExecuted = true, IsActive = cached.IsActive });

        var processIds = ProcessTargetResolver.ResolveProcessIds(target, ctx.Results);
        ProcessWindowMatcher.ProcessWindowMatch? foreground = null;
        var matchedProcessId = 0;
        var sourceReference = ProcessTargetResolver.ResolveReference(target, ctx.Results);
        var referencedWindow = sourceReference is { WindowHandle: not 0 }
            ? ProcessWindowMatcher.FindMatchingWindows(sourceReference.ProcessId, target.WindowTitleContains)
                .FirstOrDefault(window => window.Handle.ToInt64() == sourceReference.WindowHandle)
            : null;
        if (referencedWindow is not null
            && ProcessWindowMatcher.IsForegroundWindow(referencedWindow.Handle))
        {
            matchedProcessId = sourceReference.ProcessId;
            foreground = referencedWindow;
        }
        foreach (var processId in processIds)
        {
            if (matchedProcessId > 0) break;
            if (!ProcessWindowMatcher.ForegroundWindowMatches(processId, target.WindowTitleContains)) continue;
            matchedProcessId = processId;
            foreground = ProcessWindowMatcher.FindMatchingWindows(processId, target.WindowTitleContains)
                .FirstOrDefault(window => ProcessWindowMatcher.IsForegroundWindow(window.Handle));
            break;
        }

        var isActive = matchedProcessId > 0;
        ctx.Logger.LogInformation(
            "ActiveWindowStepHandler: Prozessziel '{Target}' - Fenster aktiv: {IsActive}.",
            cacheKey, isActive);
        if (cacheMs > 0)
            ctx.ActiveWindowCache[step.Id] = new ActiveWindowCacheEntry(cacheKey, isActive, DateTime.Now);

        return Task.FromResult(new ActiveWindowResult
        {
            WasExecuted = true,
            IsActive = isActive,
            Process = matchedProcessId > 0 ? ProcessTargetResolver.CreateReference(matchedProcessId) : null,
            WindowHandle = foreground?.Handle.ToInt64() ?? 0
        });
    }

    protected override ActiveWindowResult CreateDefault() => ActiveWindowResult.Default;
}
