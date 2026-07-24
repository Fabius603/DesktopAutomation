using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace TaskAutomation.Steps;

public sealed class CameraCaptureService(ILogger<CameraCaptureService> logger) : ICameraCaptureService
{
    private static readonly Guid VideoInputDeviceCategory = new("860BB310-5D01-11D0-BD3B-00A0C911CE86");
    private static readonly Guid PropertyBagId = new("55272A00-42CB-11CE-8135-00AA004BB851");
    private readonly CameraCaptureSessionPool _sessions = new();
    private bool _disposed;

    public IReadOnlyList<CameraDeviceInfo> GetAvailableCameras()
    {
        ThrowIfDisposed();
        var devices = new List<CameraDeviceInfo>();
        object? deviceEnumeratorObject = null;
        IEnumMoniker? monikerEnumerator = null;

        try
        {
            var type = Type.GetTypeFromCLSID(new Guid("62BE5D10-60EB-11D0-BD3B-00A0C911CE86"), throwOnError: true)!;
            deviceEnumeratorObject = Activator.CreateInstance(type);
            var deviceEnumerator = (ICreateDevEnum)deviceEnumeratorObject!;
            var category = VideoInputDeviceCategory;
            if (deviceEnumerator.CreateClassEnumerator(ref category, out monikerEnumerator, 0) != 0
                || monikerEnumerator is null)
                return devices;

            var monikers = new IMoniker[1];
            while (monikerEnumerator.Next(1, monikers, IntPtr.Zero) == 0)
            {
                var moniker = monikers[0];
                object? propertyBagObject = null;
                try
                {
                    moniker.GetDisplayName(null, null, out var id);
                    var propertyBagId = PropertyBagId;
                    moniker.BindToStorage(null, null, ref propertyBagId, out propertyBagObject);
                    var propertyBag = (IPropertyBag)propertyBagObject;
                    propertyBag.Read("FriendlyName", out var friendlyName, IntPtr.Zero);
                    var name = friendlyName as string;
                    devices.Add(new CameraDeviceInfo(
                        id,
                        string.IsNullOrWhiteSpace(name) ? $"Camera {devices.Count + 1}" : name,
                        devices.Count));
                }
                catch (COMException ex)
                {
                    logger.LogDebug(ex, "Kameragerät konnte nicht ausgelesen werden.");
                }
                finally
                {
                    if (propertyBagObject is not null && Marshal.IsComObject(propertyBagObject))
                        Marshal.ReleaseComObject(propertyBagObject);
                    if (Marshal.IsComObject(moniker))
                        Marshal.ReleaseComObject(moniker);
                }
            }
        }
        finally
        {
            if (monikerEnumerator is not null && Marshal.IsComObject(monikerEnumerator))
                Marshal.ReleaseComObject(monikerEnumerator);
            if (deviceEnumeratorObject is not null && Marshal.IsComObject(deviceEnumeratorObject))
                Marshal.ReleaseComObject(deviceEnumeratorObject);
        }

        return devices;
    }

    public async Task<CameraCaptureFrame> CaptureAsync(
        string cameraId,
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
            return await Task.Run(
                () => CaptureCore(session, cameraId, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    private CameraCaptureFrame CaptureCore(
        CameraCaptureSession session,
        string cameraId,
        CancellationToken cancellationToken)
    {
        var device = GetAvailableCameras()
            .FirstOrDefault(candidate => string.Equals(candidate.Id, cameraId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Die ausgewählte Kamera ist nicht mehr verfügbar.");

        var openedNow = EnsureOpen(session, device);
        using var frame = new Mat();
        var attempts = openedNow ? 12 : 3;
        for (var attempt = 0; attempt < attempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
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

    private static bool EnsureOpen(CameraCaptureSession session, CameraDeviceInfo device)
    {
        if (session.Capture is not null
            && session.Capture.IsOpened()
            && session.OpenCameraIndex == device.Index)
            return false;

        session.CloseCapture();
        session.Capture = new VideoCapture(device.Index, VideoCaptureAPIs.DSHOW);
        if (!session.Capture.IsOpened())
        {
            session.CloseCapture();
            throw new InvalidOperationException($"Die Kamera \"{device.Name}\" konnte nicht geöffnet werden.");
        }

        session.OpenCameraIndex = device.Index;
        return true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _sessions.Dispose();
    }

    [ComImport]
    [Guid("29840822-5B84-11D0-BD3B-00A0C911CE86")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ICreateDevEnum
    {
        [PreserveSig]
        int CreateClassEnumerator(
            [In] ref Guid classEnumerator,
            [Out] out IEnumMoniker? enumMoniker,
            int flags);
    }

    [ComImport]
    [Guid("55272A00-42CB-11CE-8135-00AA004BB851")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyBag
    {
        [PreserveSig]
        int Read(
            [MarshalAs(UnmanagedType.LPWStr)] string propertyName,
            [MarshalAs(UnmanagedType.Struct)] out object? value,
            IntPtr errorLog);

        [PreserveSig]
        int Write(
            [MarshalAs(UnmanagedType.LPWStr)] string propertyName,
            [MarshalAs(UnmanagedType.Struct)] ref object value);
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

    public void CloseCapture()
    {
        Capture?.Release();
        Capture?.Dispose();
        Capture = null;
        OpenCameraIndex = -1;
    }

    public void Dispose()
    {
        CloseCapture();
        Gate.Dispose();
    }
}
