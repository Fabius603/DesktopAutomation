using TaskAutomation.Jobs;
using TaskAutomation.Steps.Definitions;

namespace TaskAutomation.Logging;

public sealed record LogEventDisplay(LogText Title, LogText Summary, LogArea Category, string AreaKey, LogDiagnostic? Diagnostic);
public sealed record LogStepDisplay(LogStepExecution Execution, string TitleKey, string? IconKey, LogText Summary, LogText? Impact);

/// <summary>Frontend-neutral meaning and resource keys. No message parsing or localized string generation.</summary>
public static class LogPresentation
{
    public static LogArea CategoryForStep(JobStep step) => step switch
    {
        SaveImageStep or FileSystemOperationStep => LogArea.FileAccess,
        DesktopDuplicationStep or CameraCaptureStep => LogArea.Capture,
        TemplateMatchingStep or OcrStep or ColorDetectionStep or YOLODetectionStep or KeyPointMatchingStep => LogArea.Detection,
        StartProcessStep or TerminateProcessStep or GetProcessStep or ActiveProcessStep or ActiveWindowStep or FocusProcessStep => LogArea.Process,
        ScriptExecutionStep => LogArea.Script,
        VideoCreationStep => LogArea.Video,
        WindowsStateQueryStep or WindowsSettingChangeStep => LogArea.Windows,
        _ => LogArea.Execution
    };
    public static LogArea Category(LogEvent entry)
    {
        if (entry.Category != LogArea.General) return entry.Category;
        if (entry.DiagnosticCode is LogCodes.FileUnavailable or LogCodes.AccessDenied) return LogArea.FileAccess;
        if (entry.Source == LogSource.Automation) return LogArea.Automation;
        if (entry.Parameters.GetValueOrDefault("StepType") is { } name)
        {
            var definition = BuiltInStepDefinitions.Instance.Definitions.FirstOrDefault(item => item.StepType.Name == name);
            if (definition is not null) return CategoryForStep(definition.CreateDefault());
        }
        return entry.Source is LogSource.Job or LogSource.Makro ? LogArea.Execution : LogArea.General;
    }
    private static LogText Text(string key, IReadOnlyDictionary<string, string?>? args = null) => new(key, args ?? new Dictionary<string, string?>());
    public static LogEventDisplay Event(LogEvent entry)
    {
        var category = Category(entry);
        var diagnostic = entry.DiagnosticCode is null ? null : LogDiagnostics.Describe(entry);
        var title = diagnostic is not null ? "Log.Title." + diagnostic.Code : entry.Code switch
        {
            LogCodes.StepStarted => "Log.Event.StepStarted",
            LogCodes.StepCompleted => "Log.Event.StepCompleted",
            LogCodes.StepFailed or LogCodes.StepBackgroundFailed => "Log.Event.StepFailed",
            LogCodes.StepCancelled => "Log.Event.StepCancelled",
            LogCodes.StepSkipped => "Log.Event.StepSkipped",
            LogCodes.StepOutput => "Log.Event.ScriptOutput",
            LogCodes.Trigger => "Log.Event.Trigger",
            LogCodes.AutomationDecision => "Log.Event.AutomationDecision",
            LogCodes.RunStarted => "Log.Event.RunStarted",
            LogCodes.RunCompleted => "Log.Event.RunCompleted",
            _ => "Log.Event.Technical"
        };
        return new(Text(title), diagnostic is not null ? Text(diagnostic.CauseKey) : Summary(entry), category, "Log.Area." + category, diagnostic);
    }
    public static LogText Summary(LogEvent entry)
    {
        var p = entry.Parameters;
        if (entry.Code == LogCodes.StepFailed || entry.Code == LogCodes.StepBackgroundFailed) return Text("Log.Summary.Failed");
        if (entry.Code == LogCodes.StepCancelled) return Text(p.GetValueOrDefault("Reason") == "UserCancelled" ? "Log.Summary.UserCancelled" : "Log.Summary.Cancelled");
        if (entry.Code == LogCodes.StepSkipped) return Text(p.GetValueOrDefault("Reason") switch
        {
            "Disabled" => "Log.Summary.Disabled",
            "InactiveBranch" => "Log.Summary.BranchInactive",
            "EndPhaseSuppressed" => "Log.Summary.EndPhaseSuppressed",
            "InvalidControlFlow" => "Log.Summary.InvalidControlFlow",
            "NoInput" => "Log.Summary.NoInput",
            "CachedCapture" => "Log.Summary.CachedCapture",
            _ => "Log.Summary.Skipped"
        });
        if (entry.BranchDecision?.Reason == "ConditionUnavailable") return Text("Log.Summary.ConditionUnavailable");
        if (entry.BranchDecision is { } branch) return Text(branch.BranchActive ? "Log.Summary.BranchSelected" : "Log.Summary.BranchInactive");
        if (entry.Code == LogCodes.StepOutput) return Text("Log.Summary.ScriptOutput", new Dictionary<string, string?> { ["CharacterCount"] = p.GetValueOrDefault("CharacterCount") });
        if (p.GetValueOrDefault("CompletionScope") == "Dispatch") return Text("Log.Summary.Dispatched");
        if (p.GetValueOrDefault("Success") == "False") return Text("Log.Summary.Unsuccessful");
        if (p.GetValueOrDefault("Status") is { } status && status != "Success") return Text("Log.Summary.Unsuccessful");
        if (p.GetValueOrDefault("WasExecuted") == "False") return Text("Log.Summary.NotExecuted");
        if (p.GetValueOrDefault("ResultKind") == nameof(Steps.SaveImageResult)) return Text("Log.Summary.ImageSaved", Dimensions(p));
        if (p.GetValueOrDefault("HasImage") == "True") return Text("Log.Summary.Captured", Dimensions(p));
        if (p.GetValueOrDefault("ResultKind") == nameof(Steps.OcrResult)) return Text("Log.Summary.Ocr", new Dictionary<string, string?> { ["WordCount"] = p.GetValueOrDefault("WordCount") });
        if (p.ContainsKey("AffectedCount")) return Text("Log.Summary.Files", new Dictionary<string, string?> { ["AffectedCount"] = p.GetValueOrDefault("AffectedCount") });
        if (p.GetValueOrDefault("Exists") is "True" or "False") return Text(p["Exists"] == "True" ? "Log.Summary.Exists" : "Log.Summary.Missing");
        if (p.GetValueOrDefault("Found") is "True" or "False") return Text(p["Found"] == "True" ? "Log.Summary.Found" : "Log.Summary.NotFound");
        return Text(entry.Code == LogCodes.StepCompleted ? "Log.Summary.Completed" : "Log.Summary.Technical");
    }
    private static Dictionary<string, string?> Dimensions(Dictionary<string, string?> p) => new() { ["Width"] = p.GetValueOrDefault("Width"), ["Height"] = p.GetValueOrDefault("Height") };
    public static LogStepDisplay Step(LogStepExecution step, IReadOnlyList<LogStepSnapshot> snapshot)
    {
        BuiltInStepDefinitions.Instance.TryGetByTypeId(step.Step.TypeId, out var definition);
        var terminal = step.Events.LastOrDefault(entry => entry.Source == LogSource.Job && entry.Code is LogCodes.StepCompleted or LogCodes.StepFailed or LogCodes.StepCancelled or LogCodes.StepSkipped);
        var summary = step.Outcome == StepLogOutcome.Failed ? Text("Log.Summary.Failed") : terminal is not null ? Summary(terminal) : Text("Log.Summary." + step.Outcome);
        if (step.Summary is { } aggregate) summary = Text("Log.Summary.Aggregated", new Dictionary<string, string?>
        { ["Count"] = aggregate.Count.ToString(System.Globalization.CultureInfo.InvariantCulture) });
        var causePosition = snapshot.FirstOrDefault(item => item.Id == step.Cause?.StepId)?.Position;
        var impact = step.Cause is not null && causePosition.HasValue ? Text("Log.Impact.Prevented", new Dictionary<string, string?> { ["Position"] = causePosition.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) }) : null;
        if (impact is null && terminal?.FlowEffect is not null && terminal.FlowEffect.Kind != "NextIteration")
            impact = Text("Log.Impact.Stopped", new Dictionary<string, string?> { ["Position"] = step.Step.Position.ToString(System.Globalization.CultureInfo.InvariantCulture) });
        return new(step, definition?.Descriptor.DisplayNameKey ?? "Log.Event.UnknownStep", definition?.Descriptor.IconKey, summary, impact);
    }
}
