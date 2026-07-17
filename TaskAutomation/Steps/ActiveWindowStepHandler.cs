using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    /// <summary>
    /// Prueft, ob ein Fenster des angegebenen Prozesses das aktive Vordergrundfenster ist.
    /// </summary>
    public sealed class ActiveWindowStepHandler : JobStepHandler<ActiveWindowStep, ActiveWindowResult>
    {
        protected override Task<ActiveWindowResult> ExecuteCoreAsync(
            ActiveWindowStep step, IStepPipelineContext ctx, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var processName = ProcessWindowMatcher.NormalizeProcessName(step.Settings.ProcessName);
            var cacheMs = Math.Max(0, step.Settings.CacheMs);

            if (cacheMs > 0 &&
                ctx.ActiveWindowCache.TryGetValue(step.Id, out var cached) &&
                string.Equals(cached.ProcessName, processName, StringComparison.OrdinalIgnoreCase) &&
                (DateTime.Now - cached.Timestamp).TotalMilliseconds < cacheMs)
            {
                ctx.Logger.LogInformation(
                    "ActiveWindowStepHandler: Gecachtes Ergebnis für Prozess '{Process}' verwendet - Fenster aktiv: {IsActive}.",
                    processName,
                    cached.IsActive);
                return Task.FromResult(new ActiveWindowResult { WasExecuted = true, IsActive = cached.IsActive });
            }

            var isActive = ProcessWindowMatcher.ForegroundWindowMatches(processName);

            ctx.Logger.LogInformation(
                "ActiveWindowStepHandler: Prozess '{Process}' - Fenster aktiv: {IsActive}.",
                processName,
                isActive);

            return Task.FromResult(CacheResult(ctx, step.Id, processName, isActive));
        }

        protected override ActiveWindowResult CreateDefault() => ActiveWindowResult.Default;

        private static ActiveWindowResult CacheResult(
            IStepPipelineContext ctx,
            string stepId,
            string processName,
            bool isActive)
        {
            ctx.ActiveWindowCache[stepId] = new ActiveWindowCacheEntry(processName, isActive, DateTime.Now);
            return new ActiveWindowResult { WasExecuted = true, IsActive = isActive };
        }
    }
}
