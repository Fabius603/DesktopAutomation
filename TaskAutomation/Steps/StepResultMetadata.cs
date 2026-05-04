using System.Collections.Generic;

namespace TaskAutomation.Steps
{
    public enum ResultPropertyType { Bool, Double, String }

    public sealed record ResultPropertyDescriptor(
        string Name,
        string DisplayName,
        ResultPropertyType PropertyType);

    public sealed record ResultTypeDescriptor(
        string TypeName,
        string DisplayName,
        ResultPropertyDescriptor[] Properties);

    public static class StepResultMetadata
    {
        private static readonly ResultPropertyDescriptor[] BoolSuccess =
        [
            new("Success",     "Erfolgreich",       ResultPropertyType.Bool),
            new("WasExecuted", "Wurde ausgeführt",  ResultPropertyType.Bool),
        ];

        private static readonly Dictionary<string, ResultPropertyDescriptor[]> Properties = new()
        {
            ["TemplateMatchingStep"]   =
            [
                new("Found",       "Gefunden",         ResultPropertyType.Bool),
                new("Confidence",  "Confidence",       ResultPropertyType.Double),
                new("WasExecuted", "Wurde ausgeführt", ResultPropertyType.Bool),
            ],
            ["YOLODetectionStep"]      =
            [
                new("Found",       "Gefunden",         ResultPropertyType.Bool),
                new("Confidence",  "Confidence",       ResultPropertyType.Double),
                new("WasExecuted", "Wurde ausgeführt", ResultPropertyType.Bool),
            ],
            ["DesktopDuplicationStep"] =
            [
                new("HasImage",    "Hat Bild",         ResultPropertyType.Bool),
                new("WasExecuted", "Wurde ausgeführt", ResultPropertyType.Bool),
            ],
            ["MakroExecutionStep"]     = BoolSuccess,
            ["ScriptExecutionStep"]    = BoolSuccess,
            ["JobExecutionStep"]       = BoolSuccess,
            ["KlickOnPointStep"]       = BoolSuccess,
            ["KlickOnPoint3DStep"]     = BoolSuccess,
            ["ActiveProcessStep"]      =
            [
                new("IsRunning",    "Prozess läuft",       ResultPropertyType.Bool),
                new("WasExecuted",  "Wurde ausgeführt",   ResultPropertyType.Bool),
            ],
            ["StartProcessStep"]       = BoolSuccess,
            ["ActiveWindowStep"]       =
            [
                new("IsActive",     "Fenster aktiv",      ResultPropertyType.Bool),
                new("WasExecuted",  "Wurde ausgeführt",   ResultPropertyType.Bool),
            ],
            ["KeyPointMatchingStep"]   =
            [
                new("Found",       "Gefunden",         ResultPropertyType.Bool),
                new("Confidence",  "Confidence",       ResultPropertyType.Double),
                new("WasExecuted", "Wurde ausgeführt", ResultPropertyType.Bool),
            ],
            ["ShowImageStep"]          =
            [
                new("Success",     "Erfolgreich",      ResultPropertyType.Bool),
                new("WasExecuted", "Wurde ausgeführt", ResultPropertyType.Bool),
            ],
            ["VideoCreationStep"]      =
            [
                new("Success",     "Erfolgreich",      ResultPropertyType.Bool),
                new("WasExecuted", "Wurde ausgeführt", ResultPropertyType.Bool),
            ],
        };



        public static readonly IReadOnlyList<ResultTypeDescriptor> ResultTypes = new[]
        {
            new ResultTypeDescriptor("DetectionResult", "DetectionResult",
            [
                new ResultPropertyDescriptor("Found",       "Gefunden",         ResultPropertyType.Bool),
                new ResultPropertyDescriptor("Confidence",  "Confidence",       ResultPropertyType.Double),
                new ResultPropertyDescriptor("WasExecuted", "Wurde ausgeführt", ResultPropertyType.Bool),
            ]),
            new ResultTypeDescriptor("CaptureResult", "CaptureResult",
            [
                new ResultPropertyDescriptor("HasImage",    "Hat Bild",         ResultPropertyType.Bool),
                new ResultPropertyDescriptor("WasExecuted", "Wurde ausgeführt", ResultPropertyType.Bool),
            ]),
            new ResultTypeDescriptor("TaskResult", "TaskResult",
            [
                new ResultPropertyDescriptor("Success",     "Erfolgreich",      ResultPropertyType.Bool),
                new ResultPropertyDescriptor("WasExecuted", "Wurde ausgeführt", ResultPropertyType.Bool),
            ]),
            new ResultTypeDescriptor("OutputResult", "OutputResult",
            [
                new ResultPropertyDescriptor("Success",     "Erfolgreich",      ResultPropertyType.Bool),
                new ResultPropertyDescriptor("WasExecuted", "Wurde ausgeführt", ResultPropertyType.Bool),
            ]),
            new ResultTypeDescriptor("ActiveProcessResult", "ActiveProcessResult",
            [
                new ResultPropertyDescriptor("IsRunning",    "Prozess läuft",      ResultPropertyType.Bool),
                new ResultPropertyDescriptor("WasExecuted",  "Wurde ausgeführt",  ResultPropertyType.Bool),
            ]),
            new ResultTypeDescriptor("ActiveWindowResult", "ActiveWindowResult",
            [
                new ResultPropertyDescriptor("IsActive",    "Fenster aktiv",     ResultPropertyType.Bool),
                new ResultPropertyDescriptor("WasExecuted", "Wurde ausgeführt",  ResultPropertyType.Bool),
            ]),
        };

        public static ResultTypeDescriptor? GetResultType(string typeName)
        {
            foreach (var rt in ResultTypes)
                if (rt.TypeName == typeName) return rt;
            return null;
        }

        public static ResultPropertyDescriptor[]? GetProperties(string stepTypeName)
            => Properties.TryGetValue(stepTypeName, out var props) ? props : null;

        public static bool HasResult(string stepTypeName)
            => Properties.ContainsKey(stepTypeName);

        /// <summary>Liefert den Anzeigenamen eines Step-Typs. Delegiert an <see cref="StepPipelineRegistry"/> – einzige Quelle.</summary>
        public static string GetFriendlyName(string stepTypeName)
            => StepPipelineRegistry.GetDisplayName(stepTypeName);
    }
}
