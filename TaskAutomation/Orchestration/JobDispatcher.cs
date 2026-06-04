using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Hotkeys;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;

namespace TaskAutomation.Orchestration
{
    /// <summary>
    /// Dispatcher, der auf Hotkey-Events hört und Jobs über einen IJobExecutor startet.
    /// Unterstützt beliebig viele gleichzeitige Instanzen desselben Jobs.
    /// </summary>
    public sealed class JobDispatcher : IJobDispatcher, IJobLauncher, IDisposable
    {
        private readonly ILogger<JobDispatcher> _logger;
        private readonly IJobExecutor _executor;
        private readonly IGlobalHotkeyService _hotkeyService;

        // instanceId → (JobId, JobName, CancellationTokenSource)
        private record RunningJobEntry(Guid JobId, string JobName, CancellationTokenSource Cts);
        private readonly ConcurrentDictionary<Guid, RunningJobEntry> _jobInstances = new();

        /// <summary>Maximale Anzahl gleichzeitig laufender Job-Instanzen.</summary>
        public const int MaxJobCount = 100;

        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _makroTokens = new();

        // Debounce: maximal ein Notify-Task gleichzeitig pro Event, feuert nach 50 ms Stille.
        private int _jobsChangedPending   = 0;
        private int _makrosChangedPending = 0;

        public event EventHandler<JobErrorEventArgs>? JobErrorOccurred;

        /// <summary>
        /// Wird ausgelöst, wenn bei der Ausführung eines Job-Steps ein Fehler auftritt.
        /// </summary>
        public event EventHandler<JobStepErrorEventArgs>? JobStepErrorOccurred;

        /// <summary>
        /// Wird ausgelöst, wenn sich die Liste der laufenden Jobs ändert.
        /// </summary>
        public event Action? RunningJobsChanged;

        /// <summary>
        /// Wird ausgelöst, wenn sich die Liste der laufenden Makros ändert.
        /// </summary>
        public event Action? RunningMakrosChanged;

        /// <summary>
        /// Alle aktuell laufenden Job-Instanzen.
        /// </summary>
        public IReadOnlyCollection<RunningJobInstance> RunningJobInstances
            => _jobInstances.Select(kvp => new RunningJobInstance(kvp.Key, kvp.Value.JobId, kvp.Value.JobName)).ToArray();

        /// <summary>
        /// Distinct Job-IDs aller laufenden Instanzen (für "läuft irgendetwas"-Prüfungen).
        /// </summary>
        public IReadOnlyCollection<Guid> RunningJobIds
            => _jobInstances.Values.Select(e => e.JobId).Distinct().ToArray();

        /// <summary>
        /// IDs der aktuell laufenden Makros.
        /// </summary>
        public IReadOnlyCollection<Guid> RunningMakroIds => _makroTokens.Keys.ToArray();

        public JobDispatcher(IGlobalHotkeyService hotkeyService, IJobExecutor executor, ILogger<JobDispatcher> logger)
        {
            _logger        = logger;
            _hotkeyService = hotkeyService ?? throw new ArgumentNullException(nameof(hotkeyService));
            _executor      = executor      ?? throw new ArgumentNullException(nameof(executor));

            _hotkeyService.HotkeyPressed       += OnHotkeyPressed;
            _executor.JobErrorOccurred         += OnJobErrorOccurred;
            _executor.JobStepErrorOccurred     += OnJobStepErrorOccurred;
            _logger.LogInformation("JobDispatcher initialisiert.");
        }

        private void OnHotkeyPressed(object? sender, HotkeyPressedEventArgs e)
        {
            if (e?.Job is null)
                return;

            var jobRef = e.Job;

            if (jobRef.ActionType == HotkeyActionType.Makro)
            {
                var cmd = jobRef.Command;
                switch (cmd)
                {
                    case ActionCommand.Stop:
                        if (jobRef.MakroId.HasValue) CancelMakro(jobRef.MakroId.Value);
                        break;
                    case ActionCommand.Toggle:
                        if (jobRef.MakroId.HasValue)
                        {
                            if (_makroTokens.ContainsKey(jobRef.MakroId.Value))
                                CancelMakro(jobRef.MakroId.Value);
                            else
                                StartMakro(jobRef.MakroId.Value);
                        }
                        break;
                    default: // Start
                        if (jobRef.MakroId.HasValue) StartMakro(jobRef.MakroId.Value);
                        break;
                }
                return;
            }

            var jobCmd = jobRef.Command;
            switch (jobCmd)
            {
                case ActionCommand.Start:
                    StartJobByAction(jobRef);
                    break;
                case ActionCommand.Stop:
                    CancelJobByAction(jobRef);
                    break;
                case ActionCommand.Toggle:
                    ToggleJobByAction(jobRef);
                    break;
            }
        }

