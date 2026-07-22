using System.Collections;
using System.Globalization;
using System.Reflection;
using TaskAutomation.Jobs;
using TaskAutomation.Steps;

namespace TaskAutomation.Orchestration;

public enum JobDebugSessionState
{
    Starting,
    Paused,
    Running,
    Completed,
    Failed,
    Cancelled
}

public sealed class JobDebugStepSnapshot
{
    internal JobDebugStepSnapshot(JobStep step) => StepId = step.Id;

    public string StepId { get; }
    public string StepType { get; internal set; } = string.Empty;
    public string Phase { get; internal set; } = string.Empty;
    public int Iteration { get; internal set; }
    public JobStepDebugState State { get; internal set; }
    public string? InputDetails { get; internal set; }
    public string? ResultDetails { get; internal set; }
    public IReadOnlyList<JobDebugValueNode> OutputValues { get; internal set; } = [];
    public DateTimeOffset? StartedAt { get; internal set; }
    public DateTimeOffset? FinishedAt { get; internal set; }
    public TimeSpan? Duration => StartedAt.HasValue && FinishedAt.HasValue
        ? FinishedAt.Value - StartedAt.Value
        : null;
}

public sealed record JobDebugValueNode(
    string Name,
    string DisplayValue,
    string TypeName,
    IReadOnlyList<JobDebugValueNode> Children)
{
    public bool HasChildren => Children.Count > 0;
}

public sealed class JobDebugSession
{
    private readonly object _gate = new();
    private readonly Dictionary<string, JobDebugStepSnapshot> _snapshots = new();
    private TaskCompletionSource<bool>? _resumeSignal;
    private bool _pauseBeforeNext = true;

    internal JobDebugSession(Guid instanceId, Job job)
    {
        InstanceId = instanceId;
        JobId = job.Id;
        JobName = job.Name;
        foreach (var step in job.StartSteps.Concat(job.Steps).Concat(job.EndSteps))
        {
            step.DebugState = JobStepDebugState.None;
            step.DebugDetails = null;
        }
    }

    public Guid InstanceId { get; }
    public Guid JobId { get; }
    public string JobName { get; }
    public JobDebugSessionState State { get; private set; } = JobDebugSessionState.Starting;
    public string? CurrentStepId { get; private set; }
    public string Phase { get; private set; } = string.Empty;
    public int Iteration { get; private set; }
    public string StatusText { get; private set; } = "Debugger wird gestartet.";
    public event Action? Changed;

    public JobDebugStepSnapshot? GetSnapshot(string stepId)
    {
        lock (_gate)
            return _snapshots.GetValueOrDefault(stepId);
    }

    public IReadOnlyList<JobDebugStepSnapshot> GetSnapshots()
    {
        lock (_gate)
            return _snapshots.Values.ToArray();
    }

    internal void SetIteration(int iteration)
    {
        if (Iteration == iteration) return;
        Iteration = iteration;
        Changed?.Invoke();
    }

    internal async Task BeforeStepAsync(JobStep step, string phase, CancellationToken cancellationToken, string? details = null)
    {
        Task? waitTask = null;
        lock (_gate)
        {
            CurrentStepId = step.Id;
            Phase = phase;
            step.DebugDetails = details;
            var snapshot = GetOrCreateSnapshot(step);
            snapshot.StepType = step.GetType().Name;
            snapshot.Phase = phase;
            snapshot.Iteration = Iteration;
            snapshot.State = JobStepDebugState.Waiting;
            snapshot.InputDetails = details;
            snapshot.ResultDetails = null;
            snapshot.StartedAt = DateTimeOffset.Now;
            snapshot.FinishedAt = null;
            if (!_pauseBeforeNext && !step.IsBreakpoint)
            {
                SetRunning(step, phase);
                snapshot.State = JobStepDebugState.Running;
            }
            else
            {
                step.DebugState = JobStepDebugState.Waiting;
                State = JobDebugSessionState.Paused;
                StatusText = $"Pausiert vor {phase}: {step.GetType().Name}";
                _resumeSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                waitTask = _resumeSignal.Task;
            }
        }
        Changed?.Invoke();
        if (waitTask == null) return;
        await waitTask.WaitAsync(cancellationToken).ConfigureAwait(false);
        lock (_gate)
            SetRunning(step, phase);
        Changed?.Invoke();
    }

