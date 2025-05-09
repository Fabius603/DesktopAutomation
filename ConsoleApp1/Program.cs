using ImageCapture;
using OpenCvSharp;

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
                using (ScreenCaptureResult result = screenCapture.CaptureWindow())
                {
                    using (var resizedFrame = new Mat())
                    {
                        //Cv2.Resize(result.Image, resizedFrame, new OpenCvSharp.Size(), 0.5, 0.5);
                        //Cv2.ImShow("Captured Image", resizedFrame);
                        //resizedFrame.Dispose();
                    }
                    //Console.Clear();
                    //Console.WriteLine("FPS: " + result.Fps);
                }

                Cv2.WaitKey(1);
            }
        }
    }
}