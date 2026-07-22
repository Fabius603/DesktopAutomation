namespace TaskAutomation.Makros;

public readonly record struct MakroTimelineEntry(MakroBefehl Command, long ExecutionTimeMicroseconds);

public static class MakroTimeline
{
    public static IReadOnlyList<MakroTimelineEntry> Calculate(IEnumerable<MakroBefehl> commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        var result = new List<MakroTimelineEntry>();
        long elapsed = 0;

        foreach (var command in commands)
        {
            elapsed = checked(elapsed + Math.Max(0, command.DelayBeforeMicroseconds ?? 0));
            result.Add(new MakroTimelineEntry(command, elapsed));
            if (command is TimeoutBefehl timeout)
                elapsed = checked(elapsed + timeout.Duration * 1_000L);
        }

        return result;
    }

    public static long GetTotalDurationMicroseconds(IEnumerable<MakroBefehl> commands)
    {
        var timeline = Calculate(commands);
        if (timeline.Count == 0) return 0;
        var last = timeline[^1];
        return checked(last.ExecutionTimeMicroseconds
            + (last.Command is TimeoutBefehl timeout ? timeout.Duration * 1_000L : 0));
    }
}
