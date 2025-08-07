using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    public class MakroExecutionStepHandler : IJobStepHandler
    {
        public async Task<bool> ExecuteAsync(object step, Job jobContext, JobExecutor executor, CancellationToken ct)
        {
            var miStep = step as MakroExecutionStep;
            if (miStep == null)
            {
                return false;
            } 

            await executor.MakroExecutor.ExecuteMakro(
                executor.AllMakros[miStep.MakroName],
                executor.DxgiResources,
                ct);

            return true;
        }
    }
}
      