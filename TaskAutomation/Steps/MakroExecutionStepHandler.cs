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
        public bool Execute(object step, Job jobContext, JobExecutor executor)
        {
            var miStep = step as MakroExecutionStep;
            if (miStep == null)
            {
                return false;
            }

            if (executor.CurrentAdapter == null)
            {
                return true;
            }
            if (executor.CurrentDesktop == null)
            {
                return true;
            }

            executor.MakroExecutor.ExecuteMakro(
                executor.AllMakros[miStep.MakroName],
                executor.CurrentAdapter,
                executor.CurrentDesktop,
                executor.DxgiResources);

            return true;
        }
    }
}
