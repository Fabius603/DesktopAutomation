using System;
using System.Diagnostics;
using OpenCvSharp;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace ImageCapture
{
    public class ScreenCapture : IDisposable
    {
        public ScreenCaptureSettings Settings { get; private set; }
        public Mat LastCaptured { get; private set; }
        public IntPtr TargetProcess { get; private set; }

        private Mat _buffer;

        private Factory1 _factory;
        private Adapter1 _adapter;
        private Output1 _outputInterface;
        private OutputDuplication _duplicatedOutput;
        private Device _device;

        private int _desktopIdx = 0;
        private int _adapterIdx = 0;

        // FPS
        private readonly Stopwatch _fpsWatch = Stopwatch.StartNew();
        private int _frameCounter = 0;
        private int _currentFps = 0;

        public ScreenCapture(ScreenCaptureSettings settings)
        {
            Settings = settings;
            SetTargetProcess(settings.TargetApplication);
            InitializeDeviceAndDuplication();
        }

        public ScreenCaptureResult CaptureWindow()
        {
            FpsCounter();

            // Fensterrechteck ermitteln
            Rect windowRect;
            try
            {
                windowRect = GetWindowRect(TargetProcess);
            }
            catch
            {
                return new ScreenCaptureResult(false);
            }

            // Puffer initialisieren oder neu anpassen
            var windowSize = new OpenCvSharp.Size(windowRect.Width, windowRect.Height);
            if (_buffer == null || !_buffer.Size().Equals(windowSize))
            {
                _buffer?.Dispose();
                _buffer = new Mat(windowSize, MatType.CV_8UC3);
            }

            var windowRectOnDesktop = ScreenHelper.ClampRectToCurrentMonitor(windowRect);
            var monitorBounds = ScreenHelper.GetMonitorBoundsForWindow(windowRectOnDesktop);
            var lokalDesktopWindowRect = new Rect(
                windowRectOnDesktop.X - monitorBounds.Left,
                windowRectOnDesktop.Y - monitorBounds.Top,
                windowRect.Width,
                windowRect.Height);

            var activeApp = User32.GetActiveApplicationName();
            var newDesktop = ScreenHelper.FindOutputIndexForWindow(windowRectOnDesktop, _adapter);
            var newAdapter = ScreenHelper.FindAdapterIndexForWindow(windowRectOnDesktop, _factory);

            if (newDesktop != _desktopIdx || newAdapter != _adapterIdx)
            {
                _desktopIdx = newDesktop;
                _adapterIdx = newAdapter;
                CleanupDeviceAndDuplication();
                InitializeDeviceAndDuplication();
            }

            EnsureDevice();

            // Nur aktives Fenster?
            if (Settings.OnlyActiveWindow && activeApp != Settings.TargetApplication)
            {
                _buffer.SetTo(Scalar.Black);
                return new ScreenCaptureResult
                {
                    Image = _buffer.Clone(),
                    Fps = _currentFps,
                    WindowRect = windowRect,
                    DesktopWindowRect = windowRectOnDesktop,
                    LokalDesktopWindowRect = lokalDesktopWindowRect,
                    DesktopIdx = _desktopIdx,
                    AdapterIdx = _adapterIdx
                };
            }

            using var windowTex = GetWindowTexture(_device, windowRectOnDesktop);
            var region = GetResourceRegion(lokalDesktopWindowRect);
            var dupResult = _duplicatedOutput.TryAcquireNextFrame(1000, out _, out var desktopResource);

            if (!dupResult.Success || desktopResource == null)
            {
                desktopResource?.Dispose();
                if (LastCaptured != null)
                    LastCaptured.CopyTo(_buffer);
                else
                    _buffer.SetTo(Scalar.Black);

                return new ScreenCaptureResult
                {
                    Image = _buffer.Clone(),
                    Fps = _currentFps,
                    WindowRect = windowRect,
                    DesktopWindowRect = windowRectOnDesktop,
                    LokalDesktopWindowRect = lokalDesktopWindowRect,
                    DesktopIdx = _desktopIdx,
                    AdapterIdx = _adapterIdx
                };
            }

            try
            {
                using var desktopTex = desktopResource.QueryInterface<Texture2D>();
                _device.ImmediateContext.CopySubresourceRegion(desktopTex, 0, region, windowTex, 0);
            }
            finally
            {
                _duplicatedOutput.ReleaseFrame();
                desktopResource.Dispose();
            }

            Mat newImage = null;
            try
            {
                newImage = ImageConverter.ToMat(windowTex, _device);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim ToMat: {ex.Message}");
            }

            if (newImage != null)
            {
                LastCaptured?.Dispose();
                LastCaptured = newImage;
                newImage.CopyTo(_buffer);
            }
            else if (LastCaptured != null)
            {
                LastCaptured.CopyTo(_buffer);
            }
            else
            {
                _buffer.SetTo(Scalar.Black);
            }

            return new ScreenCaptureResult
            {
                Image = _buffer.Clone(),
                Fps = _currentFps,
                WindowRect = windowRect,
                DesktopWindowRect = windowRectOnDesktop,
                LokalDesktopWindowRect = lokalDesktopWindowRect,
                DesktopIdx = _desktopIdx,
                AdapterIdx = _adapterIdx
            };
        }

        private Rect GetWindowRect(IntPtr hwnd)
        {
            if (!User32.GetWindowRect(hwnd, out var rect))
                throw new Exception("Failed to get window rect.");
            return new Rect(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
        }

        private void EnsureDevice()
        {
            if (_device == null || _device.DeviceRemovedReason.Failure)
            {
                CleanupDeviceAndDuplication();
                InitializeDeviceAndDuplication();
            }
        }

        private void InitializeDeviceAndDuplication()
        {
            _factory = new Factory1();
            _adapter = _factory.GetAdapter1(_adapterIdx);
            _device = new Device(_adapter, DeviceCreationFlags.BgraSupport);
            _outputInterface = _adapter.GetOutput(_desktopIdx).QueryInterface<Output1>();
            _duplicatedOutput = _outputInterface.DuplicateOutput(_device);
        }

        private void CleanupDeviceAndDuplication()
        {
            LastCaptured?.Dispose(); LastCaptured = null;
            _duplicatedOutput?.Dispose(); _duplicatedOutput = null;
            _outputInterface?.Dispose(); _outputInterface = null;
            _adapter?.Dispose(); _adapter = null;
            _factory?.Dispose(); _factory = null;
            _device?.Dispose(); _device = null;
        }

        private void SetTargetProcess(string processName)
        {
            IntPtr hWnd = IntPtr.Zero;
            foreach (var proc in Process.GetProcessesByName(processName))
            {
                hWnd = proc.MainWindowHandle;
                proc.Dispose();
                if (hWnd != IntPtr.Zero) break;
            }
            if (hWnd == IntPtr.Zero) throw new Exception("Process couldn't be found!");
            TargetProcess = hWnd;
        }

        private Texture2D GetWindowTexture(Device device, Rect rect)
        {
            return new Texture2D(device, new Texture2DDescription
            {
                Width = rect.Width,
                Height = rect.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                OptionFlags = ResourceOptionFlags.None
            });
        }

        private ResourceRegion GetResourceRegion(Rect rect)
        {
            return new ResourceRegion
            {
                Left = rect.X,
                Top = rect.Y,
                Right = rect.X + rect.Width,
                Bottom = rect.Y + rect.Height,
                Front = 0,
                Back = 1
            };
        }

        public void FpsCounter()
        {
            _frameCounter++;
            if (_fpsWatch.ElapsedMilliseconds >= 1000)
            {
                _currentFps = _frameCounter;
                _frameCounter = 0;
                _fpsWatch.Restart();
            }
        }

        public void Dispose()
        {
            CleanupDeviceAndDuplication();
            _buffer?.Dispose();
            _buffer = null;
        }
    }
}
