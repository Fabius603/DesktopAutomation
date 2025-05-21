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

namespace ImageCapture.ProcessDuplication
{
    public class ProcessDuplicator : IDisposable
    {
        private ProcessDuplicatorSettings settings { get; set; }
        private string processName { get; set; }
        private IntPtr targetProcess { get; set; } = IntPtr.Zero;

        private Bitmap _latestSuccessfulImage { get; set; } = null; 
        private DesktopFrame _latestSuccessfulFrame { get; set; } = null;

        private int currentAdapterIndex { get; set; } = 0;
        private int currentOutputIndex { get; set; } = 0;

        private Rectangle winRect { get; set; } = Rectangle.Empty;
        private Rectangle clampedRect { get; set; } = Rectangle.Empty;
        private Rectangle globalRect { get; set; } = Rectangle.Empty;

        private DxgiResources dxgiResources = new DxgiResources();

        private Dictionary<string, DesktopDuplicator> desktopDuplicators = new Dictionary<string, DesktopDuplicator>();

        // Public properties with proper disposal handling
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

        public ProcessDuplicator(ProcessDuplicatorSettings settings)
        {
            this.settings = settings;
            this.processName = settings.TargetApplication;
            SetTargetProcess(settings.TargetApplication);
            
            var adapterAndOutputs = GetAllAdapterAndOutputIndices();
            InitializeDesktopDuplicators(adapterAndOutputs);
            InitializeAllDxgiResources();
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

                if (settings.OnlyActiveWindow &&
                    User32.GetActiveApplicationName() != settings.TargetApplication)
                {
                    return CreateResult(latestSuccesfullImage, winRect, clampedRect, globalRect, latestSuccesfullFrame);
                }

                (int aIdx, int oIdx) = ScreenHelper.GetAdapterAndOutputForWindowHandle(targetProcess, dxgiResources);
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
                    Console.WriteLine($"Error cropping image: {ex.Message}");
                    return CreateResult(false);
                }

                latestSuccesfullImage = croppedImage.Clone() as Bitmap;
                latestSuccesfullFrame = currentFrame.Clone();

                return CreateResult(latestSuccesfullImage, winRect, clampedRect, globalRect, latestSuccesfullFrame);
            }
            finally
            {
                currentFrame?.Dispose();
                croppedImage?.Dispose();
                currentFrame = null;
                croppedImage = null;
            }
        }

        private void InitializeAllDxgiResources()
        {
            dxgiResources.Factory = new Factory1();

            for (int adapterIdx = 0; ; adapterIdx++)
            {
                Adapter1 adapter;
                try
                {
                    adapter = dxgiResources.Factory.GetAdapter1(adapterIdx);
                }
                catch (SharpDX.SharpDXException)
                {
                    break;
                }

                dxgiResources.Adapters.Add(adapterIdx, adapter);

                for (int outputIdx = 0; ; outputIdx++)
                {
                    try
                    {
                        var output = adapter.GetOutput(outputIdx).QueryInterface<Output1>();
                        dxgiResources.Outputs.Add((adapterIdx, outputIdx), output);
                    }
                    catch (SharpDX.SharpDXException)
                    {
                        break;
                    }
                }
            }
        }

        private void InitializeDesktopDuplicators(List<(int, int)> adapterAndOutputs)
        {
            foreach ((int adapter, int output) in adapterAndOutputs)
            {
                string key = $"{adapter},{output}";
                if (!desktopDuplicators.ContainsKey(key))
                {
                    desktopDuplicators.Add(key, new DesktopDuplicator(adapter, output));
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

        private ProcessDuplicatorResult CreateResult(Bitmap image, Rectangle win, Rectangle withoutClamp, Rectangle global, DesktopFrame frame)
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
                DesktopIdx = currentOutputIndex
            };
        }

        // IDisposable Implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
