namespace TaskAutomation.Logging;

/// <summary>Called under the repository gate. Keeps one detailed success and summaries of repetitions.</summary>
internal sealed class LogStepAggregation
{
    private readonly Dictionary<Guid, LogEvent> _starts = new();
    private readonly Dictionary<Guid, IReadOnlyList<LogPath>> _paths = new();
    private readonly Dictionary<Guid, Guid> _detailed = new();
    private readonly Dictionary<Guid, DateTimeOffset> _savedAt = new();
    public IEnumerable<LogEvent> PendingStarts => _starts.Values;

    public bool Observe(LogEvent entry, ref LogRun run, out LogEvent? start, out bool changed)
    {
        start = null; changed = false;
        if (entry.Source == LogSource.Job && entry.Code == LogCodes.IterationCompleted && entry.Level < ExecutionLogLevel.Warning)
        {
            run = run with
            {
                CompletedIterations = run.CompletedIterations + 1,
                IterationDurationMs = run.IterationDurationMs + (entry.DurationMs ?? 0)
            };
            changed = true;
            return true;
        }
        if (entry.Phase != "Main" || entry.Iteration is null || entry.Context.StepId is null
            || entry.Context.StepExecutionId is not { } execution) return false;
        if (entry.Source == LogSource.Job && entry.Code == LogCodes.StepStarted
            && run.StepSummaries.Any(summary => summary.StepId == entry.Context.StepId && summary.Phase == entry.Phase))
        {
            _starts[execution] = entry;
            return true;
        }
        if (entry.Level >= ExecutionLogLevel.Warning || entry.Code is LogCodes.StepFailed or LogCodes.StepCancelled
            || entry.FlowEffect?.Kind is "StopPhase" or "EndJob")
        {
            if (run.EndedAt is null) _detailed[execution] = run.Id;
            _starts.Remove(execution, out start);
            if (_paths.Remove(execution, out var paths) && start is not null) start = start with { Paths = paths };
            if (entry.Code is LogCodes.StepFailed or LogCodes.StepCancelled or LogCodes.StepCompleted or LogCodes.StepSkipped)
                _detailed.Remove(execution);
            return false;
        }
        if (entry.Source == LogSource.Job && entry.Code is LogCodes.StepCompleted or LogCodes.StepSkipped)
        {
            if (_detailed.Remove(execution)) { _starts.Remove(execution, out start); return false; }
            var reason = entry.Parameters.GetValueOrDefault("Reason");
            var previous = run.StepSummaries.FirstOrDefault(summary => summary.StepId == entry.Context.StepId
                && summary.Phase == entry.Phase && summary.Code == entry.Code && summary.Reason == reason);
            var ranges = (previous?.Iterations ?? []).ToList();
            var coverageComplete = previous?.IterationCoverageComplete ?? true;
            var iteration = entry.Iteration.Value;
            if (ranges.Count > 0 && ranges[^1].Last + 1 == iteration)
                ranges[^1] = ranges[^1] with { Last = iteration };
            else if (ranges.Count < 256) ranges.Add(new(iteration, iteration));
            else { ranges[^1] = new(iteration, iteration); coverageComplete = false; }
            if (_paths.Remove(execution, out var paths)) entry = entry with { Paths = entry.Paths.Concat(paths).Distinct().ToArray() };
            var summary = new LogStepSummary(entry.Context.StepId, entry.Phase, entry.Code, reason,
                (previous?.Count ?? 0) + 1, (previous?.TotalDurationMs ?? 0) + (entry.DurationMs ?? 0), ranges, entry, coverageComplete);
            run = run with { StepSummaries = run.StepSummaries.Where(item => item != previous).Append(summary).ToArray() };
            changed = true;
            _starts.Remove(execution, out start);
            // The first occurrence supplies representative details; later successes only update the summary.
            if (previous is null) return false;
            start = null;
            return true;
        }
        if (_starts.ContainsKey(execution) && entry.Code is LogCodes.Message or LogCodes.StepPaths or LogCodes.StepOutput)
        {
            if (entry.Paths.Count > 0) _paths[execution] = (_paths.GetValueOrDefault(execution) ?? []).Concat(entry.Paths).Distinct().ToArray();
            return true;
        }
        return false;
    }

    public bool ShouldSave(Guid runId, DateTimeOffset now)
    {
        if (_savedAt.TryGetValue(runId, out var last) && now - last < TimeSpan.FromSeconds(1)) return false;
        _savedAt[runId] = now;
        return true;
    }

    public void EndRun(Guid runId)
    {
        foreach (var id in _starts.Where(pair => pair.Value.Context.RunId == runId).Select(pair => pair.Key).ToArray())
        { _starts.Remove(id); _paths.Remove(id); }
        _savedAt.Remove(runId);
        foreach (var id in _detailed.Where(pair => pair.Value == runId).Select(pair => pair.Key).ToArray()) _detailed.Remove(id);
    }
}
