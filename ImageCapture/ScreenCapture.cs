using System;
using System.Diagnostics;
using OpenCvSharp;
using SharpDX;
using SharpDX.Diagnostics;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;

namespace ImageCapture
{
    public class ScreenCapture : IDisposable
    {
        public ScreenCaptureSettings Settings { get; }
        public IntPtr TargetProcess { get; private set; }

        private readonly Factory1 _factory;
        private readonly Dictionary<(int adapter, int output), DuplicationContext> _contexts = new();
        private Mat _buffer;

        private int _adapterIdx = 0;
        private int _desktopIdx = 0;

        // Aktuelle Objekte je Context
        private Adapter1 _adapter;
        private Output1 _outputInterface;
        private Device _device;
        private OutputDuplication _duplicatedOutput;

        // FPS-Werte
        private readonly Stopwatch _fpsWatch = Stopwatch.StartNew();
        private int _frameCount, _currentFps;

        public ScreenCapture(ScreenCaptureSettings settings)
        {
            Settings = settings;
            SetTargetProcess(settings.TargetApplication);
            _factory = new Factory1();
            EnsureContext(_adapterIdx, _desktopIdx);
        }

        private void EnsureContext(int adapterIdx, int outputIdx)
        {
            var key = (adapterIdx, outputIdx);
            if (!_contexts.TryGetValue(key, out var ctx))
            {
                // neues Context anlegen
                var adapter = _factory.GetAdapter1(adapterIdx);
                var device = new Device(adapter, DeviceCreationFlags.BgraSupport);

                var rawOutput = adapter.GetOutput(outputIdx);
                var output = rawOutput.QueryInterface<Output1>();
                rawOutput.Dispose();

                var duplication = output.DuplicateOutput(device);

                ctx = new DuplicationContext(adapter, output, device, duplication);
                _contexts[key] = ctx;
            }
            // Felder setzen
            _adapter = ctx.Adapter;
            _outputInterface = ctx.OutputInterface;
            _device = ctx.Device;
            _duplicatedOutput = ctx.Duplication;
            _adapterIdx = adapterIdx;
            _desktopIdx = outputIdx;
        }

        public ScreenCaptureResult CaptureWindow()
        {
            if (_fpsWatch.ElapsedMilliseconds >= 1000)
            {
                _currentFps = _frameCount;
                _frameCount = 0;
                _fpsWatch.Restart();
            }
            _frameCount++;

            // Fensterrechteck
            if (!User32.GetWindowRect(TargetProcess, out var r))
                return new ScreenCaptureResult(false);
            var winRect = new Rect(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top);

            // Buffer initialisieren
            if (_buffer == null)
            {
                var size = new OpenCvSharp.Size(winRect.Width, winRect.Height);
                _buffer?.Dispose();
                _buffer = new Mat(size, MatType.CV_8UC3);
            }

            // Monitor-/Adapter-Wechsel
            var globalRect = ScreenHelper.ClampRectToCurrentMonitor(winRect);
            int newAdapter = ScreenHelper.FindAdapterIndexForWindow(globalRect, _factory);
            int newOutput = ScreenHelper.FindOutputIndexForWindow(globalRect, _adapter);
            if (newAdapter != _adapterIdx || newOutput != _desktopIdx)
                EnsureContext(newAdapter, newOutput);

            // nur aktives Fenster
            if (Settings.OnlyActiveWindow &&
                User32.GetActiveApplicationName() != Settings.TargetApplication)
            {
                return CreateResult(_buffer, winRect, globalRect);
            }

            // Duplizieren
            using var windowTex = CreateStagingTexture(globalRect);
            var region = GetResourceRegion(globalRect, winRect);
            var hr = _duplicatedOutput.TryAcquireNextFrame(0, out _, out var desktopRes);
            if (!hr.Success || desktopRes == null)
            {
                desktopRes?.Dispose();
                return CreateResult(_buffer, winRect, globalRect);
            }

            try
            {
                using var desktopTex = desktopRes.QueryInterface<Texture2D>();
                _device.ImmediateContext.CopySubresourceRegion(desktopTex, 0, region, windowTex, 0);
            }
            finally
            {
                _duplicatedOutput.ReleaseFrame();
                desktopRes.Dispose();
            }

            // Mat-Erstellung und Copy
            using var newImage = ImageConverter.ToMat(windowTex, _device);
            newImage.CopyTo(_buffer);

            return CreateResult(_buffer, winRect, globalRect);
        }

        private Texture2D CreateStagingTexture(Rect desktopRect)
        {
            if (desktopRect.Width <= 0 || desktopRect.Height <= 0)
                throw new ArgumentException("Ungültige Größe");
            return new Texture2D(_device, new Texture2DDescription
            {
                Width = desktopRect.Width,
                Height = desktopRect.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read
            });
        }

        private ResourceRegion GetResourceRegion(Rect global, Rect win)
            => new ResourceRegion
            {
                Left = global.X - ScreenHelper.GetMonitorBoundsForWindow(global).Left,
                Top = global.Y - ScreenHelper.GetMonitorBoundsForWindow(global).Top,
                Right = global.X - ScreenHelper.GetMonitorBoundsForWindow(global).Left + win.Width,
                Bottom = global.Y - ScreenHelper.GetMonitorBoundsForWindow(global).Top + win.Height,
                Front = 0,
                Back = 1
            };

        private ScreenCaptureResult CreateResult(Mat mat, Rect win, Rect global)
            => new ScreenCaptureResult
            {
                Image = mat.Clone(),
                Fps = _currentFps,
                WindowRect = win,
                DesktopWindowRect = global,
                LokalDesktopWindowRect = new Rect(
                    global.X - ScreenHelper.GetMonitorBoundsForWindow(global).Left,
                    global.Y - ScreenHelper.GetMonitorBoundsForWindow(global).Top,
                    win.Width, win.Height),
                AdapterIdx = _adapterIdx,
                DesktopIdx = _desktopIdx
            };

        private void SetTargetProcess(string name)
        {
            foreach (var p in Process.GetProcessesByName(name))
            {
                if (p.MainWindowHandle != IntPtr.Zero)
                {
                    TargetProcess = p.MainWindowHandle;
                    p.Dispose();
                    return;
                }
                p.Dispose();
            }
            throw new Exception("Prozess nicht gefunden");
        }

        public void Dispose()
        {
            foreach (var ctx in _contexts.Values) ctx.Dispose();
            _buffer?.Dispose();
            _factory.Dispose();
        }
    }
}
