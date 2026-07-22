using Common.JsonRepository;
using TaskAutomation.Automations;
using TaskAutomation.Jobs;
using TaskAutomation.Logging;
using TaskAutomation.Orchestration;

namespace TaskAutomation.Tests.TestDoubles;

internal sealed class AutomationRepository(params AutomationDefinition[] definitions) : IJsonRepository<AutomationDefinition>
{
    private readonly List<AutomationDefinition> _definitions = [.. definitions];
    public string DirectoryPath => string.Empty;
    public int SaveCalls { get; private set; }
    public Task<IReadOnlyList<AutomationDefinition>> LoadAllAsync() =>
        Task.FromResult<IReadOnlyList<AutomationDefinition>>(_definitions.ToArray());
    public Task SaveAllAsync(IEnumerable<AutomationDefinition> items) => Task.CompletedTask;
    public Task<AutomationDefinition?> LoadAsync(string name) => Task.FromResult<AutomationDefinition?>(null);
    public Task SaveAsync(AutomationDefinition item) { SaveCalls++; return Task.CompletedTask; }
    public Task DeleteAsync(string name) => Task.CompletedTask;
}

internal sealed class ManualAutomationTriggerProvider(params AutomationTriggerKind[] kinds) : IAutomationTriggerProvider
{
    public IReadOnlyCollection<AutomationTriggerKind> SupportedKinds { get; } = kinds;
    public event Action<Guid>? Triggered;
    public int StartCalls { get; private set; }
    public int StopCalls { get; private set; }
    public List<Guid> Registered { get; } = [];
    public List<Guid> Unregistered { get; } = [];
    public Task StartAsync(CancellationToken ct = default) { StartCalls++; return Task.CompletedTask; }
    public Task StopAsync(CancellationToken ct = default) { StopCalls++; return Task.CompletedTask; }
    public Task RegisterAsync(AutomationDefinition automation, CancellationToken ct = default)
    { Registered.Add(automation.Id); return Task.CompletedTask; }
    public Task UnregisterAsync(Guid automationId, CancellationToken ct = default)
    { Unregistered.Add(automationId); return Task.CompletedTask; }
    public DateTimeOffset? GetNextRun(Guid automationId) => null;
    public void Fire(Guid id) => Triggered?.Invoke(id);
}

internal sealed class RecordingAutomationLogService : IAutomationLogService
{
    public event EventHandler<AutomationLogEntry>? EntryWritten;
    public event EventHandler? LogsChanged;
    public IReadOnlyList<AutomationLog> Logs => [];
    public List<AutomationLogEntry> Entries { get; } = [];
    public void Synchronize(IEnumerable<AutomationDefinition> automations) => LogsChanged?.Invoke(this, EventArgs.Empty);
    public void Write(Guid automationId, ExecutionLogLevel level, string message, string? details = null)
    {
        var entry = new AutomationLogEntry { AutomationId = automationId, Timestamp = DateTimeOffset.UtcNow,
            Level = level, Message = message, Details = details };
        Entries.Add(entry);
        EntryWritten?.Invoke(this, entry);
    }
    public IReadOnlyList<AutomationLogEntry> ReadEntries(Guid automationId, int maxEntries = 3000) =>
        Entries.Where(entry => entry.AutomationId == automationId).TakeLast(maxEntries).ToArray();
}

internal sealed class RecordingJobDispatcher : IJobDispatcher
{
    public event EventHandler<JobErrorEventArgs>? JobErrorOccurred;
    public event EventHandler<JobStepErrorEventArgs>? JobStepErrorOccurred;
    public event Action? RunningJobsChanged;
    public event Action? DebugSessionsChanged;
    public event Action? RunningMakrosChanged;
    public HashSet<Guid> MutableRunningJobs { get; } = [];
    public HashSet<Guid> MutableRunningMakros { get; } = [];
    public IReadOnlyCollection<RunningJobInstance> RunningJobInstances => [];
    public IReadOnlyCollection<Guid> RunningJobIds => MutableRunningJobs;
    public IReadOnlyCollection<JobDebugSession> DebugSessions => [];
    public IReadOnlyCollection<Guid> RunningMakroIds => MutableRunningMakros;
    public List<(Guid Id, JobStartContext? Context)> StartedJobs { get; } = [];
    public List<Guid> CancelledJobDefinitions { get; } = [];
    public List<Guid> StartedMakros { get; } = [];
    public List<Guid> CancelledMakros { get; } = [];
    public Guid StartJob(Guid id, JobStartContext? startContext = null)
    { StartedJobs.Add((id, startContext)); return Guid.NewGuid(); }
    public JobDebugSession? StartDebugJob(Guid id) => null;
    public void DebugStep(Guid instanceId) { }
    public void DebugContinue(Guid instanceId) { }
    public void CancelDebugJob(Guid instanceId) { }
    public Task StartJobAsync(Guid id, CancellationToken ct, JobStartContext? startContext = null)
    { StartJob(id, startContext); return Task.CompletedTask; }
    public void CancelJob(Guid instanceId) { }
    public void CancelJob(string name) { }
    public void CancelJobsByDefinition(Guid jobDefinitionId) => CancelledJobDefinitions.Add(jobDefinitionId);
    public void CancelAllJobs() { }
    public void ForceStopJob(Guid instanceId) { }
    public void ForceStopJobsByDefinition(Guid jobDefinitionId) { }
    public void ForceStopAllJobs() { }
    public void StartMakro(Guid id) { StartedMakros.Add(id); RunningMakrosChanged?.Invoke(); }
    public void CancelMakro(Guid id) => CancelledMakros.Add(id);
    public void Dispose() { }
}
