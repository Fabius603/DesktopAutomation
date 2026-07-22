using WindowsInput;
using WindowsInput.Native;
using System.Runtime.InteropServices;

namespace TaskAutomation.Makros;

public interface IInputController
{
    void MoveAbsolute(double x, double y);
    void MoveRelative(int deltaX, int deltaY);
    void MouseButton(string button, bool down);
    void Key(VirtualKeyCode key, bool down);
}

public sealed class WindowsInputController : IInputController
{
    private const uint MouseEventMiddleDown = 0x0020;
    private const uint MouseEventMiddleUp = 0x0040;

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);

    private readonly InputSimulator _simulator = new();

    public void MoveAbsolute(double x, double y)
        => _simulator.Mouse.MoveMouseToPositionOnVirtualDesktop(x, y);

    public void MoveRelative(int deltaX, int deltaY)
        => _simulator.Mouse.MoveMouseBy(deltaX, deltaY);

    public void MouseButton(string button, bool down)
    {
        switch (button.Trim().ToLowerInvariant())
        {
            case "left": if (down) _simulator.Mouse.LeftButtonDown(); else _simulator.Mouse.LeftButtonUp(); break;
            case "right": if (down) _simulator.Mouse.RightButtonDown(); else _simulator.Mouse.RightButtonUp(); break;
            case "middle": mouse_event(down ? MouseEventMiddleDown : MouseEventMiddleUp, 0, 0, 0, UIntPtr.Zero); break;
            case "x1": if (down) _simulator.Mouse.XButtonDown(1); else _simulator.Mouse.XButtonUp(1); break;
            case "x2": if (down) _simulator.Mouse.XButtonDown(2); else _simulator.Mouse.XButtonUp(2); break;
            default: throw new ArgumentOutOfRangeException(nameof(button), button, "Unbekannte Maustaste.");
        }
    }

    public void Key(VirtualKeyCode key, bool down)
    {
        if (down) _simulator.Keyboard.KeyDown(key);
        else _simulator.Keyboard.KeyUp(key);
    }
}
