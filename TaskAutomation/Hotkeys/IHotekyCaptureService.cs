namespace TaskAutomation.Hotkeys
{
    public interface IGlobalHotkeyService
    {
        event Action<Guid>? AutomationHotkeyPressed;
        event Action? PausedChanged;
        event Action? EmergencyStopPressed;

        Task<(KeyModifiers Modifiers, uint VirtualKeyCode)> CaptureNextAsync(CancellationToken ct = default);
        void RegisterAutomationHotkey(Guid automationId, KeyModifiers modifiers, uint virtualKeyCode);
        void UnregisterAutomationHotkey(Guid automationId);
        void StartWithMessageLoop();
        void StartRecordHotkeys();
        IReadOnlyList<CapturedInputEvent> StopRecordHotkeys();
        string FormatKey(KeyModifiers mods, uint vk);
        string FormatMouseButton(MouseButtons button);
        bool IsPaused { get; }
        void SetPaused(bool paused);
    }
}
