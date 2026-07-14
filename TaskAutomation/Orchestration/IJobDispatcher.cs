using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using TaskAutomation.Logging;

namespace TaskAutomation.Orchestration
{
    /// <summary>
    /// Repräsentiert eine laufende Job-Instanz (eine von möglicherweise mehreren gleichzeitigen
    /// Ausführungen desselben Jobs).
    /// </summary>
    public record RunningJobInstance(Guid InstanceId, Guid JobId, string JobName);

    public interface IJobDispatcher : IDisposable
    {
        /// <summary>Wird ausgelöst, wenn bei der Ausführung eines Jobs ein Fehler auftritt.</summary>
        event EventHandler<JobErrorEventArgs>? JobErrorOccurred;

        /// <summary>Wird ausgelöst, wenn bei der Ausführung eines Job-Steps ein Fehler auftritt.</summary>
        event EventHandler<JobStepErrorEventArgs>? JobStepErrorOccurred;

        /// <summary>Wird ausgelöst, wenn sich die Liste der laufenden Jobs ändert.</summary>
        event Action? RunningJobsChanged;

        /// <summary>Wird ausgelöst, wenn sich die Liste der laufenden Makros ändert.</summary>
        event Action? RunningMakrosChanged;

        /// <summary>
        /// Alle aktuell laufenden Job-Instanzen (ein Job kann mehrfach gleichzeitig laufen).
        /// </summary>
        IReadOnlyCollection<RunningJobInstance> RunningJobInstances { get; }

        /// <summary>
        /// Distinct-Job-IDs aller aktuell laufenden Instanzen (nur für "läuft irgendetwas"-Prüfungen).
        /// </summary>
        IReadOnlyCollection<Guid> RunningJobIds { get; }

        /// <summary>IDs der aktuell laufenden Makros.</summary>
        IReadOnlyCollection<Guid> RunningMakroIds { get; }

        /// <summary>
        /// Startet eine neue Instanz des Jobs mit der angegebenen ID.
        /// Gibt die eindeutige Instanz-ID zurück, mit der diese Ausführung gestoppt werden kann.
        /// </summary>
        Guid StartJob(Guid id, JobStartContext? startContext = null);

        /// <summary>
        /// Startet eine neue Instanz und wartet auf deren Abschluss (registriert sie in RunningJobInstances).
        /// CancellationToken wird mit dem internen CTS verknüpft, sodass CancelJob ebenfalls greift.
        /// </summary>
        Task StartJobAsync(Guid id, CancellationToken ct, JobStartContext? startContext = null);

        /// <summary>
        /// Bricht eine bestimmte Job-Instanz per Instanz-ID ab.
        /// </summary>
        void CancelJob(Guid instanceId);

        /// <summary>
        /// Bricht alle laufenden Instanzen eines Jobs per Name ab.
        /// </summary>
        void CancelJob(string name);

        /// <summary>
        /// Bricht alle laufenden Instanzen eines Jobs per Job-Definitions-ID ab (asynchron, non-blocking).
        /// </summary>
        void CancelJobsByDefinition(Guid jobDefinitionId);

        /// <summary>
        /// Bricht alle laufenden Job-Instanzen ab (asynchron, non-blocking).
        /// </summary>
        void CancelAllJobs();

        /// <summary>Startet ein Makro per ID.</summary>
        void StartMakro(Guid id);

        /// <summary>Bricht ein laufendes Makro per ID ab.</summary>
        void CancelMakro(Guid id);
    }
}
