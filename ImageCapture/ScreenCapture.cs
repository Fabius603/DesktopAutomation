using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCvSharp;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using static System.Net.Mime.MediaTypeNames;
using Device = SharpDX.Direct3D11.Device;

namespace ImageCapture
{
    public class ScreenCapture : IDisposable
    {
        public ScreenCaptureSettings Settings { get; private set; }
        public Mat LastCaptured {  get; private set; }
        public IntPtr TargetProcess { get; private set; }
        private OutputDuplication _duplicatedOutput { get; set; }
        public OutputDuplication DuplicatedOutput
        {
            get
            {
                EnsureDevice();
                return _duplicatedOutput;
            }
        }
        private int _currentOutputIndex = -1;
        private Factory1 _factory;
        private Adapter1 _adapter;
        private Output1 _outputInterface;
        private Device _device { get; set; }
        public Device Device
        {
            get
            {
                EnsureDevice();
                return _device;
            }
        }
        private int _desktopIdx = 0;
        private int _adapterIdx = 0;

        public ScreenCapture(ScreenCaptureSettings settings)
        {
            this.Settings = settings;

            SetTargetProcess(Settings.TargetApplication);

            _factory = new Factory1();
            _adapter = _factory.GetAdapter1(0);
            _device = new Device(_adapter, DeviceCreationFlags.BgraSupport);

            _outputInterface = _adapter.GetOutput(0).QueryInterface<Output1>();
            _duplicatedOutput = _outputInterface.DuplicateOutput(Device);
        }

        public Mat CaptureWindow()
        {
            Rect windowRect = GetWindowRect(TargetProcess);
            Rect windowRectOnDesktop = ScreenHelper.ClampRectToCurrentMonitor(windowRect);
            var monitorBounds = ScreenHelper.GetMonitorBoundsForWindow(windowRectOnDesktop);
            int localX = windowRectOnDesktop.X - monitorBounds.Left;
            int localY = windowRectOnDesktop.Y - monitorBounds.Top;

            string activeApplication = User32.GetActiveApplicationName();

            int newDesktopIdx = ScreenHelper.FindOutputIndexForWindow(windowRectOnDesktop, _adapter);
            int newAdapterIdx = ScreenHelper.FindAdapterIndexForWindow(windowRectOnDesktop, _factory);

            if (newDesktopIdx != _desktopIdx || newAdapterIdx != _adapterIdx)
            {
                _desktopIdx = newDesktopIdx;
                _adapterIdx = newAdapterIdx;
                CleanupDeviceAndDuplication();
                InitializeDeviceAndDuplication();
            }

            EnsureDevice();

            if (Settings.OnlyActiveWindow && activeApplication != Settings.TargetApplication)
            {
                return new Mat(windowRectOnDesktop.Size, MatType.CV_8UC3, Scalar.Black);
            }

            using var windowTexture = GetWindowTexture(Device, windowRectOnDesktop);
            var region = GetResourceRegion(windowRectOnDesktop, localX, localY);

            var duplicationResult = DuplicatedOutput.TryAcquireNextFrame(1000, out var frameInfo, out var desktopResource);

            if (!duplicationResult.Success || desktopResource == null)
            {
                desktopResource?.Dispose();
                return LastCaptured?.Clone() ?? new Mat(windowRectOnDesktop.Size, MatType.CV_8UC3, Scalar.Black);
            }

            try
            {
                using var desktopTexture = desktopResource.QueryInterface<Texture2D>();
                Device.ImmediateContext.CopySubresourceRegion(
                    source: desktopTexture,
                    sourceSubresource: 0,
                    sourceRegion: region,
                    destination: windowTexture,
                    destinationSubResource: 0
                );
            }
            finally
            {
                // Frame freigeben, damit die Duplication API weiterarbeitet
                _duplicatedOutput.ReleaseFrame();
                desktopResource.Dispose();
            }

            Mat newImage = null;
            try
            {
                newImage = ImageConverter.ToMat(windowTexture, Device);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim ToMat: {ex.Message}");
            }


            Mat result;
            if (newImage != null)
            {
                // Neuer Frame erfolgreich
                LastCaptured?.Dispose();
                LastCaptured = newImage;
                result = LastCaptured.Clone();
            }
            else if (LastCaptured != null)
            {
                // Fallback auf letzten erfolgreichen Frame
                result = LastCaptured.Clone();
            }
            else
            {
                // endgültiges Fallback: schwarzes Bild in Fenstergröße
                var size = windowTexture.Description;
                result = new Mat(
                    new OpenCvSharp.Size(size.Width, size.Height),
                    MatType.CV_8UC3,
                    Scalar.Black);
            }

            return result;
        }

        private Rect GetWindowRect(IntPtr process)
        {
            try
            {
                User32.GetWindowRect(process, out var rect);
                return new OpenCvSharp.Rect(
                    rect.Left,
                    rect.Top,
                    rect.Right - rect.Left,  
                    rect.Bottom - rect.Top);
            }
            catch
            {
                throw new Exception("Failed to get window rect. Make sure the target application is running.");
            }
        }

        private void EnsureDevice()
        {
            // kein Device? gleich initialisieren
            if (_device == null)
            {
                InitializeDeviceAndDuplication();
                return;
            }

            // DeviceRemovedReason liefert HRESULT – bei Failure neu starten
            if (_device.DeviceRemovedReason.Failure)
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
            _duplicatedOutput = _outputInterface.DuplicateOutput(Device);
        }

        private void CleanupDeviceAndDuplication()
        {
            if (LastCaptured != null)
            {
                LastCaptured.Dispose();
                LastCaptured = null; 
            }
            _duplicatedOutput?.Dispose();
            _outputInterface?.Dispose();
            _device?.Dispose();
            _duplicatedOutput = null;
            _device = null;
        }

        private void SetTargetProcess(string processName)
        {
            IntPtr hWnd = IntPtr.Zero;
            foreach (var process in System.Diagnostics.Process.GetProcessesByName(processName))
            {
                hWnd = process.MainWindowHandle;
                if (hWnd != IntPtr.Zero)
                    break;
            }

            if (hWnd == IntPtr.Zero)
            {
                throw new Exception("Process couldn't be found!");
            }
            TargetProcess = hWnd;
        }

        private Texture2D GetWindowTexture(Device device, Rect windowRect)
        {
            return new Texture2D(device, new Texture2DDescription
            {
                Width = windowRect.Width,
                Height = windowRect.Height,
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

        private ResourceRegion GetResourceRegion(Rect windowRect, int localX, int localY)
        {
            return new ResourceRegion
            {
                Left = localX,
                Top = localY,
                Right = localX + windowRect.Width,
                Bottom = localY + windowRect.Height,
                Front = 0,
                Back = 1
            };
        }

        public void Dispose()
        {
            CleanupDeviceAndDuplication();
        }
    }
}
