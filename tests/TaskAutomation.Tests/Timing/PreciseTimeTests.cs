using System.Diagnostics;
using TaskAutomation.Timing;

namespace TaskAutomation.Tests.Timing;

public sealed class PreciseTimeTests
{
    [Fact] public void AddMilliseconds_ZeroReturnsSameTimestamp() => Assert.Equal(123, PreciseTime.AddMilliseconds(123, 0));
    [Fact] public void AddMilliseconds_NegativeThrows() => Assert.Throws<ArgumentOutOfRangeException>(() => PreciseTime.AddMilliseconds(0, -1));
    [Fact] public void AddMilliseconds_OneSecondAddsStopwatchFrequency() => Assert.Equal(Stopwatch.Frequency,
        PreciseTime.AddMilliseconds(0, 1000));
    [Fact] public void AddMilliseconds_FractionalTickDurationRoundsUp() => Assert.True(PreciseTime.AddMilliseconds(0, 1) >= 1);
    [Fact] public void AddMilliseconds_OverflowThrows() => Assert.Throws<OverflowException>(() => PreciseTime.AddMilliseconds(long.MaxValue, 1));
}
