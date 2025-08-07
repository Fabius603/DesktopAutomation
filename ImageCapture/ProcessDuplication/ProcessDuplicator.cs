using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageCapture.DesktopDuplication;
using OpenCvSharp;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using ImageHelperMethods;

namespace ImageCapture.ProcessDuplication
{
    public class ProcessDuplicator : IDisposable
    {
        private string processName { get; set; }
        private IntPtr targetProcess { get; set; } = IntPtr.Zero;
        private bool OnlyActiveWindow { get; set; } = false;
        private Bitmap _latestSuccessfulImage { get; set; } = null; 
        private DesktopFrame _latestSuccessfulFrame { get; set; } = null;

        private int currentAdapterIndex { get; set; } = 0;
        private int currentOutputIndex { get; set; } = 0;

        private Rectangle winRect { get; set; } = Rectangle.Empty;
        private Rectangle clampedRect { get; set; } = Rectangle.Empty;
        private Rectangle globalRect { get; set; } = Rectangle.Empty;

        private DxgiResources _dxgiResources { get; } = DxgiResources.Instance;

        private OpenCvSharp.Point offsetOnDesktop { get; set; } = new OpenCvSharp.Point(0, 0);

        private Dictionary<string, DesktopDuplicator> desktopDuplicators = new Dictionary<string, DesktopDuplicator>();
        private int _aquireFrameTimeout = 0;
        private int aquireFrameTimeout
        {
            get => _aquireFrameTimeout;
            set
            {
                _aquireFrameTimeout = value;
                foreach (var duplicator in desktopDuplicators)
                {
                    duplicator.Value.SetFrameTimeout(aquireFrameTimeout);
                }
            }
        }

        public Bitmap latestSuccesfullImage
        {
            get => _latestSuccessfulImage;
            private set
            {
                if (_latestSuccessfulImage != value)
                {
                    _latestSuccessfulImage?.Dispose();
                    _latestSuccessfulImage = value;
                }
            }
        }

        public DesktopFrame latestSuccesfullFrame
        {
            get => _latestSuccessfulFrame;
            private set
            {
                if (_latestSuccessfulFrame != value)
                {
                    _latestSuccessfulFrame?.Dispose();
                    _latestSuccessfulFrame = value;
                }
            }
        }

        public ProcessDuplicator(string targetApplication)
        {
            this.processName = targetApplication;
            SetTargetProcess(targetApplication);
            
            var adapterAndOutputs = GetAllAdapterAndOutputIndices();
            InitializeDesktopDuplicators(adapterAndOutputs);
        }

        public ProcessDuplicatorResult CaptureProcess()
        {
            DesktopFrame currentFrame = null;
            Bitmap croppedImage = null;
            try
            {
                if (!User32.GetWindowRect(targetProcess, out var r))
                    return CreateResult(false);

                globalRect = ScreenHelper.GetWindowGlobalRectangle(targetProcess);
                winRect = ScreenHelper.GetWindowRectangleOnCurrentMonitor(targetProcess);

                Rectangle currentMonitorBounds = ScreenHelper.GetMonitorBoundsForWindow(targetProcess);

                if (currentMonitorBounds.IsEmpty)
                {
                    return CreateResult(false);
                }
                if (winRect.IsEmpty)
                {
                    return CreateResult(false);
                }
                clampedRect = ScreenHelper.ClampRectToMonitor(globalRect, currentMonitorBounds);

                if(clampedRect.Width == 0 || clampedRect.Height == 0)
                {
                    return CreateResult(false);
                }

                if (OnlyActiveWindow &&
                    User32.GetActiveApplicationName() != processName)
                {
                    return CreateResult(latestSuccesfullImage, winRect, clampedRect, globalRect, latestSuccesfullFrame, offsetOnDesktop);
                }

                (int aIdx, int oIdx) = ScreenHelper.GetAdapterAndOutputForWindowHandle(targetProcess, _dxgiResources);
                if (aIdx != currentAdapterIndex || oIdx != currentOutputIndex)
                {
                    currentAdapterIndex = aIdx;
                    currentOutputIndex = oIdx;
                }

                if(currentOutputIndex == -1)
                {
                    return CreateResult(false);
                }

                currentFrame = desktopDuplicators[$"{currentAdapterIndex},{currentOutputIndex}"].GetLatestFrame();

                if (currentFrame == null)
                {
                    return CreateResult(false);
                }

                if (currentFrame.DesktopImage == null)
                {
                    currentFrame.Dispose();
                    return CreateResult(false);
                }

                try
                {
                    croppedImage = currentFrame.DesktopImage.Clone(clampedRect, currentFrame.DesktopImage.PixelFormat);
                }
                catch (Exception ex)
                {
                    return CreateResult(false);
                }

                latestSuccesfullImage = croppedImage.Clone() as Bitmap;
                latestSuccesfullFrame = currentFrame.Clone();

                offsetOnDesktop = ScreenHelper.BerechneWindowOffsetAufMonitor(targetProcess);

                return CreateResult(latestSuccesfullImage, winRect, clampedRect, globalRect, latestSuccesfullFrame, offsetOnDesktop);
            }
            finally
            {
                currentFrame?.Dispose();
                croppedImage?.Dispose();
                currentFrame = null;
                croppedImage = null;
            }
        }

