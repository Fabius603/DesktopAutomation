using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Windows.Forms;
using Common.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskAutomation.Jobs;
using TaskAutomation.Makros;

namespace TaskAutomation.Hotkeys
{
    /// <summary>
    /// Service zum globalen Abhören von Hotkeys per WinAPI und Ausführung über Thread-Pool.
    /// </summary>
    public class GlobalHotkeyService : IGlobalHotkeyService, IDisposable
    {
        private volatile bool _isPaused;
        private volatile bool _isCapturing;
        private TaskCompletionSource<(KeyModifiers mods, uint vk)>? _captureTcs;

        // WinAPI-Konstanten – Keyboard
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        // WinAPI-Konstanten – Mouse (nur Klicks, kein Move)
        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_XBUTTONUP = 0x020C;
        private const int WM_MOUSEMOVE = 0x0200;

        private static bool IsModifierVk(uint vk) =>
            vk is
                0x10 /* VK_SHIFT   */ or 0x11 /* VK_CONTROL */ or 0x12 /* VK_MENU/ALT */ or
                0xA0 /* VK_LSHIFT  */ or 0xA1 /* VK_RSHIFT  */ or
                0xA2 /* VK_LCONTROL*/ or 0xA3 /* VK_RCONTROL*/ or
                0xA4 /* VK_LMENU   */ or 0xA5 /* VK_RMENU   */ or
                0x5B /* VK_LWIN    */ or 0x5C /* VK_RWIN    */;

        // Native Methoden
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT point);

        #region WinAPI für die Message-Loop
        [DllImport("user32.dll")]
        private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        private static extern bool TranslateMessage([In] ref MSG lpMsg);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage([In] ref MSG lpMsg);
        #endregion

        // DI
        private readonly ILogger<GlobalHotkeyService> _logger;

        // Maximale Anzahl an Worker-Threads
        private const int _maxWorkerThreads = 4;

        // Datenstrukturen
        private readonly ConcurrentDictionary<Guid, (KeyModifiers Modifiers, uint VirtualKeyCode)> _automationHotkeys = new();
        private readonly BlockingCollection<Action> _workQueue = new();
        private readonly Thread[] _workers;

        // Hook-Handles
        private readonly LowLevelKeyboardProc _hookCallback;
        private IntPtr _hookId = IntPtr.Zero;

        private readonly LowLevelMouseProc _mouseHookCallback;
        private IntPtr _mouseHookId = IntPtr.Zero;

        public event Action<Guid>? AutomationHotkeyPressed;

        // Captured Input Events für Makro-Aufnahme
        public event Action<MouseDownCaptured>? MouseDownCaptured;
        public event Action<MouseUpCaptured>? MouseUpCaptured;
        public event Action<MouseMoveCaptured>? MouseMoveCaptured;

        // Edge-Only: welche VKs sind aktuell gedrückt (um Auto-Repeat zu ignorieren)
        private readonly HashSet<uint> _downKeys = new();

        public bool IsPaused => _isPaused;

        public void SetPaused(bool paused)
        {
            _isPaused = paused;
            _logger.LogInformation("Hotkey-Ausführung {State}", paused ? "pausiert" : "fortgesetzt");
            PausedChanged?.Invoke();
        }

        public event Action? PausedChanged;
        public event Action? EmergencyStopPressed;
        public event Action? RecordingHotkeyPressed;

        // VK_F10 – globaler Notfall-Stop (bypasses Pause-Zustand)
        private const uint VK_F10 = 0x79;
        private volatile uint _recordingHotkeyVirtualKey;
        private KeyModifiers _recordingHotkeyModifiers;
        private volatile bool _recordingHotkeyActivationInProgress;
        private KeyModifiers _suppressedRecordingHotkeyModifiers;

        // --- Aufnahmezustand für StartRecordHotkeys/StopRecordHotkeys ---
        private volatile bool _isHotkeyRecording;
        private readonly object _recordLock = new();
        private List<CapturedInputEvent>? _recordBuffer;
        private Stopwatch? _recordSw;
        private MakroRecordingSettings _recordingSettings = new();
        private long _lastMouseTimestampMicroseconds;

