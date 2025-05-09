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

        ScreenCapture screenCapture = new ScreenCapture(settings);
        while (true)
        {
            // CaptureWindow() liefert ein Mat, das disposed werden muss
            using (var frame = screenCapture.CaptureWindow())
            {
                // Neues Mat für das Resizing
                using (var resizedFrame = new Mat())
                {
                    Cv2.Resize(frame, resizedFrame, new OpenCvSharp.Size(), 0.5, 0.5);
                    Cv2.ImShow("Captured Image", resizedFrame);
                }
            }

            // 10 ms warten, damit OpenCV-Fenster aktualisiert wird
            Cv2.WaitKey(10);
        }
    }
}