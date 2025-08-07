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

            if (vcStep.ShowRawImage)
            {
                executor.VideoRecorder.AddFrame(executor.CurrentImage?.Clone() as Bitmap);
            }
            else if (vcStep.ShowProcessedImage)
            {
                if (executor.CurrentImageWithResult != null && !executor.CurrentImageWithResult.IsDisposed)
                {
                    executor.VideoRecorder.AddFrame(executor.CurrentImageWithResult.ToBitmap().Clone() as Bitmap);
                }
                else
                {
                    executor.VideoRecorder.AddFrame(executor.CurrentImage?.Clone() as Bitmap);
                }
            }

            return true;
        }
    }
}
