using System.Diagnostics;

namespace TaskAutomation.Timing;

public interface IPreciseDelayService
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default);
    Task DelayUntilAsync(long targetTimestamp, CancellationToken cancellationToken = default);
}

public static class PreciseTime
{
    public static long AddMicroseconds(long timestamp, long microseconds)
    {
        if (microseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(microseconds));
        if (microseconds == 0) return timestamp;
        var delta = microseconds * (double)Stopwatch.Frequency / 1_000_000d;
        return checked(timestamp + (long)Math.Ceiling(delta));
    }

    public static long AddMilliseconds(long timestamp, long milliseconds)
    {
        if (milliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(milliseconds));
        if (milliseconds == 0) return timestamp;
        var delta = milliseconds * (double)Stopwatch.Frequency / 1000d;
        return checked(timestamp + (long)Math.Ceiling(delta));
    }
}
