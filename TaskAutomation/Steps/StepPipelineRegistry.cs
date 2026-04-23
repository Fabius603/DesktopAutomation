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
    public sealed record StepPipelineInfo(
        string[] Prerequisites,
        string   Output);

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
                Output:        "CaptureResult"),

            [typeof(ProcessDuplicationStep)] = new(
                Prerequisites: [],
                Output:        "CaptureResult"),

            // ── Erkennung ──────────────────────────────────────────────────────
            [typeof(TemplateMatchingStep)] = new(
                Prerequisites: ["CaptureResult"],
                Output:        "DetectionResult"),

            [typeof(YOLODetectionStep)] = new(
                Prerequisites: ["CaptureResult"],
                Output:        "DetectionResult"),

            // ── Interaktion ────────────────────────────────────────────────────
            [typeof(KlickOnPointStep)] = new(
                Prerequisites: ["DetectionResult"],
                Output:        "TaskResult"),

            [typeof(KlickOnPoint3DStep)] = new(
                Prerequisites: ["DetectionResult"],
                Output:        "TaskResult"),

            // ── Automatisierung ────────────────────────────────────────────────
            [typeof(MakroExecutionStep)] = new(
                Prerequisites: [],
                Output:        "TaskResult"),

            [typeof(ScriptExecutionStep)] = new(
                Prerequisites: [],
                Output:        "TaskResult"),

            [typeof(JobExecutionStep)] = new(
                Prerequisites: [],
                Output:        "TaskResult"),

            [typeof(TimeoutStep)] = new(
                Prerequisites: [],
                Output:        "TaskResult"),

            // ── Ausgabe ────────────────────────────────────────────────────────
            [typeof(ShowImageStep)] = new(
                Prerequisites: ["CaptureResult"],
                Output:        "OutputResult"),

            [typeof(VideoCreationStep)] = new(
                Prerequisites: ["CaptureResult"],
                Output:        "OutputResult"),

            // ── Ablaufsteuerung ────────────────────────────────────────────────
            [typeof(IfStep)]     = new(Prerequisites: [], Output: "–"),
            [typeof(ElseIfStep)] = new(Prerequisites: [], Output: "–"),
            [typeof(ElseStep)]   = new(Prerequisites: [], Output: "–"),
            [typeof(EndIfStep)]  = new(Prerequisites: [], Output: "–"),
        };

        /// <summary>Gibt die Pipeline-Info für den angegebenen Step-Typ zurück.</summary>
        public static StepPipelineInfo? Get(Type stepType)
            => _map.TryGetValue(stepType, out var info) ? info : null;

        /// <summary>Gibt die Pipeline-Info für <typeparamref name="TStep"/> zurück.</summary>
        public static StepPipelineInfo? Get<TStep>() where TStep : JobStep
            => Get(typeof(TStep));

        // ── Name-Mapping für ViewModel-Nutzung mit String-Keys ─────────────────
        private static readonly Dictionary<string, Type> _nameMap = new(StringComparer.OrdinalIgnoreCase)
        {
            ["DesktopDuplication"] = typeof(DesktopDuplicationStep),
            ["TemplateMatching"]   = typeof(TemplateMatchingStep),
            ["YoloDetection"]      = typeof(YOLODetectionStep),
            ["KlickOnPoint"]       = typeof(KlickOnPointStep),
            ["KlickOnPoint3D"]     = typeof(KlickOnPoint3DStep),
            ["ShowImage"]          = typeof(ShowImageStep),
            ["VideoCreation"]      = typeof(VideoCreationStep),
            ["MakroExecution"]     = typeof(MakroExecutionStep),
            ["JobExecution"]       = typeof(JobExecutionStep),
            ["ScriptExecution"]    = typeof(ScriptExecutionStep),
            ["Timeout"]            = typeof(TimeoutStep),
            ["If"]                 = typeof(IfStep),
            ["ElseIf"]             = typeof(ElseIfStep),
            ["Else"]               = typeof(ElseStep),
            ["EndIf"]              = typeof(EndIfStep),
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
