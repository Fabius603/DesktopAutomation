using System.Drawing;
using OpenCvSharp;
using Microsoft.Extensions.Logging;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public sealed class DynamicRoiState
{
    public Rectangle? GlobalBounds { get; set; }
    public int FullSearchInterval { get; set; }
    public int RoiUsesSinceFullSearch { get; set; }
    public int ConsecutiveMisses { get; set; }
}

internal static class DynamicRoiResolver
{
    public static Rect? Resolve(
        ResultBinding? dynamicRoiSource,
        ICaptureStepResult capture,
        IStepPipelineContext context,
        Rect? staticRoi = null)
    {
        var resolvedBounds = ResultBindingResolver.Resolve<Rectangle>(context.Results, dynamicRoiSource);
        var dynamicRoiStepId = dynamicRoiSource?.SourceStepId;
        if (!resolvedBounds.IsSuccess
            || string.IsNullOrWhiteSpace(dynamicRoiStepId))
        {
            if (dynamicRoiSource?.IsConfigured == true)
                context.Logger.LogDebug("Dynamic ROI {StepId}: noch keine ROI vorhanden; Basis-Suchbereich wird verwendet.", dynamicRoiStepId);
            return null;
        }

        var global = resolvedBounds.FirstOrDefault;
        context.DynamicRoiStates.TryGetValue(dynamicRoiStepId, out var state);

        if (state is not null
            && state.FullSearchInterval > 0
            && state.RoiUsesSinceFullSearch >= state.FullSearchInterval)
        {
            state.RoiUsesSinceFullSearch = 0;
            context.Logger.LogInformation("Dynamic ROI {StepId}: periodische Basis-Suche nach {Interval} ROI-Durchläufen.",
                dynamicRoiStepId, state.FullSearchInterval);
            return null;
        }

        var captureBounds = capture.Bounds;
        var intersection = Rectangle.Intersect(global, captureBounds);
        if (intersection.Width <= 0 || intersection.Height <= 0) return null;
        if (state is not null)
            state.RoiUsesSinceFullSearch++;
        var resolved = new Rect(
            intersection.X - capture.Offset.X,
            intersection.Y - capture.Offset.Y,
            intersection.Width,
            intersection.Height);

        if (staticRoi is Rect baseRoi && baseRoi.Width > 0 && baseRoi.Height > 0)
        {
            var left = System.Math.Max(resolved.X, baseRoi.X);
            var top = System.Math.Max(resolved.Y, baseRoi.Y);
            var right = System.Math.Min(resolved.X + resolved.Width, baseRoi.X + baseRoi.Width);
            var bottom = System.Math.Min(resolved.Y + resolved.Height, baseRoi.Y + baseRoi.Height);
            if (right <= left || bottom <= top)
            {
                context.Logger.LogInformation(
                    "Dynamic ROI {StepId}: keine Überschneidung mit der festen ROI; feste ROI wird verwendet.",
                    dynamicRoiStepId);
                return null;
            }

            resolved = new Rect(left, top, right - left, bottom - top);
        }

        context.Logger.LogDebug("Dynamic ROI {StepId}: lokale ROI angewendet X={X}, Y={Y}, B={Width}, H={Height}; Nutzung={Use}/{Interval}.",
            dynamicRoiStepId, resolved.X, resolved.Y, resolved.Width, resolved.Height,
            state?.RoiUsesSinceFullSearch ?? 0, state?.FullSearchInterval ?? 0);
        return resolved;
    }
}
