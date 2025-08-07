using OpenCvSharp;
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
    public class ShowImageStepHandler : IJobStepHandler
    {
        public async Task<bool> ExecuteAsync(object step, Job jobContext, JobExecutor executor, CancellationToken ct)
        {
            var siStep = step as ShowImageStep;
            if (siStep == null)
            {
                return false;
            }

            if (executor.CurrentImage == null)
            {
                return true;
            }

            void ShowBitmapImage(Bitmap bitmap, string name)
            {
                var mat = bitmap.ToMat();
                ShowMatImage(mat, name);
            }

            void ShowMatImage(Mat mat, string name)
            {
                Cv2.Resize(mat, mat, new OpenCvSharp.Size(), 0.5, 0.5);
                Cv2.ImShow(name, mat);
                Cv2.WaitKey(1);
            }

            if (siStep.ShowRawImage)
            {
                string windowName = $"{siStep.WindowName} - Raw Image";
                ShowBitmapImage(executor.CurrentImage, windowName);
            }
            if (siStep.ShowProcessedImage)
            {
                string windowName = $"{siStep.WindowName} - Processed Image";
                if (executor.CurrentImageWithResult != null &&
                    !executor.CurrentImageWithResult.IsDisposed &&
                    executor.CurrentImageWithResult.Height >= 10 &&
                    executor.CurrentImageWithResult.Width >= 10)
                {
                    ShowMatImage(executor.CurrentImageWithResult, windowName);
                }
                else
                {
                    if (executor.CurrentImage != null)
                    {
                        ShowBitmapImage(executor.CurrentImage, windowName);
                    }
                }
            }
            return true;
        }
    }
}
