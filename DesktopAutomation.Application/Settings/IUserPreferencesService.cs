namespace DesktopAutomation.Application.Settings;

public interface IUserPreferencesService
{
    UserPreferences Current { get; }
    Task LoadAsync();
    Task SaveAsync();
}
