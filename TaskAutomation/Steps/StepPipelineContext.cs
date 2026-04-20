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
    /// Konkrete Implementierung von <see cref="IStepPipelineContext"/>.
    /// Wird einmal pro Job-Lauf erstellt und am Ende disposed.
    /// </summary>
    internal sealed class StepPipelineContext : IStepPipelineContext, IDisposable
    {
        private JobResultStore _results = new();

        // ── IStepPipelineContext ───────────────────────────────────────────────

        public IJobResultStore Results => _results;

        public ILogger              Logger             { get; }
        public DxgiResources        DxgiResources      { get; }
        public IReadOnlyDictionary<string, Job>   AllJobs   { get; }
        public IReadOnlyDictionary<string, Makro> AllMakros { get; }
        public IMakroExecutor       MakroExecutor      { get; }
        public IScriptExecutor      ScriptExecutor     { get; }
        public IYoloManager         YoloManager        { get; }
        public IImageDisplayService ImageDisplayService { get; }
        public Job                  CurrentJob         { get; }
        public Func<Guid, CancellationToken, Task> ExecuteJob { get; }

        public DesktopDuplicator?   DesktopDuplicator  { get; set; }
        public ProcessDuplicator?   ProcessDuplicator  { get; set; }
        public TemplateMatching?    TemplateMatcher    { get; set; }
        public StreamVideoRecorder? VideoRecorder      { get; set; }

        public Dictionary<string, DateTime> StepTimeouts { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        // ── Konstruktor ────────────────────────────────────────────────────────

        public StepPipelineContext(
            ILogger                            logger,
            DxgiResources                      dxgiResources,
            IReadOnlyDictionary<string, Job>   allJobs,
            IReadOnlyDictionary<string, Makro> allMakros,
            IMakroExecutor                     makroExecutor,
            IScriptExecutor                    scriptExecutor,
            IYoloManager                       yoloManager,
            IImageDisplayService               imageDisplayService,
            Job                                currentJob,
            Func<Guid, CancellationToken, Task> executeJob)
        {
            Logger             = logger;
            DxgiResources      = dxgiResources;
            AllJobs            = allJobs;
            AllMakros          = allMakros;
            MakroExecutor      = makroExecutor;
            ScriptExecutor     = scriptExecutor;
            YoloManager        = yoloManager;
            ImageDisplayService = imageDisplayService;
            CurrentJob         = currentJob;
            ExecuteJob         = executeJob;
        }

        // ── Iteration-Reset ────────────────────────────────────────────────────

        /// <summary>
        /// Gibt Bitmap-Ressourcen der letzten Runde frei und legt einen leeren Store an.
        /// Muss am Anfang jeder Wiederholungsrunde aufgerufen werden.
        /// </summary>
        public void ResetResults()
        {
            _results.DisposeAndClear();
            _results = new JobResultStore();
        }

        // ── Dispose ────────────────────────────────────────────────────────────

        public void Dispose()
        {
            _results.DisposeAndClear();

            try { DesktopDuplicator?.Dispose(); } catch { /* best-effort */ }
            DesktopDuplicator = null;

            // ProcessDuplicator, TemplateMatcher, VideoRecorder werden vom JobExecutor verwaltet
        }
    }
}
