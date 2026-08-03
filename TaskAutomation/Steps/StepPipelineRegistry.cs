using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

/// <summary>Runtime metadata that cannot be inferred from the portable editor descriptor.</summary>
public sealed record StepPipelineInfo(
    Type? ResultType,
    bool IsConditionSource = false,
    string DisplayName = "")
{
    public string Output => ResultType?.Name ?? "–";
}

public static class StepPipelineRegistry
{
    private static readonly IReadOnlyDictionary<Type, StepPipelineInfo> Map =
        new Dictionary<Type, StepPipelineInfo>
        {
            [typeof(DesktopDuplicationStep)] = new(typeof(DesktopDuplicationResult), DisplayName: "Desktop-Duplizierung"),
            [typeof(CameraCaptureStep)] = new(typeof(CameraCaptureResult), DisplayName: "Kameraaufnahme"),
            [typeof(FileSystemOperationStep)] = new(typeof(FileSystemOperationResult), true, "Datei oder Ordner bearbeiten"),
            [typeof(TemplateMatchingStep)] = new(typeof(TemplateMatchingResult), true, "Template Matching"),
            [typeof(ColorDetectionStep)] = new(typeof(ColorDetectionResult), true, "Farberkennung"),
            [typeof(YOLODetectionStep)] = new(typeof(YOLODetectionResult), true, "YOLO-Erkennung"),
            [typeof(KeyPointMatchingStep)] = new(typeof(KeyPointMatchingResult), true, "KeyPoint Matching"),
            [typeof(PredictMovementStep)] = new(typeof(PredictMovementResult), true, "Bewegung vorhersagen"),
            [typeof(DynamicRoiStep)] = new(typeof(DynamicRoiResult), true, "Dynamische ROI erstellen"),
            [typeof(KlickOnPointStep)] = new(typeof(KlickOnPointResult), DisplayName: "Klick auf Punkt"),
            [typeof(KlickOnPoint3DStep)] = new(typeof(KlickOnPoint3DResult), DisplayName: "Klick auf Punkt in 3D-Umgebung"),
            [typeof(MakroExecutionStep)] = new(typeof(MakroExecutionResult), DisplayName: "Makro ausführen"),
            [typeof(ScriptExecutionStep)] = new(typeof(ScriptExecutionResult), DisplayName: "Skript ausführen"),
            [typeof(JobExecutionStep)] = new(typeof(JobExecutionResult), DisplayName: "Job starten"),
            [typeof(TimeoutStep)] = new(typeof(TimeoutResult), DisplayName: "Timeout"),
            [typeof(ActiveProcessStep)] = new(typeof(ActiveProcessResult), true, "Ist Prozess aktiv"),
            [typeof(GetProcessStep)] = new(typeof(GetProcessResult), DisplayName: "Prozess ermitteln"),
            [typeof(StartProcessStep)] = new(typeof(StartProcessResult), DisplayName: "Prozess starten"),
            [typeof(TerminateProcessStep)] = new(typeof(TerminateProcessResult), DisplayName: "Prozess beenden"),
            [typeof(FocusProcessStep)] = new(typeof(FocusProcessResult), DisplayName: "Prozessfenster steuern"),
            [typeof(ShowTextStep)] = new(typeof(ShowTextResult), DisplayName: "Text auf Desktop anzeigen"),
            [typeof(UserChoiceStep)] = new(typeof(UserChoiceResult), true, "Benutzerauswahl abfragen"),
            [typeof(ActiveWindowStep)] = new(typeof(ActiveWindowResult), true, "Ist Fenster im Vordergrund"),
            [typeof(PointComparisonStep)] = new(typeof(PointComparisonResult), true, "Punkte-Vergleich"),
            [typeof(WindowsStateQueryStep)] = new(typeof(WindowsStateQueryResult), true, "Windows-Zustand abfragen"),
            [typeof(WindowsSettingChangeStep)] = new(typeof(WindowsSettingChangeResult), DisplayName: "Windows-Einstellung ändern"),
            [typeof(ShowImageStep)] = new(typeof(ShowImageResult), DisplayName: "Bild anzeigen"),
            [typeof(ShowOnDesktopStep)] = new(typeof(ShowOnDesktopResult), DisplayName: "Erkennungsergebnis auf Desktop anzeigen"),
            [typeof(VideoCreationStep)] = new(typeof(VideoCreationResult), DisplayName: "Video erstellen"),
            [typeof(SaveImageStep)] = new(typeof(SaveImageResult), true, "Bild speichern"),
            [typeof(IfStep)] = new(null, DisplayName: "If"),
            [typeof(ElseIfStep)] = new(null, DisplayName: "Else If"),
            [typeof(ElseStep)] = new(null, DisplayName: "Else"),
            [typeof(EndIfStep)] = new(null, DisplayName: "End If"),
            [typeof(EndJobStep)] = new(null, DisplayName: "Job beenden"),
            [typeof(ContinueJobStep)] = new(null, DisplayName: "Job neu starten"),
            [typeof(BlockInputStep)] = new(typeof(InputControlResult), DisplayName: "Eingaben blockieren"),
            [typeof(UnblockInputStep)] = new(typeof(InputControlResult), DisplayName: "Eingaben freigeben")
        };

    public static StepPipelineInfo? Get(Type stepType) =>
        Map.TryGetValue(stepType, out var info) ? info : null;

    public static StepPipelineInfo? Get<TStep>() where TStep : JobStep => Get(typeof(TStep));

    public static string GetDisplayName(Type stepType) => Get(stepType)?.DisplayName ?? stepType.Name;

    public static string GetDisplayName(string classTypeName)
    {
        var entry = Map.FirstOrDefault(candidate => candidate.Key.Name == classTypeName);
        return entry.Key is null || string.IsNullOrEmpty(entry.Value.DisplayName)
            ? classTypeName
            : entry.Value.DisplayName;
    }
}