        private void StartJobByAction(JobReference jobRef)
        {
            var job = FindJobByAction(jobRef);
            if (job == null)
            {
                _logger.LogWarning("No job found for action: Name='{ActionName}', ID='{JobId}'", jobRef.Name, jobRef.JobId);
                return;
            }
            try   { StartJobInternal(job); }
            catch (JobLimitExceededException ex) { JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(job.Name, ex)); }
        }

        private void CancelJobByAction(JobReference jobRef)
        {
            var job = FindJobByAction(jobRef);
            if (job != null)
                CancelAllInstancesOfJob(job.Id);
            else
                _logger.LogWarning("No job found for action: Name='{ActionName}', ID='{JobId}'", jobRef.Name, jobRef.JobId);
        }

        private void ToggleJobByAction(JobReference jobRef)
        {
            var job = FindJobByAction(jobRef);
            if (job == null)
            {
                _logger.LogWarning("No job found for action: Name='{ActionName}', ID='{JobId}'", jobRef.Name, jobRef.JobId);
                return;
            }
            if (_jobInstances.Values.Any(e => e.JobId == job.Id))
                CancelAllInstancesOfJob(job.Id);
            else
            {
                try   { StartJobInternal(job); }
                catch (JobLimitExceededException ex) { JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(job.Name, ex)); }
            }
        }

        private Job? FindJobByAction(JobReference jobRef)
        {
            if (jobRef.JobId.HasValue)
            {
                var job = _executor.AllJobs.Values.FirstOrDefault(j => j.Id == jobRef.JobId.Value);
                if (job == null)
                    _logger.LogWarning("Job with ID '{JobId}' not found", jobRef.JobId);
                return job;
            }
            else if (!string.IsNullOrWhiteSpace(jobRef.Name))
            {
                var job = _executor.AllJobs.Values.FirstOrDefault(j => string.Equals(j.Name, jobRef.Name, StringComparison.OrdinalIgnoreCase));
                if (job == null)
                    _logger.LogWarning("Job with name '{JobName}' not found", jobRef.Name);
                return job;
            }
            else
            {
                _logger.LogWarning("Action has neither JobId nor valid Name");
                return null;
            }
        }

        /// <summary>
        /// Startet eine neue Instanz des Jobs. Mehrere parallele Instanzen desselben Jobs
        /// sind ausdrücklich erlaubt. Gibt die Instanz-ID zurück.
        /// </summary>
        private Guid StartJobInternal(Job job)
        {
            if (_jobInstances.Count >= MaxJobCount)
            {
                var ex = new JobLimitExceededException(job.Name, MaxJobCount);
                _logger.LogWarning(ex.Message);
                throw ex;
            }

            var instanceId = Guid.NewGuid();
            var cts        = new CancellationTokenSource();
            var entry      = new RunningJobEntry(job.Id, job.Name, cts);
            _jobInstances[instanceId] = entry;

            FireRunningJobsChanged();

            var jobId   = job.Id;
            var jobName = job.Name;
            _ = Task.Run(async () =>
            {
                try
                {
                    await _executor.ExecuteJob(jobId, cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Job '{Name}' (Instanz {Id}) abgebrochen.", jobName, instanceId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler bei Job '{Name}' (Instanz {Id})", jobName, instanceId);
                }
                finally
                {
                    _jobInstances.TryRemove(instanceId, out _);
                    cts.Dispose();
                    FireRunningJobsChanged();
                }
            });

            return instanceId;
        }

        /// <summary>Bricht eine bestimmte Job-Instanz per Instanz-ID ab.</summary>
        private void CancelJobInstance(Guid instanceId)
        {
            if (_jobInstances.TryRemove(instanceId, out var entry))
            {
                _logger.LogDebug("Job '{Name}' (Instanz {Id}) Abbruch.", entry.JobName, instanceId);
                // Cancel() in Task.Run: läuft Callbacks synchron INNERHALB des Tasks, bevor
                // irgendwas disposed wird. CTS-Disposal übernimmt StartJobInternal's finally.
                var cts = entry.Cts;
                _ = Task.Run(() =>
                {
                    try { cts.Cancel(); }
                    catch (ObjectDisposedException) { }
                });
                FireRunningJobsChanged();
            }
            else
            {
                _logger.LogWarning("Keine laufende Job-Instanz mit ID '{InstanceId}' gefunden.", instanceId);
            }
        }

        /// <summary>Bricht alle laufenden Instanzen eines Jobs per Job-ID ab.</summary>
        private void CancelAllInstancesOfJob(Guid jobId)
        {
            var removed = new List<RunningJobEntry>();
            foreach (var kvp in _jobInstances)
            {
                if (kvp.Value.JobId == jobId && _jobInstances.TryRemove(kvp.Key, out var entry))
                    removed.Add(entry);
            }
            if (removed.Count == 0)
            {
                _logger.LogWarning("Kein laufender Job mit Job-ID '{JobId}' gefunden.", jobId);
                return;
            }
            _logger.LogInformation("Breche {Count} Instanz(en) von Job-ID '{JobId}' ab.", removed.Count, jobId);
            FireRunningJobsChanged();
            // Cancel() läuft Callbacks synchron, alle parallel – kein Dispose (StartJobInternal's finally ist Eigentümer).
            _ = Task.Run(() =>
            {
                foreach (var e in removed)
                    try { e.Cts.Cancel(); }
                    catch (ObjectDisposedException) { }
            });
        }

        /// <summary>Bricht alle laufenden Job-Instanzen ab.</summary>
        private void CancelAllJobsInternal()
        {
            var removed = new List<RunningJobEntry>();
            foreach (var kvp in _jobInstances)
            {
                if (_jobInstances.TryRemove(kvp.Key, out var entry))
                    removed.Add(entry);
            }
            if (removed.Count == 0) return;
            _logger.LogInformation("Breche alle {Count} Job-Instanzen ab.", removed.Count);
            FireRunningJobsChanged();
            _ = Task.Run(() =>
            {
                foreach (var e in removed)
                    try { e.Cts.Cancel(); }
                    catch (ObjectDisposedException) { }
            });
        }

        private void OnJobErrorOccurred(object? sender, JobErrorEventArgs e)
        {
            // Event an UI weiterleiten
            JobErrorOccurred?.Invoke(this, e);
        }

        /// <summary>Feuert <see cref="RunningJobsChanged"/> maximal einmal alle 150 ms (debounced).</summary>
        private void FireRunningJobsChanged()
        {
            if (Interlocked.CompareExchange(ref _jobsChangedPending, 1, 0) == 0)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(150).ConfigureAwait(false);
                    Interlocked.Exchange(ref _jobsChangedPending, 0);
                    RunningJobsChanged?.Invoke();
                });
            }
        }

        /// <summary>Feuert <see cref="RunningMakrosChanged"/> maximal einmal alle 150 ms (debounced).</summary>
        private void FireRunningMakrosChanged()
        {
            if (Interlocked.CompareExchange(ref _makrosChangedPending, 1, 0) == 0)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(150).ConfigureAwait(false);
                    Interlocked.Exchange(ref _makrosChangedPending, 0);
                    RunningMakrosChanged?.Invoke();
                });
            }
        }

        private void OnJobStepErrorOccurred(object? sender, JobStepErrorEventArgs e)
        {
            // Event an UI weiterleiten
            JobStepErrorOccurred?.Invoke(this, e);
        }

        /// <summary>        /// Bricht alle laufenden Instanzen eines Jobs per Job-Definitions-ID ab (asynchron).
        /// </summary>
        public void CancelJobsByDefinition(Guid jobDefinitionId) => CancelAllInstancesOfJob(jobDefinitionId);

        /// <summary>
        /// Bricht alle laufenden Job-Instanzen ab (asynchron).
        /// </summary>
        public void CancelAllJobs() => CancelAllJobsInternal();

        /// <summary>        /// Fordert den Abbruch aller laufenden Instanzen eines Jobs per Name an.
        /// </summary>
        public void CancelJob(string name)
        {
            var job = _executor.AllJobs.Values.FirstOrDefault(j =>
                string.Equals(j.Name, name, StringComparison.OrdinalIgnoreCase));
            if (job == null)
            {
                _logger.LogWarning("Kein Job mit Namen '{Name}' gefunden.", name);
                return;
            }
            CancelAllInstancesOfJob(job.Id);
        }

        /// <summary>
        /// Bricht eine bestimmte Job-Instanz per Instanz-ID ab.
        /// </summary>
        public void CancelJob(Guid instanceId) => CancelJobInstance(instanceId);

        /// <summary>
        /// Startet eine neue Instanz des Jobs per ID und gibt die Instanz-ID zurück.
        /// </summary>
        public Guid StartJob(Guid id)
        {
            var job = _executor.AllJobs.Values.FirstOrDefault(j => j.Id == id);
            if (job == null)
            {
                _logger.LogWarning("Job mit ID '{JobId}' nicht gefunden.", id);
                return Guid.Empty;
            }
            try
            {
                return StartJobInternal(job);
            }
            catch (JobLimitExceededException ex)
            {
                // Event an UI weiterleiten (zeigt Popup), dann weiterwerfen damit
                // ein aufrufender JobExecutionStep den Eltern-Job abbricht.
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(job.Name, ex));
                throw;
            }
        }

        /// <summary>
        /// Registriert die Job-Instanz in RunningJobInstances, führt den Job inline aus (kein Task.Run)
        /// und wartet auf Abschluss. CancellationToken wird mit dem internen CTS verknüpft.
        /// </summary>
        public async Task StartJobAsync(Guid id, CancellationToken ct)
        {
            var job = _executor.AllJobs.Values.FirstOrDefault(j => j.Id == id);
            if (job == null)
            {
                _logger.LogWarning("Job mit ID '{JobId}' nicht gefunden.", id);
                return;
            }

            if (_jobInstances.Count >= MaxJobCount)
            {
                var ex = new JobLimitExceededException(job.Name, MaxJobCount);
                _logger.LogWarning(ex.Message);
                throw ex;
            }

            var instanceId = Guid.NewGuid();
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _jobInstances[instanceId] = new RunningJobEntry(job.Id, job.Name, linkedCts);
            FireRunningJobsChanged();

            try
            {
                await _executor.ExecuteJob(job.Id, linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Job '{Name}' (Instanz {Id}) abgebrochen.", job.Name, instanceId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler bei Job '{Name}' (Instanz {Id})", job.Name, instanceId);
            }
            finally
            {
                _jobInstances.TryRemove(instanceId, out _);
                FireRunningJobsChanged();
            }
        }

        /// <summary>
        /// Startet ein Makro per ID.
        /// </summary>
        public void StartMakro(Guid id)
        {
            var makro = _executor.AllMakros.Values.FirstOrDefault(m => m.Id == id);
            if (makro == null)
            {
                _logger.LogWarning("Makro mit ID '{MakroId}' nicht gefunden.", id);
                return;
            }
            StartMakroInternal(makro);
        }

        private void StartMakroInternal(Makro makro)
        {
            if (_makroTokens.ContainsKey(makro.Id))
            {
                _logger.LogWarning("Makro '{Name}' läuft bereits.", makro.Name);
                return;
            }

            var cts = new CancellationTokenSource();
            if (!_makroTokens.TryAdd(makro.Id, cts))
                return;

            FireRunningMakrosChanged();

            var makroId = makro.Id;
            var makroName = makro.Name;
            _ = Task.Run(async () =>
            {
                try
                {
                    await _executor.MakroExecutor.ExecuteMakro(makro, null!, cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Makro '{Name}' abgebrochen.", makroName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler bei Makro '{Name}'", makroName);
                }
                finally
                {
                    _makroTokens.TryRemove(makroId, out _);
                    cts.Dispose();
                    FireRunningMakrosChanged();
                }
            });
        }

        /// <summary>
        /// Bricht ein laufendes Makro per ID ab.
        /// </summary>
        public void CancelMakro(Guid id)
        {
            if (_makroTokens.TryRemove(id, out var cts))
            {
                _logger.LogInformation("Makro (ID: {MakroId}) Abbruch angefordert.", id);
                cts.Cancel();
                cts.Dispose();
                FireRunningMakrosChanged();
            }
            else
            {
                _logger.LogWarning("Kein laufendes Makro mit ID '{MakroId}' gefunden.", id);
            }
        }

        /// <summary>
        /// Hebt die Event-Registrierung auf.
        /// </summary>
        public void Dispose()
        {
            _hotkeyService.HotkeyPressed -= OnHotkeyPressed;
            _executor.JobErrorOccurred -= OnJobErrorOccurred;
            _executor.JobStepErrorOccurred -= OnJobStepErrorOccurred;
        }
    }
}
