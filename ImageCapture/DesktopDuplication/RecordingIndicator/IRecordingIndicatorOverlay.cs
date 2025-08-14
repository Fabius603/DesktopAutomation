using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageCapture.DesktopDuplication.RecordingIndicator
{
    public interface IRecordingIndicatorOverlay : IDisposable
    {
        bool IsRunning { get; }
        void Start(RecordingIndicatorOptions? options = null);
        void Stop();
    }
}
