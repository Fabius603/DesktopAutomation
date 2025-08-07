using System;
using System.Collections.Generic;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;
using ImageHelperMethods;
using Common.Logging;
using Microsoft.Extensions.Logging;

namespace TaskAutomation.Makros
{
    public class MakroExecutor
    {
        private readonly InputSimulator _sim;
        private readonly ILogger<MakroExecutor> _logger;

        public MakroExecutor()
        {
            _logger = Log.Create<MakroExecutor>();
            _sim = new InputSimulator();
        }

        public async Task ExecuteMakro(Makro makro, DxgiResources dxgi, CancellationToken ct)
        {
            foreach (var befehl in makro.Befehle)
            {
                ct.ThrowIfCancellationRequested();

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
                        await Task.Delay(t.Duration, ct).ConfigureAwait(false);
                        break;

                    default:
                        _logger.LogError("Unbekannter Befehlstyp: {Typ}", befehl.GetType().Name);
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
                    _logger.LogError("Unbekannter Maustaste: {Button}", button);
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
                _logger.LogError("Unbekannter Key: {Key}", key);

            code = (VirtualKeyCode)result;
            return true;
        }
    }
}
