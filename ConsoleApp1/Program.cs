using ImageCapture.DesktopDuplication;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using SharpDX;
using SharpDX.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using ImageCapture.ProcessDuplication;

class Program
{
    static void Main(string[] args)
    {
        ProcessDuplicatorSettings settings = new ProcessDuplicatorSettings
        {
            TargetApplication = "brave",
            OnlyActiveWindow = false
        };

        ProcessDuplicator processDuplicator = new ProcessDuplicator(settings);
        while(true)
        {
            var result = processDuplicator.CaptureProcess();
            //Cv2.ImShow("test", result.ProcessImage.ToMat());
            //Cv2.WaitKey(1);
            result.Dispose();
            GC.Collect();
        }
    }
}