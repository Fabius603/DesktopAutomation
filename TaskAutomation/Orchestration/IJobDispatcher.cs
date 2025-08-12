using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskAutomation.Orchestration
{
    public interface IJobDispatcher : IDisposable
    {
        /// <summary>Bricht einen laufenden Job ab (falls vorhanden).</summary>
        void CancelJob(string name);
    }
}
