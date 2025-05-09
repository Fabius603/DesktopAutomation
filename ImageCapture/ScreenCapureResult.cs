using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;

namespace ImageCapture
{
    public class ScreenCaptureResult : IDisposable
    {
        public Mat Image { get; set; }
        public int Fps { get; set; }
        public Rect WindowRect { get; set; }
        public Rect DesktopWindowRect { get; set; }
        public Rect LokalDesktopWindowRect { get; set; }
        public int DesktopIdx { get; set; }
        public int AdapterIdx { get; set; }
        public bool ProcessFound { get; set; } = true;

        public ScreenCaptureResult() { }
        public ScreenCaptureResult(bool processFound) 
        {
            ProcessFound = processFound;
        }

        public void Dispose()
        {
            Image?.Dispose();
            Image = null;
        }
    }
}
