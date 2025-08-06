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
                Console.WriteLine("FEHLER: Step ist kein MakroExecutionStep.");
                return false;
            }

            if (executor.CurrentAdapter == null)
            {
                Console.WriteLine("    FEHLER: Kein aktueller Adapter gesetzt (CurrentAdapter ist null). Step wird übersprungen.");
                return true;
            }
            if (executor.CurrentDesktop == null)
            {
                Console.WriteLine("    FEHLER: Kein aktueller Desktop gesetzt (CurrentDesktop ist null). Step wird übersprungen.");
                return true;
            }

            executor.MakroExecutor.ExecuteMakro(
                executor.AllMakros[miStep.MakroName],
                executor.CurrentAdapter,
                executor.CurrentDesktop,
                executor.DxgiResources);

            Console.WriteLine($"    Makro '{miStep.MakroName}' wurde ausgeführt auf Adapter {executor.CurrentAdapter} und Desktop {executor.CurrentDesktop}.");
            return true;
        }
    }
}