        public void SetOnlyActiveWindow(bool onlyAktiveWindow)
        {
            OnlyActiveWindow = onlyAktiveWindow;
        }

        public void SetFrameTimeout(int timeout)
        {
            if (timeout < 0)
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be non-negative.");
            aquireFrameTimeout = timeout;
        }

        private void InitializeDesktopDuplicators(List<(int, int)> adapterAndOutputs)
        {
            foreach ((int adapter, int output) in adapterAndOutputs)
            {
                string key = $"{adapter},{output}";
                if (!desktopDuplicators.ContainsKey(key))
                {
                    var duplicator = new DesktopDuplicator(adapter, output);
                    duplicator.SetFrameTimeout(aquireFrameTimeout); 
                    desktopDuplicators.Add(key, duplicator);
                }
            }
        }

        private List<(int adapterIdx, int outputIdx)> GetAllAdapterAndOutputIndices()
        {
            var result = new List<(int, int)>();
            using (var factory = new Factory1())
            {
                int adapterIdx = 0;

                while (true)
                {
                    try
                    {
                        using (Adapter1 adapter = factory.GetAdapter1(adapterIdx))
                        {
                            int outputIdx = 0;
                            int outputCount = adapter.Outputs.Length;

                            while (outputIdx < outputCount)
                            {
                                using (Output output = adapter.GetOutput(outputIdx))
                                {
                                    result.Add((adapterIdx, outputIdx));
                                }
                                outputIdx++;
                            }
                        } 
                    }
                    catch (SharpDXException ex) when (ex.ResultCode.Code == SharpDX.DXGI.ResultCode.NotFound.Result.Code)
                    {
                        break;
                    }
                    catch (SharpDXException)
                    {
                        throw;
                    }
                    adapterIdx++;
                }
            }
            return result;
        }

        private void SetTargetProcess(string name)
        {
            Process foundProcess = null;
            foreach (var p in Process.GetProcessesByName(name))
            {
                if (p.MainWindowHandle != IntPtr.Zero)
                {
                    targetProcess = p.MainWindowHandle;
                    foundProcess = p; // Keep track of the process to dispose it
                    break; // Found the main window handle, exit loop
                }
                p.Dispose(); // Dispose processes that don't have a main window handle
            }

            if (foundProcess == null)
            {
                throw new Exception("Prozess nicht gefunden");
            }
            else
            {
                foundProcess.Dispose(); // Dispose the found process object
            }
        }

        private ProcessDuplicatorResult CreateResult(bool success)
        {
            return new ProcessDuplicatorResult(success)
            {
                ProcessImage = null, // No image on failure
                DesktopFrame = null, // No frame on failure
                Fps = 0,
                WindowRect = Rectangle.Empty,
                ClampedWindowRect = Rectangle.Empty,
                GlobalWindowRect = Rectangle.Empty,
                AdapterIdx = currentAdapterIndex,
                DesktopIdx = currentOutputIndex
            };
        }

        private ProcessDuplicatorResult CreateResult(Bitmap image, Rectangle win, Rectangle withoutClamp, Rectangle global, DesktopFrame frame, OpenCvSharp.Point offset)
        {
            return new ProcessDuplicatorResult(true)
            {
                ProcessImage = image,
                DesktopFrame = frame,
                Fps = 0, // mit richtigen Fps ersetzen
                WindowRect = withoutClamp,
                ClampedWindowRect = win,
                GlobalWindowRect = global,
                AdapterIdx = currentAdapterIndex,
                DesktopIdx = currentOutputIndex,
                WindowOffsetOnDesktop = offset
            };
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                latestSuccesfullImage = null; 
                latestSuccesfullFrame = null; 

                foreach (var kvp in desktopDuplicators)
                {
                    kvp.Value?.Dispose();
                }
                desktopDuplicators.Clear();
            }
        }

        ~ProcessDuplicator()
        {
            Dispose(disposing: false);
        }
    }
}
