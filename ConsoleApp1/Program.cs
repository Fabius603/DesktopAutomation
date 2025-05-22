using ImageCapture.DesktopDuplication;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using SharpDX;
using SharpDX.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using ImageCapture.ProcessDuplication;
using ImageCapture.Video;
using System.Diagnostics;
using System.Drawing;

class Program
{
    static async Task Main(string[] args)
    {
        string targetApplication = "brave";
        ProcessDuplicator processDuplicator = new ProcessDuplicator(targetApplication);

        //Recorder
        var recorder = new StreamVideoRecorder(1920, 1080, 60, "C:\\Users\\fjsch\\Pictures\\trash\\output.mp4");
        await recorder.StartAsync();

        //VideoImageCreator videoCreator = new VideoImageCreator();
        //videoCreator.CleanUp();
        Stopwatch stopwatch = new Stopwatch();
        while (true)
        {
            stopwatch.Restart();
            using (var result = processDuplicator.CaptureProcess())
            {

                recorder.AddFrame(result.ProcessImage);
                //videoCreator.AddFrame(result.ProcessImage);
                //ShowImage(result.ProcessImage);
            }
            stopwatch.Stop();
            Console.WriteLine($"Frame took {stopwatch.ElapsedMilliseconds} ms");

            if (Console.KeyAvailable)
            {
                Console.WriteLine("Video wird gespeichert...");
                Console.ReadKey(true);
                break;
            }
        }
        recorder.StopAndSave();

        processDuplicator.Dispose();
        recorder.Dispose();
        Cv2.DestroyAllWindows();

        //videoCreator.SaveAsMp4Async("C:\\Users\\fjsch\\Pictures\\trash\\output.mp4", 30).Wait();
        //videoCreator.CleanUp();
    }

    public static void ShowImage(Bitmap bitmap)
    {
        var mat = bitmap.ToMat();
        Cv2.Resize(mat, mat, new OpenCvSharp.Size(), 0.5, 0.5);
        Cv2.ImShow("test", mat);
        Cv2.WaitKey(1);
        mat.Dispose();
    }
}