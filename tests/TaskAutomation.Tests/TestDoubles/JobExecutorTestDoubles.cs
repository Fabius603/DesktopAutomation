using Common.JsonRepository;
using ImageCapture.DesktopDuplication.RecordingIndicator;
using ImageDetection.Model;
using ImageDetection.YOLO;
using System.Drawing;
using TaskAutomation.Events;
using TaskAutomation.Jobs;
using TaskAutomation.Logging;
using TaskAutomation.Makros;
using TaskAutomation.Orchestration;
using TaskAutomation.Scripts;
using TaskAutomation.Steps;
using TaskAutomation.Timing;

namespace TaskAutomation.Tests.TestDoubles;

internal sealed class InMemoryRepository<T>(IEnumerable<T>? items = null) : IJsonRepository<T>
{
    private List<T> _items = items?.ToList() ?? [];
    public string DirectoryPath => string.Empty;
    public Task<IReadOnlyList<T>> LoadAllAsync() => Task.FromResult<IReadOnlyList<T>>(_items.ToArray());
    public Task SaveAllAsync(IEnumerable<T> items) { _items = items.ToList(); return Task.CompletedTask; }
    public Task<T?> LoadAsync(string name) => Task.FromResult(default(T));
    public Task SaveAsync(T item) { _items.Add(item); return Task.CompletedTask; }
    public Task DeleteAsync(string name) => Task.CompletedTask;
}

internal sealed class RecordingExecutionLogService : IExecutionLogService
{
    public event EventHandler<ExecutionLogEntry>? EntryWritten;
    public event EventHandler<ExecutionLogSession>? SessionChanged;
    public List<ExecutionLogSession> MutableSessions { get; } = [];
    public List<ExecutionLogEntry> Entries { get; } = [];
    public List<(ExecutionLogSession Session, bool Success, bool Cancelled, string? Details)> Completions { get; } = [];
    public IReadOnlyList<ExecutionLogSession> Sessions => MutableSessions;

    public ExecutionLogSession BeginJob(Guid jobId, string jobName, JobStartContext? startContext = null)
    {
        var session = new ExecutionLogSession(Guid.NewGuid(), ExecutionLogKind.Job, jobId, jobName, string.Empty,
            startContext: startContext);
        MutableSessions.Add(session);
        SessionChanged?.Invoke(this, session);
        return session;
    }

    public void Write(ExecutionLogSession session, ExecutionLogLevel level, string message, string? details = null,
        string? stepId = null, string? stepType = null, long? durationMs = null, JobExecutionState? jobState = null)
    {
        var entry = new ExecutionLogEntry { SessionId = session.Id, Timestamp = DateTimeOffset.UtcNow, Level = level,
            Message = message, Details = details, StepId = stepId, StepType = stepType, DurationMs = durationMs, JobState = jobState };
        Entries.Add(entry);
        EntryWritten?.Invoke(this, entry);
    }

    public void Complete(ExecutionLogSession session, bool success, string? details = null, bool cancelled = false)
    {
        session.EndedAt = DateTimeOffset.UtcNow;
        Completions.Add((session, success, cancelled, details));
        SessionChanged?.Invoke(this, session);
    }
    public IReadOnlyList<ExecutionLogEntry> ReadEntries(Guid sessionId, int maxEntries = 2000) =>
        Entries.Where(entry => entry.SessionId == sessionId).TakeLast(maxEntries).ToArray();
    public Task<IReadOnlyList<ExecutionLogEntry>> ReadEntriesAsync(Guid sessionId, int maxEntries = 2000,
        CancellationToken cancellationToken = default) => Task.FromResult(ReadEntries(sessionId, maxEntries));
    public void ReloadSessions() { }
}

internal sealed class NoOpRecordingIndicator : IRecordingIndicatorOverlay
{
    public bool IsRunning { get; private set; }
    public void Start(RecordingIndicatorOptions? options = null) => IsRunning = true;
    public void Stop() => IsRunning = false;
    public void Dispose() { }
}

