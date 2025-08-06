using ImageCapture.ProcessDuplication;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    public class ProcessDuplicationStepHandler : IJobStepHandler
    {
        public bool Execute(object step, Job jobContext, JobExecutor executor)
        {
            var pdStep = step as ProcessDuplicationStep;
            if (pdStep == null)
            {
                return false;
            }

            executor.ProcessDuplicationResult?.Dispose(); // Vorherigen Frame freigeben
            executor.CurrentImage?.Dispose(); // Vorheriges Desktop-Bild freigeben

            if (executor.ProcessDuplicator == null)
            {
                executor.ProcessDuplicator = new ProcessDuplicator(pdStep.ProcessName);
            }

            executor.ProcessDuplicationResult = executor.ProcessDuplicator.CaptureProcess();
            if (!executor.ProcessDuplicationResult.ProcessFound)
            {
                return true; // Oder false, wenn der Job abbrechen soll
            }

            executor.CurrentDesktop = executor.ProcessDuplicationResult.DesktopIdx;
            executor.CurrentAdapter = executor.ProcessDuplicationResult.AdapterIdx;
            executor.CurrentImage = executor.ProcessDuplicationResult.ProcessImage.Clone() as Bitmap;
            executor.CurrentOffset = executor.ProcessDuplicationResult.WindowOffsetOnDesktop;
            return true;
        }
    }
}
