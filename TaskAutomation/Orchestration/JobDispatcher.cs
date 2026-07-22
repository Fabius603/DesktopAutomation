using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using TaskAutomation.Logging;

namespace TaskAutomation.Orchestration
{
    /// <summary>
    /// Zentraler Dispatcher für Jobs und Makros.
    /// Unterstützt beliebig viele gleichzeitige Instanzen desselben Jobs.
    /// </summary>
    public sealed class JobDispatcher : IJobDispatcher, IJobLauncher, IDisposable
    {
        private readonly ILogger<JobDispatcher> _logger;
        private readonly IJobExecutor _executor;

        // instanceId → Jobdefinition und einheitliche Zustands-/Abbruchsteuerung
        private sealed record RunningJobEntry(Guid JobId, string JobName, JobExecutionCancellation Cancellation, JobDebugSession? DebugSession = null);
        private readonly ConcurrentDictionary<Guid, RunningJobEntry> _jobInstances = new();

        /// <summary>Maximale Anzahl gleichzeitig laufender Job-Instanzen.</summary>
        public const int MaxJobCount = 100;

        private sealed record RunningMakroEntry(Guid MakroId, CancellationTokenSource Cts);
        private readonly ConcurrentDictionary<Guid, RunningMakroEntry> _makroInstances = new();

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
        public event Action? DebugSessionsChanged;

        /// <summary>
        /// Wird ausgelöst, wenn sich die Liste der laufenden Makros ändert.
        /// </summary>
        public event Action? RunningMakrosChanged;

        /// <summary>
        /// Alle aktuell laufenden Job-Instanzen.
        /// </summary>
        public IReadOnlyCollection<RunningJobInstance> RunningJobInstances
            => _jobInstances.Select(kvp => new RunningJobInstance(
                kvp.Key,
                kvp.Value.JobId,
                kvp.Value.JobName,
                kvp.Value.Cancellation.State)).ToArray();

        /// <summary>
        /// Distinct Job-IDs aller laufenden Instanzen (für "läuft irgendetwas"-Prüfungen).
        /// </summary>
        public IReadOnlyCollection<Guid> RunningJobIds
            => _jobInstances.Values.Select(e => e.JobId).Distinct().ToArray();

        public IReadOnlyCollection<JobDebugSession> DebugSessions
            => _jobInstances.Values.Where(entry => entry.DebugSession != null).Select(entry => entry.DebugSession!).ToArray();

        /// <summary>
        /// IDs der aktuell laufenden Makros.
        /// </summary>
        public IReadOnlyCollection<Guid> RunningMakroIds
            => _makroInstances.Values.Select(entry => entry.MakroId).Distinct().ToArray();

        public JobDispatcher(IJobExecutor executor, ILogger<JobDispatcher> logger)
        {
            _logger        = logger;
            _executor      = executor      ?? throw new ArgumentNullException(nameof(executor));

            _executor.JobErrorOccurred         += OnJobErrorOccurred;
            _executor.JobStepErrorOccurred     += OnJobStepErrorOccurred;
            _logger.LogInformation("JobDispatcher initialisiert.");
        }

        /// <summary>
        /// Startet eine neue Instanz des Jobs. Mehrere parallele Instanzen desselben Jobs
        /// sind ausdrücklich erlaubt. Gibt die Instanz-ID zurück.
        /// </summary>
        private Guid StartJobInternal(Job job, JobStartContext startContext, bool debug = false)
        {
            if (job.ActiveStepCount == 0)
            {
                var ex = new InvalidOperationException($"Job '{job.Name}' kann nicht gestartet werden, weil er keine aktiven Steps hat.");
                _logger.LogWarning(ex.Message);
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(job.Name, ex));
                return Guid.Empty;
            }

            if (_jobInstances.Count >= MaxJobCount)
            {
                var ex = new JobLimitExceededException(job.Name, MaxJobCount);
                _logger.LogWarning(ex.Message);
                throw ex;
            }

            var instanceId = Guid.NewGuid();
            var cancellation = new JobExecutionCancellation();
            var debugSession = debug ? new JobDebugSession(instanceId, job) : null;
            cancellation.StateChanged += _ => FireRunningJobsChanged();
            var entry      = new RunningJobEntry(job.Id, job.Name, cancellation, debugSession);
            _jobInstances[instanceId] = entry;

