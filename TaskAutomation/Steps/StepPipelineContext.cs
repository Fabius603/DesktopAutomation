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
using TaskAutomation.Orchestration;

namespace TaskAutomation.Steps
{
    /// <summary>
    /// Konkrete Implementierung von <see cref="IStepPipelineContext"/>.
    /// Wird einmal pro Job-Lauf erstellt und am Ende disposed.
    /// </summary>
    internal sealed class StepPipelineContext : IStepPipelineContext, IDisposable
    {
        private JobResultStore _results;
        private readonly IReadOnlyDictionary<Guid, (ValueProviderSourceDescriptor Descriptor, string Value)> _secrets;

        // ── IStepPipelineContext ───────────────────────────────────────────────

        public IJobResultStore Results => _results;
        public OwnedExecutionScope OwnedExecutions { get; private set; }
        private readonly JobExecutionCancellation? _cancellation;
        private TimeSpan StopBudget => TimeSpan.FromSeconds(Math.Clamp(CurrentJob.EndPhaseTimeoutSeconds,
            Job.MinEndPhaseTimeoutSeconds, Job.MaxEndPhaseTimeoutSeconds));
        public void BeginEndExecutions()
        {
            OwnedExecutions.Dispose();
            OwnedExecutions = new OwnedExecutionScope(forceToken: _cancellation?.EndPhaseToken ?? default, stopBudget: StopBudget);
        }
        public Guid ResourceOwnerId { get; } = Guid.NewGuid();
        public string ResourceKey(string stepId) => $"{ResourceOwnerId:N}:{stepId}";
        private readonly Dictionary<string, IDisposable> _modelLeases = new(StringComparer.OrdinalIgnoreCase);
        public async Task AcquireYoloModelAsync(string model, CancellationToken ct)
        {
            lock (_modelLeases) if (_modelLeases.ContainsKey(model)) return;
            var lease = await YoloManager.AcquireModelAsync(model, ct).ConfigureAwait(false);
            lock (_modelLeases)
            {
                if (!_modelLeases.TryAdd(model, lease)) lease.Dispose();
            }
        }
        public IDictionary<string, DynamicRoiState> DynamicRoiStates { get; } =
            new Dictionary<string, DynamicRoiState>(StringComparer.OrdinalIgnoreCase);

        public ILogger Logger { get; }
        public DxgiResources DxgiResources { get; }
        public IReadOnlyDictionary<string, Job> AllJobs { get; }
        public IReadOnlyDictionary<string, Makro> AllMakros { get; }
        public IMakroExecutor MakroExecutor { get; }
        public IScriptExecutor ScriptExecutor { get; }
        public IYoloManager YoloManager { get; }
        public IImageDisplayService ImageDisplayService { get; }
        public IDesktopResultOverlay DesktopResultOverlay { get; }
        public ExecutionLogSession? ExecutionLogSession { get; }
        public IExecutionLogService ExecutionLogService { get; }
        public Job CurrentJob { get; }
        public Func<Guid, CancellationToken, Task> ExecuteJob { get; }
        public Func<Guid, CancellationToken, Task>? StartJobViaDispatcherAsync { get; }

        public IDesktopCaptureService DesktopCaptureService { get; }
        public ICameraCaptureService CameraCaptureService { get; }
        public ISet<string> OpenedWindowNames { get; } = new HashSet<string>(StringComparer.Ordinal);
        public TemplateMatching? TemplateMatcher { get; set; }
        public ColorDetector? ColorDetector { get; set; }
        public KeyPointMatcher? KeyPointMatcher { get; set; }
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> _loadedYoloModels =
            new(StringComparer.OrdinalIgnoreCase);
        public void RegisterYoloModel(string model) => _loadedYoloModels.TryAdd(model, 0);
        public IReadOnlyCollection<string> LoadedYoloModels => _loadedYoloModels.Keys.ToArray();

        private readonly Func<int, int, int, IVideoRecorder> _videoRecorderFactory;
        public IVideoRecorder CreateVideoRecorder(int width, int height, int fps) => _videoRecorderFactory(width, height, fps);

