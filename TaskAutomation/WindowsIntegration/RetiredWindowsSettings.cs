using TaskAutomation.Steps;

namespace TaskAutomation.WindowsIntegration;

/// <summary>Persisted IDs remain readable, but unsupported system mutations are no longer offered or executed.</summary>
public static class RetiredWindowsSettings
{
    public static IReadOnlyList<string> Ids { get; } = Array.AsReadOnly(new[] {
        "audio.default_output", "audio.default_input", "personalization.theme", "notifications.focus_mode" });
    public static bool Contains(string id) => Ids.Contains(id, StringComparer.Ordinal);

    public static WindowsSettingChangeResult Result(string id) => WindowsSettingChangeResult.Failed(
        id, WindowsCapabilityStatus.Unsupported, "setting.removed",
        "This Windows setting change was removed because no supported automatic Windows API is available.");
}
