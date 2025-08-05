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
        bool Execute(object step, Job jobContext, JobExecutor executor);
    }
}
