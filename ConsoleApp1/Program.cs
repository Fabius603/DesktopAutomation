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
        string targetApplication = "discord";
        ProcessDuplicator processDuplicator = new ProcessDuplicator(targetApplication);

        VideoCreator videoCreator = new VideoCreator();
        videoCreator.CleanUp();
        int i = 0;
        while (true)
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

            if (Console.KeyAvailable)
            {
                Console.WriteLine("Video wird gespeichert...");
                Console.ReadKey(true);
                break;
            }
        }
        videoCreator.SaveAsMp4Async("C:\\Users\\fjsch\\Pictures\\trash\\output.mp4", 30).Wait();
        videoCreator.CleanUp();
    }
}