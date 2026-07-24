using ImageCapture.ProcessDuplication;
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
    /// Konkrete Implementierung von <see cref="IStepPipelineContext"/>.
    /// Wird einmal pro Job-Lauf erstellt und am Ende disposed.
    /// </summary>
    internal sealed class StepPipelineContext : IStepPipelineContext, IDisposable
    {
        private JobResultStore _results = new();

        // ── IStepPipelineContext ───────────────────────────────────────────────

        public IJobResultStore Results => _results;
        public IDictionary<string, DynamicRoiState> DynamicRoiStates { get; } =
            new Dictionary<string, DynamicRoiState>(StringComparer.OrdinalIgnoreCase);

        public ILogger              Logger             { get; }
        public DxgiResources        DxgiResources      { get; }
        public IReadOnlyDictionary<string, Job>   AllJobs   { get; }
        public IReadOnlyDictionary<string, Makro> AllMakros { get; }
        public IMakroExecutor       MakroExecutor      { get; }
        public IScriptExecutor      ScriptExecutor     { get; }
        public IYoloManager         YoloManager        { get; }
        public IImageDisplayService ImageDisplayService { get; }
        public IDesktopResultOverlay DesktopResultOverlay { get; }
        public ExecutionLogSession? ExecutionLogSession { get; }
        public IExecutionLogService ExecutionLogService { get; }
        public Job                  CurrentJob         { get; }
        public Func<Guid, CancellationToken, Task> ExecuteJob { get; }
        public Func<Guid, Guid>?                    StartJobViaDispatcher { get; }
        public Func<Guid, CancellationToken, Task>? StartJobViaDispatcherAsync { get; }
        public Action<Guid>?                        CancelJobViaDispatcher { get; }

        public IDesktopCaptureService DesktopCaptureService { get; }
        public ICameraCaptureService CameraCaptureService { get; }
        public ISet<string>  OpenedWindowNames    { get; } = new HashSet<string>(StringComparer.Ordinal);
        public IList<Guid>   ChildJobInstanceIds  { get; } = new List<Guid>();
        public ProcessDuplicator?   ProcessDuplicator  { get; set; }
        public TemplateMatching?    TemplateMatcher    { get; set; }
        public ColorDetector?       ColorDetector      { get; set; }
        public KeyPointMatcher?     KeyPointMatcher    { get; set; }
        public StreamVideoRecorder? VideoRecorder      { get; set; }

        public Dictionary<string, DateTime> StepTimeouts { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, PredictMovementState> PredictMovementStates { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, ActiveWindowCacheEntry> ActiveWindowCache { get; } =
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
            IDesktopResultOverlay              desktopResultOverlay,
            Job                                currentJob,
            Func<Guid, CancellationToken, Task> executeJob,
            IDesktopCaptureService             desktopCaptureService,
            ICameraCaptureService              cameraCaptureService,
            ExecutionLogSession?                executionLogSession = null,
            IExecutionLogService?               executionLogService = null,
            Func<Guid, Guid>?                  startJobViaDispatcher  = null,
            Action<Guid>?                      cancelJobViaDispatcher = null,
            Func<Guid, CancellationToken, Task>? startJobViaDispatcherAsync = null)
        {
            Logger                     = logger;
            DxgiResources              = dxgiResources;
            AllJobs                    = allJobs;
            AllMakros                  = allMakros;
            MakroExecutor              = makroExecutor;
            ScriptExecutor             = scriptExecutor;
            YoloManager                = yoloManager;
            ImageDisplayService        = imageDisplayService;
            DesktopResultOverlay       = desktopResultOverlay;
            ExecutionLogSession        = executionLogSession;
            ExecutionLogService        = executionLogService
                ?? throw new ArgumentNullException(nameof(executionLogService));
            CurrentJob                 = currentJob;
            ExecuteJob                 = executeJob;
            DesktopCaptureService      = desktopCaptureService;
            CameraCaptureService       = cameraCaptureService;
            StartJobViaDispatcher      = startJobViaDispatcher;
            CancelJobViaDispatcher     = cancelJobViaDispatcher;
            StartJobViaDispatcherAsync = startJobViaDispatcherAsync;
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

        /// <summary>
        /// Beginnt eine neue Phase/Runde, behält aber Ergebnisse früherer Lifecycle-Phasen.
        /// </summary>
        public void ResetResults(IEnumerable<string> retainedStepIds)
        {
            _results.RetainOnly(retainedStepIds);
        }

        // ── Dispose ────────────────────────────────────────────────────────────

        public void Dispose()
        {
            _results.DisposeAndClear();
            ProcessDuplicator?.Dispose();
            TemplateMatcher?.Dispose();
            // DesktopCaptureService ist ein Singleton und wird NICHT hier disposed.
            ColorDetector?.Dispose();
        }
    }
}
