using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using DirectShowLib;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using TaskAutomation.Jobs;

namespace TaskAutomation.Steps;

public sealed class CameraCaptureService(ILogger<CameraCaptureService> logger) : ICameraCaptureService
{
    private readonly CameraCaptureSessionPool _sessions = new();
    private bool _disposed;

    public IReadOnlyList<CameraDeviceInfo> GetAvailableCameras()
    {
        ThrowIfDisposed();
        var devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
        try
        {
            return devices.Select((device, index) => new CameraDeviceInfo(
                device.DevicePath,
                string.IsNullOrWhiteSpace(device.Name) ? $"Camera {index + 1}" : device.Name,
                index)).ToArray();
        }
        finally
        {
            foreach (var device in devices) device.Dispose();
        }
    }

    public IReadOnlyList<CameraCaptureMode> GetSupportedModes(string cameraId)
    {
        ThrowIfDisposed();
        var devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
        var device = devices.FirstOrDefault(candidate =>
            string.Equals(candidate.DevicePath, cameraId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Die ausgewählte Kamera ist nicht mehr verfügbar.");

        IFilterGraph2? graph = null;
        ICaptureGraphBuilder2? builder = null;
        IBaseFilter? source = null;
        object? streamConfigObject = null;
        try
        {
            graph = (IFilterGraph2)new FilterGraph();
            builder = (ICaptureGraphBuilder2)new CaptureGraphBuilder2();
            DsError.ThrowExceptionForHR(builder.SetFiltergraph(graph));

            var filterId = typeof(IBaseFilter).GUID;
            device.Mon.BindToObject(null, null, ref filterId, out var sourceObject);
            source = (IBaseFilter)sourceObject;
            DsError.ThrowExceptionForHR(graph.AddFilter(source, device.Name));

            var category = PinCategory.Capture;
            var mediaType = MediaType.Video;
            var streamConfigId = typeof(IAMStreamConfig).GUID;
            DsError.ThrowExceptionForHR(builder.FindInterface(
                category, mediaType, source, streamConfigId, out streamConfigObject));
            var streamConfig = (IAMStreamConfig)streamConfigObject;
            DsError.ThrowExceptionForHR(streamConfig.GetNumberOfCapabilities(out var count, out var size));

            var capabilities = Marshal.AllocCoTaskMem(size);
            try
            {
                var modes = new List<CameraCaptureMode>();
                for (var index = 0; index < count; index++)
                {
                    AMMediaType? media = null;
                    try
                    {
                        if (streamConfig.GetStreamCaps(index, out media, capabilities) < 0
                            || media?.formatPtr == IntPtr.Zero)
                            continue;

                        var isVideoInfo2 = media.formatType == DirectShowLib.FormatType.VideoInfo2;
                        var header = isVideoInfo2
                            ? null
                            : Marshal.PtrToStructure<VideoInfoHeader>(media.formatPtr);
                        var header2 = isVideoInfo2
                            ? Marshal.PtrToStructure<VideoInfoHeader2>(media.formatPtr)
                            : null;
                        var averageFrameTime = header?.AvgTimePerFrame ?? header2?.AvgTimePerFrame ?? 0;
                        var bitmapHeader = header?.BmiHeader ?? header2?.BmiHeader
                            ?? throw new InvalidOperationException("Unbekanntes Kameraformat.");
                        var fps = averageFrameTime > 0
                            ? 10_000_000d / averageFrameTime
                            : 0d;
                        modes.Add(new CameraCaptureMode(
                            Math.Abs(bitmapHeader.Width),
                            Math.Abs(bitmapHeader.Height),
                            Math.Round(fps, 2),
                            GetPixelFormat(media.subType)));
                    }
                    finally
                    {
                        if (media is not null)
                            DsUtils.FreeAMMediaType(media);
                    }
                }

                var orderedModes = OrderModes(modes);
                logger.LogDebug(
                    "Kamera {CameraName} meldet {ModeCount} unterstützte Aufnahmemodi.",
                    device.Name,
                    orderedModes.Count);
                return orderedModes;
            }
            finally
            {
                Marshal.FreeCoTaskMem(capabilities);
            }
        }
        finally
        {
            ReleaseCom(streamConfigObject);
            ReleaseCom(source);
            ReleaseCom(builder);
            ReleaseCom(graph);
            foreach (var candidate in devices) candidate.Dispose();
        }
    }

    internal static IReadOnlyList<CameraCaptureMode> OrderModes(IEnumerable<CameraCaptureMode> modes) =>
        modes.Where(mode => mode.Width > 0 && mode.Height > 0)
            .Distinct()
            .OrderByDescending(mode => (long)mode.Width * mode.Height)
            .ThenByDescending(mode => mode.FramesPerSecond)
            .ThenBy(mode => mode.PixelFormat, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public async Task<CameraCaptureFrame> CaptureAsync(
        string cameraId,
        CameraCaptureOptions options,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(cameraId))
            throw new ArgumentException("Es wurde keine Kamera ausgewählt.", nameof(cameraId));

        ThrowIfDisposed();
        var session = _sessions.GetOrAdd(cameraId);
        await session.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ThrowIfDisposed();
            var capture = await Task.Run(
                () => CaptureCore(session, cameraId, options, cancellationToken),
                CancellationToken.None).ConfigureAwait(false);
            if (capture is null)
                cancellationToken.ThrowIfCancellationRequested();
            return capture
                ?? throw new InvalidOperationException("Die Kameraaufnahme wurde ohne Ergebnis beendet.");
        }
        finally
        {
            session.Gate.Release();
        }
    }

    private CameraCaptureFrame? CaptureCore(
        CameraCaptureSession session,
        string cameraId,
        CameraCaptureOptions options,
        CancellationToken cancellationToken)
    {
        var device = GetAvailableCameras()
            .FirstOrDefault(candidate => string.Equals(candidate.Id, cameraId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Die ausgewählte Kamera ist nicht mehr verfügbar.");

        var requestedMode = ResolveMode(cameraId, options);
        var openedNow = EnsureOpen(session, device, requestedMode);
        using var frame = new Mat();
        var attempts = openedNow ? 12 : 3;
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            // Nicht aus dem ThreadPool-Worker werfen: Der erwartete Jobabbruch wird
            // nach dem Worker-Await im normalen asynchronen Aufrufpfad signalisiert.
            if (cancellationToken.IsCancellationRequested)
                return null;
            if (session.Capture!.Read(frame) && !frame.Empty())
            {
                if (!openedNow || attempt >= 1)
                    return new CameraCaptureFrame(BitmapConverter.ToBitmap(frame), DateTime.UtcNow);
            }
            Thread.Sleep(50);
        }

        session.CloseCapture();
        throw new InvalidOperationException("Die Kamera hat kein Bild geliefert.");
    }

    private CameraCaptureMode? ResolveMode(string cameraId, CameraCaptureOptions options)
    {
        if (options.QualityMode == CameraQualityMode.Automatic)
            return null;

        var modes = GetSupportedModes(cameraId);
        if (modes.Count == 0)
            throw new InvalidOperationException("Die Kamera meldet keine unterstützten Qualitätsstufen.");
        if (options.QualityMode == CameraQualityMode.HighestAvailable)
            return modes[0];

        return modes.FirstOrDefault(mode =>
                   mode.Width == options.Width
                   && mode.Height == options.Height
                   && Math.Abs(mode.FramesPerSecond - options.FramesPerSecond) < 0.02
                   && string.Equals(mode.PixelFormat, options.PixelFormat, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException(
                   "Die ausgewählte Kameraqualität wird von der Kamera nicht mehr unterstützt.");
    }

    private static bool EnsureOpen(
        CameraCaptureSession session,
        CameraDeviceInfo device,
        CameraCaptureMode? mode)
    {
        if (session.Capture is not null
            && session.Capture.IsOpened()
            && session.OpenCameraIndex == device.Index
            && Equals(session.OpenMode, mode))
            return false;

        session.CloseCapture();
        session.Capture = new VideoCapture(device.Index, VideoCaptureAPIs.DSHOW);
        if (!session.Capture.IsOpened())
        {
            session.CloseCapture();
            throw new InvalidOperationException($"Die Kamera \"{device.Name}\" konnte nicht geöffnet werden.");
        }

        if (mode is not null)
        {
            if (TryGetFourCc(mode.PixelFormat, out var fourCc))
                session.Capture.Set(VideoCaptureProperties.FourCC, fourCc);
            session.Capture.Set(VideoCaptureProperties.FrameWidth, mode.Width);
            session.Capture.Set(VideoCaptureProperties.FrameHeight, mode.Height);
            if (mode.FramesPerSecond > 0)
                session.Capture.Set(VideoCaptureProperties.Fps, mode.FramesPerSecond);
        }

        session.OpenCameraIndex = device.Index;
        session.OpenMode = mode;
        return true;
    }

    private static string GetPixelFormat(Guid subtype)
    {
        var value = subtype.ToByteArray();
        var chars = new[]
        {
            (char)value[0], (char)value[1], (char)value[2], (char)value[3]
        };
        return chars.All(character => character is >= ' ' and <= '~')
            ? new string(chars)
            : subtype.ToString("D");
    }

    private static bool TryGetFourCc(string format, out int fourCc)
    {
        fourCc = 0;
        if (format.Length != 4 || format.Any(character => character is < ' ' or > '~'))
            return false;
        fourCc = VideoWriter.FourCC(format[0], format[1], format[2], format[3]);
        return true;
    }

    private static void ReleaseCom(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
            Marshal.ReleaseComObject(value);
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _sessions.Dispose();
    }
}

internal sealed class CameraCaptureSessionPool : IDisposable
{
    private readonly ConcurrentDictionary<string, CameraCaptureSession> _sessions =
        new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public CameraCaptureSession GetOrAdd(string cameraId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _sessions.GetOrAdd(cameraId, static _ => new CameraCaptureSession());
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var session in _sessions.Values)
            session.Dispose();
        _sessions.Clear();
    }
}

internal sealed class CameraCaptureSession : IDisposable
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
    public VideoCapture? Capture { get; set; }
    public int OpenCameraIndex { get; set; } = -1;
    public CameraCaptureMode? OpenMode { get; set; }

    public void CloseCapture()
    {
        Capture?.Release();
        Capture?.Dispose();
        Capture = null;
        OpenCameraIndex = -1;
        OpenMode = null;
    }

    public void Dispose()
    {
        CloseCapture();
        Gate.Dispose();
    }
}
