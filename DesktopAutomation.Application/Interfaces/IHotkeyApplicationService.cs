using TaskAutomation.Hotkeys;

namespace DesktopAutomation.Application.Interfaces
{
    public interface IHotkeyApplicationService
    {
        Task<IReadOnlyList<HotkeyDefinition>> LoadAllAsync();
        Task SaveAsync(HotkeyDefinition hotkey);
        Task DeleteAsync(Guid id);
        string GetStoragePath();
    }
}