            FireRunningJobsChanged();
            if (debugSession != null) DebugSessionsChanged?.Invoke();

            var jobId   = job.Id;
            var jobName = job.Name;
            _ = Task.Run(async () =>
            {
                try
                {
                    await _executor.ExecuteJob(jobId, startContext, cancellation, debugSession).ConfigureAwait(false);
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
                    if (debugSession != null && debugSession.State is not (JobDebugSessionState.Completed or JobDebugSessionState.Cancelled or JobDebugSessionState.Failed))
                        debugSession.Finish(cancellation.ExecutionToken.IsCancellationRequested ? JobDebugSessionState.Cancelled : JobDebugSessionState.Completed);
                    _jobInstances.TryRemove(instanceId, out _);
                    cancellation.Dispose();
                    FireRunningJobsChanged();
                    if (debugSession != null) DebugSessionsChanged?.Invoke();
                }
            });

            return instanceId;
        }

        /// <summary>Bricht eine bestimmte Job-Instanz per Instanz-ID ab.</summary>
        private void CancelJobInstance(Guid instanceId)
        {
            if (_jobInstances.TryGetValue(instanceId, out var entry)
                && entry.Cancellation.State.CanRequestStop())
            {
                _logger.LogDebug("Job '{Name}' (Instanz {Id}) Abbruch.", entry.JobName, instanceId);
                var cancellation = entry.Cancellation;
                _ = Task.Run(() =>
                {
                    cancellation.RequestStop();
                });
            }
            else
            {
                _logger.LogDebug("Für Job-Instanz '{InstanceId}' ist kein normaler Stop mehr möglich.", instanceId);
            }
        }

        /// <summary>Bricht alle laufenden Instanzen eines Jobs per Job-ID ab.</summary>
        private void CancelAllInstancesOfJob(Guid jobId)
        {
            var matches = new List<RunningJobEntry>();
            foreach (var kvp in _jobInstances)
            {
                if (kvp.Value.JobId == jobId && kvp.Value.Cancellation.State.CanRequestStop())
                    matches.Add(kvp.Value);
            }
            if (matches.Count == 0)
            {
                _logger.LogDebug("Kein laufender Job mit Job-ID '{JobId}' gefunden.", jobId);
                return;
            }
            _logger.LogInformation("Breche {Count} Instanz(en) von Job-ID '{JobId}' ab.", matches.Count, jobId);
            _ = Task.Run(() =>
            {
                foreach (var e in matches)
                    e.Cancellation.RequestStop();
            });
        }

        /// <summary>Bricht alle laufenden Job-Instanzen ab.</summary>
        private void CancelAllJobsInternal()
        {
            var matches = new List<RunningJobEntry>();
            foreach (var kvp in _jobInstances)
            {
                if (kvp.Value.Cancellation.State.CanRequestStop())
                    matches.Add(kvp.Value);
            }
            if (matches.Count == 0) return;
            _logger.LogInformation("Breche alle {Count} Job-Instanzen ab.", matches.Count);
            _ = Task.Run(() =>
            {
                foreach (var e in matches)
                    e.Cancellation.RequestStop();
            });
        }

