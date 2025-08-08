using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskAutomation.Jobs;
using OpenCvSharp.Extensions;

namespace TaskAutomation.Steps
{
    public class VideoCreationStepHandler : IJobStepHandler
    {
        public async Task<bool> ExecuteAsync(object step, Job jobContext, JobExecutor executor, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            var vcStep = step as VideoCreationStep;
            if (vcStep == null)
            {
                return false;
            }

            if (executor.VideoRecorder == null)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(vcStep.SavePath))
            {
                executor.VideoRecorder.OutputDirectory = vcStep.SavePath;
            }
            if (!string.IsNullOrEmpty(vcStep.FileName))
            {
                executor.VideoRecorder.FileName = vcStep.FileName;
            }

            if (executor.CurrentImage == null || executor.CurrentImage.Width == 0 && executor.CurrentImage.Height == 0)
            {
                return false;
            }

            if (vcStep.ShowRawImage)
            {
                ct.ThrowIfCancellationRequested();
                executor.VideoRecorder.AddFrame(executor.CurrentImage?.Clone() as Bitmap);
            }
            else if (vcStep.ShowProcessedImage)
            {
                if (executor.CurrentImageWithResult != null && !executor.CurrentImageWithResult.IsDisposed)
                {
                    ct.ThrowIfCancellationRequested();
                    executor.VideoRecorder.AddFrame(executor.CurrentImageWithResult.ToBitmap().Clone() as Bitmap);
                }
                else
                {
                    ct.ThrowIfCancellationRequested();
                    executor.VideoRecorder.AddFrame(executor.CurrentImage?.Clone() as Bitmap);
                }
            }

            return true;
        }
    }
}
