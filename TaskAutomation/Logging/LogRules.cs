namespace TaskAutomation.Logging;

public static class LogOutcomeRules
{
    public static LogOutcome Complete(LogOutcome requested, int errors, int warnings)
        => requested is LogOutcome.Stopped or LogOutcome.Failed or LogOutcome.Interrupted ? requested
            : errors > 0 ? LogOutcome.WithErrors : warnings > 0 ? LogOutcome.WithWarnings : LogOutcome.Successful;
}

public static class LogAmbient
{
    private static readonly AsyncLocal<LogContext?> Context = new();
    public static LogContext Current => Context.Value ?? new();
    public static IDisposable Push(LogContext context)
    {
        var previous = Context.Value; Context.Value = context;
        return new Scope(() => Context.Value = previous);
    }
    private sealed class Scope(Action restore) : IDisposable { public void Dispose() => restore(); }
}

public static class LogDiagnostics
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Exception, ProblemIdentity> Problems = new();
    private sealed record ProblemIdentity(Guid Id);
    public static Guid ProblemId(Exception error, Guid? scopeId = null)
        => Problems.GetValue(error, _ => new(scopeId ?? Guid.NewGuid())).Id;
    // Exception messages can contain materialized inputs or output. Keep technical evidence without those values.
    public static string ExceptionDetails(Exception error)
        => $"{error.GetType().FullName} (HResult=0x{error.HResult:X8})\n{error.StackTrace}"
            + (error.InnerException is { } inner ? "\nInner: " + ExceptionDetails(inner) : "");
    public static string Code(Exception error) => error switch
    {
        DirectoryNotFoundException or FileNotFoundException => LogCodes.FileUnavailable,
        UnauthorizedAccessException => LogCodes.AccessDenied,
        _ => LogCodes.Unexpected
    };
    public static LogDiagnostic Describe(LogEvent entry)
    {
        var code = entry.DiagnosticCode is LogCodes.FileUnavailable or LogCodes.AccessDenied ? entry.DiagnosticCode : LogCodes.Unexpected;
        var actions = new List<LogAction>();
        if (entry.Context.RunId.HasValue) actions.Add(new("OpenRun", entry.Context.RunId, entry.Context.StepId));
        if (entry.Context.StepId is not null) actions.Add(new("OpenStep", entry.Context.RunId, entry.Context.StepId));
        if (code is LogCodes.FileUnavailable or LogCodes.AccessDenied)
        {
            actions.AddRange(PathActions(entry));
        }
        return new(code, $"Log.Cause.{code}", $"Log.Help.{code}", actions);
    }

    public static IReadOnlyList<LogAction> PathActions(LogEvent entry)
    {
        IEnumerable<string> paths = entry.Paths.Select(path => path.Value);
        if (entry.Paths.Count == 0 && entry.Parameters.GetValueOrDefault("Path") is { } legacy) paths = [legacy];
        return paths.Where(path => !string.IsNullOrWhiteSpace(path) && !path.Contains("[redacted]", StringComparison.Ordinal)
            && path.IndexOfAny(Path.GetInvalidPathChars()) < 0 && Path.IsPathFullyQualified(path))
            .Distinct(StringComparer.OrdinalIgnoreCase).Select(path => new LogAction("CheckPath", Path: path)).ToArray();
    }
}
