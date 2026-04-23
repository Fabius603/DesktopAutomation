using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;
using ImageHelperMethods;
using Common.Logging;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace TaskAutomation.Makros
{
    public class MakroExecutor : IMakroExecutor
    {
        private readonly InputSimulator _sim;
        private readonly ILogger<MakroExecutor> _logger;

        /// <summary>
        /// Delays ≤ this threshold are handled with a Stopwatch spin-wait instead of
        /// Task.Delay to avoid the ~15.6 ms Windows timer-resolution floor.
        /// Raises CPU usage briefly but gives sub-millisecond precision.
        /// </summary>
        private const int SpinWaitThresholdMs = 20;

        public MakroExecutor(ILogger<MakroExecutor> logger)
        {
            _logger = logger;
            _sim = new InputSimulator();
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        public async Task ExecuteMakro(Makro makro, DxgiResources dxgi, CancellationToken ct)
        {
            // Drift-Kompensation: Stopwatch misst die tatsächlich verstrichene Zeit.
            // scheduledElapsedMs ist die Summe aller TimeoutBefehle laut Aufnahme.
            // Bei jedem Timeout wird nur die *verbleibende* Zeit gewartet, d.h.
            // Overshoot eines vorherigen Sleeps wird automatisch abgezogen.
            var sw = Stopwatch.StartNew();
            long scheduledElapsedMs = 0;

            foreach (var befehl in makro.Befehle)
            {
                ct.ThrowIfCancellationRequested();
                switch (befehl)
                {
                    case MouseMoveAbsoluteBefehl m:
                        var xy = ScreenHelper.ToAbsoluteVirtual(m.X, m.Y);
                        _sim.Mouse.MoveMouseToPositionOnVirtualDesktop(xy.Item1, xy.Item2);
                        break;

                    case MouseMoveRelativeBefehl m:
                        _sim.Mouse.MoveMouseBy(m.DeltaX, m.DeltaY);
                        break;

                    case MouseDownBefehl m:
                        PressMouse(m.Button, down: true);
                        break;

                    case MouseUpBefehl m:
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
                        scheduledElapsedMs += t.Duration;
                        long toWaitMs = scheduledElapsedMs - sw.ElapsedMilliseconds;
                        if (toWaitMs > 0)
                            await DelayCompensatedAsync(toWaitMs, ct).ConfigureAwait(false);
                        // toWaitMs ≤ 0: OS hat beim vorherigen Sleep zu lang gewartet →
                        // kein weiteres Sleep, Drift wird damit abgebaut.
                        break;

                    default:
                        _logger.LogError("Unbekannter Befehlstyp: {Typ}", befehl.GetType().Name);
                        break;
                }
            }
        }

        /// <summary>
        /// Präzises Warten: für kurze Zeitspannen (≤ SpinWaitThresholdMs) wird ein
        /// Stopwatch-Spin-Wait verwendet, der die ~15.6 ms Windows-Timer-Auflösungs-
        /// Untergrenze von Task.Delay umgeht. Für längere Zeitspannen schläft Task.Delay
        /// den Großteil, der Rest wird ausgesponnen.
        /// </summary>
        private static async Task DelayCompensatedAsync(long ms, CancellationToken ct)
        {
            if (ms <= 0) return;

            if (ms > SpinWaitThresholdMs)
            {
                // Grobe Wartezeit via Task.Delay, SpinWaitThresholdMs früh aufwachen
                int coarse = (int)(ms - SpinWaitThresholdMs);
                await Task.Delay(coarse, ct).ConfigureAwait(false);
            }

            // Rest präzise ausschwingen
            var spin = Stopwatch.StartNew();
            long remaining = ms - (ms > SpinWaitThresholdMs ? (long)(ms - SpinWaitThresholdMs) : 0);
            while (spin.ElapsedMilliseconds < remaining)
                ct.ThrowIfCancellationRequested();
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
