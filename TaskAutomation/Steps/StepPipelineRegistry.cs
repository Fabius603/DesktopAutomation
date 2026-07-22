using System;
using System.Collections.Generic;
using System.Linq;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    /// <summary>
    /// Beschreibt Voraussetzungen (Eingaben) und Ausgabe eines Step-Typs
    /// in der Ausführungs-Pipeline.
    /// </summary>
    /// <param name="IsConditionSource">
    /// Gibt an, ob das Ergebnis dieses Steps in If-Bedingungen ausgewertet werden kann.
    /// Nur TemplateMatching, YOLO, ActiveProcess und ActiveWindow liefern auswertbare Ergebnisse.
    /// </param>
    /// <param name="DisplayName">
    /// Anzeigename des Step-Typs (einzige Quelle für den UI-Namen – wird von Dialog, Karten und Konvertern verwendet).
    /// </param>
    public sealed record StepPipelineInfo(
        string[] Prerequisites,
        Type?    ResultType,
        bool     IsConditionSource = false,
        string   DisplayName       = "")
    {
        public string Output => ResultType?.Name ?? "–";
    }

    /// <summary>
    /// Statisches Registry das für jeden bekannten <see cref="JobStep"/>-Typ
    /// die Pipeline-Metadaten (Voraussetzungen, Ausgabe) speichert.
    /// Wird von der UI genutzt, um dem Anwender anzuzeigen, was ein Step braucht
    /// und was er liefert.
    /// </summary>
    public static class StepPipelineRegistry
    {
        private static readonly Dictionary<Type, StepPipelineInfo> _map = new()
        {
            // ── Erfassung ──────────────────────────────────────────────────────
            [typeof(DesktopDuplicationStep)] = new(
                Prerequisites: [],
                ResultType:    typeof(DesktopDuplicationResult),
                DisplayName:   "Desktop-Duplizierung"),

            [typeof(ProcessDuplicationStep)] = new(
                Prerequisites: [],
                ResultType:    typeof(ProcessDuplicationResult),
                DisplayName:   "Prozess-Fensteraufnahme"),

            // ── Erkennung ──────────────────────────────────────────────────────
            [typeof(TemplateMatchingStep)] = new(
                Prerequisites:     ["Image"],
                ResultType:        typeof(TemplateMatchingResult),
                IsConditionSource: true,
                DisplayName:       "Template Matching"),

            [typeof(ColorDetectionStep)] = new(
                Prerequisites:     ["Image"],
                ResultType:        typeof(ColorDetectionResult),
                IsConditionSource: true,
                DisplayName:       "Farberkennung"),

            [typeof(YOLODetectionStep)] = new(
                Prerequisites:     ["Image"],
                ResultType:        typeof(YOLODetectionResult),
                IsConditionSource: true,
                DisplayName:       "YOLO-Erkennung"),

            [typeof(KeyPointMatchingStep)] = new(
                Prerequisites:     ["Image"],
                ResultType:        typeof(KeyPointMatchingResult),
                IsConditionSource: true,
                DisplayName:       "KeyPoint Matching"),

            [typeof(PredictMovementStep)] = new(
                Prerequisites:     ["Points"],
                ResultType:        typeof(PredictMovementResult),
                IsConditionSource: true,
                DisplayName:       "Bewegung vorhersagen"),

            [typeof(DynamicRoiStep)] = new(
                Prerequisites: ["Rectangles"],
                ResultType: typeof(DynamicRoiResult),
                IsConditionSource: true,
                DisplayName: "Dynamische ROI erstellen"),

            // ── Interaktion ────────────────────────────────────────────────────
            [typeof(KlickOnPointStep)] = new(
                Prerequisites: ["Points"],
                ResultType:    typeof(KlickOnPointResult),
                DisplayName:   "Klick auf Punkt"),

            [typeof(KlickOnPoint3DStep)] = new(
                Prerequisites: ["Points"],
                ResultType:    typeof(KlickOnPoint3DResult),
                DisplayName:   "Klick auf Punkt in 3D-Umgebung"),

            // ── Automatisierung ────────────────────────────────────────────────
            [typeof(MakroExecutionStep)] = new(
                Prerequisites: [],
                ResultType:    typeof(MakroExecutionResult),
                DisplayName:   "Makro ausführen"),

            [typeof(ScriptExecutionStep)] = new(
                Prerequisites: [],
                ResultType:    typeof(ScriptExecutionResult),
                DisplayName:   "Skript ausführen"),

            [typeof(JobExecutionStep)] = new(
                Prerequisites: [],
                ResultType:    typeof(JobExecutionResult),
                DisplayName:   "Job starten"),

            [typeof(TimeoutStep)] = new(
                Prerequisites: [],
                ResultType:    typeof(TimeoutResult),
                DisplayName:   "Timeout"),

            [typeof(ActiveProcessStep)] = new(
                Prerequisites:     [],
                ResultType:        typeof(ActiveProcessResult),
                IsConditionSource: true,
                DisplayName:       "Ist Prozess aktiv"),

            [typeof(GetProcessStep)] = new(
                Prerequisites: [],
                ResultType:    typeof(GetProcessResult),
                DisplayName:   "Prozess ermitteln"),

            [typeof(StartProcessStep)] = new(
                Prerequisites: [],
                ResultType:    typeof(StartProcessResult),
                DisplayName:   "Prozess starten"),

            [typeof(TerminateProcessStep)] = new(
                Prerequisites: [],
                ResultType:    typeof(TerminateProcessResult),
                DisplayName:   "Prozess beenden"),

            [typeof(FocusProcessStep)] = new(
                Prerequisites: [],
                ResultType:    typeof(FocusProcessResult),
                DisplayName:   "Prozessfenster steuern"),

            [typeof(ShowTextStep)] = new(
                Prerequisites: [],
                ResultType:    typeof(ShowTextResult),
                DisplayName:   "Text auf Desktop anzeigen"),

            [typeof(ActiveWindowStep)] = new(
                Prerequisites:     [],
                ResultType:        typeof(ActiveWindowResult),
                IsConditionSource: true,
                DisplayName:       "Ist Fenster im Vordergrund"),

            // ── Abfrage ────────────────────────────────────────────────────────
            [typeof(PointComparisonStep)] = new(
                Prerequisites:     [],
                ResultType:        typeof(PointComparisonResult),
                IsConditionSource: true,
                DisplayName:       "Punkte-Vergleich"),

            [typeof(WindowsStateQueryStep)] = new(
                Prerequisites:     [],
                ResultType:        typeof(WindowsStateQueryResult),
                IsConditionSource: true,
                DisplayName:       "Windows-Zustand abfragen"),

            // ── Ausgabe ────────────────────────────────────────────────────────
            [typeof(ShowImageStep)] = new(
                Prerequisites: ["Image"],
                ResultType:    typeof(ShowImageResult),
                DisplayName:   "Bild anzeigen"),

            [typeof(ShowOnDesktopStep)] = new(
                Prerequisites: ["Detections"],
                ResultType:    typeof(ShowOnDesktopResult),
                DisplayName:   "Erkennungsergebnis auf Desktop anzeigen"),

            [typeof(VideoCreationStep)] = new(
                Prerequisites: ["Image"],
                ResultType:    typeof(VideoCreationResult),
                DisplayName:   "Video erstellen"),

            // ── Ablaufsteuerung ────────────────────────────────────────────────
            [typeof(IfStep)]     = new(Prerequisites: [], ResultType: null, DisplayName: "If"),
            [typeof(ElseIfStep)] = new(Prerequisites: [], ResultType: null, DisplayName: "Else If"),
            [typeof(ElseStep)]   = new(Prerequisites: [], ResultType: null, DisplayName: "Else"),
            [typeof(EndIfStep)]  = new(Prerequisites: [], ResultType: null, DisplayName: "End If"),
            [typeof(EndJobStep)] = new(Prerequisites: [], ResultType: null, DisplayName: "Job beenden"),
            [typeof(ContinueJobStep)] = new(Prerequisites: [], ResultType: null, DisplayName: "Job neu starten"),
            [typeof(BlockInputStep)] = new(Prerequisites: [], ResultType: typeof(InputControlResult), DisplayName: "Eingaben blockieren"),
            [typeof(UnblockInputStep)] = new(Prerequisites: [], ResultType: typeof(InputControlResult), DisplayName: "Eingaben freigeben"),
        };

        /// <summary>Gibt die Pipeline-Info für den angegebenen Step-Typ zurück.</summary>
        public static StepPipelineInfo? Get(Type stepType)
            => _map.TryGetValue(stepType, out var info) ? info : null;

        /// <summary>Gibt die Pipeline-Info für <typeparamref name="TStep"/> zurück.</summary>
        public static StepPipelineInfo? Get<TStep>() where TStep : JobStep
            => Get(typeof(TStep));

        /// <summary>Gibt den Anzeigenamen für einen Step-Typ zurück (z. B. "Template Matching").</summary>
        public static string GetDisplayName(Type stepType)
            => Get(stepType)?.DisplayName ?? stepType.Name;

        /// <summary>Gibt den Anzeigenamen anhand des C#-Klassennamens zurück (z. B. "TemplateMatchingStep").</summary>
        public static string GetDisplayName(string classTypeName)
        {
            foreach (var kvp in _map)
                if (kvp.Key.Name == classTypeName)
                    return string.IsNullOrEmpty(kvp.Value.DisplayName) ? classTypeName : kvp.Value.DisplayName;
            return classTypeName;
        }

        // ── Name-Mapping für ViewModel-Nutzung mit String-Keys ─────────────────
        private static readonly Dictionary<string, Type> _nameMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["DesktopDuplication"] = typeof(DesktopDuplicationStep),
            ["TemplateMatching"]   = typeof(TemplateMatchingStep),
            ["ColorDetection"]     = typeof(ColorDetectionStep),
            ["PredictMovement"]    = typeof(PredictMovementStep),
            ["YoloDetection"]      = typeof(YOLODetectionStep),
            ["KlickOnPoint"]       = typeof(KlickOnPointStep),
            ["KlickOnPoint3D"]     = typeof(KlickOnPoint3DStep),
            ["ShowImage"]          = typeof(ShowImageStep),
            ["ShowOnDesktop"]      = typeof(ShowOnDesktopStep),
            ["VideoCreation"]      = typeof(VideoCreationStep),
            ["MakroExecution"]     = typeof(MakroExecutionStep),
            ["JobExecution"]       = typeof(JobExecutionStep),
            ["ScriptExecution"]    = typeof(ScriptExecutionStep),
            ["Timeout"]            = typeof(TimeoutStep),
            ["ActiveProcess"]      = typeof(ActiveProcessStep),
            ["GetProcess"]         = typeof(GetProcessStep),
            ["StartProcess"]       = typeof(StartProcessStep),
            ["TerminateProcess"]   = typeof(TerminateProcessStep),
            ["FocusProcess"]       = typeof(FocusProcessStep),
            ["ShowText"]           = typeof(ShowTextStep),
            ["ActiveWindow"]       = typeof(ActiveWindowStep),
            ["KeyPointMatching"]   = typeof(KeyPointMatchingStep),
            ["PointComparison"]    = typeof(PointComparisonStep),
            ["DynamicRoi"]         = typeof(DynamicRoiStep),
            ["WindowsStateQuery"]  = typeof(WindowsStateQueryStep),
            ["If"]                 = typeof(IfStep),
            ["ElseIf"]             = typeof(ElseIfStep),
            ["Else"]               = typeof(ElseStep),
            ["EndIf"]              = typeof(EndIfStep),
            ["EndJob"]             = typeof(EndJobStep),
            ["ContinueJob"]        = typeof(ContinueJobStep),
            ["BlockInput"]         = typeof(BlockInputStep),
            ["UnblockInput"]       = typeof(UnblockInputStep),
        };

        /// <summary>Gibt die Pipeline-Info für den Step-Typ-Namen zurück (z. B. "TemplateMatching").</summary>
        public static StepPipelineInfo? GetByName(string name)
            => _nameMap.TryGetValue(name, out var t) ? Get(t) : null;

    }
}
