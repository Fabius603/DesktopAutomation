namespace TaskAutomation.Logging;

public static class LogTimeline
{
    public static IReadOnlyList<LogStepExecution> Build(LogRun run, IReadOnlyList<LogEvent> events)
    {
        var result = new List<LogStepExecution>();
        var iterations = events.Where(entry => entry.Phase == "Main" && entry.Iteration.HasValue)
            .Select(entry => entry.Iteration!.Value).Distinct().Order().ToArray();
        foreach (var step in run.Steps)
        {
            var own = events.Where(entry => entry.Context.StepId == step.Id
                && entry.Source == LogSource.Job && entry.Code is LogCodes.StepStarted or LogCodes.StepCompleted
                    or LogCodes.StepFailed or LogCodes.StepSkipped or LogCodes.StepCancelled).ToArray();
            var groups = own.GroupBy(entry => (entry.Context.StepExecutionId, entry.Iteration));
            foreach (var group in groups)
            {
                var ordered = group.OrderBy(entry => entry.Sequence).ToArray();
                var last = ordered.Last();
                var related = group.Key.StepExecutionId.HasValue ? events.Where(entry =>
                    entry.Context.StepExecutionId == group.Key.StepExecutionId).OrderBy(entry => entry.Sequence).ToArray() : ordered;
                var outcome = last.Code switch
                {
                    LogCodes.StepFailed => StepLogOutcome.Failed,
                    LogCodes.StepCancelled => StepLogOutcome.Cancelled,
                    LogCodes.StepSkipped => StepLogOutcome.Skipped,
                    LogCodes.StepCompleted => related.Any(entry => entry.Level == ExecutionLogLevel.Error) ? StepLogOutcome.Failed
                        : related.Any(entry => entry.Level == ExecutionLogLevel.Warning) ? StepLogOutcome.Warning : StepLogOutcome.Successful,
                    _ => run.EndedAt.HasValue || run.Outcome == LogOutcome.Interrupted ? StepLogOutcome.Unknown : StepLogOutcome.Running
                };
                result.Add(new(step, group.Key.StepExecutionId, step.Phase, group.Key.Iteration,
                    outcome, last.Parameters.GetValueOrDefault("Reason"), last.DurationMs, related));
            }
            IEnumerable<int?> expected = step.Phase == "Main" ? iterations.Select(iteration => (int?)iteration) : new int?[] { null };
            if (step.Phase == "Main" && iterations.Length == 0) expected = new int?[] { null };
            foreach (var iteration in expected.Where(iteration => !own.Any(entry => entry.Iteration == iteration)))
            {
                var completed = run.EndedAt.HasValue;
                result.Add(new(step, null, step.Phase, iteration,
                    !step.Enabled ? StepLogOutcome.Skipped : !run.IsComplete ? StepLogOutcome.Unknown
                        : completed ? StepLogOutcome.NotExecuted : StepLogOutcome.Pending,
                    !step.Enabled ? "Disabled" : completed ? run.CompletionReason : null, null, []));
            }
        }
        return result.OrderBy(entry => entry.Phase == "Start" ? 0 : entry.Phase == "Main" ? 1 : 2)
            .ThenBy(entry => entry.Iteration).ThenBy(entry => entry.Step.Position).ToArray();
    }
}