        public IDictionary<string, IVideoRecorder> VideoRecorders { get; } = new Dictionary<string, IVideoRecorder>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, DateTime> StepTimeouts { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, PredictMovementState> PredictMovementStates { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, ActiveWindowCacheEntry> ActiveWindowCache { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public IDictionary<string, TaskAutomation.Contracts.Geometry.PixelPoint> Last3DMovements { get; } =
            new Dictionary<string, TaskAutomation.Contracts.Geometry.PixelPoint>(StringComparer.OrdinalIgnoreCase);

        public IDictionary<string, long> Last3DInputTimestamps { get; } =
            new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);

        // ── Konstruktor ────────────────────────────────────────────────────────

        public StepPipelineContext(
            ILogger logger,
            DxgiResources dxgiResources,
            IReadOnlyDictionary<string, Job> allJobs,
            IReadOnlyDictionary<string, Makro> allMakros,
            IMakroExecutor makroExecutor,
            IScriptExecutor scriptExecutor,
            IYoloManager yoloManager,
            IImageDisplayService imageDisplayService,
            IDesktopResultOverlay desktopResultOverlay,
            Job currentJob,
            Func<Guid, CancellationToken, Task> executeJob,
            IDesktopCaptureService desktopCaptureService,
            ICameraCaptureService cameraCaptureService,
            ExecutionLogSession? executionLogSession = null,
            IExecutionLogService? executionLogService = null,
            Func<Guid, CancellationToken, Task>? startJobViaDispatcherAsync = null,
            IReadOnlyDictionary<Guid, (ValueProviderSourceDescriptor Descriptor, string Value)>? secrets = null,
            JobExecutionCancellation? cancellation = null,
            Func<int, int, int, IVideoRecorder>? videoRecorderFactory = null)
        {
            _videoRecorderFactory = videoRecorderFactory ?? ((width, height, fps) => new StreamVideoRecorder(width, height, fps));
            _cancellation = cancellation;
            OwnedExecutions = new OwnedExecutionScope(() => cancellation?.RequestStop(), cancellation?.EndPhaseToken ?? default,
                TimeSpan.FromSeconds(Math.Clamp(currentJob.EndPhaseTimeoutSeconds, Job.MinEndPhaseTimeoutSeconds, Job.MaxEndPhaseTimeoutSeconds)));
            Logger = logger;
            DxgiResources = dxgiResources;
            AllJobs = allJobs;
            AllMakros = allMakros;
            MakroExecutor = makroExecutor;
            ScriptExecutor = scriptExecutor;
            YoloManager = yoloManager;
            ImageDisplayService = imageDisplayService;
            DesktopResultOverlay = desktopResultOverlay;
            ExecutionLogSession = executionLogSession;
            ExecutionLogService = executionLogService
                ?? throw new ArgumentNullException(nameof(executionLogService));
            CurrentJob = currentJob;
            _secrets = secrets
                ?? new Dictionary<Guid, (ValueProviderSourceDescriptor Descriptor, string Value)>();
            _results = CreateResultStore();
            ExecuteJob = executeJob;
            DesktopCaptureService = desktopCaptureService;
            CameraCaptureService = cameraCaptureService;
            StartJobViaDispatcherAsync = startJobViaDispatcherAsync;
        }

        private JobResultStore CreateResultStore()
        {
            var results = new JobResultStore(CurrentJob.Variables, _secrets, CurrentJob.LocalValues);
            foreach (var step in CurrentJob.EnumerateAllSteps()) results.RegisterStep(step);
            return results;
        }

        // ── Iteration-Reset ────────────────────────────────────────────────────

        /// <summary>
        /// Gibt Bitmap-Ressourcen der letzten Runde frei und legt einen leeren Store an.
        /// Muss am Anfang jeder Wiederholungsrunde aufgerufen werden.
        /// </summary>
        public void ResetResults()
        {
            _results.DisposeAndClear();
            _results = CreateResultStore();
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
            OwnedExecutions.Dispose();
            lock (_modelLeases)
            {
                foreach (var lease in _modelLeases.Values) lease.Dispose();
                _modelLeases.Clear();
            }
            TemplateMatcher?.Dispose();
            // DesktopCaptureService ist ein Singleton und wird NICHT hier disposed.
            ColorDetector?.Dispose();
        }
    }
}
