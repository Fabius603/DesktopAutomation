namespace DesktopAutomationApp.Behaviors;

/// <summary>Presentation thresholds; structural validity remains in the move command.</summary>
internal static class StepDragPreviewPolicy
{
    public const double GapHeight = 76;
    public static bool CanSnap(double distance, bool retainingTarget) =>
        double.IsFinite(distance) && distance <= (retainingTarget ? 44 : 30);

    public static double ScrollVelocity(double position, double viewport)
    {
        if (!double.IsFinite(position) || !double.IsFinite(viewport) || viewport <= 0 || position < 0 || position > viewport) return 0;
        var edge = Math.Min(72, viewport / 3);
        if (position < edge) return -360 * Math.Pow(1 - position / edge, 2);
        if (position > viewport - edge) return 360 * Math.Pow(1 - (viewport - position) / edge, 2);
        return 0;
    }

    public static double EaseScrollVelocity(double current, double target, double elapsed)
    {
        // Stop immediately outside the edge zone, and start reversals from rest.
        if (target == 0) return 0;
        if (Math.Sign(current) != Math.Sign(target)) current = 0;
        return current + (target - current) * (1 - Math.Exp(-Math.Clamp(elapsed, 0, 0.05) / 0.12));
    }
}
