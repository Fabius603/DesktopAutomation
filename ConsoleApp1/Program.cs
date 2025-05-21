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
        //DesktopDuplicator desktopDuplicator = new DesktopDuplicator(1);
        //while(true)
        //{
        //    var result = desktopDuplicator.GetLatestFrame();
        //    Cv2.ImShow("test", result.DesktopImage.ToMat());
        //    Cv2.WaitKey(1);
        //    result.Dispose();
        //    GC.Collect();
        //}

        ProcessDuplicatorSettings settings = new ProcessDuplicatorSettings
        {
            TargetApplication = "brave",
            OnlyActiveWindow = false
        };
        int i = 0;
        ProcessDuplicator processDuplicator = new ProcessDuplicator(settings);
        while (true)
        {
            var result = processDuplicator.CaptureProcess();
            var mat = result.ProcessImage.ToMat();
            Cv2.ImShow("test", mat);
            Cv2.WaitKey(1);
            mat.Dispose();
            result.Dispose();
        }
    }
}