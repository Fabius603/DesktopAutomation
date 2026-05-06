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
        string   Output,
        bool     IsConditionSource = false,
        string   DisplayName       = "");

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
                Output:        "CaptureResult",
                DisplayName:   "Desktop-Duplizierung"),

            [typeof(ProcessDuplicationStep)] = new(
                Prerequisites: [],
                Output:        "CaptureResult",
                DisplayName:   "Prozess-Fensteraufnahme"),

            // ── Erkennung ──────────────────────────────────────────────────────
            [typeof(TemplateMatchingStep)] = new(
                Prerequisites:     ["CaptureResult"],
                Output:            "DetectionResult",
                IsConditionSource: true,
                DisplayName:       "Template Matching"),

            [typeof(YOLODetectionStep)] = new(
                Prerequisites:     ["CaptureResult"],
                Output:            "DetectionResult",
                IsConditionSource: true,
                DisplayName:       "YOLO-Erkennung"),

            [typeof(KeyPointMatchingStep)] = new(
                Prerequisites:     ["CaptureResult"],
                Output:            "DetectionResult",
                IsConditionSource: true,
                DisplayName:       "KeyPoint Matching"),

            // ── Interaktion ────────────────────────────────────────────────────
            [typeof(KlickOnPointStep)] = new(
                Prerequisites: ["DetectionResult"],
                Output:        "TaskResult",
                DisplayName:   "Klick auf Punkt"),

            [typeof(KlickOnPoint3DStep)] = new(
                Prerequisites: ["DetectionResult"],
                Output:        "TaskResult",
                DisplayName:   "Klick auf Punkt in 3D-Umgebung"),

            // ── Automatisierung ────────────────────────────────────────────────
            [typeof(MakroExecutionStep)] = new(
                Prerequisites: [],
                Output:        "TaskResult",
                DisplayName:   "Makro ausführen"),

            [typeof(ScriptExecutionStep)] = new(
                Prerequisites: [],
                Output:        "TaskResult",
                DisplayName:   "Skript ausführen"),

            [typeof(JobExecutionStep)] = new(
                Prerequisites: [],
                Output:        "TaskResult",
                DisplayName:   "Job starten"),

            [typeof(TimeoutStep)] = new(
                Prerequisites: [],
                Output:        "TaskResult",
                DisplayName:   "Timeout"),

            [typeof(ActiveProcessStep)] = new(
                Prerequisites:     [],
                Output:            "ActiveProcessResult",
                IsConditionSource: true,
                DisplayName:       "Ist Prozess aktiv"),

            [typeof(StartProcessStep)] = new(
                Prerequisites: [],
                Output:        "TaskResult",
                DisplayName:   "Prozess starten"),

            [typeof(FocusProcessStep)] = new(
                Prerequisites: [],
                Output:        "TaskResult",
                DisplayName:   "Prozess in Vordergrund"),

            [typeof(ShowTextStep)] = new(
                Prerequisites: [],
                Output:        "TaskResult",
                DisplayName:   "Text auf Desktop anzeigen"),

            [typeof(ActiveWindowStep)] = new(
                Prerequisites:     [],
                Output:            "ActiveWindowResult",
                IsConditionSource: true,
                DisplayName:       "Ist Fenster im Vordergrund"),

            // ── Ausgabe ────────────────────────────────────────────────────────
            [typeof(ShowImageStep)] = new(
                Prerequisites: ["CaptureResult"],
                Output:        "OutputResult",
                DisplayName:   "Bild anzeigen"),

            [typeof(ShowOnDesktopStep)] = new(
                Prerequisites: ["DetectionResult"],
                Output:        "OutputResult",
                DisplayName:   "Auf Desktop anzeigen"),

            [typeof(VideoCreationStep)] = new(
                Prerequisites: ["CaptureResult"],
                Output:        "OutputResult",
                DisplayName:   "Video erstellen"),

            // ── Ablaufsteuerung ────────────────────────────────────────────────
            [typeof(IfStep)]     = new(Prerequisites: [], Output: "–", DisplayName: "If"),
            [typeof(ElseIfStep)] = new(Prerequisites: [], Output: "–", DisplayName: "Else If"),
            [typeof(ElseStep)]   = new(Prerequisites: [], Output: "–", DisplayName: "Else"),
            [typeof(EndIfStep)]  = new(Prerequisites: [], Output: "–", DisplayName: "End If"),
            [typeof(EndJobStep)] = new(Prerequisites: [], Output: "–", DisplayName: "Job beenden"),
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
            ["StartProcess"]       = typeof(StartProcessStep),
            ["FocusProcess"]       = typeof(FocusProcessStep),
            ["ShowText"]           = typeof(ShowTextStep),
            ["ActiveWindow"]       = typeof(ActiveWindowStep),
            ["KeyPointMatching"]   = typeof(KeyPointMatchingStep),
            ["If"]                 = typeof(IfStep),
            ["ElseIf"]             = typeof(ElseIfStep),
            ["Else"]               = typeof(ElseStep),
            ["EndIf"]              = typeof(EndIfStep),
            ["EndJob"]             = typeof(EndJobStep),
        };

        /// <summary>Gibt die Pipeline-Info für den Step-Typ-Namen zurück (z. B. "TemplateMatching").</summary>
        public static StepPipelineInfo? GetByName(string name)
            => _nameMap.TryGetValue(name, out var t) ? Get(t) : null;

        // ── Validierung ────────────────────────────────────────────────────────

        /// <summary>
        /// Beschreibt eine fehlende Voraussetzung in der Step-Kette.
        /// </summary>
        public sealed record StepChainError(int StepIndex, string StepTypeName, string MissingPrerequisite);

        /// <summary>
        /// Prüft, ob alle Voraussetzungen in der übergebenen Step-Sequenz erfüllt sind.
        /// Gibt eine Liste der Verstöße zurück (leer = gültig).
        /// </summary>
        public static IReadOnlyList<StepChainError> ValidateStepChain(IEnumerable<JobStep> steps)
        {
            var errors    = new List<StepChainError>();
            var available = new HashSet<string>(StringComparer.Ordinal);
            int index     = 0;

            foreach (var step in steps)
            {
                var info = Get(step.GetType());
                if (info != null)
                {
                    // Control-flow markers (If/ElseIf/Else/EndIf) are transparent
                    // to the pipeline validator – they have no prerequisites and
                    // produce no output that other steps depend on.
                    bool isControlFlow = step is IfStep or ElseIfStep or ElseStep or EndIfStep;
                    if (!isControlFlow)
                    {
                        foreach (var prereq in info.Prerequisites)
                        {
                            if (!available.Contains(prereq))
                                errors.Add(new StepChainError(index, step.GetType().Name, prereq));
                        }
                        available.Add(info.Output);
                    }
                }
                index++;
            }

            return errors;
        }
    }
}
