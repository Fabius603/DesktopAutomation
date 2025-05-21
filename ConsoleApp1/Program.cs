using ImageCapture.DesktopDuplication;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using SharpDX;
using SharpDX.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using ImageCapture.ProcessDuplication;
using ImageCapture.Video;

class Program
{
    static async Task Main(string[] args)
    {
        ProcessDuplicatorSettings settings = new ProcessDuplicatorSettings
        {
            TargetApplication = "brave",
            OnlyActiveWindow = false
        };
        ProcessDuplicator processDuplicator = new ProcessDuplicator(settings);

        VideoCreator videoCreator = new VideoCreator();
        videoCreator.CleanUp();
        int i = 0;
        while (i < 200)
        {
            i++;
            var result = processDuplicator.CaptureProcess();
            var mat = result.ProcessImage.ToMat();
            videoCreator.AddFrame(result.ProcessImage);
            Cv2.Resize(mat, mat, new OpenCvSharp.Size(), 0.5, 0.5);
            Cv2.ImShow("test", mat);
            Cv2.WaitKey(1);
            mat.Dispose();
            result.Dispose();
        }
        videoCreator.SaveAsMp4Async("C:\\Users\\fjsch\\Pictures\\trash\\output.mp4", 30).Wait();
        videoCreator.CleanUp();
    }
}