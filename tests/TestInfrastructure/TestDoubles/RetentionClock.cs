namespace TaskAutomation.Tests.TestDoubles;

internal sealed class RetentionClock : TimeProvider
{
    private DateTimeOffset _now = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);
    private readonly List<ManualTimer> _timers = [];
    public override DateTimeOffset GetUtcNow() => _now;
    public bool AllTimersDisposed => _timers.All(timer => timer.Disposed);
    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        var timer = new ManualTimer(this, callback, state);
        timer.Change(dueTime, period); _timers.Add(timer); return timer;
    }
    public void Advance(TimeSpan duration)
    {
        _now += duration;
        foreach (var timer in _timers.ToArray()) timer.FireIfDue();
    }
    private sealed class ManualTimer(RetentionClock clock, TimerCallback callback, object? state) : ITimer
    {
        private DateTimeOffset _due;
        private TimeSpan _period;
        public bool Disposed { get; private set; }
        public bool Change(TimeSpan dueTime, TimeSpan period) { _due = clock._now + dueTime; _period = period; return !Disposed; }
        public void FireIfDue()
        {
            if (Disposed || clock._now < _due) return;
            _due = clock._now + _period; callback(state);
        }
        public void Dispose() => Disposed = true;
        public ValueTask DisposeAsync() { Dispose(); return ValueTask.CompletedTask; }
    }
}
