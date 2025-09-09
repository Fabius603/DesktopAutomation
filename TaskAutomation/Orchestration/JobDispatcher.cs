using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Hotkeys;
using TaskAutomation.Jobs;
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
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _jobTokens;

        /// <summary>
        /// Wird ausgelöst, wenn bei der Ausführung eines Jobs ein Fehler auftritt.
        /// </summary>
        public event EventHandler<JobErrorEventArgs>? JobErrorOccurred;

        /// <summary>
        /// Wird ausgelöst, wenn bei der Ausführung eines Job-Steps ein Fehler auftritt.
        /// </summary>
        public event EventHandler<JobStepErrorEventArgs>? JobStepErrorOccurred;

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

            _jobTokens = new ConcurrentDictionary<string, CancellationTokenSource>(StringComparer.OrdinalIgnoreCase);
            _hotkeyService.HotkeyPressed += OnHotkeyPressed;
            _executor.JobErrorOccurred += OnJobErrorOccurred;
            _executor.JobStepErrorOccurred += OnJobStepErrorOccurred;
            _logger.LogInformation("JobDispatcher initialisiert.");
        }

        private void OnHotkeyPressed(object? sender, HotkeyPressedEventArgs e)
        {
            if (e?.Action is null)
                return;

            var actionDef = e.Action;
            var cmd = actionDef.Command;

            switch (cmd)
            {
                case ActionCommand.Start:
                    StartJobByAction(actionDef);
                    break;
                case ActionCommand.Stop:
                    CancelJobByAction(actionDef);
                    break;
                case ActionCommand.Toggle:
                    ToggleJobByAction(actionDef);
                    break;
            }
        }

        private void StartJobByAction(ActionDefinition actionDef)
        {
            var jobName = FindJobNameByAction(actionDef);
            if (jobName != null)
            {
                StartJob(jobName);
            }
            else
            {
                _logger.LogWarning("No job found for action: Name='{ActionName}', ID='{JobId}'", actionDef.Name, actionDef.JobId);
            }
        }

        private void CancelJobByAction(ActionDefinition actionDef)
        {
            var jobName = FindJobNameByAction(actionDef);
            if (jobName != null)
            {
                CancelJob(jobName);
            }
            else
            {
                _logger.LogWarning("No job found for action: Name='{ActionName}', ID='{JobId}'", actionDef.Name, actionDef.JobId);
            }
        }

        private void ToggleJobByAction(ActionDefinition actionDef)
        {
            var jobName = FindJobNameByAction(actionDef);
            if (jobName != null)
            {
                if (_jobTokens.ContainsKey(jobName))
                    CancelJob(jobName);
                else
                    StartJob(jobName);
            }
            else
            {
                _logger.LogWarning("No job found for action: Name='{ActionName}', ID='{JobId}'", actionDef.Name, actionDef.JobId);
            }
        }

        private string? FindJobNameByAction(ActionDefinition actionDef)
        {
            // Try to find by ID first, fallback to name for backward compatibility
            if (actionDef.JobId.HasValue)
            {
                var job = _executor.AllJobs.Values.FirstOrDefault(j => j.Id == actionDef.JobId.Value);
                if (job != null)
                {
                    return job.Name;
                }
                _logger.LogWarning("Job with ID '{JobId}' not found", actionDef.JobId);
                return null;
            }
            else if (!string.IsNullOrWhiteSpace(actionDef.Name))
            {
                if (_executor.AllJobs.ContainsKey(actionDef.Name))
                {
                    return actionDef.Name;
                }
                _logger.LogWarning("Job with name '{JobName}' not found", actionDef.Name);
                return null;
            }
            else
            {
                _logger.LogWarning("Action has neither JobId nor valid Name");
                return null;
            }
        }

        private void StartJob(string name)
        {
            if (_jobTokens.ContainsKey(name))
            {
                _logger.LogWarning("Job '{Name}' läuft bereits.", name);
                return;
            }

            var cts = new CancellationTokenSource();
            if (!_jobTokens.TryAdd(name, cts))
                return;

            _ = Task.Run(async () =>
            {
                try
                {
                    await _executor.ExecuteJob(name, cts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Job '{Name}' abgebrochen.", name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler bei Job '{Name}'", name);
                }
                finally
                {
                    _jobTokens.TryRemove(name, out _);
                    cts.Dispose();
                }
            });
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
            if (_jobTokens.TryRemove(name, out var cts))
            {
                _logger.LogInformation("Job '{Name}' Abbruch angefordert.", name);
                cts.Cancel();
                cts.Dispose();
            }
            else
            {
                _logger.LogWarning("Kein laufender Job '{Name}' gefunden.", name);
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
