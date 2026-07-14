using System.Diagnostics;

namespace TaskAutomation.Timing;

public interface IPreciseDelayService
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);
    Task DelayUntilAsync(long targetTimestamp, CancellationToken cancellationToken = default);
}

public static class PreciseTime
{
    public static long AddMilliseconds(long timestamp, long milliseconds)
    {
        if (milliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(milliseconds));
        if (milliseconds == 0) return timestamp;
        var delta = milliseconds * (double)Stopwatch.Frequency / 1000d;
        return checked(timestamp + (long)Math.Ceiling(delta));
    }
}
