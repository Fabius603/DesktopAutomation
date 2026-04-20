using System.Drawing;
using OpenCvSharp;

namespace TaskAutomation.Steps
{
    /// <summary>
    /// Basisklasse für alle Step-Ergebnisse. Wird im IJobResultStore gespeichert
    /// und ist per StepTyp oder Step-ID abrufbar – gibt immer einen sinnvollen Default zurück.
    /// </summary>
    public abstract record StepResultBase
    {
        /// <summary>True wenn der Step tatsächlich ausgeführt wurde (false = Default-Wert).</summary>
        public bool WasExecuted { get; init; }
    }

    /// <summary>
    /// Ergebnis von Capture-Steps (DesktopDuplication, ProcessDuplication).
    /// Enthält das aufgenommene Bild sowie Bildschirm-Bounds und Offset für Koordinaten-Umrechnung.
    /// </summary>
    public sealed record CaptureResult : StepResultBase
    {
        public Bitmap?              Image          { get; init; }
        public Bitmap?              ProcessedImage { get; init; }
        public System.Drawing.Rectangle Bounds    { get; init; }
        public System.Drawing.Point                Offset         { get; init; }

        public bool HasImage => Image is not null;

        public static readonly CaptureResult Default = new() { WasExecuted = false };
    }

    /// <summary>
    /// Ergebnis von Detection-Steps (TemplateMatching, YOLO).
    /// Enthält ob etwas gefunden wurde, den globalen Bildschirm-Punkt und ein annotiertes Bild.
    /// </summary>
    public sealed record DetectionResult : StepResultBase
    {
        public bool    Found          { get; init; }
        public System.Drawing.Point?  Point          { get; init; }
        public double  Confidence     { get; init; }
        public Bitmap? ProcessedImage { get; init; }

        public static readonly DetectionResult Default = new() { WasExecuted = false, Found = false };
    }

    /// <summary>
    /// Ergebnis von Aktions-Steps (Click, Makro, Script, JobExecution).
    /// </summary>
    public sealed record TaskResult : StepResultBase
    {
        public bool    Success      { get; init; }
        public string? ErrorMessage { get; init; }

        public static readonly TaskResult Default = new() { WasExecuted = false };
    }

    /// <summary>
    /// Ergebnis von Ausgabe-Steps (ShowImage, VideoCreation).
    /// </summary>
    public sealed record OutputResult : StepResultBase
    {
        public bool Success { get; init; }

        public static readonly OutputResult Default = new() { WasExecuted = false };
    }
}