    public void Step()
    {
        lock (_gate)
        {
            if (State != JobDebugSessionState.Paused) return;
            _pauseBeforeNext = true;
            _resumeSignal?.TrySetResult(true);
        }
    }

    public void Continue()
    {
        lock (_gate)
        {
            if (State != JobDebugSessionState.Paused) return;
            _pauseBeforeNext = false;
            _resumeSignal?.TrySetResult(true);
        }
    }

    internal void MarkCompleted(JobStep step, string? details = null, object? result = null)
    {
        lock (_gate)
        {
            step.DebugDetails = details;
            step.DebugState = JobStepDebugState.Completed;
            var snapshot = GetOrCreateSnapshot(step);
            snapshot.State = JobStepDebugState.Completed;
            snapshot.ResultDetails = details;
            snapshot.OutputValues = JobDebugValueFormatter.CreateOutputValues(result, details);
            snapshot.FinishedAt = DateTimeOffset.Now;
        }
        Changed?.Invoke();
    }

    internal void MarkSkipped(JobStep step, string reason)
    {
        lock (_gate)
        {
            step.DebugDetails = reason;
            step.DebugState = JobStepDebugState.Skipped;
            var snapshot = GetOrCreateSnapshot(step);
            snapshot.StepType = step.GetType().Name;
            snapshot.Phase = Phase;
            snapshot.Iteration = Iteration;
            snapshot.State = JobStepDebugState.Skipped;
            snapshot.ResultDetails = reason;
            snapshot.OutputValues = JobDebugValueFormatter.CreateOutputValues(null, reason);
            snapshot.FinishedAt = DateTimeOffset.Now;
        }
        Changed?.Invoke();
    }

    internal async Task PauseAfterFailureAsync(JobStep step, Exception exception, CancellationToken cancellationToken)
    {
        Task waitTask;
        lock (_gate)
        {
            step.DebugDetails = exception.Message;
            step.DebugState = JobStepDebugState.Failed;
            var snapshot = GetOrCreateSnapshot(step);
            snapshot.State = JobStepDebugState.Failed;
            snapshot.ResultDetails = exception.Message;
            snapshot.OutputValues = JobDebugValueFormatter.CreateOutputValues(null, exception.Message);
            snapshot.FinishedAt = DateTimeOffset.Now;
            State = JobDebugSessionState.Paused;
            StatusText = $"Fehler in {step.GetType().Name}: {exception.Message}";
            _resumeSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            waitTask = _resumeSignal.Task;
        }
        Changed?.Invoke();
        await waitTask.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    internal void Finish(JobDebugSessionState state)
    {
        State = state;
        CurrentStepId = null;
        StatusText = state switch
        {
            JobDebugSessionState.Completed => "Debug-Ausführung erfolgreich abgeschlossen.",
            JobDebugSessionState.Cancelled => "Debug-Ausführung abgebrochen.",
            _ => "Debug-Ausführung fehlgeschlagen."
        };
        lock (_gate)
            _resumeSignal?.TrySetResult(true);
        Changed?.Invoke();
    }

    private void SetRunning(JobStep step, string phase)
    {
        step.DebugState = JobStepDebugState.Running;
        GetOrCreateSnapshot(step).State = JobStepDebugState.Running;
        State = JobDebugSessionState.Running;
        StatusText = $"{phase}: {step.GetType().Name} wird ausgeführt.";
    }

    private JobDebugStepSnapshot GetOrCreateSnapshot(JobStep step)
    {
        if (_snapshots.TryGetValue(step.Id, out var snapshot)) return snapshot;
        snapshot = new JobDebugStepSnapshot(step) { StepType = step.GetType().Name };
        _snapshots.Add(step.Id, snapshot);
        return snapshot;
    }
}

internal static class JobDebugValueFormatter
{
    private const int MaxDepth = 6;
    private const int MaxItems = 100;

