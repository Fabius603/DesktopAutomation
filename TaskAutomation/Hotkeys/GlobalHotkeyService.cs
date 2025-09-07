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
using Common.JsonRepository;

namespace TaskAutomation.Hotkeys
{
    /// <summary>
    /// Service zum globalen Abhören von Hotkeys per WinAPI und Ausführung über Thread-Pool.
    /// </summary>
    public class GlobalHotkeyService : IGlobalHotkeyService, IDisposable
    {
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
        private readonly IJsonRepository<HotkeyDefinition> _repository;

        // Maximale Anzahl an Worker-Threads
        private const int _maxWorkerThreads = 4;

        // Datenstrukturen
        private readonly Dictionary<string, HotkeyDefinition> _definitions;
        private readonly BlockingCollection<Action> _workQueue = new();
        private readonly Thread[] _workers;

        // Hook-Handles
        private readonly LowLevelKeyboardProc _hookCallback;
        private IntPtr _hookId = IntPtr.Zero;

        private readonly LowLevelMouseProc _mouseHookCallback;
        private IntPtr _mouseHookId = IntPtr.Zero;

        public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

        // Captured Input Events für Makro-Aufnahme
        public event Action<MouseDownCaptured>? MouseDownCaptured;
        public event Action<MouseUpCaptured>? MouseUpCaptured;
        public event Action<MouseMoveCaptured>? MouseMoveCaptured;

        // Edge-Only: welche VKs sind aktuell gedrückt (um Auto-Repeat zu ignorieren)
        private readonly HashSet<uint> _downKeys = new();

        public IReadOnlyDictionary<string, HotkeyDefinition> Hotkeys => _definitions;

        // --- Aufnahmezustand für StartRecordHotkeys/StopRecordHotkeys ---
        private volatile bool _isHotkeyRecording;
        private readonly object _recordLock = new();
        private List<CapturedInputEvent>? _recordBuffer;
        private Stopwatch? _recordSw;
        private TimeSpan _lastAt;

        // MouseMove throttling für Aufnahme
        private (int x, int y) _lastMousePos = (-1, -1);
        private const int MouseMoveThreshold = 5; // Mindestabstand in Pixeln

        public GlobalHotkeyService(ILogger<GlobalHotkeyService> logger, IJsonRepository<HotkeyDefinition> repo)
        {
            _logger = logger;
            _repository = repo;
            _definitions = new(StringComparer.OrdinalIgnoreCase);
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
            _ = ReloadFromRepositoryAsync();

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

        /// <summary>
        /// Registriert einen Hotkey mit zugehörigem Action-Namen.
        /// </summary>
        public void RegisterHotkey(string name, KeyModifiers modifiers, uint virtualKeyCode, ActionDefinition action)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name darf nicht leer sein.", nameof(name));
            if (string.IsNullOrWhiteSpace(action.Name))
                throw new ArgumentException("ActionName darf nicht leer sein.", nameof(action.Name));

            if (IsModifierVk(virtualKeyCode) && modifiers == KeyModifiers.None)
                throw new ArgumentException("Ein Modifier darf nicht allein als Hotkey registriert werden.", nameof(virtualKeyCode));

            var def = new HotkeyDefinition(name, modifiers, virtualKeyCode, action);
            _definitions[name] = def;
            _logger.LogInformation("Hotkey registriert: {Name} => Action '{ActionName}', Command '{ActionCommand}'",
                name, action.Name, action.Command);
        }

        public void UnregisterHotkey(string name)
        {
            _definitions.Remove(name);
            _logger.LogInformation("Hotkey '{Name}' entfernt.", name);
        }

        public void UnregisterAllHotkeys()
        {
            _definitions.Clear();
            _logger.LogInformation("Alle Hotkeys entfernt.");
        }