        // MouseMove throttling für Aufnahme
        private (int x, int y) _lastMousePos = (-1, -1);

        public GlobalHotkeyService(ILogger<GlobalHotkeyService> logger)
        {
            _logger = logger;
            _hookCallback = HookCallback;
            _mouseHookCallback = MouseHookCallback;
            _workers = new Thread[_maxWorkerThreads];

            // Worker-Threads starten
            for (int i = 0; i < _maxWorkerThreads; i++)
            {
                _workers[i] = new Thread(WorkLoop)
                {
                    IsBackground = true,
                    Name = $"HotkeyWorker_{i}"
                };
                _workers[i].Start();
            }

            // Message Loop starten
            StartWithMessageLoop();

            _logger.LogInformation("GlobalHotkeyService initialisiert mit {WorkerCount} Worker-Threads.", _maxWorkerThreads);
        }

        /// <summary>
        /// Liefert die nächste Tastenkombination (Modifiers + VirtualKey). Währenddessen
        /// wird das normale Hotkey-Matching/Dispatching ausgesetzt.
        /// </summary>
        public Task<(KeyModifiers Modifiers, uint VirtualKeyCode)> CaptureNextAsync(CancellationToken ct = default)
        {
            if (_isCapturing)
                throw new InvalidOperationException("Capture ist bereits aktiv.");

            _isCapturing = true;
            _captureTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

            if (ct.CanBeCanceled)
            {
                ct.Register(() =>
                {
                    var tcs = _captureTcs;
                    _isCapturing = false;
                    _captureTcs = null;
                    tcs?.TrySetCanceled(ct);
                });
            }

            return _captureTcs!.Task;
        }

        public void RegisterAutomationHotkey(Guid automationId, KeyModifiers modifiers, uint virtualKeyCode)
        {
            if (virtualKeyCode == 0)
                throw new ArgumentException("Ein Automation-Hotkey benötigt eine Taste.", nameof(virtualKeyCode));
            if (IsModifierVk(virtualKeyCode) && modifiers == KeyModifiers.None)
                throw new ArgumentException("Ein Modifier darf nicht allein als Hotkey registriert werden.", nameof(virtualKeyCode));

            _automationHotkeys[automationId] = (modifiers, virtualKeyCode);
            _logger.LogInformation("Automation-Hotkey registriert: {AutomationId}", automationId);
        }

        public void UnregisterAutomationHotkey(Guid automationId)
        {
            _automationHotkeys.TryRemove(automationId, out _);
        }