        private void ForceStopAllJobsInternal()
        {
            var matches = _jobInstances.Values.ToArray();
            if (matches.Length == 0) return;
            _logger.LogWarning("Erzwinge den Abbruch aller {Count} Job-Instanzen.", matches.Length);
            _ = Task.Run(() =>
            {
                foreach (var entry in matches)
                    entry.Cancellation.ForceStop();
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

        public void ForceStopJob(Guid instanceId)
        {
            if (_jobInstances.TryGetValue(instanceId, out var entry))
                _ = Task.Run(() => entry.Cancellation.ForceStop());
        }

        public void ForceStopJobsByDefinition(Guid jobDefinitionId)
        {
            var matches = _jobInstances.Values.Where(entry => entry.JobId == jobDefinitionId).ToArray();
            _ = Task.Run(() =>
            {
                foreach (var entry in matches)
                    entry.Cancellation.ForceStop();
            });
        }

        public void ForceStopAllJobs() => ForceStopAllJobsInternal();

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
        public Guid StartJob(Guid id, JobStartContext? startContext = null)
        {
            var job = _executor.AllJobs.Values.FirstOrDefault(j => j.Id == id);
            if (job == null)
            {
                _logger.LogWarning("Job mit ID '{JobId}' nicht gefunden.", id);
                return Guid.Empty;
            }
            try
            {
                return StartJobInternal(job, startContext ?? JobStartContext.Manual);
            }
            catch (JobLimitExceededException ex)
            {
                // Event an UI weiterleiten (zeigt Popup), dann weiterwerfen damit
                // ein aufrufender JobExecutionStep den Eltern-Job abbricht.
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(job.Name, ex));
                throw;
            }
        }

        public JobDebugSession? StartDebugJob(Guid id)
        {
            var job = _executor.AllJobs.Values.FirstOrDefault(candidate => candidate.Id == id);
            if (job == null || DebugSessions.Any(session => session.JobId == id)) return null;
            var instanceId = StartJobInternal(job, JobStartContext.Manual, debug: true);
            return DebugSessions.FirstOrDefault(session => session.InstanceId == instanceId);
        }

        public void DebugStep(Guid instanceId)
        {
            if (_jobInstances.TryGetValue(instanceId, out var entry)) entry.DebugSession?.Step();
        }

        public void DebugContinue(Guid instanceId)
        {
            if (_jobInstances.TryGetValue(instanceId, out var entry)) entry.DebugSession?.Continue();
        }

        public void CancelDebugJob(Guid instanceId)
        {
            if (_jobInstances.TryGetValue(instanceId, out var entry) && entry.DebugSession != null)
            {
                entry.Cancellation.ForceStop();
                entry.DebugSession.Finish(JobDebugSessionState.Cancelled);
            }
        }

        /// <summary>
        /// Registriert die Job-Instanz in RunningJobInstances, führt den Job inline aus (kein Task.Run)
        /// und wartet auf Abschluss. CancellationToken wird mit dem internen CTS verknüpft.
        /// </summary>
        public async Task StartJobAsync(Guid id, CancellationToken ct, JobStartContext? startContext = null)
        {
            var job = _executor.AllJobs.Values.FirstOrDefault(j => j.Id == id);
            if (job == null)
            {
                _logger.LogWarning("Job mit ID '{JobId}' nicht gefunden.", id);
                return;
            }

            if (job.ActiveStepCount == 0)
            {
                var ex = new InvalidOperationException($"Job '{job.Name}' kann nicht gestartet werden, weil er keine aktiven Steps hat.");
                _logger.LogWarning(ex.Message);
                JobErrorOccurred?.Invoke(this, new JobErrorEventArgs(job.Name, ex));
                return;
            }

            if (_jobInstances.Count >= MaxJobCount)
            {
                var ex = new JobLimitExceededException(job.Name, MaxJobCount);
                _logger.LogWarning(ex.Message);
                throw ex;
            }

            var instanceId = Guid.NewGuid();
            using var cancellation = new JobExecutionCancellation(ct);
            cancellation.StateChanged += _ => FireRunningJobsChanged();
            _jobInstances[instanceId] = new RunningJobEntry(job.Id, job.Name, cancellation);
            FireRunningJobsChanged();

            try
            {
                await _executor.ExecuteJob(job.Id, startContext ?? JobStartContext.Manual, cancellation).ConfigureAwait(false);
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
            var cts = new CancellationTokenSource();
            var instanceId = Guid.NewGuid();
            if (!_makroInstances.TryAdd(instanceId, new RunningMakroEntry(makro.Id, cts)))
            {
                cts.Dispose();
                return;
            }

            FireRunningMakrosChanged();

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
                    _makroInstances.TryRemove(instanceId, out _);
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
            var matches = _makroInstances.Values.Where(entry => entry.MakroId == id).ToArray();
            if (matches.Length > 0)
            {
                _logger.LogInformation("Abbruch für {Count} Makro-Instanz(en) (ID: {MakroId}) angefordert.", matches.Length, id);
                foreach (var entry in matches)
                    entry.Cts.Cancel();
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
            _executor.JobErrorOccurred -= OnJobErrorOccurred;
            _executor.JobStepErrorOccurred -= OnJobStepErrorOccurred;
        }
    }
}
