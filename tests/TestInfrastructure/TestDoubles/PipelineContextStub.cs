using ImageCapture.Video;
using ImageDetection.Algorithms.ColorDetection;
using ImageDetection.Algorithms.KeyPointMatching;
using ImageDetection.Algorithms.TemplateMatching;
using ImageDetection.YOLO;
using ImageHelperMethods;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TaskAutomation.Events;
using TaskAutomation.Jobs;
using TaskAutomation.Logging;
using TaskAutomation.Makros;
using TaskAutomation.Scripts;
using TaskAutomation.Steps;

namespace TaskAutomation.Tests.TestDoubles;

internal sealed class PipelineContextStub : IStepPipelineContext
{
    public PipelineContextStub(IEnumerable<JobVariable>? variables = null, IEnumerable<LocalValue>? localValues = null) =>
        Results = new JobResultStore(variables, localValues: localValues);

    public TaskAutomation.Orchestration.OwnedExecutionScope? OwnedExecutions { get; init; }
    public Guid ResourceOwnerId { get; init; }
    public IJobResultStore Results { get; }
    public IDictionary<string, DynamicRoiState> DynamicRoiStates { get; } = new Dictionary<string, DynamicRoiState>();
    public ILogger Logger { get; } = NullLogger.Instance;
    public DxgiResources DxgiResources { get; init; } = null!;
    public IReadOnlyDictionary<string, Job> AllJobs { get; init; } = new Dictionary<string, Job>();
    public IReadOnlyDictionary<string, Makro> AllMakros { get; init; } = new Dictionary<string, Makro>();
    public IMakroExecutor MakroExecutor { get; init; } = new NoOpMakroExecutor();
    public IScriptExecutor ScriptExecutor { get; init; } = new DelegateScriptExecutor();
    public IYoloManager YoloManager { get; init; } = new NoOpYoloManager();
    public IImageDisplayService ImageDisplayService { get; init; } = new NoOpImageDisplayService();
    public IDesktopResultOverlay DesktopResultOverlay { get; init; } = new RecordingDesktopResultOverlay();
    public ExecutionLogSession? ExecutionLogSession => null;
    public IExecutionLogService ExecutionLogService { get; init; } = new RecordingExecutionLogService();
    public Job CurrentJob { get; init; } = new();
    public Func<Guid, CancellationToken, Task> ExecuteJob { get; init; } = (_, _) => Task.CompletedTask;
    public Func<Guid, CancellationToken, Task>? StartJobViaDispatcherAsync { get; init; }
    public IDesktopCaptureService DesktopCaptureService { get; init; } = new NoOpDesktopCaptureService();
    public ICameraCaptureService CameraCaptureService { get; init; } = new NoOpCameraCaptureService();
    public ISet<string> OpenedWindowNames { get; } = new HashSet<string>();
    public TemplateMatching? TemplateMatcher { get; set; }
    public ColorDetector? ColorDetector { get; set; }
    public KeyPointMatcher? KeyPointMatcher { get; set; }
    public Func<int, int, int, IVideoRecorder> RecorderFactory { get; init; } =
        (width, height, fps) => new StreamVideoRecorder(width, height, fps);
    public IVideoRecorder CreateVideoRecorder(int width, int height, int fps) => RecorderFactory(width, height, fps);
    public IDictionary<string, IVideoRecorder> VideoRecorders { get; } = new Dictionary<string, IVideoRecorder>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, DateTime> StepTimeouts { get; } = new();
    public IDictionary<string, TaskAutomation.Contracts.Geometry.PixelPoint> Last3DMovements { get; } =
        new Dictionary<string, TaskAutomation.Contracts.Geometry.PixelPoint>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<string, long> Last3DInputTimestamps { get; } =
        new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, PredictMovementState> PredictMovementStates { get; } = new();
    public Dictionary<string, ActiveWindowCacheEntry> ActiveWindowCache { get; } = new();
}
