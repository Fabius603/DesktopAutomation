using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageCapture.DesktopDuplication;
using OpenCvSharp;

namespace ImageCapture.ProcessDuplication
{
    public class ProcessDuplicatorResult : IDisposable
    {
        public Bitmap ProcessImage { get; set; }
        public DesktopFrame DesktopFrame { get; set; }
        public int Fps { get; set; }
        public Rectangle WindowRect { get; set; }
        public Rectangle ClampedWindowRect { get; set; }
        public Rectangle GlobalWindowRect { get; set; }
        public int DesktopIdx { get; set; }
        public int AdapterIdx { get; set; }
        public bool ProcessFound { get; set; } = true;

        public ProcessDuplicatorResult() { }
        public ProcessDuplicatorResult(bool processFound)
        {
            ProcessFound = processFound;
        }

        public void Dispose()
        {
            ProcessImage?.Dispose();
            ProcessImage = null;
            DesktopFrame?.Dispose();
            DesktopFrame = null;
            WindowRect = Rectangle.Empty;
            ClampedWindowRect = Rectangle.Empty;
            GlobalWindowRect = Rectangle.Empty;
        }
    }
}
