using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskAutomation.Hotkeys
{
    public interface IGlobalHotkeyService
    {
        /// <summary>
        /// Aktuell registrierte Hotkeys (Name → Definition).
        /// Achtung: In der konkreten Klasse ist dies ein Dictionary.
        /// Für Consumer empfehlenswert: IReadOnlyDictionary.
        /// </summary>
        IReadOnlyDictionary<string, HotkeyDefinition> Hotkeys { get; }


        /// <summary>
        /// Wird ausgelöst, wenn ein registrierter Hotkey gedrückt wurde.
        /// </summary>
        event EventHandler<HotkeyPressedEventArgs>? HotkeyPressed;

        /// <summary>
        /// Liefert die nächste Tastenkombination (Modifier + VK). 
        /// Währenddessen ist reguläres Matching ausgesetzt.
        /// </summary>
        Task<(KeyModifiers Modifiers, uint VirtualKeyCode)> CaptureNextAsync(CancellationToken ct = default);

        /// <summary>
        /// Registriert/aktualisiert einen Hotkey.
        /// </summary>
        void RegisterHotkey(string name, KeyModifiers modifiers, uint virtualKeyCode, ActionDefinition action);

        /// <summary>
        /// Entfernt einen registrierten Hotkey.
        /// </summary>
        void UnregisterHotkey(string name);

        /// <summary>
        /// Entfernt alle registrierten Hotkeys.
        /// </summary>
        void UnregisterAllHotkeys();

        /// <summary>
        /// Lädt Hotkeys aus dem Repository neu und registriert nur aktive.
        /// </summary>
        Task ReloadFromRepositoryAsync();

        /// <summary>
        /// Startet den Low-Level-Hook in eigenem STA-Thread mit Message-Loop.
        /// </summary>
        void StartWithMessageLoop();

        void StartRecordHotkeys();

        IReadOnlyList<CapturedInputEvent> StopRecordHotkeys();

        string FormatKey(KeyModifiers mods, uint vk);

        string FormatMouseButton(MouseButtons button);
    }
}
