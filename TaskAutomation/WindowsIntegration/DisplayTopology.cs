using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TaskAutomation.WindowsIntegration;

public enum DisplayTopologyKind : uint { Internal = 1, Duplicate = 2, Extend = 4, External = 8 }

public interface IDisplayTopologyController
{
    void Apply(DisplayTopologyKind topology);
}

public sealed class WindowsDisplayTopologyController : IDisplayTopologyController
{
    public void Apply(DisplayTopologyKind topology) => DisplayTopology.Apply(topology);
}

/// <summary>Uses the documented SetDisplayConfig database topologies, without shell command switches.</summary>
public static class DisplayTopology
{
    public static DisplayTopologyKind FromMode(string mode) => mode switch
    {
        "internal" => DisplayTopologyKind.Internal,
        "duplicate" => DisplayTopologyKind.Duplicate,
        "extend" => DisplayTopologyKind.Extend,
        "external" => DisplayTopologyKind.External,
        _ => throw new ArgumentException("Unknown display mode.", nameof(mode))
    };

    public static void Apply(DisplayTopologyKind topology)
    {
        if (!Enum.IsDefined(topology)) throw new ArgumentOutOfRangeException(nameof(topology));
        var result = SetDisplayConfig(0, IntPtr.Zero, 0, IntPtr.Zero, 0x80u | (uint)topology); // SDC_APPLY
        if (result != 0) throw new Win32Exception(result);
    }

    [DllImport("user32.dll")]
    private static extern int SetDisplayConfig(uint pathCount, IntPtr paths, uint modeCount, IntPtr modes, uint flags);
}
