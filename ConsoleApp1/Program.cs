using ImageCapture;
using OpenCvSharp;
using SharpDX;
using SharpDX.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

class Program
{
    static void Main(string[] args)
    {
        ScreenCaptureSettings settings = new ScreenCaptureSettings
        {
            TargetApplication = "brave" // Set your target application name here
        };

        int i = 0;
        using (ScreenCapture screenCapture = new ScreenCapture(settings))
        {
            while (true)
            {
                i++;
                ScreenCaptureResult result = screenCapture.CaptureWindow();

                var resizedFrame = new Mat();

                //Cv2.Resize(result.Image, resizedFrame, new OpenCvSharp.Size(), 0.5, 0.5);
                //Cv2.ImShow("Captured Image", resizedFrame);
                //Console.Clear();
                //Console.WriteLine("FPS: " + result.Fps);
                resizedFrame.Dispose();
                result.Dispose();
                Cv2.WaitKey(1);
            }
        }
    }
}