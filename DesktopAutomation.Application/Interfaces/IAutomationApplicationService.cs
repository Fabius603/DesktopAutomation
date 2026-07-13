using TaskAutomation.Automations;

namespace DesktopAutomation.Application.Interfaces
{
    public interface IAutomationApplicationService
    {
        Task<IReadOnlyList<AutomationDefinition>> LoadAllAsync();
        Task SaveAsync(AutomationDefinition automation);
        Task DeleteAsync(Guid id);
        Task TriggerAsync(Guid id);
        string GetStoragePath();
    }
}
