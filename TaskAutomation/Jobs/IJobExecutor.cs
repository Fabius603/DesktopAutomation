using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TaskAutomation.Jobs
{
    public interface IJobExecutor
    {
        /// <summary>
        /// Führt den benannten Job aus und gibt ein Task zurück,
        /// das mit dem CancellationToken abgebrochen werden kann.
        /// </summary>
        Task ExecuteJob(string actionName, CancellationToken ct);
    }
}
