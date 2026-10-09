using ImageCapture.Video;
using ImageDetection.Algorithms.ColorDetection;
using ImageDetection.Algorithms.KeyPointMatching;
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
using TaskAutomation.Logging;
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
        TaskAutomation.Orchestration.OwnedExecutionScope? OwnedExecutions => null;
        Guid ResourceOwnerId => ExecutionLogSession?.Id ?? Guid.Empty;
        string ResourceKey(string stepId) => ResourceOwnerId == Guid.Empty ? stepId : $"{ResourceOwnerId:N}:{stepId}";
        Task AcquireYoloModelAsync(string model, CancellationToken ct) => YoloManager.EnsureModelAsync(model, ct);
        IDictionary<string, DynamicRoiState> DynamicRoiStates { get; }

        // ── Read-only Services ─────────────────────────────────────────────────

        ILogger Logger { get; }
        DxgiResources DxgiResources { get; }
        IReadOnlyDictionary<string, Job> AllJobs { get; }
        IReadOnlyDictionary<string, Makro> AllMakros { get; }
        IMakroExecutor MakroExecutor { get; }
        IScriptExecutor ScriptExecutor { get; }
        IYoloManager YoloManager { get; }
        IImageDisplayService ImageDisplayService { get; }
        IDesktopResultOverlay DesktopResultOverlay { get; }
        ExecutionLogSession? ExecutionLogSession { get; }
        IExecutionLogService ExecutionLogService { get; }

        /// <summary>Der aktuell laufende Job (für Zyklusprüfungen in JobExecutionStep).</summary>
        Job CurrentJob { get; }

        /// <summary>
        /// Startet einen Sub-Job aus einem Step heraus (z.B. JobExecutionStep).
        /// Delegate auf <see cref="IJobExecutor.ExecuteJob(Guid, CancellationToken)"/>.
        /// </summary>
        Func<Guid, CancellationToken, Task> ExecuteJob { get; }

        /// <summary>
        /// Startet einen Sub-Job über den Dispatcher und wartet auf Abschluss.
        /// Null wenn kein Dispatcher verdrahtet ist (z.B. in Tests).
        /// </summary>
        Func<Guid, CancellationToken, Task>? StartJobViaDispatcherAsync { get; }

        // ── Per-Job-Ressourcen (lazy von Handlern gesetzt) ─────────────────────

        /// <summary>
        /// Gemeinsamer Singleton-Dienst für den Desktop-Screenshot.
        /// Wird von <see cref="DesktopDuplicationStepHandler"/> verwendet statt einer
        /// eigenen <c>DesktopDuplicator</c>-Instanz.
        /// </summary>
        IDesktopCaptureService DesktopCaptureService { get; }
        ICameraCaptureService CameraCaptureService { get; }

        /// <summary>
        /// Alle in diesem Job-Lauf geöffneten Bildvorschau-Fenster (WindowName).
        /// Wird von ShowImageStepHandler befüllt; JobExecutor schließt bei Job-Ende nur diese Fenster.
        /// </summary>
        ISet<string> OpenedWindowNames { get; }

        TemplateMatching? TemplateMatcher { get; set; }
        ColorDetector? ColorDetector { get; set; }
        KeyPointMatcher? KeyPointMatcher { get; set; }
        void RegisterYoloModel(string model) { }
        IReadOnlyCollection<string> LoadedYoloModels => Array.Empty<string>();

        IVideoRecorder CreateVideoRecorder(int width, int height, int fps) => new StreamVideoRecorder(width, height, fps);

        IDictionary<string, IVideoRecorder> VideoRecorders { get; }

        /// <summary>Timeout-Tracking pro Step-ID (verhindert zu schnelle Wiederholungen).</summary>
        Dictionary<string, DateTime> StepTimeouts { get; }

        /// <summary>Last successfully sent relative movement per 3D step, retained across iterations.</summary>
        IDictionary<string, TaskAutomation.Contracts.Geometry.PixelPoint> Last3DMovements { get; }

        /// <summary>QPC timestamp after the last successful 3D input, retained across iterations.</summary>
        IDictionary<string, long> Last3DInputTimestamps { get; }

        /// <summary>Bewegungshistorie pro PredictMovement-Step-ID.</summary>
        Dictionary<string, PredictMovementState> PredictMovementStates { get; }

        /// <summary>Optionaler ActiveWindow-Cache pro Step-ID.</summary>
        Dictionary<string, ActiveWindowCacheEntry> ActiveWindowCache { get; }
    }
}