    public static IReadOnlyList<JobDebugValueNode> CreateOutputValues(object? result, string? fallback)
    {
        if (result != null)
            return BuildObjectChildren(result, 0, new HashSet<object>(ReferenceEqualityComparer.Instance));
        return string.IsNullOrWhiteSpace(fallback)
            ? []
            : [new JobDebugValueNode("Status", fallback, "String", [])];
    }

    private static IReadOnlyList<JobDebugValueNode> BuildObjectChildren(
        object value,
        int depth,
        HashSet<object> visited)
    {
        if (depth >= MaxDepth) return [];
        var type = value.GetType();
        if (!type.IsValueType && value is not string && !visited.Add(value)) return [];

        try
        {
            if (value is IDictionary dictionary)
                return dictionary.Cast<DictionaryEntry>().Take(MaxItems)
                    .Select(entry => CreateNode($"[{FormatSimple(entry.Key)}]", entry.Value, depth + 1, visited))
                    .ToArray();

            if (value is IEnumerable enumerable && value is not string)
                return enumerable.Cast<object?>().Take(MaxItems)
                    .Select((item, index) => CreateNode($"[{index}]", item, depth + 1, visited))
                    .ToArray();

            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.CanRead
                                   && property.GetIndexParameters().Length == 0
                                   && property.GetCustomAttribute<ResultHiddenAttribute>() == null)
                .OrderBy(property => property.MetadataToken)
                .Select(property =>
                {
                    object? propertyValue;
                    try { propertyValue = property.GetValue(value); }
                    catch { propertyValue = null; }
                    return CreateNode(Humanize(property.Name), propertyValue, depth + 1, visited);
                })
                .ToArray();
        }
        finally
        {
            if (!type.IsValueType && value is not string) visited.Remove(value);
        }
    }

    private static JobDebugValueNode CreateNode(string name, object? value, int depth, HashSet<object> visited)
    {
        if (value == null) return new JobDebugValueNode(name, "null", "null", []);
        var type = value.GetType();
        if (IsSimple(type))
            return new JobDebugValueNode(name, FormatSimple(value), FriendlyTypeName(type), []);
        if (value is System.Drawing.Bitmap bitmap)
            return new JobDebugValueNode(name, $"{bitmap.Width} × {bitmap.Height} px", "Bitmap", []);

        var children = BuildObjectChildren(value, depth, visited);
        var summary = value is ICollection collection
            ? $"{collection.Count} Elemente"
            : FriendlyTypeName(type);
        return new JobDebugValueNode(name, summary, FriendlyTypeName(type), children);
    }

    private static bool IsSimple(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
               || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan)
               || type == typeof(Guid) || type == typeof(Uri);
    }

    private static string FormatSimple(object? value) => value switch
    {
        null => "null",
        bool boolean => boolean ? "True" : "False",
        DateTime dateTime => dateTime.ToLocalTime().ToString("G", CultureInfo.CurrentCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToLocalTime().ToString("G", CultureInfo.CurrentCulture),
        TimeSpan timeSpan => timeSpan.ToString("c", CultureInfo.InvariantCulture),
        double number => number.ToString("0.###", CultureInfo.CurrentCulture),
        float number => number.ToString("0.###", CultureInfo.CurrentCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.CurrentCulture) ?? string.Empty,
        _ => value.ToString() ?? string.Empty
    };

    private static string FriendlyTypeName(Type type)
    {
        var nullableType = Nullable.GetUnderlyingType(type);
        if (nullableType != null) return FriendlyTypeName(nullableType) + "?";
        if (!type.IsGenericType) return type.Name;
        var name = type.Name.Split('`')[0];
        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(FriendlyTypeName))}>";
    }

    private static string Humanize(string value)
    {
        var result = System.Text.RegularExpressions.Regex.Replace(value, "(?<=[a-z0-9])([A-Z])", " $1");
        return result.Replace(" Id", " ID", StringComparison.Ordinal)
            .Replace(" Utc", " UTC", StringComparison.Ordinal);
    }
}
