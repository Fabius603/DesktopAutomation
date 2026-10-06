using System.Drawing;
using ImageHelperMethods;

namespace ImageCapture.DesktopDuplication;

public sealed record DesktopMonitor(string DeviceName, Rectangle Bounds);

/// <summary>Polling owns the cache; only CopyFrame transfers an independent image to the caller.</summary>
public interface IDesktopDuplicationSession : IDisposable
{
    Rectangle Bounds { get; }
    bool HasImage { get; }
    bool UpdateFrame(int timeoutMilliseconds);
    DesktopFrame CopyFrame(bool captureCursor);
}

public interface IDesktopDuplicationSessionFactory
{
    DesktopMonitor ResolveMonitor(int index, string? deviceName);
    IDesktopDuplicationSession Create(DesktopMonitor monitor);
}

public sealed class DesktopDuplicationSessionFactory : IDesktopDuplicationSessionFactory
{
    public DesktopMonitor ResolveMonitor(int index, string? deviceName)
    {
        var screens = ScreenHelper.GetScreens();
        var screen = string.IsNullOrWhiteSpace(deviceName)
            ? index >= 0 && index < screens.Length ? screens[index] : null
            : screens.FirstOrDefault(s => string.Equals(s.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase));
        if (screen is null)
            throw new InvalidOperationException("The selected capture monitor is not connected.");
        return new DesktopMonitor(screen.DeviceName, screen.Bounds);
    }

    public IDesktopDuplicationSession Create(DesktopMonitor monitor) => new DesktopDuplicator(monitor.DeviceName);
}
