using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace TaskAutomation.Logging;

public static class StepLogEvents
{
    public static string Phase(string phase) => phase switch { "Startphase" or "Start" => "Start", "Endphase" or "End" => "End", _ => "Main" };
    public static ExecutionLogLevel ResultLevel(object? result) => result switch
    {
        IActionExecutionResult { Success: false } => ExecutionLogLevel.Warning,
        WindowsStateQueryResult { Status: not TaskAutomation.WindowsIntegration.WindowsCapabilityStatus.Success } => ExecutionLogLevel.Warning,
        _ => ExecutionLogLevel.Information
    };
    public static Dictionary<string, string?> Result(object? result) => StepLogResults.Summarize(result);
    public static LogEvent Create(JobStep step, string code, string phase, int? iteration = null,
        ExecutionLogLevel level = ExecutionLogLevel.Information, string? reason = null, long? durationMs = null,
        Exception? error = null, object? result = null)
    {
        var parameters = Result(result);
        parameters["StepType"] = step.GetType().Name;
        parameters["Reason"] = reason;
        if (step is EndJobStep { Settings: not null } end) parameters["SkipEndSteps"] = end.Settings.SkipEndSteps.ToString();
        if (step is ScriptExecutionStep { Settings: not null } script) parameters["CompletionScope"] = script.Settings.WaitForExit ? "Execution" : "Dispatch";
        if (step is JobExecutionStep { Settings: not null } job) parameters["CompletionScope"] = job.Settings.WaitForCompletion ? "Execution" : "Dispatch";
        if (error is FileNotFoundException { FileName: { } missingPath }) parameters["Path"] = missingPath;
        if (!parameters.ContainsKey("Path") && step is SaveImageStep { Settings: not null } save)
            parameters["Path"] = Path.Combine(save.Settings.SavePath ?? "", save.Settings.FileName ?? "");
        return new LogEvent
        {
            Code = code,
            Context = LogAmbient.Current with { StepId = step.Id },
            Phase = Phase(phase),
            Iteration = iteration,
            Level = level,
            DurationMs = durationMs,
            Message = code switch
            {
                LogCodes.StepStarted => "Step gestartet.",
                LogCodes.StepCompleted => "Step abgeschlossen.",
                LogCodes.StepFailed => "Step fehlgeschlagen.",
                LogCodes.StepSkipped => "Step übersprungen.",
                LogCodes.StepCancelled => "Step abgebrochen.",
                _ => code
            },
            Parameters = parameters,
            DiagnosticCode = error is null ? null : LogDiagnostics.Code(error),
            ProblemId = level >= ExecutionLogLevel.Warning ? error is not null
                ? LogDiagnostics.ProblemId(error, LogAmbient.Current.StepExecutionId)
                : LogAmbient.Current.StepExecutionId ?? Guid.NewGuid() : null,
            Details = error is null ? null : LogDiagnostics.ExceptionDetails(error)
        };
    }
}