        /// <summary>
        /// Startet den Keyboard- und Mouse-Hook in einem eigenen STA-Thread inklusive Message-Loop,
        /// damit HookCallbacks auch ohne GUI-Framework zuverlässig aufgerufen werden.
        /// </summary>
        public void StartWithMessageLoop()
        {
            var thread = new Thread(() =>
            {
                if (_hookId == IntPtr.Zero)
                    SetupKeyboardHook();

                if (_mouseHookId == IntPtr.Zero)
                    SetupMouseHook();

                MSG msg;
                while (GetMessage(out msg, IntPtr.Zero, 0, 0))
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }
            })
            {
                IsBackground = true,
                Name = "HotkeyMessageLoop",
                ApartmentState = ApartmentState.STA
            };
            thread.Start();
        }

        private void SetupKeyboardHook()
        {
            using var proc = Process.GetCurrentProcess();
            using var mod = proc.MainModule!;
            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookCallback, GetModuleHandle(mod.ModuleName), 0);
            if (_hookId == IntPtr.Zero)
                throw new InvalidOperationException("Keyboard-Hook konnte nicht gesetzt werden.");
            _logger.LogInformation("Keyboard-Hook gesetzt.");
        }

        private void SetupMouseHook()
        {
            using var proc = Process.GetCurrentProcess();
            using var mod = proc.MainModule!;
            _mouseHookId = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookCallback, GetModuleHandle(mod.ModuleName), 0);
            if (_mouseHookId == IntPtr.Zero)
                throw new InvalidOperationException("Mouse-Hook konnte nicht gesetzt werden.");
            _logger.LogInformation("Mouse-Hook gesetzt.");
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                uint vk = (uint)Marshal.ReadInt32(lParam);

                switch (msg)
                {
                    case WM_KEYDOWN:
                    case WM_SYSKEYDOWN:
                        if (_downKeys.Add(vk)) // erste Down-Flanke (kein Auto-Repeat)
                        {
                            // ----- GLOBALER NOTFALL-STOP: F10 immer feuern (bypass Pause) -----
                            if (vk == VK_F10 && !_isCapturing && !_isHotkeyRecording)
                            {
                                _workQueue.Add(() => EmergencyStopPressed?.Invoke());
                                break;
                            }

                            if (IsModifierVk(vk) && !_isHotkeyRecording)
                                break;

                            // ----- CAPTURE einer einzelnen Kombi -----
                            if (_isCapturing)
                            {
                                var mods = GetCurrentModifiers(); // aktuell gehaltene Modifier
                                var tcs = _captureTcs;
                                _isCapturing = false;
                                _captureTcs = null;
                                tcs?.TrySetResult((mods, vk));   // (Modifier, Haupttaste)
                                break; // Capturing beendet, keine weitere Verarbeitung
                            }

                            // Der Aufnahme-Hotkey steuert die UI und wird niemals selbst aufgezeichnet.
                            if (IsRecordingHotkey(vk))
                            {
                                _recordingHotkeyActivationInProgress = true;
                                _suppressedRecordingHotkeyModifiers = _recordingHotkeyModifiers;
                                _workQueue.Add(() => RecordingHotkeyPressed?.Invoke());
                                break;
                            }

                            // ----- RECORDING: KeyDown protokollieren, kein Matching -----
                            if (_isHotkeyRecording)
                            {
                                AddRecordedEvent(new KeyDownCaptured(vk));
                                break;
                            }

                            // ----- NORMALBETRIEB: nur Nicht-Modifier lösen aus -----
                            if (!IsModifierVk(vk))
                            {
                                var currentMods = GetCurrentModifiers();
                                // zuerst Kombi versuchen, sonst Single
                                if (!TryExec(vk, currentMods) && currentMods == KeyModifiers.None)
                                    TryExec(vk, KeyModifiers.None);
                            }
                        }
                        break;

                    case WM_KEYUP:
                    case WM_SYSKEYUP:
                        _downKeys.Remove(vk);

                        // Auch die KeyUp-Flanke des Aufnahme-Hotkeys gehoert nicht ins Makro.
                        if (vk == _recordingHotkeyVirtualKey && _recordingHotkeyActivationInProgress)
                        {
                            _recordingHotkeyActivationInProgress = false;
                            break;
                        }

                        var releasedModifier = ModifierForVirtualKey(vk);
                        if (releasedModifier != KeyModifiers.None
                            && (_suppressedRecordingHotkeyModifiers & releasedModifier) != 0)
                        {
                            _suppressedRecordingHotkeyModifiers &= ~releasedModifier;
                            break;
                        }

                        // RECORDING: KeyUp protokollieren, kein Matching
                        if (_isHotkeyRecording)
                        {
                            AddRecordedEvent(new KeyUpCaptured(vk));
                            break;
                        }
                        break;
                }
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _isHotkeyRecording)
            {
                int msg = wParam.ToInt32();
                var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                int x = data.pt.x, y = data.pt.y;

                switch (msg)
                {
                    case WM_LBUTTONDOWN: AddRecordedEvent(new MouseDownCaptured(MouseButtons.Left, x, y)); break;
                    case WM_LBUTTONUP: AddRecordedEvent(new MouseUpCaptured(MouseButtons.Left, x, y)); break;
                    case WM_RBUTTONDOWN: AddRecordedEvent(new MouseDownCaptured(MouseButtons.Right, x, y)); break;
                    case WM_RBUTTONUP: AddRecordedEvent(new MouseUpCaptured(MouseButtons.Right, x, y)); break;
                    case WM_MBUTTONDOWN: AddRecordedEvent(new MouseDownCaptured(MouseButtons.Middle, x, y)); break;
                    case WM_MBUTTONUP: AddRecordedEvent(new MouseUpCaptured(MouseButtons.Middle, x, y)); break;
                    case WM_XBUTTONDOWN:
                        AddRecordedEvent(new MouseDownCaptured((((data.mouseData >> 16) & 0xFFFF) == 1) ? MouseButtons.X1 : MouseButtons.X2, x, y));
                        break;
                    case WM_XBUTTONUP:
                        AddRecordedEvent(new MouseUpCaptured((((data.mouseData >> 16) & 0xFFFF) == 1) ? MouseButtons.X1 : MouseButtons.X2, x, y));
                        break;
                    case WM_MOUSEMOVE:
                        var nowUs = ElapsedMicroseconds();
                        var distanceReached = Math.Abs(x - _lastMousePos.x) >= _recordingSettings.MinimumDistancePixels ||
                                              Math.Abs(y - _lastMousePos.y) >= _recordingSettings.MinimumDistancePixels;
                        var intervalReached = nowUs - _lastMouseTimestampMicroseconds >= _recordingSettings.MinimumIntervalMicroseconds;
                        if (_recordingSettings.Mode != MakroRecordingMode.ClicksOnly && distanceReached && intervalReached)
                        {
                            _lastMousePos = (x, y);
                            _lastMouseTimestampMicroseconds = nowUs;
                            AddRecordedEvent(new MouseMoveCaptured(x, y), nowUs);
                        }
                        break;
                }
            }

            return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
        }

        private bool TryExec(uint vk, KeyModifiers mods)
        {
            if (_isPaused) return false;

            var matched = false;
            foreach (var automation in _automationHotkeys)
            {
                if (automation.Value.VirtualKeyCode != vk || automation.Value.Modifiers != mods)
                    continue;

                var automationId = automation.Key;
                _workQueue.Add(() => AutomationHotkeyPressed?.Invoke(automationId));
                matched = true;
            }

            return matched;
        }

        private bool IsRecordingHotkey(uint virtualKeyCode)
            => virtualKeyCode != 0
               && virtualKeyCode == _recordingHotkeyVirtualKey
               && GetCurrentModifiers() == _recordingHotkeyModifiers;

        private static KeyModifiers ModifierForVirtualKey(uint virtualKeyCode) => virtualKeyCode switch
        {
            0x10 or 0xA0 or 0xA1 => KeyModifiers.Shift,
            0x11 or 0xA2 or 0xA3 => KeyModifiers.Control,
            0x12 or 0xA4 or 0xA5 => KeyModifiers.Alt,
            0x5B or 0x5C => KeyModifiers.Windows,
            _ => KeyModifiers.None
        };

        public void SetRecordingHotkey(KeyModifiers modifiers, uint virtualKeyCode)
        {
            if (virtualKeyCode == 0 || IsModifierVk(virtualKeyCode))
                throw new ArgumentException("Der Aufnahme-Hotkey benoetigt eine Nicht-Modifier-Taste.", nameof(virtualKeyCode));
            if (virtualKeyCode == VK_F10)
                throw new ArgumentException("F10 ist fuer den Notfall-Stopp reserviert.", nameof(virtualKeyCode));
            _recordingHotkeyModifiers = modifiers;
            _recordingHotkeyVirtualKey = virtualKeyCode;
        }

        public void ClearRecordingHotkey()
        {
            _recordingHotkeyVirtualKey = 0;
            _recordingHotkeyModifiers = KeyModifiers.None;
            _recordingHotkeyActivationInProgress = false;
            _suppressedRecordingHotkeyModifiers = KeyModifiers.None;
        }

        /// <summary>
        /// Ermittelt aktuelle Modifier-Tasten per WinAPI.
        /// </summary>
        private static KeyModifiers GetCurrentModifiers()
        {
            bool ctrl = (GetKeyState(0x11) & 0x8000) != 0;
            bool alt = (GetKeyState(0x12) & 0x8000) != 0;
            bool shift = (GetKeyState(0x10) & 0x8000) != 0;
            bool win = (GetKeyState(0x5B) & 0x8000) != 0 || (GetKeyState(0x5C) & 0x8000) != 0;

            KeyModifiers mods = KeyModifiers.None;
            if (ctrl) mods |= KeyModifiers.Control;
            if (alt) mods |= KeyModifiers.Alt;
            if (shift) mods |= KeyModifiers.Shift;
            if (win) mods |= KeyModifiers.Windows;
            return mods;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public UIntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private void WorkLoop()
        {
            foreach (var action in _workQueue.GetConsumingEnumerable())
            {
                try { action(); }
                catch (Exception ex) { _logger.LogError(ex, "Fehler im Hotkey-Worker."); }
            }
        }

        // -----------------------------
        // Recording-API (ohne MouseMove)
        // -----------------------------

        /// <summary>
        /// Beginnt die Aufzeichnung von KeyDown/KeyUp und MouseDown/MouseUp.
        /// Während der Aufzeichnung werden keine Hotkeys gematcht.
        /// </summary>
        public void StartRecordHotkeys(MakroRecordingSettings? settings = null)
        {
            lock (_recordLock)
            {
                if (_isHotkeyRecording) return;
                _recordingSettings = (settings ?? new MakroRecordingSettings()).Clone();
                _recordBuffer = new List<CapturedInputEvent>(256);
                _recordSw = Stopwatch.StartNew();
                _lastMouseTimestampMicroseconds = 0;
                if (GetCursorPos(out var cursor))
                {
                    _lastMousePos = (cursor.x, cursor.y);
                    _recordBuffer.Add(new MouseMoveCaptured(cursor.x, cursor.y) { TimestampMicroseconds = 0 });
                }
                _isHotkeyRecording = true;
                _logger.LogInformation("Hotkey-Aufnahme gestartet.");
            }
        }

        /// <summary>
        /// Beendet die Aufzeichnung und liefert die Ereignisliste (inkl. Timeout-Events).
        /// </summary>
        public IReadOnlyList<CapturedInputEvent> StopRecordHotkeys()
        {
            lock (_recordLock)
            {
                if (!_isHotkeyRecording)
                    return Array.Empty<CapturedInputEvent>();

                _isHotkeyRecording = false;
                _recordSw?.Stop();
                var result = _recordBuffer ?? new List<CapturedInputEvent>(0);
                _recordBuffer = null;
                _recordSw = null;
                _logger.LogInformation("Hotkey-Aufnahme gestoppt. Events: {Count}", result.Count);
                return result.AsReadOnly();
            }
        }

        private long ElapsedMicroseconds()
            => _recordSw is null ? 0 : _recordSw.ElapsedTicks * 1_000_000L / Stopwatch.Frequency;

        private void AddRecordedEvent(CapturedInputEvent ev, long? timestampMicroseconds = null)
        {
            lock (_recordLock)
            {
                if (!_isHotkeyRecording || _recordBuffer is null || _recordSw is null)
                    return;
                _recordBuffer.Add(ev with { TimestampMicroseconds = timestampMicroseconds ?? ElapsedMicroseconds() });
            }
        }

        public string FormatKey(KeyModifiers mods, uint vk)
        {
            return HotkeyTextFormatter.Format(mods, vk, "+");
        }


        public string FormatMouseButton(MouseButtons button)
        {
            return button switch
            {
                MouseButtons.Left => "Left",
                MouseButtons.Right => "Right",
                MouseButtons.Middle => "Middle",
                MouseButtons.X1 => "X1",
                MouseButtons.X2 => "X2",
                _ => button.ToString()
            };
        }

        public void Dispose()
        {
            if (_hookId != IntPtr.Zero)
                UnhookWindowsHookEx(_hookId);
            if (_mouseHookId != IntPtr.Zero)
                UnhookWindowsHookEx(_mouseHookId);

            _workQueue.CompleteAdding();
            foreach (var t in _workers) t.Join();
            _logger.LogInformation("GlobalHotkeyService disposed.");
        }
    }
}