        public async Task ReloadFromRepositoryAsync()
        {
            try
            {
                var entries = await _repository.LoadAllAsync().ConfigureAwait(false);

                UnregisterAllHotkeys();
                foreach (var e in entries)
                    if (e.Active)
                        RegisterHotkey(e.Name, e.Modifiers, e.VirtualKeyCode, e.Action);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Laden der Hotkeys aus dem Repository.");
            }
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

                            // ----- RECORDING: KeyDown protokollieren, kein Matching -----
                            if (_isHotkeyRecording)
                            {
                                AddWithTimeout(new KeyDownCaptured(vk));
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

                        // RECORDING: KeyUp protokollieren, kein Matching
                        if (_isHotkeyRecording)
                        {
                            AddWithTimeout(new KeyUpCaptured(vk));
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
                    case WM_LBUTTONDOWN: AddWithTimeout(new MouseDownCaptured(MouseButtons.Left, x, y)); break;
                    case WM_LBUTTONUP: AddWithTimeout(new MouseUpCaptured(MouseButtons.Left, x, y)); break;
                    case WM_RBUTTONDOWN: AddWithTimeout(new MouseDownCaptured(MouseButtons.Right, x, y)); break;
                    case WM_RBUTTONUP: AddWithTimeout(new MouseUpCaptured(MouseButtons.Right, x, y)); break;
                    case WM_MBUTTONDOWN: AddWithTimeout(new MouseDownCaptured(MouseButtons.Middle, x, y)); break;
                    case WM_MBUTTONUP: AddWithTimeout(new MouseUpCaptured(MouseButtons.Middle, x, y)); break;
                    case WM_XBUTTONDOWN:
                        AddWithTimeout(new MouseDownCaptured((((data.mouseData >> 16) & 0xFFFF) == 1) ? MouseButtons.X1 : MouseButtons.X2, x, y));
                        break;
                    case WM_XBUTTONUP:
                        AddWithTimeout(new MouseUpCaptured((((data.mouseData >> 16) & 0xFFFF) == 1) ? MouseButtons.X1 : MouseButtons.X2, x, y));
                        break;
                    case WM_MOUSEMOVE:
                        // Throttling: nur bei signifikanter Bewegung aufnehmen
                        if (Math.Abs(x - _lastMousePos.x) >= MouseMoveThreshold || 
                            Math.Abs(y - _lastMousePos.y) >= MouseMoveThreshold)
                        {
                            _lastMousePos = (x, y);
                            AddWithTimeout(new MouseMoveCaptured(x, y));
                        }
                        break;
                }
            }

            return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
        }

        private bool TryExec(uint vk, KeyModifiers mods)
        {
            foreach (var def in _definitions.Values)
            {
                if (!def.Active) continue;
                if (def.VirtualKeyCode != vk) continue;

                // exakte Mod-Kongruenz
                if (def.Modifiers == mods)
                {
                    _workQueue.Add(() =>
                        HotkeyPressed?.Invoke(this, new HotkeyPressedEventArgs(def.Action))
                    );

                    _logger.LogDebug("Hotkey erkannt: {Name} (Action: {ActionName}, Command: {Command})",
                        def.Name, def.Action.Name, def.Action.Command);
                    return true;
                }
            }
            return false;
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
        public void StartRecordHotkeys()
        {
            lock (_recordLock)
            {
                if (_isHotkeyRecording) return;
                _recordBuffer = new List<CapturedInputEvent>(256);
                _recordSw = Stopwatch.StartNew();
                _lastAt = TimeSpan.Zero;
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

        private void AddWithTimeout(CapturedInputEvent ev)
        {
            lock (_recordLock)
            {
                if (!_isHotkeyRecording || _recordBuffer is null || _recordSw is null)
                    return;

                var now = _recordSw.Elapsed;
                var delta = now - _lastAt;

                // minimale Schwelle gegen Jitter (0ms/1ms Events)
                if (delta.TotalMilliseconds > 0.5)
                {
                    _recordBuffer.Add(new TimeoutEvent((int)Math.Round(delta.TotalMilliseconds)));
                }

                _recordBuffer.Add(ev);
                _lastAt = now;
            }
        }

        public string FormatKey(KeyModifiers mods, uint vk)
        {
            var parts = new List<string>(4);
            if (mods.HasFlag(KeyModifiers.Control)) parts.Add("Ctrl");
            if (mods.HasFlag(KeyModifiers.Shift)) parts.Add("Shift");
            if (mods.HasFlag(KeyModifiers.Alt)) parts.Add("Alt");
            if (mods.HasFlag(KeyModifiers.Windows)) parts.Add("Win");

            // VK → WPF Key-Name
            var key = System.Windows.Input.KeyInterop.KeyFromVirtualKey(unchecked((int)vk));
            parts.Add(key.ToString());

            return string.Join("+", parts);
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
