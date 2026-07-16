using System.Drawing;
using OpenCvSharp;
using Microsoft.Extensions.Logging;

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
    public static Rect? Resolve(string? dynamicRoiStepId, CaptureResult capture, IStepPipelineContext context)
    {
        if (string.IsNullOrWhiteSpace(dynamicRoiStepId)
            || !context.DynamicRoiStates.TryGetValue(dynamicRoiStepId, out var state)
            || state.GlobalBounds is not Rectangle global)
        {
            if (!string.IsNullOrWhiteSpace(dynamicRoiStepId))
                context.Logger.LogDebug("Dynamic ROI {StepId}: noch keine ROI vorhanden; Basis-Suchbereich wird verwendet.", dynamicRoiStepId);
            return null;
        }

        if (state.FullSearchInterval > 0 && state.RoiUsesSinceFullSearch >= state.FullSearchInterval)
        {
            state.RoiUsesSinceFullSearch = 0;
            context.Logger.LogInformation("Dynamic ROI {StepId}: periodische Basis-Suche nach {Interval} ROI-Durchläufen.",
                dynamicRoiStepId, state.FullSearchInterval);
            return null;
        }

        var captureBounds = capture.Bounds;
        var intersection = Rectangle.Intersect(global, captureBounds);
        if (intersection.Width <= 0 || intersection.Height <= 0) return null;
        state.RoiUsesSinceFullSearch++;
        var resolved = new Rect(
            intersection.X - capture.Offset.X,
            intersection.Y - capture.Offset.Y,
            intersection.Width,
            intersection.Height);
        context.Logger.LogDebug("Dynamic ROI {StepId}: lokale ROI angewendet X={X}, Y={Y}, B={Width}, H={Height}; Nutzung={Use}/{Interval}.",
            dynamicRoiStepId, resolved.X, resolved.Y, resolved.Width, resolved.Height,
            state.RoiUsesSinceFullSearch, state.FullSearchInterval);
        return resolved;
    }
}
