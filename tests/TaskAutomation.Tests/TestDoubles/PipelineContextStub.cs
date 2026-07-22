using ImageCapture.ProcessDuplication;
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
    public IJobResultStore Results { get; } = new JobResultStore();
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
    public IExecutionLogService ExecutionLogService => null!;
    public Job CurrentJob { get; init; } = new();
    public Func<Guid, CancellationToken, Task> ExecuteJob { get; init; } = (_, _) => Task.CompletedTask;
    public Func<Guid, Guid>? StartJobViaDispatcher { get; init; }
    public Func<Guid, CancellationToken, Task>? StartJobViaDispatcherAsync { get; init; }
    public Action<Guid>? CancelJobViaDispatcher { get; init; }
    public IDesktopCaptureService DesktopCaptureService { get; init; } = new NoOpDesktopCaptureService();
    public ISet<string> OpenedWindowNames { get; } = new HashSet<string>();
    public IList<Guid> ChildJobInstanceIds { get; } = new List<Guid>();
    public ProcessDuplicator? ProcessDuplicator { get; set; }
    public TemplateMatching? TemplateMatcher { get; set; }
    public ColorDetector? ColorDetector { get; set; }
    public KeyPointMatcher? KeyPointMatcher { get; set; }
    public StreamVideoRecorder? VideoRecorder { get; set; }
    public Dictionary<string, DateTime> StepTimeouts { get; } = new();
    public Dictionary<string, PredictMovementState> PredictMovementStates { get; } = new();
    public Dictionary<string, ActiveWindowCacheEntry> ActiveWindowCache { get; } = new();
}
