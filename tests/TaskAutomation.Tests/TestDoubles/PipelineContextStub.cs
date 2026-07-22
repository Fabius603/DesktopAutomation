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
    public DxgiResources DxgiResources => null!;
    public IReadOnlyDictionary<string, Job> AllJobs { get; } = new Dictionary<string, Job>();
    public IReadOnlyDictionary<string, Makro> AllMakros { get; } = new Dictionary<string, Makro>();
    public IMakroExecutor MakroExecutor => null!;
    public IScriptExecutor ScriptExecutor => null!;
    public IYoloManager YoloManager => null!;
    public IImageDisplayService ImageDisplayService => null!;
    public IDesktopResultOverlay DesktopResultOverlay { get; init; } = new RecordingDesktopResultOverlay();
    public ExecutionLogSession? ExecutionLogSession => null;
    public IExecutionLogService ExecutionLogService => null!;
    public Job CurrentJob { get; } = new();
    public Func<Guid, CancellationToken, Task> ExecuteJob => (_, _) => Task.CompletedTask;
    public Func<Guid, Guid>? StartJobViaDispatcher => null;
    public Func<Guid, CancellationToken, Task>? StartJobViaDispatcherAsync => null;
    public Action<Guid>? CancelJobViaDispatcher => null;
    public IDesktopCaptureService DesktopCaptureService => null!;
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
