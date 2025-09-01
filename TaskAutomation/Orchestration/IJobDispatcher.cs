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

        /// <summary>Bricht einen laufenden Job ab (falls vorhanden).</summary>
        void CancelJob(string name);
    }
}
