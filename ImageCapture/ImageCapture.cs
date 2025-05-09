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
using Device = SharpDX.Direct3D11.Device;

namespace ImageCapture
{
    public class ImageCapture : IDisposable
    {
        private ImageCaptureSettings _settings;
        private IntPtr _targetProcess;
        private Device _device;
        private OutputDuplication _duplicatedOutput;

        public ImageCapture(ImageCaptureSettings settings)
        {
            this._settings = settings;

            SetTargetProcess(_settings.TargetApplication);

            var factory = new Factory1();
            var adapter = factory.GetAdapter1(0);
            _device = new Device(adapter);

            var output = adapter.GetOutput(0).QueryInterface<Output1>();
            _duplicatedOutput = output.DuplicateOutput(_device);
        }

        public Mat CaptureWindow()
        {
            Rect windowRect = GetWindowRect(_targetProcess);
            string activeApplication = User32.GetActiveApplicationName();

            if (_settings.OnlyActiveWindow && activeApplication != _settings.TargetApplication)
            {
                // Gibt ein Schwarzes Mat in größe des Fensters zurück
                return new Mat(new OpenCvSharp.Size(windowRect.Width, windowRect.Height), MatType.CV_8UC3, Scalar.Black);
            }

            Texture2D windowTexture = GetWindowTexture(_device, windowRect);
            ResourceRegion resourceRegion = GetResourceRegion(windowRect);


            _duplicatedOutput.TryAcquireNextFrame(1000, out var frameInfo, out var desktopResource);
            using var desktopTexture = desktopResource.QueryInterface<Texture2D>();

            _device.ImmediateContext.CopySubresourceRegion(
                source: desktopTexture,
                sourceSubresource: 0,
                sourceRegion: resourceRegion,
                destination: windowTexture,
                destinationSubResource: 0);

            Mat image = ImageConverter.ToMat(windowTexture, _device);

            return image;
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

        private void SetTargetProcess(string processName)
        {
            IntPtr hWnd = IntPtr.Zero;
            foreach (var process in System.Diagnostics.Process.GetProcessesByName(processName))
            {
                hWnd = process.MainWindowHandle;
                if (hWnd != IntPtr.Zero)
                    break;
            }
            _targetProcess = hWnd;
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

        private ResourceRegion GetResourceRegion(Rect windowRect)
        {
            return new ResourceRegion
            {
                Left = windowRect.X,
                Top = windowRect.Y,
                Right = windowRect.X + windowRect.Width,
                Bottom = windowRect.Y + windowRect.Height,
                Front = 0,
                Back = 1
            };
        }

        public void Dispose()
        {
            _duplicatedOutput?.Dispose();
            _device?.Dispose();
        }
    }
}
