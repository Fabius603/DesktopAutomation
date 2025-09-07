using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskAutomation.Hotkeys
{
    public abstract record CapturedInputEvent;

    public sealed record TimeoutEvent(int Milliseconds) : CapturedInputEvent;

    public sealed record KeyDownCaptured(uint VirtualKey) : CapturedInputEvent;
    public sealed record KeyUpCaptured(uint VirtualKey) : CapturedInputEvent;

    public sealed record MouseMoveCaptured(int X, int Y) : CapturedInputEvent;
    public sealed record MouseDownCaptured(MouseButtons Button, int X, int Y) : CapturedInputEvent;
    public sealed record MouseUpCaptured(MouseButtons Button, int X, int Y) : CapturedInputEvent;
}
