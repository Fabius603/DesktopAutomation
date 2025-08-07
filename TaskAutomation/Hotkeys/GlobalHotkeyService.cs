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

namespace TaskAutomation.Hotkeys
{
    /// <summary>
    /// Service zum globalen Abhören von Hotkeys per WinAPI und Ausführung über Thread-Pool.
    /// </summary>
    public class GlobalHotkeyService : IDisposable
    {
        // WinAPI-Konstanten
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

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

        // Logger
        private readonly ILogger<GlobalHotkeyService> _logger;
        
        // Datenstrukturen
        private readonly Dictionary<string, HotkeyDefinition> _definitions;
        private readonly BlockingCollection<Action> _workQueue = new();
        private readonly Thread[] _workers;

        // Hook-Handle
        private readonly LowLevelKeyboardProc _hookCallback;
        private IntPtr _hookId = IntPtr.Zero;

        // Config path
        private string _hotkeyFolderPath = Path.Combine(AppContext.BaseDirectory, "Configs\\Hotkey");

        // Lazy-instantiated Singleton
        private static readonly Lazy<GlobalHotkeyService> _lazy =
            new(() => new GlobalHotkeyService());

        /// <summary>
        /// Globale Instanz des GlobalHotkeyService
        /// </summary>
        public static GlobalHotkeyService Instance => _lazy.Value;

        public event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

        /// <summary>
        /// Initialisiert den Service mit einer bestimmten Anzahl Worker-Threads.
        /// </summary>
        private GlobalHotkeyService(int workerCount = 4)
        {
            _logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<GlobalHotkeyService>();
            _definitions = new(StringComparer.OrdinalIgnoreCase);
            _workQueue = new BlockingCollection<Action>();
            _hookCallback = HookCallback;
            _hookId = IntPtr.Zero;
            _workers = new Thread[workerCount];

            // Worker-Threads starten
            for (int i = 0; i < workerCount; i++)
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
           
            // Hotkey-Definitionen laden
            LoadFromJson(_hotkeyFolderPath);

            _logger.LogInformation("GlobalHotkeyService initialisiert mit {WorkerCount} Worker-Threads.", workerCount);
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

            var def = new HotkeyDefinition(name, modifiers, virtualKeyCode, action);
            _definitions[name] = def;
            _logger.LogInformation("Hotkey registriert: {Name} => Action '{ActionName}', Command '{ActionCommand}", name, action.Name, action.Command);
        }

        /// <summary>
        /// Entfernt einen registrierten Hotkey.
        /// </summary>
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

        /// <summary>
        /// Lädt Hotkey-Definitionen aus JSON und registriert sie.
        /// JSON-Felder: Name, Modifiers, VirtualKeyCode, ActionName.
        /// </summary>
        public void LoadFromJson(string path)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            options.Converters.Add(new JsonStringEnumConverter());
            var entries = new List<HotkeyDefinition>();

            if (Directory.Exists(path))
            {
                // Verzeichnis: alle JSON-Dateien einlesen
                 foreach (var file in Directory.GetFiles(path, "*.json"))
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var list = JsonSerializer.Deserialize<List<HotkeyDefinition>>(json, options);
                        if (list != null)
                            entries.AddRange(list);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Konnte Hotkeys nicht aus Datei '{File}' laden.", file);
                    }
                }
            }

            UnregisterAllHotkeys();
            foreach (var e in entries)
                RegisterHotkey(e.Name, e.Modifiers, e.VirtualKeyCode, e.Action);
        }

        /// <summary>
        /// Startet den Keyboard-Hook in einem eigenen STA-Thread inklusive Message-Loop,
        /// damit HookCallback auch ohne GUI-Framework zuverlässig aufgerufen wird.
        /// </summary>
        public void StartWithMessageLoop()
        {
            var thread = new Thread(() =>
            {
                // 1) Hook installieren (falls noch nicht geschehen)
                if (_hookId == IntPtr.Zero)
                    SetupHook();

                // 2) Message-Loop starten
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

        /// <summary>
        /// Setzt den Low-Level-Keyboard-Hook.
        /// </summary>
        private void SetupHook()
        {
            using var proc = Process.GetCurrentProcess();
            using var mod = proc.MainModule!;
            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookCallback, GetModuleHandle(mod.ModuleName), 0);
            if (_hookId == IntPtr.Zero)
                throw new InvalidOperationException("Keyboard-Hook konnte nicht gesetzt werden.");
            _logger.LogInformation("Keyboard-Hook gesetzt.");
        }

        /// <summary>
        /// Callback für Hook-Nachrichten. Liest den Virtual-Key-Code aus und verarbeitet ihn.
        /// </summary>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
             if (nCode >= 0 && (wParam.ToInt32() == WM_KEYDOWN || wParam.ToInt32() == WM_SYSKEYDOWN))
            {
                uint vk = (uint)Marshal.ReadInt32(lParam);
                KeyModifiers mods = GetCurrentModifiers();
                ProcessKey(vk, mods);
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private void ProcessKey(uint vkCode, KeyModifiers mods)
        {
            foreach (var def in _definitions.Values)
            {
                if (def.VirtualKeyCode != vkCode)
                    continue;

                if (def.Modifiers != KeyModifiers.None)
                {
                    if (def.Modifiers != mods)
                        continue;
                }

                _workQueue.Add(() =>
                    HotkeyPressed?.Invoke(
                        this,
                        new HotkeyPressedEventArgs(def.Action)
                    )
                );
                    
                _logger.LogDebug("Hotkey erkannt: {Name} (Action: {ActionName}, Command: {Command})", def.Name, def.Action.Name, def.Action.Command);
                break;
            }
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
        private struct POINT
        {
            public int x;
            public int y;
        }

        /// <summary>
        /// Worker-Schleife: Führt Callbacks aus der BlockingCollection aus.
        /// </summary>
        private void WorkLoop()
        {
            foreach (var action in _workQueue.GetConsumingEnumerable())
            {
                try { action(); }
                catch (Exception ex) { _logger.LogError(ex, "Fehler im Hotkey-Worker."); }
            }
        }

        /// <summary>
        /// Entfernt den Hook und stoppt die Worker-Threads.
        /// </summary>
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