internal sealed class NoOpImageDisplayService : IImageDisplayService
{
    public sealed record DisplayCall(string WindowName, Bitmap Image, ImageDisplayType DisplayType);
    public event EventHandler<ImageDisplayRequestedEventArgs>? ImageDisplayRequested;
    public List<string> ClosedWindows { get; } = [];
    public List<DisplayCall> DisplayCalls { get; } = [];
    public void DisplayImage(string windowName, Bitmap image, ImageDisplayType displayType)
    {
        DisplayCalls.Add(new(windowName, image, displayType));
        ImageDisplayRequested?.Invoke(this, new(windowName, image, displayType));
    }
    public void CloseWindow(string windowName) => ClosedWindows.Add(windowName);
    public void CloseAllWindows() { }
}

internal sealed class NoOpYoloManager : IYoloManager
{
    public event Action<string, ModelDownloadStatus, int, string?>? DownloadProgressChanged;
    public Task EnsureModelAsync(string modelKey, CancellationToken ct = default) => Task.CompletedTask;
    public bool HasSession(string modelKey) => false;
    public List<string> GetAvailableModels() => [];
    public List<string> GetClassesForModel(string modelKey) => [];
    public Task<IDetectionResult?> DetectAsync(string modelKey, string objectName, Bitmap bitmap, float threshold,
        Rectangle? roi = null, CancellationToken ct = default) => Task.FromResult<IDetectionResult?>(null);
    public bool UnloadModel(string modelKey) => true;
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class RecordingYoloManager : IYoloManager
{
    public event Action<string, ModelDownloadStatus, int, string?>? DownloadProgressChanged;
    public List<(string Model, CancellationToken CancellationToken)> EnsureCalls { get; } = [];
    public List<(string Model, string ClassName, Bitmap Image, float Threshold, Rectangle? Roi, CancellationToken CancellationToken)> DetectCalls { get; } = [];
    public IDetectionResult? DetectionResult { get; set; }
    public Func<CancellationToken, Task>? EnsureAction { get; set; }

    public async Task EnsureModelAsync(string modelKey, CancellationToken ct = default)
    {
        EnsureCalls.Add((modelKey, ct));
        if (EnsureAction is not null)
            await EnsureAction(ct);
    }

    public Task<IDetectionResult?> DetectAsync(string modelKey, string objectName, Bitmap bitmap, float threshold,
        Rectangle? roi = null, CancellationToken ct = default)
    {
        DetectCalls.Add((modelKey, objectName, bitmap, threshold, roi, ct));
        return Task.FromResult(DetectionResult);
    }

    public bool HasSession(string modelKey) => false;
    public List<string> GetAvailableModels() => [];
    public List<string> GetClassesForModel(string modelKey) => [];
    public bool UnloadModel(string modelKey) => true;
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

internal sealed class DelegateScriptExecutor : IScriptExecutor
{
    public Func<string, string, CancellationToken, Task> Execute { get; set; } = (_, _, _) => Task.CompletedTask;
    public Task ExecuteScriptFile(string scriptPath, string arguments, CancellationToken ct,
        Action<string, bool>? outputCallback = null) => Execute(scriptPath, arguments, ct);
}

internal sealed class NoOpMakroExecutor : IMakroExecutor
{
    public Task ExecuteMakro(Makro makro, ImageHelperMethods.DxgiResources dxgi, CancellationToken ct) => Task.CompletedTask;
}

internal sealed class NoOpDesktopCaptureService : IDesktopCaptureService
{
    public Task<CaptureFrame> CaptureAsync(int monitorIdx, CancellationToken ct, bool captureCursor = false) =>
        Task.FromResult(CaptureFrame.Default);
    public void Dispose() { }
}

internal sealed class ControlledDelayService : IPreciseDelayService
{
    public int Calls { get; private set; }
    public Action<int>? OnDelay { get; set; }
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        OnDelay?.Invoke(++Calls);
        return Task.CompletedTask;
    }
    public Task DelayUntilAsync(long targetTimestamp, CancellationToken cancellationToken = default) =>
        DelayAsync(TimeSpan.Zero, cancellationToken);
}
