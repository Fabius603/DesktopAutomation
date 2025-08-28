using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    public interface IJobStepHandler
    {
        Task<bool> ExecuteAsync(object step, Job job, IJobExecutor ctx, CancellationToken ct);
    }
}
