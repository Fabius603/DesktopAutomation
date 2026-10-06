namespace TaskAutomation.Logging;

public static class LogTimeline
{
    public static IReadOnlyList<LogStepExecution> Build(LogRun run, IReadOnlyList<LogEvent> events)
    {
        var result = new List<LogStepExecution>();
        var lifecycle = events.Where(entry => entry.Source == LogSource.Job && entry.Context.StepId is not null
            && entry.Code is LogCodes.StepStarted or LogCodes.StepCompleted or LogCodes.StepFailed or LogCodes.StepSkipped or LogCodes.StepCancelled)
            .ToLookup(entry => entry.Context.StepId!);
        var executions = events.Where(entry => entry.Context.StepExecutionId.HasValue)
            .GroupBy(entry => entry.Context.StepExecutionId!.Value)
            .ToDictionary(group => group.Key, group => group.OrderBy(entry => entry.Sequence).ToArray());
        var effects = events.Where(entry => entry.FlowEffect is not null).OrderByDescending(entry => entry.Sequence).ToArray();
        var problems = events.Where(entry => entry.Context.StepId is not null && entry.Context.StepExecutionId.HasValue
            && entry.Level >= ExecutionLogLevel.Warning).ToLookup(entry => entry.Context.StepId!);
        var iterations = events.Where(entry => entry.Phase == "Main" && entry.Iteration.HasValue)
            .Select(entry => entry.Iteration!.Value).Distinct().Order().ToArray();
        foreach (var step in run.Steps)
        {
            var own = lifecycle[step.Id].ToArray();
            var summaries = run.StepSummaries.Where(summary => summary.StepId == step.Id && summary.Phase == step.Phase).ToArray();
            var lifecycleIds = own.Select(entry => entry.Context.StepExecutionId).ToHashSet();
            own = own.Concat(problems[step.Id].Where(entry => !lifecycleIds.Contains(entry.Context.StepExecutionId))).ToArray();
            var groups = own.GroupBy(entry => (entry.Context.StepExecutionId, entry.Iteration));
            foreach (var group in groups)
            {
                var ordered = group.OrderBy(entry => entry.Sequence).ToArray();
                var last = ordered.Last();
                var related = group.Key.StepExecutionId.HasValue ? executions[group.Key.StepExecutionId.Value] : ordered;
                var outcome = last.Code switch
                {
                    LogCodes.StepFailed => StepLogOutcome.Failed,
                    LogCodes.StepCancelled => StepLogOutcome.Cancelled,
                    LogCodes.StepSkipped => StepLogOutcome.Skipped,
                    LogCodes.StepCompleted => related.Any(entry => entry.Level == ExecutionLogLevel.Error) ? StepLogOutcome.Failed
                        : related.Any(entry => entry.Level == ExecutionLogLevel.Warning) ? StepLogOutcome.Warning : StepLogOutcome.Successful,
                    _ when last.Level == ExecutionLogLevel.Error => StepLogOutcome.Failed,
                    _ when last.Level == ExecutionLogLevel.Warning => StepLogOutcome.Warning,
                    _ => run.EndedAt.HasValue || run.Outcome == LogOutcome.Interrupted ? StepLogOutcome.Unknown : StepLogOutcome.Running
                };
                if (outcome is StepLogOutcome.Successful or StepLogOutcome.Skipped && summaries.Any(summary => summary.Count > 1
                    && summary.Code == last.Code && summary.Reason == last.Parameters.GetValueOrDefault("Reason")
                    && group.Key.Iteration.HasValue && summary.Iterations.Any(range => range.First <= group.Key.Iteration && range.Last >= group.Key.Iteration))) continue;
                result.Add(new(step, group.Key.StepExecutionId, step.Phase, group.Key.Iteration,
                    outcome, last.Parameters.GetValueOrDefault("Reason"), last.DurationMs, related));
            }
            foreach (var summary in summaries.Where(summary => summary.Count > 1
                || !own.Any(entry => entry.Id == summary.LastEvent.Id)))
                result.Add(new(step, summary.LastEvent.Context.StepExecutionId, step.Phase, null,
                    summary.Code == LogCodes.StepSkipped ? StepLogOutcome.Skipped : StepLogOutcome.Successful,
                    summary.Reason, summary.TotalDurationMs, [summary.LastEvent], Summary: summary));
            IEnumerable<int?> expected = step.Phase == "Main" ? iterations.Select(iteration => (int?)iteration) : new int?[] { null };
            if (step.Phase == "Main" && iterations.Length == 0) expected = new int?[] { null };
            var observedIterations = own.Select(entry => entry.Iteration).ToHashSet();
            foreach (var iteration in expected.Where(iteration => !observedIterations.Contains(iteration)
                && !(iteration.HasValue && summaries.Any(summary => summary.Iterations.Any(range => range.First <= iteration && range.Last >= iteration)
                    || !summary.IterationCoverageComplete && iteration <= summary.LastEvent.Iteration))
                && !(iteration is null && summaries.Length > 0)))
            {
                var completed = run.EndedAt.HasValue;
                result.Add(new(step, null, step.Phase, iteration,
                    !step.Enabled ? StepLogOutcome.Skipped : !run.IsComplete ? StepLogOutcome.Unknown
                        : completed ? StepLogOutcome.NotExecuted : StepLogOutcome.Pending,
                    !step.Enabled ? "Disabled" : completed ? run.CompletionReason : null, null, []));
            }
        }
        return result.Select(item => item with { Cause = CauseFor(item, run, effects) })
            .OrderBy(entry => entry.Phase == "Start" ? 0 : entry.Phase == "Main" ? 1 : 2)
            .ThenBy(entry => entry.Iteration).ThenBy(entry => entry.Step.Position).ToArray();
    }

    private static LogStepCause? CauseFor(LogStepExecution item, LogRun run, IReadOnlyList<LogEvent> events)
    {
        if (!run.IsComplete || item.Outcome is not (StepLogOutcome.NotExecuted or StepLogOutcome.Skipped)) return null;
        var cause = events.Where(entry => entry.Source == LogSource.Job && entry.FlowEffect is not null
            && entry.Context.StepId != item.Step.Id && entry.Context.StepId is not null)
            .OrderByDescending(entry => entry.Sequence).FirstOrDefault(entry =>
            {
                var source = run.Steps.FirstOrDefault(step => step.Id == entry.Context.StepId);
                if (source is null) return false;
                if (item.Reason == "EndPhaseSuppressed") return entry.FlowEffect?.Kind == "EndJob" && entry.Parameters.GetValueOrDefault("SkipEndSteps") == "True";
                if (item.Outcome != StepLogOutcome.NotExecuted) return false;
                return source.Phase == item.Phase && entry.Iteration == item.Iteration && source.Position < item.Step.Position
                    || source.Phase == "Start" && item.Phase == "Main" && entry.FlowEffect?.Kind is "StopPhase" or "EndJob";
            });
        return cause is null ? null : new(cause.Context.StepId!, cause.Context.StepExecutionId,
            cause.Parameters.GetValueOrDefault("Reason") ?? cause.FlowEffect!.Kind, cause.Phase ?? "Unknown", cause.Iteration);
    }
}
