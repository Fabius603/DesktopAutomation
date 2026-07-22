using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using WindowsInput;
using WindowsInput.Native;
using ImageHelperMethods;
using Common.Logging;
using Microsoft.Extensions.Logging;
using TaskAutomation.Timing;

namespace TaskAutomation.Makros
{
    public class MakroExecutor : IMakroExecutor
    {
        private readonly IInputController _input;
        private readonly ILogger<MakroExecutor> _logger;
        private readonly IPreciseDelayService _delayService;

        public MakroExecutor(
            ILogger<MakroExecutor> logger,
            IPreciseDelayService delayService,
            IInputController input)
        {
            _logger = logger;
            _delayService = delayService;
            _input = input;
        }


        public async Task ExecuteMakro(Makro makro, DxgiResources dxgi, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(makro);
            WarnIfAbsoluteRecordingEnvironmentChanged(makro);
            // Alle Timeout-Befehle liegen auf einer absoluten Stopwatch-Zeitachse.
            // Dadurch wird eine Abweichung nicht auf nachfolgende Wartezeiten addiert.
            long scheduledElapsedMs = 0;
            long scheduledElapsedUs = 0;
            var startedAt = Stopwatch.GetTimestamp();
            var pressedMouseButtons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var pressedKeys = new HashSet<VirtualKeyCode>();

            try
            {
                foreach (var befehl in makro.Befehle)
                {
                    ct.ThrowIfCancellationRequested();
                    if (befehl.DelayBeforeMicroseconds is > 0)
                    {
                        scheduledElapsedUs += befehl.DelayBeforeMicroseconds.Value;
                        var preciseTarget = PreciseTime.AddMicroseconds(startedAt, scheduledElapsedUs + scheduledElapsedMs * 1_000L);
                        await _delayService.DelayUntilAsync(preciseTarget, ct).ConfigureAwait(false);
                    }

                    switch (befehl)
                    {
                        case MouseMoveAbsoluteBefehl m:
                            var xy = ScreenHelper.ToAbsoluteVirtual(m.X, m.Y);
                            _input.MoveAbsolute(xy.Item1, xy.Item2);
                            break;

                        case MouseMoveRelativeBefehl m:
                            _input.MoveRelative(m.DeltaX, m.DeltaY);
                            break;

                        case MouseDownBefehl m:
                            _input.MouseButton(m.Button, true);
                            pressedMouseButtons.Add(m.Button);
                            break;

                        case MouseUpBefehl m:
                            _input.MouseButton(m.Button, false);
                            pressedMouseButtons.Remove(m.Button);
                            break;

                        case KeyDownBefehl k when TryMapKey(k.Key, out var vkDown):
                            _input.Key(vkDown, true);
                            pressedKeys.Add(vkDown);
                            break;

                        case KeyUpBefehl k when TryMapKey(k.Key, out var vkUp):
                            _input.Key(vkUp, false);
                            pressedKeys.Remove(vkUp);
                            break;

                        case TimeoutBefehl t:
                            scheduledElapsedMs += t.Duration;
                            var targetTimestamp = PreciseTime.AddMicroseconds(startedAt, scheduledElapsedUs + scheduledElapsedMs * 1_000L);
                            await _delayService.DelayUntilAsync(targetTimestamp, ct).ConfigureAwait(false);
                            break;

                        default:
                            _logger.LogError("Unbekannter oder ungueltiger Befehlstyp: {Typ}", befehl.GetType().Name);
                            break;
                    }
                }
            }
            finally
            {
                foreach (var key in pressedKeys)
                    TryRelease(() => _input.Key(key, false), $"Taste {key}");
                foreach (var button in pressedMouseButtons)
                    TryRelease(() => _input.MouseButton(button, false), $"Maustaste {button}");
            }
        }

        private void WarnIfAbsoluteRecordingEnvironmentChanged(Makro makro)
        {
            if (makro.RecordingSettings.Mode != MakroRecordingMode.ScreenAccurateAbsolute
                || makro.RecordedEnvironment is not { } recorded)
                return;

            var current = ScreenHelper.GetVirtualDesktopBounds();
            if (current.X != recorded.VirtualDesktopX || current.Y != recorded.VirtualDesktopY
                || current.Width != recorded.VirtualDesktopWidth || current.Height != recorded.VirtualDesktopHeight)
            {
                _logger.LogWarning(
                    "Das bildschirmgenaue Makro wurde fuer Desktop {RecordedX},{RecordedY} {RecordedWidth}x{RecordedHeight} aufgenommen, aktuell ist {CurrentX},{CurrentY} {CurrentWidth}x{CurrentHeight} aktiv.",
                    recorded.VirtualDesktopX, recorded.VirtualDesktopY, recorded.VirtualDesktopWidth, recorded.VirtualDesktopHeight,
                    current.X, current.Y, current.Width, current.Height);
            }
        }

        private void TryRelease(Action release, string description)
        {
            try { release(); }
            catch (Exception ex) { _logger.LogWarning(ex, "Freigabe von {Input} fehlgeschlagen.", description); }
        }

        private bool TryMapKey(string key, out VirtualKeyCode code)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key darf nicht leer sein.");

            key = key.Trim().ToUpperInvariant();

            // Sonderbehandlung für Buchstaben und Zahlen
            if (key.Length == 1 && char.IsLetterOrDigit(key[0]))
            {
                key = "VK_" + key;
            }

            if (!Enum.TryParse<VirtualKeyCode>(key, ignoreCase: true, out code))
            {
                _logger.LogError("Unbekannter Key: {Key}", key);
                return false;
            }
            return true;
        }
    }
}
