using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Common.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskAutomation.Jobs;
using TaskAutomation.Persistence;

namespace TaskAutomation.Hotkeys
{
    /// <summary>
    /// Service zum globalen Abhören von Hotkeys per WinAPI und Ausführung über Thread-Pool.
    /// </summary>
    public class GlobalHotkeyService : IGlobalHotkeyService, IDisposable
    {
        private volatile bool _isCapturing;
        private TaskCompletionSource<(KeyModifiers mods, uint vk)>? _captureTcs;

        // WinAPI-Konstanten
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private static bool IsModifierVk(uint vk) =>
            vk is
                0x10 /* VK_SHIFT   */ or 0x11 /* VK_CONTROL */ or 0x12 /* VK_MENU/ALT */ or
                0xA0 /* VK_LSHIFT  */ or 0xA1 /* VK_RSHIFT  */ or
                0xA2 /* VK_LCONTROL*/ or 0xA3 /* VK_RCONTROL*/ or
                0xA4 /* VK_LMENU   */ or 0xA5 /* VK_RMENU   */ or
                0x5B /* VK_LWIN    */ or 0x5C /* VK_RWIN    */;

        // Native Methoden
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

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

        // Hook-Handle
        private readonly LowLevelKeyboardProc _hookCallback;
        private IntPtr _hookId = IntPtr.Zero;

        public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

        // Edge-Only: welche VKs sind aktuell gedrückt (um Auto-Repeat zu ignorieren)
        private readonly HashSet<uint> _downKeys = new();

        public IReadOnlyDictionary<string, HotkeyDefinition> Hotkeys => _definitions;

        public GlobalHotkeyService(ILogger<GlobalHotkeyService> logger, IJsonRepository<HotkeyDefinition> repo)
        {
            _logger = logger;
            _repository = repo;
            _definitions = new(StringComparer.OrdinalIgnoreCase);
            _hookCallback = HookCallback;
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
            _logger.LogInformation("Hotkey registriert: {Name} => Action '{ActionName}', Command '{ActionCommand}",
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
        /// Startet den Keyboard-Hook in einem eigenen STA-Thread inklusive Message-Loop,
        /// damit HookCallback auch ohne GUI-Framework zuverlässig aufgerufen wird.
        /// </summary>
        public void StartWithMessageLoop()
        {
            var thread = new Thread(() =>
            {
                if (_hookId == IntPtr.Zero)
                    SetupHook();

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

        private void SetupHook()
        {
            using var proc = Process.GetCurrentProcess();
            using var mod = proc.MainModule!;
            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookCallback, GetModuleHandle(mod.ModuleName), 0);
            if (_hookId == IntPtr.Zero)
                throw new InvalidOperationException("Keyboard-Hook konnte nicht gesetzt werden.");
            _logger.LogInformation("Keyboard-Hook gesetzt.");
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
                            if (IsModifierVk(vk))
                                break;

                            // ----- CAPTURE: nur Nicht-Modifier starten die Aufnahme -----
                            if (_isCapturing)
                            {
                                var mods = GetCurrentModifiers(); // aktuell gehaltene Modifier
                                var tcs = _captureTcs;
                                _isCapturing = false;
                                _captureTcs = null;
                                tcs?.TrySetResult((mods, vk));   // (Modifier, Haupttaste)
                                break; // Capturing beendet, keine weitere Verarbeitung
                            }

                            // ----- NORMALBETRIEB: nur Nicht-Modifier lösen aus -----
                            var currentMods = GetCurrentModifiers();
                            // zuerst Kombi versuchen, sonst Single
                            if (!TryExec(vk, currentMods) && currentMods == KeyModifiers.None)
                                TryExec(vk, KeyModifiers.None);
                        }
                        break;

                    case WM_KEYUP:
                    case WM_SYSKEYUP:
                        _downKeys.Remove(vk); // nur Zustand pflegen
                        break;
                }
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
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

        private void WorkLoop()
        {
            foreach (var action in _workQueue.GetConsumingEnumerable())
            {
                try { action(); }
                catch (Exception ex) { _logger.LogError(ex, "Fehler im Hotkey-Worker."); }
            }
        }

        public void Dispose()
        {
            if (_hookId != IntPtr.Zero)
                UnhookWindowsHookEx(_hookId);
            _workQueue.CompleteAdding();
            foreach (var t in _workers) t.Join();
            _logger.LogInformation("GlobalHotkeyService disposed.");
        }
    }
}
