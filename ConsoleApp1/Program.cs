using ImageCapture.DesktopDuplication;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using SharpDX;
using SharpDX.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

class Program
{
    static void Main(string[] args)
    {
        DesktopDuplicator desktopDuplicator0 = new DesktopDuplicator(0);
        DesktopDuplicator desktopDuplicator1 = new DesktopDuplicator(1);
        DesktopFrame frame = null;

        while (true)
        {
            frame = desktopDuplicator0.GetLatestFrame();
            frame = desktopDuplicator1.GetLatestFrame();
            //Cv2.ImShow("test", frame.DesktopImage.ToMat());
            //Cv2.WaitKey(1);
            frame.Dispose();
            GC.Collect();
        }
    }
}