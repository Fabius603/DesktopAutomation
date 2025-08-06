using ImageCapture.DesktopDuplication;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps
{
    public class DesktopDuplicationStepHandler : IJobStepHandler
    {
        public bool Execute(object step, Job jobContext, JobExecutor executor)
        {
            var ddStep = step as DesktopDuplicationStep;
            if (ddStep == null)
            {
                return false;
            }

            executor.CurrentImage?.Dispose();
            executor.CurrentDesktopFrame?.Dispose();

            if (executor.DesktopDuplicator == null)
            {
                executor.DesktopDuplicator = new DesktopDuplicator(ddStep.GraphicsCardAdapter, ddStep.OutputDevice);
            }
            try
            {
                executor.CurrentDesktopFrame = executor.DesktopDuplicator.GetLatestFrame();
                executor.CurrentDesktop = ddStep.OutputDevice;
                executor.CurrentAdapter = ddStep.GraphicsCardAdapter;
                executor.CurrentImage = executor.CurrentDesktopFrame?.DesktopImage?.Clone() as Bitmap;
            }
            catch
            {
                return true;
            }

            return true;
        }
    }
}
