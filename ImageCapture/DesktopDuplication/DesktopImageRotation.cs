using System.Drawing;
using SharpDX.DXGI;

namespace ImageCapture.DesktopDuplication;

public static class DesktopImageRotation
{
    public static RotateFlipType ToRotateFlip(DisplayModeRotation rotation) => rotation switch
    {
        DisplayModeRotation.Rotate90 => RotateFlipType.Rotate90FlipNone,
        DisplayModeRotation.Rotate180 => RotateFlipType.Rotate180FlipNone,
        DisplayModeRotation.Rotate270 => RotateFlipType.Rotate270FlipNone,
        _ => RotateFlipType.RotateNoneFlipNone
    };
}
