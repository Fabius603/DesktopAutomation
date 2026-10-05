namespace DesktopAutomationApp.Behaviors;

/// <summary>Presentation thresholds; structural validity remains in the move command.</summary>
internal static class StepDragPreviewPolicy
{
    public const double GapHeight = 76;
    public static bool CanSnap(double distance, bool retainingTarget) =>
        double.IsFinite(distance) && distance <= (retainingTarget ? 44 : 30);

    public static double ScrollVelocity(double position, double viewport)
    {
        if (viewport <= 0 || position < 0 || position > viewport) return 0;
        var edge = Math.Min(56, viewport / 3);
        if (position < edge) return -600 * Math.Pow(1 - position / edge, 2);
        if (position > viewport - edge) return 600 * Math.Pow(1 - (viewport - position) / edge, 2);
        return 0;
    }
}
