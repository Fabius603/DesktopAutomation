using ImageCapture.DesktopDuplication;
using ImageCapture.ProcessDuplication;
using ImageCapture.Video;
using ImageDetection.Algorithms.TemplateMatching;
using ImageDetection.YOLO;
using ImageHelperMethods;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Events;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using TaskAutomation.Scripts;

namespace TaskAutomation.Steps
{
    /// <summary>
    /// Laufzeit-Kontext der an jeden Step-Handler übergeben wird.
    /// Enthält ausschließlich Services und per-Job-Ressourcen – kein geteilter Bitmap-State.
    /// Der Bitmap-State wandert in den <see cref="IJobResultStore"/>.
    /// </summary>
    public interface IStepPipelineContext
    {
        // ── Pipeline-Ergebnisse ────────────────────────────────────────────────

        /// <summary>
        /// Typsicherer Store für alle Step-Ergebnisse dieser Ausführungsrunde.
        /// Gibt für nicht-ausgeführte Steps immer einen sinnvollen Default zurück.
        /// </summary>
        IJobResultStore Results { get; }

        // ── Read-only Services ─────────────────────────────────────────────────

        ILogger              Logger             { get; }
        DxgiResources        DxgiResources      { get; }
        IReadOnlyDictionary<string, Job>   AllJobs   { get; }
        IReadOnlyDictionary<string, Makro> AllMakros { get; }
        IMakroExecutor       MakroExecutor      { get; }
        IScriptExecutor      ScriptExecutor     { get; }
        IYoloManager         YoloManager        { get; }
        IImageDisplayService ImageDisplayService { get; }

        /// <summary>Der aktuell laufende Job (für Zyklusprüfungen in JobExecutionStep).</summary>
        Job CurrentJob { get; }

        /// <summary>
        /// Startet einen Sub-Job aus einem Step heraus (z.B. JobExecutionStep).
        /// Delegate auf <see cref="IJobExecutor.ExecuteJob(Guid, CancellationToken)"/>.
        /// </summary>
        Func<Guid, CancellationToken, Task> ExecuteJob { get; }

        // ── Per-Job-Ressourcen (lazy von Handlern gesetzt) ─────────────────────

        DesktopDuplicator?   DesktopDuplicator  { get; set; }
        ProcessDuplicator?   ProcessDuplicator  { get; set; }
        TemplateMatching?    TemplateMatcher    { get; set; }
        StreamVideoRecorder? VideoRecorder      { get; set; }

        /// <summary>Timeout-Tracking pro Step-ID (verhindert zu schnelle Wiederholungen).</summary>
        Dictionary<string, DateTime> StepTimeouts { get; }
    }
}
