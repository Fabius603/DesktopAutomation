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
    public sealed class JobDispatcher : IDisposable
    {
        private readonly ILogger<JobDispatcher> _logger;
        private readonly IJobExecutor _executor;
        private readonly GlobalHotkeyService _hotkeyService;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _jobTokens;

        /// <summary>
        /// Erstellt einen neuen Dispatcher mit dem gegebenen JobExecutor.
        /// </summary>
        /// <param name="hotkeyService">Instanz des GlobalHotkeyService</param>
        /// <param name="executor">Executor, der Jobs per Name ausführt</param>
        /// <param name="logger">Logger für JobDispatcher</param>
        public JobDispatcher(
            GlobalHotkeyService hotkeyService,
            IJobExecutor executor)
        {
            _hotkeyService = hotkeyService ?? throw new ArgumentNullException(nameof(hotkeyService));
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _logger = Log.Create<JobDispatcher>();

            _jobTokens = new ConcurrentDictionary<string, CancellationTokenSource>(StringComparer.OrdinalIgnoreCase);
            _hotkeyService.HotkeyPressed += OnHotkeyPressed;
            _logger.LogInformation("JobDispatcher initialisiert.");
        }

        private void OnHotkeyPressed(object? sender, HotkeyPressedEventArgs e)
        {
            if (e?.Action is null)
                return;

            var actionDef = e.Action;
            var name = actionDef.Name;
            var cmd = actionDef.Command;

            switch (cmd)
            {
                case ActionCommand.Start:
                    StartJob(name);
                    break;
                case ActionCommand.Stop:
                    CancelJob(name);
                    break;
                case ActionCommand.Toggle:
                    if (_jobTokens.ContainsKey(name))
                        CancelJob(name);
                    else
                        StartJob(name);
                    break;
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
        }
    }
}
