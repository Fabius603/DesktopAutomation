using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Hotkeys;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;
using Common.Logging;

namespace TaskAutomation.Orchestration
{
    /// <summary>
    /// Dispatcher, der auf Hotkey-Events hört und Jobs über einen IJobExecutor startet.
    /// </summary>
    public sealed class JobDispatcher : IJobDispatcher, IDisposable
    {
        private readonly ILogger<JobDispatcher> _logger;
        private readonly IJobExecutor _executor;
        private readonly IGlobalHotkeyService _hotkeyService;
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _jobTokens;
        private readonly ConcurrentDictionary<Guid, CancellationTokenSource> _makroTokens;

        /// <summary>
        /// Wird ausgelöst, wenn bei der Ausführung eines Jobs ein Fehler auftritt.
        /// </summary>
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
        /// IDs der aktuell laufenden Jobs.
        /// </summary>
        public IReadOnlyCollection<Guid> RunningJobIds => _jobTokens.Keys.ToArray();

        /// <summary>
        /// IDs der aktuell laufenden Makros.
        /// </summary>
        public IReadOnlyCollection<Guid> RunningMakroIds => _makroTokens.Keys.ToArray();

        /// <summary>
        /// Erstellt einen neuen Dispatcher mit dem gegebenen JobExecutor.
        /// </summary>
        /// <param name="hotkeyService">Instanz des GlobalHotkeyService</param>
        /// <param name="executor">Executor, der Jobs per Name ausführt</param>
        /// <param name="logger">Logger für JobDispatcher</param>
        public JobDispatcher(IGlobalHotkeyService hotkeyService, IJobExecutor executor, ILogger<JobDispatcher> logger)
        {
            _logger = logger;

            _hotkeyService = hotkeyService ?? throw new ArgumentNullException(nameof(hotkeyService));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));

            _jobTokens = new ConcurrentDictionary<Guid, CancellationTokenSource>();
            _makroTokens = new ConcurrentDictionary<Guid, CancellationTokenSource>();
            _hotkeyService.HotkeyPressed += OnHotkeyPressed;
            _executor.JobErrorOccurred += OnJobErrorOccurred;
            _executor.JobStepErrorOccurred += OnJobStepErrorOccurred;
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
            if (job != null)
            {
                StartJob(job);
            }
            else
            {
                _logger.LogWarning("No job found for action: Name='{ActionName}', ID='{JobId}'", jobRef.Name, jobRef.JobId);
            }
        }

        private void CancelJobByAction(JobReference jobRef)
        {
            var job = FindJobByAction(jobRef);
            if (job != null)
            {
                CancelJobById(job.Id);
            }
            else
            {
                _logger.LogWarning("No job found for action: Name='{ActionName}', ID='{JobId}'", jobRef.Name, jobRef.JobId);
            }
        }

        private void ToggleJobByAction(JobReference jobRef)
        {
            var job = FindJobByAction(jobRef);
            if (job != null)
            {
                if (_jobTokens.ContainsKey(job.Id))
                    CancelJobById(job.Id);
                else
                    StartJob(job);
            }
            else
            {
                _logger.LogWarning("No job found for action: Name='{ActionName}', ID='{JobId}'", jobRef.Name, jobRef.JobId);
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

        private void StartJob(Job job)
        {
            if (_jobTokens.ContainsKey(job.Id))
            {
                _logger.LogWarning("Job '{Name}' läuft bereits.", job.Name);
                return;
            }

            var cts = new CancellationTokenSource();
            if (!_jobTokens.TryAdd(job.Id, cts))
                return;

            RunningJobsChanged?.Invoke();

            var jobId = job.Id;
            var jobName = job.Name;
            _ = Task.Run(async () =>
            {
                try
                {
                    await _executor.ExecuteJob(jobId, cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Job '{Name}' abgebrochen.", jobName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler bei Job '{Name}'", jobName);
                }
                finally
                {
                    _jobTokens.TryRemove(jobId, out _);
                    cts.Dispose();
                    RunningJobsChanged?.Invoke();
                }
            });
        }

        private void CancelJobById(Guid id)
        {
            if (_jobTokens.TryRemove(id, out var cts))
            {
                _logger.LogInformation("Job (ID: {JobId}) Abbruch angefordert.", id);
                cts.Cancel();
                cts.Dispose();
                RunningJobsChanged?.Invoke();
            }
            else
            {
                _logger.LogWarning("Kein laufender Job mit ID '{JobId}' gefunden.", id);
            }
        }

        private void OnJobErrorOccurred(object? sender, JobErrorEventArgs e)
        {
            // Event an UI weiterleiten
            JobErrorOccurred?.Invoke(this, e);
        }

        private void OnJobStepErrorOccurred(object? sender, JobStepErrorEventArgs e)
        {
            // Event an UI weiterleiten
            JobStepErrorOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Fordert den Abbruch eines laufenden Jobs an.
        /// </summary>
        public void CancelJob(string name)
        {
            var job = _executor.AllJobs.Values.FirstOrDefault(j => string.Equals(j.Name, name, StringComparison.OrdinalIgnoreCase));
            if (job == null)
            {
                _logger.LogWarning("Kein Job mit Namen '{Name}' gefunden.", name);
                return;
            }
            CancelJobById(job.Id);
        }

        /// <summary>
        /// Fordert den Abbruch eines laufenden Jobs per ID an.
        /// </summary>
        public void CancelJob(Guid id) => CancelJobById(id);

        /// <summary>
        /// Startet einen Job per ID.
        /// </summary>
        public void StartJob(Guid id)
        {
            var job = _executor.AllJobs.Values.FirstOrDefault(j => j.Id == id);
            if (job == null)
            {
                _logger.LogWarning("Job mit ID '{JobId}' nicht gefunden.", id);
                return;
            }
            StartJob(job);
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
            StartMakro(makro);
        }

        private void StartMakro(Makro makro)
        {
            if (_makroTokens.ContainsKey(makro.Id))
            {
                _logger.LogWarning("Makro '{Name}' läuft bereits.", makro.Name);
                return;
            }

            var cts = new CancellationTokenSource();
            if (!_makroTokens.TryAdd(makro.Id, cts))
                return;

            RunningMakrosChanged?.Invoke();

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
                    RunningMakrosChanged?.Invoke();
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
                RunningMakrosChanged?.Invoke();
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
