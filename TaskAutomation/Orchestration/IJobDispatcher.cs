using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Jobs;

namespace TaskAutomation.Orchestration
{
    public interface IJobDispatcher : IDisposable
    {
        /// <summary>
        /// Wird ausgelöst, wenn bei der Ausführung eines Jobs ein Fehler auftritt.
        /// </summary>
        event EventHandler<JobErrorEventArgs>? JobErrorOccurred;

        /// <summary>
        /// Wird ausgelöst, wenn bei der Ausführung eines Job-Steps ein Fehler auftritt.
        /// </summary>
        event EventHandler<JobStepErrorEventArgs>? JobStepErrorOccurred;

        /// <summary>
        /// Wird ausgelöst, wenn sich die Liste der laufenden Jobs ändert.
        /// </summary>
        event Action? RunningJobsChanged;

        /// <summary>
        /// Wird ausgelöst, wenn sich die Liste der laufenden Makros ändert.
        /// </summary>
        event Action? RunningMakrosChanged;

        /// <summary>
        /// IDs der aktuell laufenden Jobs.
        /// </summary>
        IReadOnlyCollection<Guid> RunningJobIds { get; }

        /// <summary>
        /// IDs der aktuell laufenden Makros.
        /// </summary>
        IReadOnlyCollection<Guid> RunningMakroIds { get; }

        /// <summary>Bricht einen laufenden Job ab (falls vorhanden).</summary>
        void CancelJob(string name);

        /// <summary>Bricht einen laufenden Job per ID ab (falls vorhanden).</summary>
        void CancelJob(Guid id);

        /// <summary>Startet einen Job per ID.</summary>
        void StartJob(Guid id);

        /// <summary>Startet ein Makro per ID.</summary>
        void StartMakro(Guid id);

        /// <summary>Bricht ein laufendes Makro per ID ab.</summary>
        void CancelMakro(Guid id);
    }
}
