using System;
using System.Collections.Generic;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;
using ImageHelperMethods;

namespace TaskAutomation.Makros
{
    public class MakroExecutor
    {
        private readonly InputSimulator _sim;

        public MakroExecutor()
        {
            _sim = new InputSimulator();
        }

        public void ExecuteMakro(Makro makro, int adapterIdx, int desktopIdx, DxgiResources dxgi)
        {
            foreach (var befehl in makro.Befehle)
            {
                switch (befehl)
                {
                    case MouseMoveBefehl m:
                        _sim.Mouse.MoveMouseToPositionOnVirtualDesktop(
                            ScreenHelper.BerechneAbsoluteX(m.X, makro.AdapterIndex, makro.DesktopIndex, dxgi),
                            ScreenHelper.BerechneAbsoluteY(m.Y, makro.AdapterIndex, makro.DesktopIndex, dxgi));
                        break;

                    case MouseDownBefehl m:
                        _sim.Mouse.MoveMouseToPositionOnVirtualDesktop(
                            ScreenHelper.BerechneAbsoluteX(m.X, makro.AdapterIndex, makro.DesktopIndex, dxgi),
                            ScreenHelper.BerechneAbsoluteY(m.Y, makro.AdapterIndex, makro.DesktopIndex, dxgi));
                        PressMouse(m.Button, down: true);
                        break;

                    case MouseUpBefehl m:
                        _sim.Mouse.MoveMouseToPositionOnVirtualDesktop(
                            ScreenHelper.BerechneAbsoluteX(m.X, makro.AdapterIndex, makro.DesktopIndex, dxgi),
                            ScreenHelper.BerechneAbsoluteY(m.Y, makro.AdapterIndex, makro.DesktopIndex, dxgi));
                        PressMouse(m.Button, down: false);
                        break;

                    case KeyDownBefehl k:
                        if (MapKey(k.Key, out var vkDown))
                            _sim.Keyboard.KeyDown(vkDown);
                        break;

                    case KeyUpBefehl k:
                        if (MapKey(k.Key, out var vkUp))
                            _sim.Keyboard.KeyUp(vkUp);
                        break;

                    case TimeoutBefehl t:
                        Thread.Sleep(t.Duration);
                        break;

                    default:
                        Console.WriteLine($"Unbekannter Befehlstyp: {befehl.Typ}");
                        break;
                }
            }
        }

        private void PressMouse(string button, bool down)
        {
            switch (button.ToLower())
            {
                case "left":
                    if (down) _sim.Mouse.LeftButtonDown();
                    else _sim.Mouse.LeftButtonUp();
                    break;
                case "right":
                    if (down) _sim.Mouse.RightButtonDown();
                    else _sim.Mouse.RightButtonUp();
                    break;
                case "middle":
                    if (down) _sim.Mouse.XButtonDown(2);
                    else _sim.Mouse.XButtonUp(2);
                    break;
                default:
                    Console.WriteLine($"Unbekannte Maustaste: {button}");
                    break;
            }
        }

        private bool MapKey(string key, out VirtualKeyCode code)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key darf nicht leer sein.");

            key = key.Trim().ToUpperInvariant();

            // Sonderbehandlung für Buchstaben und Zahlen
            if (key.Length == 1 && char.IsLetterOrDigit(key[0]))
            {
                key = "VK_" + key;
            }

            if (!Enum.TryParse(typeof(VirtualKeyCode), key, ignoreCase: true, out var result))
                throw new ArgumentException($"'{key}' ist kein gültiger VirtualKeyCode.");

            code = (VirtualKeyCode)result;
            return true;
        }
    }
}
